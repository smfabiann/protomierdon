using System;
using System.Collections.Generic;

public enum CardSuit
{
    Hearts,
    Diamonds,
    Clubs,
    Spades
}

public enum CardRank
{
    Ace = 1, Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten,
    Jack = 11, Queen = 12, King = 13
}

public enum StickerType
{
    None,
    PlusTwo,
    PlusFive,
    Double,
    Invert,
}

public class PokerCardData
{
    public CardSuit Suit { get; private set; }
    public CardRank Rank { get; private set; }
    public List<StickerType> Stickers { get; private set; } = new();

    public PokerCardData(CardSuit suit, CardRank rank)
    {
        Suit = suit;
        Rank = rank;
    }

    public void AddSticker(StickerType sticker)
    {
        if (sticker != StickerType.None)
        {
            Stickers.Add(sticker);
        }
    }

    public int BaseValue => Rank == CardRank.Ace ? 14 : (int)Rank;

    public int EffectiveValue
    {
        get
        {
            int val = BaseValue;
            foreach (var st in Stickers)
            {
                if (st == StickerType.PlusTwo) val += 2;
                else if (st == StickerType.PlusFive) val += 5;
            }
            foreach (var st in Stickers)
            {
                if (st == StickerType.Double) val *= 2;
            }
            return val;
        }
    }

    public bool HasInvertRule => Stickers.Contains(StickerType.Invert);

    public string StickerLabel
    {
        get
        {
            if (Stickers.Count == 0) return "";
            var labels = new List<string>();
            foreach (var st in Stickers)
            {
                labels.Add(st switch
                {
                    StickerType.PlusTwo  => "[+2]",
                    StickerType.PlusFive => "[+5]",
                    StickerType.Double   => "[x2]",
                    StickerType.Invert   => "[INV]",
                    _                    => ""
                });
            }
            return string.Join("", labels);
        }
    }
}