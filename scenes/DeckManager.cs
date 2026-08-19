using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public enum GamePhase { Idle, WaitingForAction, Resolving }
public enum WarResult { PlayerWins, DealerWins, Draw }

public partial class DeckManager : Node3D
{
    [Export] public PackedScene CardVisualScene;
    [Export] public PokerTable  Table          { get; set; }
    [Export] public PegoteUI    UI             { get; set; }
    [Export] public Marker3D    DeckSpawnPoint { get; set; }

    [ExportGroup("Animation Settings")]
    [Export] public bool  EnableAnimations { get; set; } = true;
    [Export] public float MoveDuration     { get; set; } = 0.45f / 2f;
    [Export] public float DealDelay        { get; set; } = 0.15f / 2f;

    [ExportGroup("Game Configuration")]
    [Export] public int TotalCardsInGame { get; set; } = 10;
    [Export] public int StartingStickers { get; set; } = 3;
    [Export] public int MaxStickers { get; set; } = 5;

    public int PlayerPoints { get; private set; } = 0;
    public int DealerPoints { get; private set; } = 0;

    private int _roundsPlayed = 0;
    private int _stickersUsedByPlayer = 0;
    private int _stickersUsedByDealer = 0;

    private readonly List<PokerCardData> _deckPile = new();
    private readonly RandomNumberGenerator _rng = new();

    private PokerCardData _playerCardData;
    private PokerCardData _dealerCardData;
    private CardVisual    _playerCard;
    private CardVisual    _dealerCard;

    private GamePhase _phase = GamePhase.Idle;
    public List<StickerType> PlayerStickers { get; private set; } = new();

    public override void _Ready()
    {
        _rng.Randomize();
        CardMaterialCache.Initialize();
        if (UI != null) UI.Manager = this;

        StartGame();
    }

    public void StartGame()
    {
        PlayerPoints = 0;
        DealerPoints = 0;
        _roundsPlayed = 0;
        _stickersUsedByPlayer = 0;
        _stickersUsedByDealer = 0;
        _phase = GamePhase.Idle;

        PlayerStickers.Clear();
        int initialCount = Mathf.Clamp(StartingStickers, 0, MaxStickers);
        for (int i = 0; i < initialCount; i++) PlayerStickers.Add(GetRandomSticker());

        ClearTable();
        GenerateDeck();

        UI?.OnGameStarted(PlayerPoints, DealerPoints, _deckPile.Count, PlayerStickers);
    }

    public void SetUIVisible(bool visible)
    {
        if (UI != null) UI.Visible = visible;
    }

    public async void DrawCards()
    {
        if (_phase != GamePhase.Idle) return;
        
        if (_deckPile.Count < 2)
        {
            EndGame();
            return;
        }

        _phase = GamePhase.WaitingForAction;
        UI?.SetPhase_WaitingForAction(PlayerStickers);

        ClearTable();
        await Delay(0.1f);

        _playerCardData = PopCard();
        _dealerCardData = PopCard();

        _playerCard = await SpawnCard(_playerCardData, isPlayer: true);
        await Delay(DealDelay);
        _dealerCard = await SpawnCard(_dealerCardData, isPlayer: false);

        UI?.UpdateCurrentCards(_playerCardData, _dealerCardData);
    }

    public void ApplyPlayerSticker(int stickerIndex)
    {
        if (_phase != GamePhase.WaitingForAction) return;
        if (stickerIndex < 0 || stickerIndex >= PlayerStickers.Count) return;

        StickerType st = PlayerStickers[stickerIndex];
        PlayerStickers.RemoveAt(stickerIndex);
        _stickersUsedByPlayer++;
        
        _playerCardData.AddSticker(st);
        _playerCard?.UpdateVisuals();
        UI?.ShowStatus($"Has pegado {st} a tu carta");
        
        UI?.OnPlayerStickerApplied(PlayerStickers, _playerCardData, _dealerCardData);
    }

    public async void ResolveClash()
    {
        if (_phase != GamePhase.WaitingForAction) return;
        _phase = GamePhase.Resolving;
        _roundsPlayed++;

        UI?.SetPhase_Resolving();
        UI?.ShowStatus("Luchando...");

        if (_dealerCardData.BaseValue < _playerCardData.EffectiveValue && _rng.Randf() < 0.35f)
        {
            _dealerCardData.AddSticker(GetRandomSticker());
            _stickersUsedByDealer++;
            _dealerCard?.UpdateVisuals();
            UI?.ShowStatus($"El dealer pego {_dealerCardData.StickerLabel}!");
            UI?.UpdateCurrentCards(_playerCardData, _dealerCardData);
            await Delay(1.2f);
        }

        await Delay(0.5f);

        WarResult result = EvaluateWar(_playerCardData, _dealerCardData);

        switch (result)
        {
            case WarResult.PlayerWins: 
                PlayerPoints++;
                if (PlayerStickers.Count < MaxStickers)
                {
                    PlayerStickers.Add(GetRandomSticker());
                }
                break;
            case WarResult.DealerWins: DealerPoints++; break;
        }

        bool cardsRemaining = _deckPile.Count >= 2;
        UI?.OnRoundResult(result, playerCardData: _playerCardData, dealerCardData: _dealerCardData,
                          PlayerPoints, DealerPoints, _deckPile.Count, cardsRemaining, PlayerStickers);

        if (!cardsRemaining)
        {
            await Delay(1.8f);
            EndGame();
        }
        else
        {
            _phase = GamePhase.Idle;
        }
    }

