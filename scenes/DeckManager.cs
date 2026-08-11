using Godot;
using System;
using System.Collections.Generic;

public partial class DeckManager : Node3D
{
	[Export] public PackedScene CardVisualScene;

	private List<PokerCardData> deck = new List<PokerCardData>();
	private void generateDeck()
	{
		deck.Clear();
		Array rankArray = Enum.GetValues(typeof(CardRank));
		Array suitArray = Enum.GetValues(typeof(CardSuit));

		for (int i=0; i < rankArray.Length; i++)
		{
			for (int j=0; j< suitArray.Length; j++)
			{
				PokerCardData carta = new PokerCardData((CardSuit)suitArray.GetValue(j), (CardRank)rankArray.GetValue(i));
				deck.Add(carta);
			}
		}
	}

	// Logica un Pilin mas eficiente a retirar cartas en la posicion n-1, efectivamente un pop y evitar O(n) por carta retirada
	public PokerCardData PopCard()
	{
		if (deck.Count == 0) 
		{
			GD.PrintErr("Mazo vacio");
			return null; 
		}

		int ultimoIndice = deck.Count - 1; 
		PokerCardData cartaRobada = deck[ultimoIndice]; 
		deck.RemoveAt(ultimoIndice); 

		return cartaRobada;
	}

	private void distributeCardsVisual()
	{
		if (deck.Count == 0) return;

		int ultimaCarta = deck.Count - 1;
		PokerCardData cardLogic = PopCard();

		// yo no cachaba esta wea antes, se pueden instanciar escenas y colocarlas en alguna parte
		CardVisual newCard3D = CardVisualScene.Instantiate<CardVisual>();
		newCard3D.InjectConfiguration(cardLogic);

		// metodo de Godot, le colocamos un hijo al nodo DeckManager, en este caso, la scene de la carta instanciada
		AddChild(newCard3D);
		newCard3D.Position = new Vector3(0, 2, 0);
		GD.Print("Se crea una carta en el nivel");
	}

	// Called when the node enters the scene tree for the first time.
	// public override void _Ready()
	// {
	// 	generateDeck();
	// 	distributeCardsVisual();
	// }
}
