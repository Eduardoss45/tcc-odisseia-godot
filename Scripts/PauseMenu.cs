using Godot;
using System;

public partial class PauseMenu : CanvasLayer
{
	private Button _resumeBtn;
	private Button _quitBtn;

	public override void _Ready()
	{
		_resumeBtn = GetNodeOrNull<Button>("VBoxContainer/ResumeBtn");
		_quitBtn = GetNodeOrNull<Button>("VBoxContainer/QuitBtn");
		_resumeBtn.Pressed += OnResumePressed;
		_quitBtn.Pressed += OnQuitPressed;
		this.Visible = false;
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("ui_cancel"))
		{
			TogglePause();
		}
	}

	private void TogglePause()
	{
		GetTree().Paused = !GetTree().Paused;
		this.Visible = GetTree().Paused;
	}

	private void OnResumePressed()
	{
		GD.Print("Continua");
		GetTree().Paused = false;
		this.Visible = false;
	}

	private void OnQuitPressed()
	{
		GD.Print("Fechou");
		GetTree().Quit();
	}
}
