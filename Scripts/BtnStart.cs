using Godot;
using System;

public partial class BtnStart : TextureRect
{
    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            GD.Print("Clicou no botão de iniciar");
            
            // Troca para a cena "game.tscn"
            GetTree().ChangeSceneToFile("res://Cenas/carregamento.tscn");
        }
    }
}
