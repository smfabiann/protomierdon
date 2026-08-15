using Godot;
using System.Collections.Generic;

public static class CardMaterialCache
{
    private static readonly Dictionary<string, StandardMaterial3D> _materialCache = new();
    private static bool _isInitialized = false;

    public static void Initialize()
    {
        if (_isInitialized) return;

        // Pre-cargar los 52 materiales
        foreach (CardSuit suit in System.Enum.GetValues(typeof(CardSuit)))
        {
            foreach (CardRank rank in System.Enum.GetValues(typeof(CardRank)))
            {
                string key = GetKey(suit, rank);
                string path = $"res://assets/PNG/poker_cards/{GetFileName(suit, rank)}";

                var texture = GD.Load<Texture2D>(path);
                if (texture != null)
                {
                    StandardMaterial3D mat = new StandardMaterial3D();
                    mat.AlbedoTexture = texture;

                    _materialCache[key] = mat;
                }
            }
        }

        _isInitialized = true;
    }

    public static StandardMaterial3D GetMaterial(CardSuit suit, CardRank rank)
    {
        string key = GetKey(suit, rank);
        return _materialCache.GetValueOrDefault(key);
    }

    private static string GetKey(CardSuit suit, CardRank rank) => $"{suit}_{rank}";

    private static string GetFileName(CardSuit suit, CardRank rank)
    {
        char prefix = suit switch
        {   
            CardSuit.Clubs => 'c',
            CardSuit.Hearts => 'c',
            CardSuit.Diamonds => 'c',
            CardSuit.Spades => 'c',
            _ => 'c'
            // CardSuit.Clubs => 'c',
            // CardSuit.Hearts => 'h',
            // CardSuit.Diamonds => 'd',
            // CardSuit.Spades => 's',
            // _ => 'c'
        };
        return $"{prefix}{((int)rank).ToString("D2")}.png";
    }
}