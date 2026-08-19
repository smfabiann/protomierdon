using Godot;
using System;

public partial class TableGameTrigger : Area3D
{
	[Export] public DeckManager DeckManagerNode;
	[Export] public Camera3D ReferenceCamera;
	[Export] public Camera3D MainCamera;
	[Export] public Player PlayerNode;

	private Label3D _promptLabel;
	private Vector3 _initialLabelPos;
	private double _timeCounter = 0.0;
	private bool _isPlayingCards = false;
	private bool _isPlayerInside = false;

	public override void _Ready()
	{
		if (DeckManagerNode == null)
		{
			GD.PrintErr("[TableGameTrigger] Falta asignar DeckManagerNode en el Inspector");
		}

		_promptLabel = GetNodeOrNull<Label3D>("Label3D");
		if (_promptLabel != null)
		{
			_promptLabel.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
			_promptLabel.FontSize = 42;
			_promptLabel.OutlineSize = 12;
			_promptLabel.OutlineModulate = Colors.Black;
			_promptLabel.Modulate = new Color(1f, 0.88f, 0.3f);
			_promptLabel.Visible = false;
			_initialLabelPos = _promptLabel.Position;
		}
	}

	public override void _Process(double delta)
	{
		if (_promptLabel != null && _promptLabel.Visible)
		{
			_timeCounter += delta * 4.0;
			float bobOffset = (float)Math.Sin(_timeCounter) * 0.08f;
			_promptLabel.Position = _initialLabelPos + new Vector3(0, bobOffset, 0);
		}
	}

	private void _on_body_entered(Node3D body)
	{
		if (body is Player)
		{
			_isPlayerInside = true;
			if (!_isPlayingCards && _promptLabel != null)
			{
				_promptLabel.Visible = true;
			}
		}
	}

	private void _on_body_exited(Node3D body)
	{
		if (body is Player)
		{
			_isPlayerInside = false;
			if (_promptLabel != null)
			{
				_promptLabel.Visible = false;
			}
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		bool isPressedE = (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.E);
		bool isAccept = @event.IsActionPressed("ui_accept");

		if (_isPlayerInside && !_isPlayingCards && (isPressedE || isAccept))
		{
			GetViewport().SetInputAsHandled();
			EnterTableMode();
		}
		else if (_isPlayingCards && @event.IsActionPressed("ui_cancel"))
		{
			GetViewport().SetInputAsHandled();
			ExitTableMode();
		}
	}

	private void EnterTableMode()
	{
		_isPlayingCards = true;

		if (_promptLabel != null)
		{
			_promptLabel.Visible = false;
		}

		ReferenceCamera?.MakeCurrent();
		Input.MouseMode = Input.MouseModeEnum.Visible;
		PlayerNode?.SetProcessUnhandledInput(false);
		PlayerNode?.SetPhysicsProcess(false);

		if (DeckManagerNode != null)
		{
			DeckManagerNode.SetUIVisible(true);
			DeckManagerNode.StartGame();
		}
	}

	private void ExitTableMode()
	{
		_isPlayingCards = false;

		MainCamera?.MakeCurrent();
		Input.MouseMode = Input.MouseModeEnum.Captured;
		PlayerNode?.SetProcessUnhandledInput(true);
		PlayerNode?.SetPhysicsProcess(true);

		if (DeckManagerNode != null)
		{
			DeckManagerNode.SetUIVisible(false);
			DeckManagerNode.ClearTable();
		}

		if (_isPlayerInside && _promptLabel != null)
		{
			_promptLabel.Visible = true;
		}
	}
}