    private WarResult EvaluateWar(PokerCardData player, PokerCardData dealer)
    {
        bool invertActive = player.HasInvertRule || dealer.HasInvertRule;

        int pVal = player.EffectiveValue;
        int dVal = dealer.EffectiveValue;

        if (pVal == dVal) return WarResult.Draw;

        bool playerHigher = pVal > dVal;
        if (invertActive) playerHigher = !playerHigher;

        return playerHigher ? WarResult.PlayerWins : WarResult.DealerWins;
    }

    private void EndGame()
    {
        WarResult finalResult;
        if (PlayerPoints > DealerPoints)      finalResult = WarResult.PlayerWins;
        else if (DealerPoints > PlayerPoints) finalResult = WarResult.DealerWins;
        else                                  finalResult = WarResult.Draw;

        UI?.OnGameOver(finalResult, PlayerPoints, DealerPoints, _roundsPlayed, _stickersUsedByPlayer, PlayerStickers.Count);
    }

    private async Task<CardVisual> SpawnCard(PokerCardData data, bool isPlayer)
    {
        if (CardVisualScene == null || data == null) return null;

        CardVisual card = CardVisualScene.Instantiate<CardVisual>();
        AddChild(card);
        card.InjectConfiguration(data);
        card.RotationDegrees = new Vector3(-90f, -90f, 0f);

        if (DeckSpawnPoint != null) card.GlobalPosition = DeckSpawnPoint.GlobalPosition;

        Vector3 target = GetCardTargetPosition(isPlayer);

        if (EnableAnimations)
        {
            Tween tween = CreateTween();
            tween.TweenProperty(card, "global_position", target, MoveDuration)
                 .SetTrans(Tween.TransitionType.Cubic)
                 .SetEase(Tween.EaseType.Out);
            await ToSignal(tween, Tween.SignalName.Finished);
        }
        else
        {
            card.GlobalPosition = target;
        }

        return card;
    }

    private Vector3 GetCardTargetPosition(bool isPlayer)
    {
        if (Table == null) return Vector3.Zero;
        Vector3 a = Table.CornerLeft.GlobalPosition;
        Vector3 b = Table.CornerRight.GlobalPosition;
        float t = 0.5f;
        float zRat = isPlayer ? 0.25f : 0.75f;
        return new Vector3(Mathf.Lerp(a.X, b.X, t), a.Y + 0.01f, Mathf.Lerp(a.Z, b.Z, zRat));
    }

    private void GenerateDeck()
    {
        var fullPool = new List<PokerCardData>();
        foreach (CardRank rank in Enum.GetValues(typeof(CardRank)))
        {
            foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
            {
                fullPool.Add(new PokerCardData(suit, rank));
            }
        }

        for (int i = fullPool.Count - 1; i > 0; i--)
        {
            int j = _rng.RandiRange(0, i);
            (fullPool[i], fullPool[j]) = (fullPool[j], fullPool[i]);
        }

        int targetCount = Mathf.Clamp(TotalCardsInGame, 2, 52);
        if (targetCount % 2 != 0) targetCount++;

        _deckPile.Clear();
        for (int i = 0; i < targetCount && i < fullPool.Count; i++)
        {
            _deckPile.Add(fullPool[i]);
        }
    }

    private StickerType GetRandomSticker()
    {
        var stickers = new[] { StickerType.PlusTwo, StickerType.PlusFive, StickerType.Double, StickerType.Invert };
        return stickers[_rng.RandiRange(0, stickers.Length - 1)];
    }

    public PokerCardData PopCard()
    {
        if (_deckPile.Count == 0) return null;
        int last = _deckPile.Count - 1;
        PokerCardData card = _deckPile[last];
        _deckPile.RemoveAt(last);
        return card;
    }

    public void ClearTable()
    {
        if (IsInstanceValid(_playerCard)) { _playerCard.QueueFree(); _playerCard = null; }
        if (IsInstanceValid(_dealerCard)) { _dealerCard.QueueFree(); _dealerCard = null; }
    }

    private async Task Delay(float seconds)
    {
        if (seconds <= 0 || !IsInsideTree()) return;
        await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
    }
}