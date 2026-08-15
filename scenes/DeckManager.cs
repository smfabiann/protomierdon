using Godot;
using System;
using System.Collections.Generic;

public partial class DeckManager : Node3D
{
    // Escena visual de la carta (se asigna en el Inspector)
    [Export] public PackedScene CardVisualScene;
    // Referencia a la mesa de juego (se asigna en el Inspector)
    [Export] public PokerTable Table { get; set; }
    private readonly List<PokerCardData> _deck = new();
    private readonly List<CardVisual> _draw = new();

    public override void _Ready()
    {
        // 1. Precargar los materiales en memoria una sola vez al arrancar
        CardMaterialCache.Initialize();

        // 2. Comprobaciones de seguridad (Null Checks)
        if (Table == null)
        {
            GD.PrintErr("[DeckManager] ERROR: La variable 'Table' es NULL. Asígnala en el Inspector.");
            return;
        }

        if (Table.CornerLeft == null || Table.CornerRight == null)
        {
            GD.PrintErr("[DeckManager] ERROR: 'CornerLeft' o 'CornerRight' en PokerTable son NULL. Asígnalos en el Inspector.");
            return;
        }
    }

    // Inicia una ronda/partida
    public void StartGame()
    {
        ClearTable(); // Limpia cualquier carta previa antes de empezar

        int segments = 5;
        generateDeck();
        ShuffleDeck();

        for (int i = 0; i < segments; i++)
        {
            distributeCardVisual();
        }

        Vector3 pointA = Table.CornerLeft.GlobalPosition;
        Vector3 pointB = Table.CornerRight.GlobalPosition;

        // Calculamos la profundidad (Z) y la altura (Y) sobre la mesa
        float zPosition = Mathf.Lerp(pointA.Z, pointB.Z, 0.2f);
        float tableHeight = pointA.Y + 0.01f; // Pequeño margen para evitar Z-Fighting

        GD.Print("[DeckManager] Posicionando cartas en la mesa...");

        for (int i = 0; i < segments; i++)
        {
            // Distribución proporcional dejando margen a los costados
            float t = (float)(i + 1) / (segments + 1);

            Vector3 newPosition = new Vector3(
                Mathf.Lerp(pointA.X, pointB.X, t), // Eje X distribuido
                tableHeight,                       // Eje Y fijo
                zPosition                          // Eje Z fijo
            );

            _draw[i].GlobalPosition = newPosition;
            GD.Print($"[DeckManager] Carta [{i}] colocada en: {newPosition}");
        }
    }

    // Genera las 52 cartas estándar de la baraja
    private void generateDeck()
    {
        _deck.Clear();
        Array rankArray = Enum.GetValues(typeof(CardRank));
        Array suitArray = Enum.GetValues(typeof(CardSuit));

        for (int i = 0; i < rankArray.Length; i++)
        {
            for (int j = 0; j < suitArray.Length; j++)
            {
                PokerCardData carta = new PokerCardData(
                    (CardSuit)suitArray.GetValue(j), 
                    (CardRank)rankArray.GetValue(i)
                );
                _deck.Add(carta);
            }
        }
    }

    // Barajado estándar Fisher-Yates: O(n)
    public void ShuffleDeck()
    {
        if (_deck.Count <= 1) return;

        for (int i = _deck.Count - 1; i > 0; i--)
        {
            int j = GD.RandRange(0, i);
            (_deck[i], _deck[j]) = (_deck[j], _deck[i]); // Intercambio con tuplas C#
        }

        GD.Print("[DeckManager] Mazo barajado exitosamente.");
    }

    // Retira la última carta del mazo (O(1))
    public PokerCardData PopCard()
    {
        if (_deck.Count == 0)
        {
            GD.PrintErr("[DeckManager] Error: El mazo está vacío.");
            return null;
        }

        int ultimoIndice = _deck.Count - 1;
        PokerCardData cartaRobada = _deck[ultimoIndice];
        _deck.RemoveAt(ultimoIndice);

        return cartaRobada;
    }

    // Instancia visualmente una carta y le inyecta sus datos
    private void distributeCardVisual()
    {
        if (_deck.Count == 0 || CardVisualScene == null) return;

        PokerCardData cardLogic = PopCard();

        CardVisual newCard3D = CardVisualScene.Instantiate<CardVisual>();
        AddChild(newCard3D);
        
        newCard3D.InjectConfiguration(cardLogic);
        _draw.Add(newCard3D);
    }

    // Limpia las cartas visuales y reinicia las listas
    public void ClearTable()
    {
        _deck.Clear();

        foreach (CardVisual card in _draw)
        {
            if (IsInstanceValid(card))
            {
                card.QueueFree();
            }
        }

        _draw.Clear();
    }
}