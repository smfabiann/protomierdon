using System;

// Definimos los tipos de cartas que existen
public enum CardSuit
{
    Hearts,
    Diamonds,
    Clubs,
    Spades
}

// Definilos los valores de las cartas
public enum CardRank {
    // Viendo si usar esta
    // Two = 2, Three, Four, Five, Six, Seven, Eight, Nine, Ten,
    // Jack = 11, Queen = 12, King = 13, Ace = 14
    Ace = 1, Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten,
    Jack = 11, Queen = 12, King = 13
}

public class PokerCardData {
    public CardSuit Suit {get; private set;}
    public CardRank Rank {get; private set;}

    // constructor
    public PokerCardData(CardSuit suit, CardRank rank)
    {
        Suit = suit;
        Rank = rank;
    }
};