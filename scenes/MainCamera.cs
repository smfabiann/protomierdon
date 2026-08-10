using Godot;
using System;

public partial class MainCamera : Camera3D
{
	[Export] public Node3D Target;
	[Export] public float SmoothSpeed = 50.0f;	// cambiar este wea despues para que no sea por defecto
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Target == null) return;

		GlobalTransform = GlobalTransform.InterpolateWith(
			Target.GlobalTransform,
			SmoothSpeed * (float)delta
		);
		
	}

	public void SetTarget(Node3D newTarget)
	{
		Target = newTarget;
	}
}
