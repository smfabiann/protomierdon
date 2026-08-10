using Godot;
using System;

public partial class TableGameTrigger : Area3D
{
	[Export] public Camera3D ReferenceCamera;
	[Export] public Camera3D MainCamera;
	[Export] public Player PlayerNode;

	private bool _isPlayingCards = false;
	private bool _isPlayerInside = false; 
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Entrada
	private void _on_body_entered(Node3D body)
	{
		GD.Print("Muerte a Antonio Campos");
		if (body is Player)
		{
			_isPlayerInside = true;
		}
	}
	// Salida
	private void _on_body_exited(Node3D body)
	{
		if (body is Player)
		{
			_isPlayerInside = false;
		}
	}

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_isPlayerInside && !_isPlayingCards && @event.IsActionPressed("ui_accept"))
		{
			EnterTableMode();
		} else if (_isPlayingCards && @event.IsActionPressed("ui_cancel"))
		{
			ExitTableMode();
		}
    }

	private void EnterTableMode()
	{
		_isPlayingCards = true;
		// Cambiamos la camara
		ReferenceCamera.MakeCurrent();
		Input.MouseMode = Input.MouseModeEnum.Visible;
		PlayerNode.SetProcessUnhandledInput(false);
        PlayerNode.SetPhysicsProcess(false);

		GD.Print("MODO SEXO ACTIVADO!!!!");
	}

	private void ExitTableMode()
	{
		_isPlayingCards = false;
		MainCamera.MakeCurrent();
		Input.MouseMode = Input.MouseModeEnum.Captured;
		PlayerNode.SetProcessUnhandledInput(true);
        PlayerNode.SetPhysicsProcess(true);
	}
	// // Called every frame. 'delta' is the elapsed time since the previous frame.
	// public override void _Process(double delta)
	// {
	// }
}
