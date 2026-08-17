using Godot;

public partial class SkyJumpTrigger : Area3D
{
	[Export] public SkyJumpGame GameNode;
	[Export] public Camera3D MainCamera;
	[Export] public Player PlayerNode;

	private bool _isPlayingSkyJump = false;
	private bool _isPlayerInside = false;

	public override void _Ready()
	{
		if (GameNode == null)
		{
			GD.PrintErr("[SkyJumpTrigger] Falta asignar GameNode en el Inspector.");
		}
	}

	// Entrada del jugador al área
	private void _on_body_entered(Node3D body)
	{
		if (body is Player)
		{
			_isPlayerInside = true;
		}
	}

	// Salida del jugador del área
	private void _on_body_exited(Node3D body)
	{
		if (body is Player)
		{
			_isPlayerInside = false;
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_isPlayerInside && !_isPlayingSkyJump && @event.IsActionPressed("ui_accept"))
		{
			EnterSkyJumpMode();
		}
		else if (_isPlayingSkyJump && @event.IsActionPressed("ui_cancel"))
		{
			ExitSkyJumpMode();
		}
	}

	private void EnterSkyJumpMode()
	{
		_isPlayingSkyJump = true;
		// Desactivamos al jugador 3D
		Input.MouseMode = Input.MouseModeEnum.Visible;
		PlayerNode.SetProcessUnhandledInput(false);
		PlayerNode.SetPhysicsProcess(false);
		// Iniciamos el minijuego
		GameNode.StartGame();
	}

	private void ExitSkyJumpMode()
	{
		_isPlayingSkyJump = false;
		// Detenemos el minijuego
		GameNode.StopGame();
		// Restauramos la cámara principal y el jugador
		MainCamera.MakeCurrent();
		Input.MouseMode = Input.MouseModeEnum.Captured;
		PlayerNode.SetProcessUnhandledInput(true);
		PlayerNode.SetPhysicsProcess(true);
	}
}
