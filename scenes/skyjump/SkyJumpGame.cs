using Godot;
using System;
using System.Collections.Generic;

public partial class SkyJumpGame : Node3D
{
	// ================= CONSTANTES =================

	// Dimensiones del área de juego
	private const float LevelWidth = 16.0f;
	private const float PlatformWidth = 2.8f;
	private const float PlatformHeight = 0.35f;
	private const float PlatformDepth = 1.5f;
	private const float StartPlatformWidth = 5.0f;

	// Espaciado vertical entre plataformas
	private const float MinSpacingY = 2.0f;
	private const float MaxSpacingY = 3.8f;

	// Física del jumper
	private const float BounceVelocity = 13.0f;
	private const float SpringBounceVelocity = 20.0f;
	private const float MoveSpeed = 12.0f;
	private const float JumperGravity = 28.0f;
	private const float JumperRadius = 0.45f;

	// Cámara
	private const float CameraSize = 18.0f;
	private const float CameraZOffset = 20.0f;
	private const float ArenaYOffset = 50.0f;

	// Generación y limpieza de plataformas
	private const float GenerateAhead = 50.0f;
	private const float CleanupBehind = 25.0f;

	// ================= ESTADO DEL JUEGO =================

	private bool _isActive = false;
	private bool _gameOver = false;
	private float _maxHeight = 0f;
	private float _cameraY = 0f;
	private int _score = 0;
	private int _highScore = 0;

	// ================= NODOS =================

	private CharacterBody3D _jumper;
	private Camera3D _gameCamera;
	private SkyJumpUI _ui;
	private readonly List<SkyJumpPlatform> _platforms = new();
	private float _highestPlatformY = 0f;
	private Vector3 _arenaOrigin;

	// ================= RNG =================

	private readonly RandomNumberGenerator _rng = new();

	// ================= API PÚBLICA =================

	public Camera3D GetCamera() => _gameCamera;
	public bool IsActive => _isActive;

	public void SetUI(SkyJumpUI ui)
	{
		_ui = ui;
	}

	public override void _Ready()
	{
		_rng.Randomize();
		// El arena se posiciona alto para que no se vea el suelo del nivel principal
		_arenaOrigin = GlobalPosition + new Vector3(0, ArenaYOffset, 0);
		CreateCamera();
		CreateJumper();
	}

	/// <summary>
	/// Inicia (o reinicia) el minijuego.
	/// </summary>
	public void StartGame()
	{
		_isActive = true;
		_gameOver = false;
		_maxHeight = 0f;
		_score = 0;
		_highestPlatformY = 0f;

		ClearPlatforms();

		// Posicionar el jumper encima de la plataforma inicial
		_jumper.GlobalPosition = _arenaOrigin + new Vector3(0, 2.0f, 0);
		_jumper.Velocity = new Vector3(0, BounceVelocity, 0);
		_jumper.Visible = true;
		_jumper.CollisionMask = 8;

		// Plataforma inicial (más ancha y centrada)
		CreatePlatform(
			_arenaOrigin + new Vector3(0, 0f, 0),
			SkyJumpPlatform.PlatformType.Normal,
			StartPlatformWidth
		);

		// Generar plataformas iniciales
		GeneratePlatforms();

		// Resetear cámara
		_cameraY = _arenaOrigin.Y + CameraSize * 0.3f;
		UpdateCameraPosition();
		_gameCamera.MakeCurrent();

		// UI
		if (_ui != null)
		{
			_ui.Visible = true;
			_ui.ShowGameUI();
			_ui.UpdateScore(0);
			_ui.UpdateHighScore(_highScore);
		}
	}

	/// <summary>
	/// Detiene el minijuego y limpia todo.
	/// </summary>
	public void StopGame()
	{
		_isActive = false;
		_gameOver = false;
		_jumper.Visible = false;
		_jumper.Velocity = Vector3.Zero;
		ClearPlatforms();

		if (_ui != null)
		{
			_ui.Visible = false;
		}
	}

	public void SetUIVisible(bool visible)
	{
		if (_ui != null)
		{
			_ui.Visible = visible;
		}
	}

	// ================= PHYSICS LOOP =================

	public override void _PhysicsProcess(double delta)
	{
		if (!_isActive || _gameOver) return;
		ProcessJumper((float)delta);
	}

