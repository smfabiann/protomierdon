using Godot;
using System;

public partial class MainCamera : Camera3D
{
	[Export] public Node3D Target;

	public override void _Ready()
	{
	}

	public override void _Process(double delta)
	{
		if (Target == null) return;
		GlobalTransform = Target.GlobalTransform;
	}

	public void SmoothCameraTransition(float smoothSpeed, double delta)
	{
		if (Target == null) return;
		GlobalTransform = GlobalTransform.InterpolateWith(
			Target.GlobalTransform,
			smoothSpeed * (float)delta
		);
	}
}
