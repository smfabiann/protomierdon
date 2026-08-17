using Godot;
using System;

public partial class CardVisual : Node3D
{
    [Export] private MeshInstance3D _mesh;
    private ShaderMaterial _shaderMaterial;

    public PokerCardData DatosCarta { get; private set; }

    public override void _Ready()
    {
        _mesh ??= GetNode<MeshInstance3D>("MeshInstance3D");

        // Duplicamos el material para que cada carta tenga su propia instancia única
        // y no cambien todas las cartas a la vez al asignar la textura
        if (_mesh.MaterialOverride is ShaderMaterial baseMat)
        {
            _shaderMaterial = (ShaderMaterial)baseMat.Duplicate();
            _mesh.MaterialOverride = _shaderMaterial;
        }
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

        if (_shaderMaterial == null && _mesh.MaterialOverride is ShaderMaterial mat)
        {
            _shaderMaterial = (ShaderMaterial)mat.Duplicate();
            _mesh.MaterialOverride = _shaderMaterial;
        }

        // 1. Obtener las texturas desde la caché
        Texture2D textureFront = CardMaterialCache.GetTexture(DatosCarta.Suit, DatosCarta.Rank);
        Texture2D textureBack  = CardMaterialCache.GetTexture("back");

        if (textureFront == null)
        {
            GD.PrintErr($"[CardVisual] No se encontró textura en caché para: {DatosCarta.Suit}_{DatosCarta.Rank}");
            return;
        }

        // 2. Asignar los parámetros directamente al Shader
        _shaderMaterial?.SetShaderParameter("texture_front", textureFront);
        _shaderMaterial?.SetShaderParameter("texture_back", textureBack);
    }
}