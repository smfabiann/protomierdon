using Godot;
using System;

public partial class CardVisual : Node3D
{
    [Export] private MeshInstance3D _mesh;

    public PokerCardData DatosCarta { get; private set; }

    public override void _Ready()
    {
        _mesh ??= GetNode<MeshInstance3D>("MeshInstance3D");
    }

    public void InjectConfiguration(PokerCardData data)
    {
        DatosCarta = data;
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (DatosCarta == null) return;

        _mesh ??= GetNode<MeshInstance3D>("MeshInstance3D");

        // 1. Obtener la referencia al material pre-cargado desde la caché estática
        StandardMaterial3D material = CardMaterialCache.GetMaterial(DatosCarta.Suit, DatosCarta.Rank);

        if (material == null)
        {
            GD.PrintErr($"[CardVisual] No se encontró el material en caché para: {DatosCarta.Suit}_{DatosCarta.Rank}");
            return;
        }

        // 2. Asignarlo directamente al MeshInstance3D
        _mesh.MaterialOverride = material;
    }
}