	private void ProcessJumper(float delta)
	{
		Vector3 velocity = _jumper.Velocity;

		// --- Gravedad ---
		velocity.Y -= JumperGravity * delta;

		// --- Input horizontal ---
		float inputX = 0f;
		if (Input.IsActionPressed("ui_left")) inputX -= 1f;
		if (Input.IsActionPressed("ui_right")) inputX += 1f;
		velocity.X = inputX * MoveSpeed;
		velocity.Z = 0f;

		// --- One-way platforms ---
		// Cuando sube, no colisiona con plataformas (las atraviesa)
		// Cuando cae, sí colisiona (aterriza encima)
		_jumper.CollisionMask = velocity.Y > 0.5f ? (uint)0 : (uint)8;

		_jumper.Velocity = velocity;
		_jumper.MoveAndSlide();

		// Bloquear eje Z
		Vector3 pos = _jumper.GlobalPosition;
		pos.Z = _arenaOrigin.Z;
		_jumper.GlobalPosition = pos;

		// --- Detectar aterrizaje y rebotar ---
		if (_jumper.IsOnFloor())
		{
			for (int i = 0; i < _jumper.GetSlideCollisionCount(); i++)
			{
				var collision = _jumper.GetSlideCollision(i);
				var collider = collision.GetCollider();
				if (collider is SkyJumpPlatform platform)
				{
					float bounce = platform.Type == SkyJumpPlatform.PlatformType.Spring
						? SpringBounceVelocity
						: BounceVelocity;
					_jumper.Velocity = new Vector3(_jumper.Velocity.X, bounce, 0f);
					platform.OnPlayerLanded();
					break;
				}
			}
		}

		// --- Wrap horizontal (aparece del otro lado) ---
		float halfWidth = LevelWidth / 2.0f;
		pos = _jumper.GlobalPosition;
		if (pos.X < _arenaOrigin.X - halfWidth)
		{
			pos.X += LevelWidth;
			_jumper.GlobalPosition = pos;
		}
		else if (pos.X > _arenaOrigin.X + halfWidth)
		{
			pos.X -= LevelWidth;
			_jumper.GlobalPosition = pos;
		}

		// --- Actualizar puntaje (altura máxima) ---
		float currentHeight = _jumper.GlobalPosition.Y - _arenaOrigin.Y;
		if (currentHeight > _maxHeight)
		{
			_maxHeight = currentHeight;
			_score = Mathf.FloorToInt(_maxHeight);
			_ui?.UpdateScore(_score);
		}

		// --- Cámara: sube suavemente, nunca baja ---
		float targetCameraY = _arenaOrigin.Y + _maxHeight + CameraSize * 0.3f;
		if (targetCameraY > _cameraY)
		{
			_cameraY = Mathf.Lerp(_cameraY, targetCameraY, 4.0f * delta);
		}
		UpdateCameraPosition();

		// --- Game Over: cayó debajo de la vista de la cámara ---
		float cameraBottom = _cameraY - CameraSize / 2.0f - 2.0f;
		if (_jumper.GlobalPosition.Y < cameraBottom)
		{
			TriggerGameOver();
		}

		// --- Generación y limpieza ---
		GeneratePlatforms();
		CleanupPlatforms();
	}

	private void TriggerGameOver()
	{
		_gameOver = true;
		_isActive = false;
		_jumper.Velocity = Vector3.Zero;

		if (_score > _highScore)
		{
			_highScore = _score;
		}

		_ui?.ShowGameOver(_score, _highScore);
	}

	// ================= CÁMARA =================

	private void UpdateCameraPosition()
	{
		_gameCamera.GlobalPosition = new Vector3(
			_arenaOrigin.X,
			_cameraY,
			_arenaOrigin.Z + CameraZOffset
		);
	}

	// ================= GENERACIÓN DE PLATAFORMAS =================

	private void GeneratePlatforms()
	{
		float generateUpTo = _cameraY + GenerateAhead;

		while (_arenaOrigin.Y + _highestPlatformY < generateUpTo)
		{
			float spacing = _rng.RandfRange(MinSpacingY, MaxSpacingY);
			_highestPlatformY += spacing;

			float halfPlayable = (LevelWidth - PlatformWidth) / 2.0f;
			float xOffset = _rng.RandfRange(-halfPlayable, halfPlayable);

			// Determinar tipo de plataforma
			// La dificultad aumenta con la altura
			var type = SkyJumpPlatform.PlatformType.Normal;
			float roll = _rng.Randf();
			float fragileChance = Mathf.Min(0.12f + _highestPlatformY * 0.0008f, 0.35f);
			float springChance = 0.08f;

			if (roll < springChance)
				type = SkyJumpPlatform.PlatformType.Spring;
			else if (roll < springChance + fragileChance)
				type = SkyJumpPlatform.PlatformType.Fragile;

			Vector3 position = _arenaOrigin + new Vector3(xOffset, _highestPlatformY, 0f);
			CreatePlatform(position, type);
		}
	}

