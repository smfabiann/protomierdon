using Godot;

public partial class SkyJumpPlatform : StaticBody3D
{
	public enum PlatformType { Normal, Fragile, Spring }

	public PlatformType Type { get; private set; } = PlatformType.Normal;

	private MeshInstance3D _mesh;
	private float _fragileTimer = -1f;
	private bool _breaking = false;

	/// <summary>
	/// Velocidad de rebote que este tipo de plataforma otorga al jugador.
	/// </summary>
	public float BounceVelocity => Type == PlatformType.Spring ? 20f : 13f;

	/// <summary>
	/// Configura la plataforma creando su colisión y mesh visual.
	/// </summary>
	public void Setup(PlatformType type, Vector3 size)
	{
		Type = type;

		// --- Colisión ---
		var collision = new CollisionShape3D();
		var shape = new BoxShape3D();
		shape.Size = size;
		collision.Shape = shape;
		AddChild(collision);

		// --- Mesh visual ---
		_mesh = new MeshInstance3D();
		var boxMesh = new BoxMesh();
		boxMesh.Size = size;
		_mesh.Mesh = boxMesh;

		var material = new StandardMaterial3D();
		material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;

		switch (type)
		{
			case PlatformType.Normal:
				material.AlbedoColor = new Color(0.3f, 0.85f, 0.4f);
				break;
			case PlatformType.Fragile:
				material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
				material.AlbedoColor = new Color(0.95f, 0.35f, 0.25f, 0.8f);
				break;
			case PlatformType.Spring:
				material.AlbedoColor = new Color(1.0f, 0.85f, 0.1f);
				break;
		}

		_mesh.MaterialOverride = material;
		AddChild(_mesh);

		// Indicador visual extra para Spring: una línea/franja encima
		if (type == PlatformType.Spring)
		{
			var indicator = new MeshInstance3D();
			var indicatorMesh = new BoxMesh();
			indicatorMesh.Size = new Vector3(size.X * 0.6f, size.Y * 0.5f, size.Z * 0.5f);
			indicator.Mesh = indicatorMesh;
			indicator.Position = new Vector3(0, size.Y * 0.5f, 0);

			var indicatorMat = new StandardMaterial3D();
			indicatorMat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
			indicatorMat.AlbedoColor = new Color(1.0f, 0.5f, 0.0f);
			indicator.MaterialOverride = indicatorMat;
			AddChild(indicator);
		}
	}

	/// <summary>
	/// Llamado cuando el jugador aterriza en esta plataforma.
	/// </summary>
	public void OnPlayerLanded()
	{
		if (Type == PlatformType.Fragile && !_breaking)
		{
			_breaking = true;
			_fragileTimer = 0.4f;
		}
	}

	public override void _Process(double delta)
	{
		if (!_breaking) return;

		_fragileTimer -= (float)delta;

		// Efecto de parpadeo antes de romperse
		if (_mesh != null)
		{
			_mesh.Visible = ((int)(_fragileTimer * 15)) % 2 == 0;
		}

		if (_fragileTimer <= 0)
		{
			QueueFree();
		}
	}
}
