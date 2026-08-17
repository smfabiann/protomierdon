using Godot;
using System;
using System.Collections.Generic;

public static class CardMaterialCache
{
    private static readonly Dictionary<string, Texture2D> _textureCache = new();
    private static bool _isInitialized = false;

    public static void Initialize()
    {
        if (_isInitialized) return;

        // Pre-cargar las 52 texturas de las cartas
        foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
        {
            foreach (CardRank rank in Enum.GetValues(typeof(CardRank)))
            {
                string key = GetKey(suit, rank);
                string path = $"res://assets/PNG/poker_cards/{GetFileName(suit, rank)}";

                var texture = GD.Load<Texture2D>(path);
                if (texture != null)
                {
                    _textureCache[key] = texture;
                }
                else
                {
                    GD.PrintErr($"[CardMaterialCache] No se pudo cargar: {path}");
                }
            }
        }

        // Cargar el reverso de la carta
        Texture2D textureBack = GD.Load<Texture2D>("res://assets/PNG/poker_cards/back.png");
        if (textureBack != null)
        {
            _textureCache["back"] = textureBack;
        }
        else
        {
            GD.PrintErr("[CardMaterialCache] No se pudo cargar el reverso 'back.png'");
        }

        _isInitialized = true;
    }

    public static Texture2D GetTexture(CardSuit suit, CardRank rank)
    {
        string key = GetKey(suit, rank);
        return _textureCache.GetValueOrDefault(key);
    }

    public static Texture2D GetTexture(string key)
    {
        return _textureCache.GetValueOrDefault(key);
    }

    private static string GetKey(CardSuit suit, CardRank rank) => $"{suit}_{rank}";

    private static string GetFileName(CardSuit suit, CardRank rank)
    {
        char prefix = suit switch
        {
            CardSuit.Clubs    => 'c',
            CardSuit.Hearts   => 'c',
            CardSuit.Diamonds => 'c',
            CardSuit.Spades   => 'c',
            _                 => 'c'
            // CardSuit.Clubs    => 'c',
            // CardSuit.Hearts   => 'h',
            // CardSuit.Diamonds => 'd',
            // CardSuit.Spades   => 's',
            // _                 => 'c'
        };
        return $"{prefix}{((int)rank).ToString("D2")}.png";
    }
}