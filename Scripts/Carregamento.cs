using Godot;
using System;

public partial class Carregamento : MarginContainer
{
	public override void _Ready()
	{
		// Acessa o singleton global Dialogic
		var dialogic = GetNode("/root/Dialogic");

		// Conecta ao sinal de término da timeline (sem parâmetros)
		dialogic.Connect("timeline_ended", new Callable(this, nameof(OnTimelineEnded)));

		// Inicia a timeline chamada "timeline"
		dialogic.Call("start", "carregamento");
	}

	private void OnTimelineEnded()
	{
		GD.Print("Timeline terminou!");

		// Troca para a cena do jogo
		GetTree().ChangeSceneToFile("res://Cenas/Introducao.tscn");
	}
}
