using Godot;
using System;

public partial class TableTop : Node3D
{
	[Export] public MainCamera Camera;
	[Export] public Node3D BoardCameraPivot;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// if (Camera != null && BoardCameraPivot != null)
        // {
        //     Camera.SetTarget(BoardCameraPivot);
        // }
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	// public override void _Process(double delta)
	// {
	// }
}
