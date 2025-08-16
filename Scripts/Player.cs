using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Player : CharacterBody2D
{
	[Export] public float BaseSpeed = 200.0f;
	[Export] public float DashBonus = 200.0f;
	[Export] public float DashDuration = 0.3f;
	[Export] public float DashCooldown = 2.0f;

	private Sprite2D sprite;
	private Camera2D camera;
	private Queue<int> directionHistory = new Queue<int>();
	private const int MaxHistorySize = 8;
	private int animationFrame = 0;
	private float frameTimer = 0f;
	private float frameDuration = 0.1f;

	private bool isDashing = false;
	private float dashTimeLeft = 0f;
	private float dashCooldownLeft = 0f;

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


		if (dashTimeLeft > 0)
		{
			dashTimeLeft -= (float)delta;
			if (dashTimeLeft <= 0)
				isDashing = false;
		}
		if (dashCooldownLeft > 0)
			dashCooldownLeft -= (float)delta;


		if (Input.IsKeyPressed(Key.Q) && dashCooldownLeft <= 0 && !isDashing)
		{
			isDashing = true;
			dashTimeLeft = DashDuration;
			dashCooldownLeft = DashCooldown;
		}

		Vector2 direction = Vector2.Zero;
		if (Input.IsKeyPressed(Key.W)) direction.Y -= 1;
		if (Input.IsKeyPressed(Key.S)) direction.Y += 1;
		if (Input.IsKeyPressed(Key.A)) direction.X -= 1;
		if (Input.IsKeyPressed(Key.D)) direction.X += 1;

		if (camera != null)
		{
			bool isMoving = direction != Vector2.Zero;
			Vector2 targetZoom;
			if (isDashing && isMoving)
				targetZoom = new Vector2(1.6f, 1.6f);
			else if (isMoving)
				targetZoom = new Vector2(1.5f, 1.5f);
			else
				targetZoom = new Vector2(1f, 1f);
			camera.Zoom = camera.Zoom.Lerp(targetZoom, 5f * (float)delta);
		}

		if (direction != Vector2.Zero)
		{
			direction = direction.Normalized();
			float currentSpeed = BaseSpeed;
			if (isDashing)
				currentSpeed += DashBonus;
			Velocity = direction * currentSpeed;
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
