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

    public static Texture2D GetStickerTexture(StickerType type)
    {
        if (type == StickerType.None) return null;
        string key = $"sticker_{type}";
        if (_textureCache.TryGetValue(key, out var cached)) return cached;

        string fileName = type switch
        {
            StickerType.PlusTwo  => "plus2.png",
            StickerType.PlusFive => "plus5.png",
            StickerType.Double   => "double.png",
            StickerType.Invert   => "invert.png",
            _                    => null
        };

        if (fileName != null)
        {
            string path = $"res://assets/PNG/stickers/{fileName}";
            if (ResourceLoader.Exists(path))
            {
                var tex = GD.Load<Texture2D>(path);
                if (tex != null)
                {
                    _textureCache[key] = tex;
                    return tex;
                }
            }
        }

        return null;
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
        };
        return $"{prefix}{((int)rank).ToString("D2")}.png";
    }
}