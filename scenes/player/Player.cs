 using Godot;
using System;

public partial class Player : CharacterBody3D
{
	[ExportGroup("Camera")]
	[Export] public float  MouseSensitivity = 0.002f;
	[Export] public float MinPitch = -89.0f;
    [Export] public float MaxPitch = 89.0f;
	private Node3D _cameraPivot;
	public const float Speed = 5.0f;
	public const float JumpVelocity = 4.5f;

	public override void _Ready()
	{
		// atrapamos el mouse
		Input.MouseMode = Input.MouseModeEnum.Captured;
		// referencia al pivote de la cámara
		_cameraPivot = GetNode<Node3D>("PlayerCameraPivot");
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// Mouse Movement Handler
		// Recuerda que se utilizara un povite, no me acuerdo porque
		if (@event is InputEventMouseMotion mouseMotion)
		{
			// rota el jugador horizontalmente
			RotateY(-mouseMotion.Relative.X * MouseSensitivity);
			Vector3 pivotRot = _cameraPivot.Rotation;
			pivotRot.X -= mouseMotion.Relative.Y * MouseSensitivity;

			// pa que no de la vuelta completa
			pivotRot.X = Mathf.Clamp(pivotRot.X, Mathf.DegToRad(MinPitch), Mathf.DegToRad(MaxPitch));
			_cameraPivot.Rotation = pivotRot;
		}
		// Captura o libera mouse
		if (@event.IsActionPressed("ui_cancel"))
		{
			if (Input.MouseMode == Input.MouseModeEnum.Captured)
			{
				Input.MouseMode = Input.MouseModeEnum.Visible;
			} else
			{
				Input.MouseMode = Input.MouseModeEnum.Captured;
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		// Handle Jump.
		// if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
		// {
		// 	velocity.Y = JumpVelocity;
		// }

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * Speed;
			velocity.Z = direction.Z * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();
	}
}
