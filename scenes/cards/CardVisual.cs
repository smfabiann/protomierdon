using Godot;
using System;
using System.Collections.Generic;

public partial class CardVisual : Node3D
{
    [Export] private MeshInstance3D _mesh;
    private ShaderMaterial _shaderMaterial;

    public PokerCardData DatosCarta { get; private set; }

    private readonly List<Vector3> _stickerPositions = new();
    private readonly List<float>   _stickerRotations = new();

    public override void _Ready()
    {
        _mesh ??= GetNode<MeshInstance3D>("MeshInstance3D");

        if (_mesh.MaterialOverride is ShaderMaterial baseMat)
        {
            _shaderMaterial = (ShaderMaterial)baseMat.Duplicate();
            _mesh.MaterialOverride = _shaderMaterial;
        }
    }

    public void InjectConfiguration(PokerCardData data)
    {
        DatosCarta = data;
        _stickerPositions.Clear();
        _stickerRotations.Clear();
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

        Texture2D textureFront = CardMaterialCache.GetTexture(DatosCarta.Suit, DatosCarta.Rank);
        Texture2D textureBack  = CardMaterialCache.GetTexture("back");

        if (textureFront == null)
        {
            GD.PrintErr($"[CardVisual] No se encontro textura en cache para: {DatosCarta.Suit}_{DatosCarta.Rank}");
            return;
        }

        _shaderMaterial?.SetShaderParameter("texture_front", textureFront);
        _shaderMaterial?.SetShaderParameter("texture_back", textureBack);

        UpdateStickerBadges();
    }

    private void UpdateStickerBadges()
    {
        foreach (Node child in GetChildren())
        {
            if (child.Name.ToString().StartsWith("Sticker_"))
            {
                child.QueueFree();
            }
        }

        if (DatosCarta == null || DatosCarta.Stickers.Count == 0) return;

        while (_stickerPositions.Count < DatosCarta.Stickers.Count)
        {
            float randX = (float)GD.RandRange(-0.22f, 0.22f);
            float randY = (float)GD.RandRange(-0.32f, 0.32f);
            int idx = _stickerPositions.Count;
            float layerZ = 0.015f + (idx * 0.005f);

            _stickerPositions.Add(new Vector3(randX, randY, layerZ));
            _stickerRotations.Add((float)GD.RandRange(-35f, 35f));
        }

        for (int i = 0; i < DatosCarta.Stickers.Count; i++)
        {
            StickerType st = DatosCarta.Stickers[i];
            Texture2D tex = CardMaterialCache.GetStickerTexture(st);
            Vector3 pos = _stickerPositions[i];
            float rotZ = _stickerRotations[i];

            if (tex != null)
            {
                var sprite = new Sprite3D();
                sprite.Name = $"Sticker_Sprite_{i}";
                sprite.Texture = tex;
                sprite.PixelSize = 0.0025f;
                sprite.Shaded = false;
                sprite.DoubleSided = false;
                sprite.RenderPriority = 2 + i;
                sprite.Position = pos;
                sprite.RotationDegrees = new Vector3(0f, 0f, rotZ);
                AddChild(sprite);
            }
            else
            {
                var lbl = new Label3D();
                lbl.Name = $"Sticker_Label_{i}";
                lbl.Text = st switch
                {
                    StickerType.PlusTwo  => "[+2]",
                    StickerType.PlusFive => "[+5]",
                    StickerType.Double   => "[x2]",
                    StickerType.Invert   => "[INV]",
                    _                    => ""
                };
                lbl.PixelSize = 0.004f;
                lbl.FontSize = 80;
                lbl.OutlineSize = 24;
                lbl.OutlineModulate = Colors.Black;
                lbl.Shaded = false;
                lbl.DoubleSided = false;
                lbl.RenderPriority = 2 + i;
                lbl.Position = new Vector3(pos.X, pos.Y, pos.Z + 0.002f);
                lbl.RotationDegrees = new Vector3(0f, 0f, rotZ);
                lbl.Modulate = st switch
                {
                    StickerType.PlusTwo  => new Color(0.2f, 1.0f, 0.4f),
                    StickerType.PlusFive => new Color(1.0f, 0.9f, 0.2f),
                    StickerType.Double   => new Color(0.3f, 0.8f, 1.0f),
                    StickerType.Invert   => new Color(1.0f, 0.3f, 0.3f),
                    _                    => Colors.White
                };
                AddChild(lbl);
            }
        }
    }
}