using Godot;
using System;

public partial class CardVisual : Node3D
{
    // Opción recomendada: Exportarlo para asignarlo en el inspector, o buscarlo con GetNode
    [Export] private MeshInstance3D _mesh;

    public PokerCardData DatosCarta { get; private set; }

    public override void _Ready()
    {
        // Si no está asignado por el inspector, lo busca por nombre en los hijos
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

        // Asegurarse de tener la referencia al nodo
        _mesh ??= GetNode<MeshInstance3D>("MeshInstance3D");

        // 1. Obtener la ruta de la textura basada en los datos
        string texturePath = GetTexturePath(DatosCarta);

        // 2. Cargar la textura desde el sistema de archivos
        var texture = GD.Load<Texture2D>(texturePath);

        if (texture == null)
        {
            GD.PrintErr($"[CardVisual] No se encontró la textura: {texturePath}");
            return;
        }

        // 3. Crear un material único para esta carta y asignarle la textura
        var material = new StandardMaterial3D();
        material.AlbedoTexture = texture;

        // Si quieres que se vea por ambos lados (opcional):
        material.CullMode = BaseMaterial3D.CullModeEnum.Disabled;

        // 4. Asignarlo al MeshInstance3D
        _mesh.MaterialOverride = material;
    }

    private string GetTexturePath(PokerCardData data)
    {
        // Mapeamos el palo a la letra inicial de tu archivo (ej: 'c' para Clubs)
        char suitPrefix = data.Suit switch
        {
			CardSuit.Clubs => 'c',
            CardSuit.Hearts => 'c',
            CardSuit.Diamonds => 'c',
            CardSuit.Spades => 'c',
            _ => 'c'
            // Mientras tanto
            // CardSuit.Clubs => 'c',
            // CardSuit.Hearts => 'h',
            // CardSuit.Diamonds => 'd',
            // CardSuit.Spades => 's',
            // _ => 'c'
        };

        // Formatea el número a dos dígitos: 1 -> "01", 5 -> "05"
        int rankNumber = (int)data.Rank;
        string rankStr = rankNumber.ToString("D2");

        // Caso especial si tu As se llama "a01.png" en lugar de "s01.png"/"c01.png":
        // string fileName = (data.Rank == CardRank.Ace) ? $"a01.png" : $"{suitPrefix}{rankStr}.png";

        string fileName = $"{suitPrefix}{rankStr}.png";

        return $"res://PNG/poker_cards/{fileName}";
    }
}