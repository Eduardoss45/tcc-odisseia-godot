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
	[Export] public float AttackFaceLock = 0.2f;
	private float attackFaceTimer = 0f;
	private Vector2 attackDir = Vector2.Zero;
	private int attackLine = 0;

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

	[Export] public string AttackSpriteSheetPath { get; set; } = "";
	public int AttackHFrames { get; set; } = 5;
	public int AttackVFrames { get; set; } = 8;
	public int AttackWidth { get; set; } = 64;
	public int AttackHeight { get; set; } = 64;

	private int attackFrame = 0;
	private float attackFrameTimer = 0f;
	private float attackFrameDuration = 0.05f; // velocidade da animação
	private bool isAttacking = false;

	private int maxHp = 10;
	private int hp;
	private int maxEnergy = 10;
	private float energy;
	private Hud hud;
	private bool faceLocked = false;
	private Vector2 faceLockDir = Vector2.Zero;
	[Export] public float ShootCooldown = 0.75f; // intervalo de disparos
	private float shootCooldownLeft = 0f;
	public bool CanAttack { get; set; } = true;


	[Export] public float HpRegenRate = 0.5f;     // velocidade de regeneração 0.5 HP/s
	[Export] public float EnergyRegenRate = 2f;   // velocidade de regeneração 2 Energia/s
	private float hpRegenBuffer = 0f;


	public override void _Ready()
	{
		hp = maxHp;
		energy = maxEnergy;
		hud = GetTree().Root.GetNode<Hud>("World/Hud");
		hud.UpdateLife(hp);
		hud.UpdateEnergyByValue(energy, maxEnergy);
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
		ApplySprite();
		ApplyAttackSprite();
		Position = new Vector2(600, 350);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (sprite == null) return;

		if (sprite == null) return;

		if (hp > 0 && hp < maxHp)
		{
			hpRegenBuffer += HpRegenRate * (float)delta;
			if (hpRegenBuffer >= 1f)
			{
				int amount = (int)hpRegenBuffer;
				hp += amount;
				if (hp > maxHp) hp = maxHp;
				hpRegenBuffer -= amount;
				hud.UpdateLife(hp);
			}
		}

		if (energy < maxEnergy)
		{
			energy += EnergyRegenRate * (float)delta;
			if (energy > maxEnergy) energy = maxEnergy;
			hud.UpdateEnergyByValue(energy, maxEnergy);
		}


		if (isAiming)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			if (crosshair != null) crosshair.GlobalPosition = GetGlobalMousePosition();
			return;
		}

		if (isAttacking)
		{
			attackFrameTimer += (float)delta;
			if (attackFrameTimer >= attackFrameDuration)
			{
				attackFrameTimer = 0f;
				attackFrame++;

				if (attackFrame >= AttackHFrames)
				{
					isAttacking = false;

					ApplySprite();
					sprite.FrameCoords = new Vector2I(0, GetDirectionIndex(attackDir));
				}
				else
				{
					sprite.FrameCoords = new Vector2I(attackFrame, attackLine);
				}
			}
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
			if (energy >= 3f)
			{
				energy -= 3f;
				hud.UpdateEnergyByValue(energy, maxEnergy);

				isDashing = true;
				dashTimeLeft = DashDuration;
				dashCooldownLeft = DashCooldown;
			}
		}

		Vector2 direction = Vector2.Zero;
		if (Input.IsKeyPressed(Key.W)) direction.Y -= 1;
		if (Input.IsKeyPressed(Key.S)) direction.Y += 1;
		if (Input.IsKeyPressed(Key.A)) direction.X -= 1;
		if (Input.IsKeyPressed(Key.D)) direction.X += 1;

		UpdateCameraZoom(delta, direction);
		UpdateMovement(delta, direction);

		if (shootCooldownLeft > 0)
			shootCooldownLeft -= (float)delta;

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

		if (@event.IsActionPressed("attack") && !isAttacking)
		{
			if (energy >= 2f)
			{
				energy -= 2f;
				hud.UpdateEnergyByValue(energy, maxEnergy);
				StartAttack();
			}
		}


	}

	private void ShootArrow()
	{
		if (!CanAttack) return;
		if (ArrowScene == null || crosshair == null) return;
		if (shootCooldownLeft > 0) return;

		if (energy < 1f) return;
		energy -= 1f;
		hud.UpdateEnergyByValue(energy, maxEnergy);

		shootCooldownLeft = ShootCooldown;

		Vector2 dir = (crosshair.GlobalPosition - GlobalPosition).Normalized();
		lastAttackDir = dir;

		faceLocked = true;
		faceLockDir = dir;

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

			faceLocked = false;

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


		if (attackFaceTimer > 0f)
		{
			int attackIndex = GetDirectionIndex(attackDir);

			if (Velocity == Vector2.Zero)
				sprite.FrameCoords = new Vector2I(attackIndex, 0);
			else
				sprite.FrameCoords = new Vector2I(animationFrame, attackIndex + 1);
		}
	}

	private int GetDirectionIndex(Vector2 dir)
	{
		if (dir == Vector2.Zero) return 0;

		float deg = dir.Angle() * 180f / MathF.PI;
		if (deg < 0) deg += 360f;

		if (deg >= 337.5f || deg < 22.5f) return 6;   // → direita
		if (deg >= 22.5f && deg < 67.5f) return 7;    // ↗️ diagonal
		if (deg >= 67.5f && deg < 112.5f) return 0;   // ↑ cima
		if (deg >= 112.5f && deg < 157.5f) return 1;  // ↖️ diagonal
		if (deg >= 157.5f && deg < 202.5f) return 2;  // ← esquerda
		if (deg >= 202.5f && deg < 247.5f) return 3;  // ↙️ diagonal
		if (deg >= 247.5f && deg < 292.5f) return 4;  // ↓ baixo
		if (deg >= 292.5f && deg < 337.5f) return 5;  // ↘️ diagonal

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

		if (faceLocked && faceLockDir != Vector2.Zero)
			return GetDirectionIndex(faceLockDir);


		if (isAttacking && attackDir != Vector2.Zero)
			return GetDirectionIndex(attackDir);

		if (isAiming && lastAttackDir != Vector2.Zero)
			return GetDirectionIndex(lastAttackDir);

		return GetMostFrequentDirection();
	}



	private void FaceAttackDirection()
	{
		Vector2 dir = (GetGlobalMousePosition() - GlobalPosition).Normalized();
		if (dir == Vector2.Zero) return;

		lastAttackDir = dir;
		attackDir = dir;

		int dirIndex = GetDirectionIndex(dir);
		sprite.FrameCoords = new Vector2I(dirIndex, 0);
	}

	public void ApplyAttackSprite()
	{
		var spriteNode = GetNodeOrNull<Sprite2D>("AttackSprite2D");
		if (spriteNode != null && !string.IsNullOrEmpty(AttackSpriteSheetPath))
		{
			var tex = GD.Load<Texture2D>(AttackSpriteSheetPath);
			if (tex != null)
			{
				spriteNode.Texture = tex;
				spriteNode.Hframes = AttackHFrames;
				spriteNode.Vframes = AttackVFrames;
				spriteNode.Visible = false;
			}
		}
	}
	private void StartAttack()
	{

		if (!CanAttack) return;
		if (isAttacking) return;

		isAttacking = true;
		attackFrame = 0;
		attackFrameTimer = 0f;

		attackDir = (GetGlobalMousePosition() - GlobalPosition).Normalized();
		attackLine = GetDirectionIndex(attackDir);


		Velocity = Vector2.Zero;

		faceLocked = true;
		faceLockDir = attackDir;

		if (!string.IsNullOrEmpty(AttackSpriteSheetPath) && sprite != null)
		{
			var tex = GD.Load<Texture2D>(AttackSpriteSheetPath);
			if (tex != null)
			{
				sprite.Texture = tex;
				sprite.Hframes = AttackHFrames;
				sprite.Vframes = AttackVFrames;
			}
		}

		sprite.FrameCoords = new Vector2I(attackFrame, attackLine);
	}
	public void TakeDamage(int amount)
	{
		hp -= amount;
		if (hp < 0) hp = 0;

		GD.Print($"Player tomou {amount} de dano! Vida atual: {hp}");

		hud.UpdateLife(hp);

		if (hp <= 0)
		{
			Die();
		}
	}

	private void Die()
	{
		GD.Print("Player morreu!");
		//  Reiniciar a cena, mostrar game over etc.
	}

	private void OnHitPlayer(Player player)
	{
		player.TakeDamage(1);
	}
}
