using Godot;
using System;

public partial class MainMenu : Control
{
	[Export] public string GameScenePath { get; set; } = "res://scenes/level.tscn";

	public override void _Ready()
	{
		var container = GetNodeOrNull<VBoxContainer>("MarginContainer/VBoxContainer");
		if (container != null && GetNodeOrNull<Button>("MarginContainer/VBoxContainer/ButtonExit") == null)
		{
			var btnExit = new Button();
			btnExit.Name = "ButtonExit";
			btnExit.Text = "SALIR";
			btnExit.CustomMinimumSize = new Vector2(200, 50);
			btnExit.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
			btnExit.Pressed += _on_button_exit_pressed;
			container.AddChild(btnExit);
		}
	}

	private void _on_button_play_pressed()
	{
		Error result = GetTree().ChangeSceneToFile(GameScenePath);
		if (result != Error.Ok)
		{
			GD.PrintErr($"No se pudo cargar la escena: {GameScenePath}. Error: {result}");
		}
	}

	private void _on_button_exit_pressed()
	{
		GetTree().Quit();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel"))
		{
			GetTree().Quit();
		}
	}
}
