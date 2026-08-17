using Godot;
using System;
using System.Collections.Generic;

public partial class DeckManager : Node3D
{
    [Export] public PackedScene CardVisualScene;
    [Export] public PokerTable Table { get; set; }
    [Export] public BlackjackUI UI { get; set; }

    private readonly List<PokerCardData> _deckPile = new();
    private readonly List<CardVisual> _dealersHand = new();
    private readonly List<CardVisual> _playersHand = new();

    private bool _gameOver = false;

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

        StartGame();
    }

    public void StartGame()
    {
        _gameOver = false;
        ClearTable();
        GenerateDeck();
        ShuffleDeckPile();

        // Reparto inicial estándar: 2 al jugador, 2 al dealer
        TakesCardPlayer();
        TakesCardPlayer();
        
        TakesCardDealer(); // Primera carta del dealer (boca arriba)
        TakesCardDealer(); // Segunda carta del dealer (boca abajo)

        UpdatePositions();

        // La segunda carta del dealer (índice 1) queda boca abajo
        SetCardFlipped(_dealersHand[0], true);

        // Actualizar UI
        int pScore = CalculateHandScore(_playersHand);
        UI?.UpdatePlayerScore(pScore);
        UI?.UpdateDealerScore(GetSingleCardValue(_dealersHand[0].DatosCarta), hidden: true);

        // Chequeo de Blackjack natural (21 a la primera)
        if (pScore == 21)
        {
            PlayerStand();
        }
    }

    public void SetUIVisible(bool isVisible)
    {
        UI.Visible = isVisible;
    }

    // ================= ACCIONES DEL JUGADOR =================

    public void PlayerHit()
    {
        if (_gameOver) return;

        TakesCardPlayer();
        UpdatePositions();

        int score = CalculateHandScore(_playersHand);
        UI?.UpdatePlayerScore(score);

        if (score > 21)
        {
            EndGame("¡Te pasaste! (Bust) La casa gana.");
        }
        else if (score == 21)
        {
            PlayerStand();
        }
        UpdatePositions();
    }

    public void PlayerStand()
    {
        if (_gameOver) return;

        // Turno del dealer: Voltea su carta oculta
        SetCardFlipped(_dealersHand[0], false);

        int dealerScore = CalculateHandScore(_dealersHand);
        UI?.UpdateDealerScore(dealerScore, hidden: false);

        // Regla oficial: El dealer pide cartas hasta tener 17 o más
        while (dealerScore < 17)
        {
            TakesCardDealer();
            UpdatePositions();
            dealerScore = CalculateHandScore(_dealersHand);
            UI?.UpdateDealerScore(dealerScore, hidden: false);
        }

        EvaluateWinner();
        UpdatePositions();
    }

    private void EvaluateWinner()
    {
        int playerScore = CalculateHandScore(_playersHand);
        int dealerScore = CalculateHandScore(_dealersHand);

        if (dealerScore > 21)
        {
            EndGame($"¡El Dealer se pasó con {dealerScore}! ¡Ganaste!");
        }
        else if (playerScore > dealerScore)
        {
            EndGame($"¡Ganaste! ({playerScore} vs {dealerScore})");
        }
        else if (playerScore < dealerScore)
        {
            EndGame($"La casa gana ({dealerScore} vs {playerScore}).");
        }
        else
        {
            EndGame($"Empate (Push) a {playerScore} puntos.");
        }
    }

    private void EndGame(string resultMessage)
    {
        _gameOver = true;
        UI?.ShowResult(resultMessage);
    }

    // ================= CÁLCULO DE PUNTOS BLACKJACK =================

    public int CalculateHandScore(List<CardVisual> hand)
    {
        int total = 0;
        int aceCount = 0;

        foreach (var card in hand)
        {
            int val = GetSingleCardValue(card.DatosCarta);
            if (card.DatosCarta.Rank == CardRank.Ace)
            {
                aceCount++;
                total += 11; // Se cuenta inicialmente como 11
            }
            else
            {
                total += val;
            }
        }

        // Si se pasa de 21, cada As puede reducirse de 11 a 1 (-10 puntos)
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

    // ================= REPARTO Y VISUALES =================

    private void TakesCardPlayer()
    {
        PokerCardData cardLogic = PopCard();
        if (cardLogic == null || CardVisualScene == null) return;

        CardVisual newCard = CardVisualScene.Instantiate<CardVisual>();
        AddChild(newCard);
        newCard.InjectConfiguration(cardLogic);
        SetCardFlipped(newCard, false); // Boca arriba
        _playersHand.Add(newCard);
    }

    private void TakesCardDealer()
    {
        PokerCardData cardLogic = PopCard();
        if (cardLogic == null || CardVisualScene == null) return;

        CardVisual newCard = CardVisualScene.Instantiate<CardVisual>();
        AddChild(newCard);
        newCard.InjectConfiguration(cardLogic);
        SetCardFlipped(newCard, false); // cara arriba
        _dealersHand.Add(newCard);
    }

    private void SetCardFlipped(CardVisual card, bool faceDown)
    {
        card.RotationDegrees = faceDown 
            ? new Vector3(90.0f, 90.0f, 0.0f) 
            : new Vector3(-90.0f, -90.0f, 0.0f);
    }
    private void UpdatePositions()
    {
        PositionHand(_playersHand, 0.2f);
        PositionHand(_dealersHand, 0.8f);
    }

    private void PositionHand(List<CardVisual> hand, float zRatio)
    {
        Vector3 pointA = Table.CornerLeft.GlobalPosition;
        Vector3 pointB = Table.CornerRight.GlobalPosition;

        float zPosition = Mathf.Lerp(pointA.Z, pointB.Z, zRatio);
        float tableHeight = pointA.Y + 0.01f;

        for (int i = 0; i < hand.Count; i++)
        {
            float t = (float)(i + 1) / (hand.Count + 1);
            Vector3 newPosition = new Vector3(
                Mathf.Lerp(pointA.X, pointB.X, t),
                tableHeight,
                zPosition
            );
            hand[i].GlobalPosition = newPosition;
        }
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

        foreach (CardVisual card in _playersHand)
            if (IsInstanceValid(card)) card.QueueFree();
        _playersHand.Clear();

        foreach (CardVisual card in _dealersHand)
            if (IsInstanceValid(card)) card.QueueFree();
        _dealersHand.Clear();
    }
}