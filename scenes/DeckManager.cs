using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class DeckManager : Node3D
{
    [Export] public PackedScene CardVisualScene;
    [Export] public PokerTable Table { get; set; }
    [Export] public BlackjackUI UI { get; set; }
    [Export] public Marker3D DeckSpawnPoint { get; set; }

    
    [ExportGroup("Animation Settings")]
    [Export] public bool EnableAnimations { get; set; } = true;
    [Export] public float MoveDuration { get; set; } = 0.45f/SPEED_FACTOR;
    [Export] public float FlipDuration { get; set; } = 0.35f/SPEED_FACTOR;
    [Export] public float DealDelay { get; set; } = 0.15f/SPEED_FACTOR;
    
    
    const float SPEED_FACTOR = 2f;
    private readonly List<PokerCardData> _deckPile = new();
    private readonly List<CardVisual> _dealersHand = new();
    private readonly List<CardVisual> _playersHand = new();

    private bool _gameOver = false;
    private bool _isDealing = false;

    public override void _Ready()
    {
        CardMaterialCache.Initialize();

        if (UI == null)
        {
            GD.PrintErr("[DeckManager] ERROR: 'UI' es null al intentar cambiar visibilidad.");
            return;
        }
        if (Table == null || Table.CornerLeft == null || Table.CornerRight == null)
        {
            GD.PrintErr("[DeckManager] ERROR: Referencias de PokerTable no asignadas.");
            return;
        }
        if (DeckSpawnPoint == null)
        {
            GD.PrintErr("[DeckManager] ERROR: 'DeckSpawnPoint' no esta asignado en el inspector.");
        }

        StartGame();
    }

    public async void StartGame()
    {
        if (_isDealing) return;
        _isDealing = true;
        _gameOver = false;

        UI?.SetButtonsEnabled(false);
        ClearTable();
        GenerateDeck();
        ShuffleDeckPile();

        // Reparto inicial secuencial (1 a 1): Jugador -> Dealer -> Jugador -> Dealer (oculta)
        // 1. Primera carta al jugador
        await DealCardToPlayer();
        UI?.UpdatePlayerScore(CalculateHandScore(_playersHand));
        await Delay(DealDelay);

        // 2. Primera carta al dealer (boca arriba)
        await DealCardToDealer(faceDown: false);
        if (_dealersHand.Count > 0)
        {
            UI?.UpdateDealerScore(GetSingleCardValue(_dealersHand[0].DatosCarta), hidden: true);
        }
        await Delay(DealDelay);

        // 3. Segunda carta al jugador
        await DealCardToPlayer();
        int pScore = CalculateHandScore(_playersHand);
        UI?.UpdatePlayerScore(pScore);
        await Delay(DealDelay);

        // 4. Segunda carta al dealer (boca abajo)
        await DealCardToDealer(faceDown: true);

        _isDealing = false;

        // Chequeo de Blackjack natural
        if (pScore == 21)
        {
            PlayerStand();
        }
        else
        {
            UI?.SetButtonsEnabled(true);
        }
    }

    public void SetUIVisible(bool isVisible)
    {
        if (UI != null) UI.Visible = isVisible;
    }

    // ================= ACCIONES DE JUEGO =================

    public async void PlayerHit()
    {
        if (_gameOver || _isDealing) return;
        _isDealing = true;
        UI?.SetButtonsEnabled(false);

        await DealCardToPlayer();

        int score = CalculateHandScore(_playersHand);
        UI?.UpdatePlayerScore(score);

        _isDealing = false;

        if (score > 21)
        {
            EndGame(GameResult.DealerWin, score, CalculateHandScore(_dealersHand));
        }
        else if (score == 21)
        {
            PlayerStand();
        }
        else
        {
            UI?.SetButtonsEnabled(true);
        }
    }

    public async void PlayerStand()
    {
        if (_gameOver || _isDealing) return;
        _isDealing = true;
        UI?.SetButtonsEnabled(false);

        // Turno del dealer: Revela su segunda carta
        if (_dealersHand.Count > 1)
        {
            await SetCardFlippedAnimated(_dealersHand[1], faceDown: false);
            await Delay(0.2f);
        }

        int dealerScore = CalculateHandScore(_dealersHand);
        UI?.UpdateDealerScore(dealerScore, hidden: false);

        // Regla oficial: El dealer pide cartas hasta tener 17 o más, una a una
        while (dealerScore < 17)
        {
            await Delay(0.35f);
            await DealCardToDealer(faceDown: false);
            dealerScore = CalculateHandScore(_dealersHand);
            UI?.UpdateDealerScore(dealerScore, hidden: false);
        }

        _isDealing = false;
        EvaluateWinner();
    }

    private void EvaluateWinner()
    {
        int playerScore = CalculateHandScore(_playersHand);
        int dealerScore = CalculateHandScore(_dealersHand);

        if (dealerScore > 21)
            EndGame(GameResult.PlayerWin, playerScore, dealerScore);
        else if (playerScore > dealerScore)
            EndGame(GameResult.PlayerWin, playerScore, dealerScore);
        else if (playerScore < dealerScore)
            EndGame(GameResult.DealerWin, playerScore, dealerScore);
        else
            EndGame(GameResult.Draw, playerScore, dealerScore);
    }

    private void EndGame(GameResult result, int playerScore, int dealerScore)
    {
        _gameOver = true;
        UI?.ShowResult(result, playerScore, dealerScore);
    }

    // ================= REPARTO Y VISUALES =================

    public async Task DealCardToPlayer()
    {
        await DealCardTo(_playersHand, faceDown: false);
    }

    public async Task DealCardToDealer(bool faceDown)
    {
        await DealCardTo(_dealersHand, faceDown: faceDown);
    }

    private async Task DealCardTo(List<CardVisual> targetHand, bool faceDown)
    {
        PokerCardData cardLogic = PopCard();
        if (cardLogic == null || CardVisualScene == null) return;

        CardVisual newCard = CardVisualScene.Instantiate<CardVisual>();
        AddChild(newCard);
        newCard.InjectConfiguration(cardLogic);

        // Establecer rotación inicial (boca arriba o boca abajo)
        ApplyCardRotation(newCard, faceDown);

        // Aparecer en la posición del mazo
        if (DeckSpawnPoint != null)
        {
            newCard.GlobalPosition = DeckSpawnPoint.GlobalPosition;
        }
        else if (Table != null && Table.CornerLeft != null)
        {
            newCard.GlobalPosition = Table.CornerLeft.GlobalPosition + new Vector3(0, 1.5f, 0);
        }

        targetHand.Add(newCard);

        // Animar el movimiento hacia la mesa y reorganizar la mano
        float zRatio = (targetHand == _playersHand) ? 0.3f : 0.8f;
        await PositionHand(targetHand, zRatio);
    }

    private async Task PositionHand(List<CardVisual> hand, float zRatio)
    {
        if (Table == null || Table.CornerLeft == null || Table.CornerRight == null || hand.Count == 0) return;

        Vector3 pointA = Table.CornerLeft.GlobalPosition;
        Vector3 pointB = Table.CornerRight.GlobalPosition;

        float zPosition = Mathf.Lerp(pointA.Z, pointB.Z, zRatio);
        float tableHeight = pointA.Y + 0.01f;

        if (EnableAnimations)
        {
            Tween tween = CreateTween().SetParallel(true);
            for (int i = 0; i < hand.Count; i++)
            {
                if (!IsInstanceValid(hand[i])) continue;

                float t = (float)(i + 1) / (hand.Count + 1);
                Vector3 targetPosition = new Vector3(
                    Mathf.Lerp(pointA.X, pointB.X, t),
                    tableHeight,
                    zPosition
                );

                tween.TweenProperty(hand[i], "global_position", targetPosition, MoveDuration)
                    .SetTrans(Tween.TransitionType.Cubic)
                    .SetEase(Tween.EaseType.Out);
            }

            await ToSignal(tween, Tween.SignalName.Finished);
        }
        else
        {
            for (int i = 0; i < hand.Count; i++)
            {
                if (!IsInstanceValid(hand[i])) continue;

                float t = (float)(i + 1) / (hand.Count + 1);
                Vector3 targetPosition = new Vector3(
                    Mathf.Lerp(pointA.X, pointB.X, t),
                    tableHeight,
                    zPosition
                );
                hand[i].GlobalPosition = targetPosition;
            }
        }
    }

    private void ApplyCardRotation(CardVisual card, bool faceDown)
    {
        card.RotationDegrees = faceDown 
            ? new Vector3(90.0f, 90.0f, 0.0f) 
            : new Vector3(-90.0f, -90.0f, 0.0f);
    }

    private async Task SetCardFlippedAnimated(CardVisual card, bool faceDown)
    {
        if (!IsInstanceValid(card)) return;

        Vector3 targetRotation = faceDown 
            ? new Vector3(90.0f, 90.0f, 0.0f) 
            : new Vector3(-90.0f, -90.0f, 0.0f);

        if (EnableAnimations)
        {
            Tween tween = CreateTween();
            tween.TweenProperty(card, "rotation_degrees", targetRotation, FlipDuration)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.Out);
            await ToSignal(tween, Tween.SignalName.Finished);
        }
        else
        {
            card.RotationDegrees = targetRotation;
        }
    }

    private async Task Delay(float seconds)
    {
        if (seconds <= 0 || !IsInsideTree()) return;
        await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
    }

    // ================= CÁLCULO DE PUNTOS =================

    public int CalculateHandScore(List<CardVisual> hand)
    {
        int total = 0;
        int aceCount = 0;

        foreach (var card in hand)
        {
            if (!IsInstanceValid(card) || card.DatosCarta == null) continue;

            int val = GetSingleCardValue(card.DatosCarta);
            if (card.DatosCarta.Rank == CardRank.Ace)
            {
                aceCount++;
                total += 11;
            }
            else
            {
                total += val;
            }
        }

        while (total > 21 && aceCount > 0)
        {
            total -= 10;
            aceCount--;
        }

        return total;
    }

    private int GetSingleCardValue(PokerCardData card)
    {
        return card.Rank switch
        {
            CardRank.Ace => 11,
            CardRank.King or CardRank.Queen or CardRank.Jack => 10,
            _ => (int)card.Rank
        };
    }

    // ================= GESTIÓN DEL MAZO =================

    private void GenerateDeck()
    {
        _deckPile.Clear();
        foreach (CardRank rank in Enum.GetValues(typeof(CardRank)))
        {
            foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
            {
                _deckPile.Add(new PokerCardData(suit, rank));
            }
        }
    }

    public void ShuffleDeckPile()
    {
        for (int i = _deckPile.Count - 1; i > 0; i--)
        {
            int j = GD.RandRange(0, i);
            (_deckPile[i], _deckPile[j]) = (_deckPile[j], _deckPile[i]);
        }
    }

    public PokerCardData PopCard()
    {
        if (_deckPile.Count == 0) return null;
        int lastIndex = _deckPile.Count - 1;
        PokerCardData card = _deckPile[lastIndex];
        _deckPile.RemoveAt(lastIndex);
        return card;
    }

    public void ClearTable()
    {
        _deckPile.Clear();
        _isDealing = false;

        foreach (CardVisual card in _playersHand)
            if (IsInstanceValid(card)) card.QueueFree();
        _playersHand.Clear();

        foreach (CardVisual card in _dealersHand)
            if (IsInstanceValid(card)) card.QueueFree();
        _dealersHand.Clear();
    }
}