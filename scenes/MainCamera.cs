using Godot;
using System;

public partial class MainCamera : Camera3D
{
	[Export] public Node3D Target;
	// [Export] public float SmoothSpeed = 50.0f;	// cambiar este wea despues para que no sea por defecto

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		// no puede no tener un target
		if (Target == null) return;

		// se usara un punto de vista instantaneo
		GlobalTransform = Target.GlobalTransform;
		// es una transicion de un punto a otro
		// GlobalTransform = GlobalTransform.InterpolateWith(
		// 	Target.GlobalTransform,
		// 	SmoothSpeed * (float)delta
		// );
	}

	public void SmoothCameraTransition(float SmoothSpeed, double delta)
	{
		GlobalTransform = GlobalTransform.InterpolateWith(
			Target.GlobalTransform,
			SmoothSpeed * (float) delta
		);
		return;
	}

	// Sera usado despues
	// public void SetTarget(Node3D newTarget)
	// {
	// 	Target = newTarget;
	// }
}
