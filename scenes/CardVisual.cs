using Godot;
using System;

public partial class CardVisual : Node3D
{
	public PokerCardData DatosCarta {get; private set;}

	public void InjectConfiguration(PokerCardData data)
	{
		DatosCarta = data;
		UpdateVisuals();
	}

	public void UpdateVisuals()
	{
		// TODO: actualizar el aspecto de la carta acorde de los datos
        GD.Print("TODO: actualizar visual de la carta;");
	}
}
