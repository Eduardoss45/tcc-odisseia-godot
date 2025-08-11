using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Player : CharacterBody2D
{
	[Export]
	public float Speed = 200.0f;
	private Sprite2D sprite;
	private Camera2D camera;
	private Queue<int> directionHistory = new Queue<int>();
	private const int MaxHistorySize = 8;
	private int animationFrame = 0;
	private float frameTimer = 0f;
	private float frameDuration = 0.1f;
	public override void _Ready()
	{
		Position = new Vector2(600, 350);
		sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
		camera = GetNodeOrNull<Camera2D>("Camera2D");
		if (sprite != null)
		{
			sprite.Hframes = 8;
			sprite.Vframes = 9;
		}
	}
	public override void _PhysicsProcess(double delta)
	{
		if (sprite == null)
			return;
		Vector2 direction = Vector2.Zero;
		if (Input.IsKeyPressed(Key.W)) direction.Y -= 1;
		if (Input.IsKeyPressed(Key.S)) direction.Y += 1;
		if (Input.IsKeyPressed(Key.A)) direction.X -= 1;
		if (Input.IsKeyPressed(Key.D)) direction.X += 1;
		if (direction != Vector2.Zero)
		{
			direction = direction.Normalized();
			Velocity = direction * Speed;
			MoveAndSlide();
			int dirIndex = GetDirectionIndex(direction);
			directionHistory.Enqueue(dirIndex);
			if (directionHistory.Count > MaxHistorySize)
				directionHistory.Dequeue();
			frameTimer += (float)delta;
			if (frameTimer >= frameDuration)
			{
				frameTimer = 0f;
				animationFrame = (animationFrame + 1) % sprite.Hframes;
			}
			sprite.FrameCoords = new Vector2I(animationFrame, dirIndex + 1);
		}
		else
		{
			Velocity = Vector2.Zero;
			frameTimer = 0f;
			animationFrame = 0;
			int idleDir = GetMostFrequentDirection();
			sprite.FrameCoords = new Vector2I(idleDir, 0);
		}
	}

	private int GetDirectionIndex(Vector2 dir)
	{
		dir = dir.Normalized();
		if (dir.X < 0 && dir.Y > 0) return 1;
		if (dir.X < 0 && dir.Y < 0) return 3;
		if (dir.X > 0 && dir.Y < 0) return 5;
		if (dir.X > 0 && dir.Y > 0) return 7;
		if (dir.Y > 0) return 0;
		if (dir.X < 0) return 2;
		if (dir.Y < 0) return 4;
		if (dir.X > 0) return 6;
		return 0;
	}

	private int GetMostFrequentDirection()
	{
		if (directionHistory.Count == 0)
			return 0;
		return directionHistory
			.GroupBy(x => x)
			.OrderByDescending(g => g.Count())
			.First().Key;
	}
}