	private SkyJumpPlatform CreatePlatform(Vector3 position, SkyJumpPlatform.PlatformType type, float customWidth = -1f)
	{
		var platform = new SkyJumpPlatform();
		platform.CollisionLayer = 8;  // Bit 3 — capa de plataformas
		platform.CollisionMask = 0;   // No detecta nada (es estática)
		AddChild(platform);
		platform.GlobalPosition = position;

		float width = customWidth > 0 ? customWidth : PlatformWidth;
		platform.Setup(type, new Vector3(width, PlatformHeight, PlatformDepth));
		_platforms.Add(platform);
		return platform;
	}

	private void CleanupPlatforms()
	{
		float cutoffY = _cameraY - CleanupBehind;

		for (int i = _platforms.Count - 1; i >= 0; i--)
		{
			if (!IsInstanceValid(_platforms[i]))
			{
				_platforms.RemoveAt(i);
				continue;
			}
			if (_platforms[i].GlobalPosition.Y < cutoffY)
			{
				_platforms[i].QueueFree();
				_platforms.RemoveAt(i);
			}
		}
	}

	private void ClearPlatforms()
	{
		foreach (var platform in _platforms)
		{
			if (IsInstanceValid(platform))
				platform.QueueFree();
		}
		_platforms.Clear();
	}

	// ================= CREACIÓN DE NODOS =================

	private void CreateCamera()
	{
		_gameCamera = new Camera3D();
		_gameCamera.Projection = Camera3D.ProjectionType.Orthogonal;
		_gameCamera.Size = CameraSize;
		_gameCamera.Near = 0.1f;
		_gameCamera.Far = 200.0f;
		AddChild(_gameCamera);
		_cameraY = _arenaOrigin.Y + CameraSize * 0.3f;
		UpdateCameraPosition();
	}

	private void CreateJumper()
	{
		_jumper = new CharacterBody3D();
		_jumper.CollisionLayer = 4;   // Bit 2 — capa del jumper
		_jumper.CollisionMask = 8;    // Bit 3 — detecta plataformas
		_jumper.FloorSnapLength = 0f;
		_jumper.FloorStopOnSlope = false;
		_jumper.UpDirection = Vector3.Up;

		// Forma de colisión: esfera
		var collisionShape = new CollisionShape3D();
		var sphereShape = new SphereShape3D();
		sphereShape.Radius = JumperRadius;
		collisionShape.Shape = sphereShape;
		_jumper.AddChild(collisionShape);

		// Mesh visual: esfera azul
		var meshInstance = new MeshInstance3D();
		var sphereMesh = new SphereMesh();
		sphereMesh.Radius = JumperRadius;
		sphereMesh.Height = JumperRadius * 2.0f;
		meshInstance.Mesh = sphereMesh;

		var material = new StandardMaterial3D();
		material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
		material.AlbedoColor = new Color(0.2f, 0.45f, 1.0f);
		meshInstance.MaterialOverride = material;
		_jumper.AddChild(meshInstance);

		// Ojitos para darle personalidad (miran hacia la cámara = +Z)
		AddEye(_jumper, new Vector3(-0.15f, 0.12f, 0.38f));
		AddEye(_jumper, new Vector3(0.15f, 0.12f, 0.38f));

		AddChild(_jumper);
		_jumper.Visible = false;
	}

	private void AddEye(Node3D parent, Vector3 localPos)
	{
		// Ojo blanco
		var eye = new MeshInstance3D();
		var eyeMesh = new SphereMesh();
		eyeMesh.Radius = 0.09f;
		eyeMesh.Height = 0.18f;
		eye.Mesh = eyeMesh;
		eye.Position = localPos;

		var eyeMat = new StandardMaterial3D();
		eyeMat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
		eyeMat.AlbedoColor = Colors.White;
		eye.MaterialOverride = eyeMat;
		parent.AddChild(eye);

		// Pupila negra
		var pupil = new MeshInstance3D();
		var pupilMesh = new SphereMesh();
		pupilMesh.Radius = 0.045f;
		pupilMesh.Height = 0.09f;
		pupil.Mesh = pupilMesh;
		pupil.Position = new Vector3(0, 0, 0.055f);

		var pupilMat = new StandardMaterial3D();
		pupilMat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
		pupilMat.AlbedoColor = Colors.Black;
		pupil.MaterialOverride = pupilMat;
		eye.AddChild(pupil);
	}
}
