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

	[Export] public string SpriteSheetPath { get; set; } = "";
	[Export] public int SpriteHFrames { get; set; } = 8;
	[Export] public int SpriteVFrames { get; set; } = 9;
	[Export] public string CrosshairPath { get; set; } = "";
	public PackedScene ArrowScene { get; set; }
	public string ArrowSpriteSheetPath { get; set; }
	public int ArrowHFrames { get; set; } = 1;
	public int ArrowVFrames { get; set; } = 1;
	public int ArrowWidth { get; set; } = 64;
	public int ArrowHeight { get; set; } = 64;

	private Sprite2D sprite;
	private Camera2D camera;
	private Sprite2D crosshair;

	private Vector2 lastAttackDir = Vector2.Zero;
	private bool isAiming = false;
	private Queue<int> directionHistory = new Queue<int>();
	private const int MaxHistorySize = 8;
	private int animationFrame = 0;
	private float frameTimer = 0f;
	private float frameDuration = 0.1f;

	private bool isDashing = false;
	private float dashTimeLeft = 0f;
	private float dashCooldownLeft = 0f;
	public const uint PlayerLayer = 0;

	public override void _Ready()
	{
		sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
		camera = GetNodeOrNull<Camera2D>("Camera2D");
		crosshair = GetNodeOrNull<Sprite2D>("Crosshair");

		var hitbox = GD.Load<PackedScene>("res://Cenas/Hitbox.tscn");
		var playerHitbox = hitbox.Instantiate<Node2D>();
		AddChild(playerHitbox);
		playerHitbox.Position = Vector2.Zero;

		if (sprite != null && !string.IsNullOrEmpty(SpriteSheetPath))
		{
			var tex = GD.Load<Texture2D>(SpriteSheetPath);
			if (tex != null)
			{
				sprite.Texture = tex;
				sprite.Hframes = SpriteHFrames;
				sprite.Vframes = SpriteVFrames;
			}
		}

		if (crosshair != null && !string.IsNullOrEmpty(CrosshairPath))
		{
			var crossTex = GD.Load<Texture2D>(CrosshairPath);
			if (crossTex != null) crosshair.Texture = crossTex;
			crosshair.Visible = false;
		}

		Position = new Vector2(600, 350);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (sprite == null) return;

		if (isAiming)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			if (crosshair != null) crosshair.GlobalPosition = GetGlobalMousePosition();
			return;
		}

		if (dashTimeLeft > 0)
		{
			dashTimeLeft -= (float)delta;
			if (dashTimeLeft <= 0) isDashing = false;
		}
		if (dashCooldownLeft > 0) dashCooldownLeft -= (float)delta;

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

		UpdateCameraZoom(delta, direction);
		UpdateMovement(delta, direction);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("aim"))
		{
			isAiming = true;
			if (crosshair != null) crosshair.Visible = true;
		}
		if (@event.IsActionReleased("aim"))
		{
			isAiming = false;
			if (crosshair != null) crosshair.Visible = false;
		}

		if (isAiming && @event.IsActionPressed("shoot"))
			ShootArrow();
	}

	private void ShootArrow()
	{
		if (ArrowScene == null || crosshair == null) return;

		Vector2 dir = (crosshair.GlobalPosition - GlobalPosition).Normalized();
		lastAttackDir = dir;

		var arrow = ArrowScene.Instantiate<Arrow>();
		GetParent().AddChild(arrow);

		arrow.GlobalPosition = GlobalPosition + dir * 20;
		arrow.velocity = dir * 400;

		if (!string.IsNullOrEmpty(ArrowSpriteSheetPath))
		{
			arrow.SpritePath = ArrowSpriteSheetPath;
			arrow.ApplySprite();
		}

		arrow.UpdateRotation();
		arrow.CollisionMask &= ~(1u << (int)PlayerLayer);

		int dirIndex = GetDirectionIndex(lastAttackDir);
		sprite.FrameCoords = new Vector2I(dirIndex, 0);
	}

	private void UpdateCameraZoom(double delta, Vector2 direction)
	{
		if (camera == null) return;

		bool isMoving = direction != Vector2.Zero;
		Vector2 targetZoom = isDashing && isMoving ? new Vector2(1.6f, 1.6f) :
							   isMoving ? new Vector2(1.5f, 1.5f) :
							   new Vector2(1f, 1f);

		camera.Zoom = camera.Zoom.Lerp(targetZoom, 5f * (float)delta);
	}

	private void UpdateMovement(double delta, Vector2 direction)
	{
		if (direction != Vector2.Zero)
		{
			direction = direction.Normalized();
			float currentSpeed = BaseSpeed + (isDashing ? DashBonus : 0);
			Velocity = direction * currentSpeed;
			MoveAndSlide();

			int dirIndex = GetDirectionIndex(direction);
			directionHistory.Enqueue(dirIndex);
			if (directionHistory.Count > MaxHistorySize) directionHistory.Dequeue();

			lastAttackDir = direction;

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

			int idleDir = GetIdleDirection();
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
		if (directionHistory.Count == 0) return 0;
		return directionHistory.GroupBy(x => x)
							   .OrderByDescending(g => g.Count())
							   .First().Key;
	}

	public void ApplySprite()
	{
		var spriteNode = GetNodeOrNull<Sprite2D>("Sprite2D");
		if (spriteNode != null && !string.IsNullOrEmpty(SpriteSheetPath))
		{
			var tex = GD.Load<Texture2D>(SpriteSheetPath);
			if (tex != null)
			{
				spriteNode.Texture = tex;
				spriteNode.Hframes = SpriteHFrames;
				spriteNode.Vframes = SpriteVFrames;
			}
		}
	}

	private int GetIdleDirection()
	{
		if (isAiming && lastAttackDir != Vector2.Zero)
			return GetDirectionIndex(lastAttackDir);
		return GetMostFrequentDirection();
	}
}
