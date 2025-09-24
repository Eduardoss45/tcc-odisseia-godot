using Godot;
using System;

public partial class BtnQuit : TextureRect
{
    public override void _Ready()
    {
        MouseFilter = Control.MouseFilterEnum.Stop;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && 
            mouseEvent.Pressed && 
            mouseEvent.ButtonIndex == MouseButton.Left)
        {
            GD.Print("Clicou no botão de sair");
            GetTree().Quit(); // 🔽 Fecha o jogo
        }
    }
}