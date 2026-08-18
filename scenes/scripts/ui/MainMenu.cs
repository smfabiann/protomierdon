using Godot;
using System;

public partial class MainMenu : Control
{
	[Export] public string GameScenePath { get; set; } = "res://scenes/level.tscn";

	private void _on_button_play_pressed()
	{
		 GD.Print("Iniciando el juego...");
        
        // Obtenemos el árbol de escenas (SceneTree) y cambiamos a la escena del nivel
        Error result = GetTree().ChangeSceneToFile(GameScenePath);
        
        // Es buena práctica verificar si el cambio de escena fue exitoso
        if (result != Error.Ok)
        {
            GD.PrintErr($"No se pudo cargar la escena: {GameScenePath}. Error: {result}");
        }
	}
}
