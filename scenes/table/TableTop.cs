using Godot;
using System;

public partial class TableTop : Node3D
{
	[Export] public MainCamera Camera;
	[Export] public Node3D BoardCameraPivot;

	public override void _Ready()
	{
	}
}
