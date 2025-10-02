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


	[Export] public int AttackWidth { get; set; } = 64;
	[Export] public int AttackHeight { get; set; } = 64;


	[Export] public string SpriteSheetPath { get; set; } = "";
	[Export] public int SpriteHFrames { get; set; } = 8;
	[Export] public int SpriteVFrames { get; set; } = 9;

	[Export] public string CrosshairPath { get; set; } = "";

	// Ataque corpo a corpo
	[Export] public string AttackSpriteSheetPath { get; set; } = "";
	[Export] public int AttackHFrames { get; set; } = 5;
	[Export] public int AttackVFrames { get; set; } = 8;
	private int attackFrame = 0;
	private float attackFrameTimer = 0f;
	private float attackFrameDuration = 0.05f;

	private int attackLine = 0;
	private Vector2 attackDir = Vector2.Zero;
	private bool isAttacking = false;

	// Arco
	[Export] public string BowSpriteSheetPath { get; set; } = "res://Sprites/shooting_animation_spritesheet.png";
	[Export] public int BowHFrames { get; set; } = 3;
	[Export] public int BowVFrames { get; set; } = 8;
	[Export] public float ShootCooldown = 0.25f;
	private int shootFrame = 0;
	private float shootFrameTimer = 0f;
	private float frameDuration = 0.1f; // tempo entre frames de movimento
	private float shootFrameDuration = 0.15f;
	private int shootLine = 0;
	private bool isShooting = false;

	public PackedScene ArrowScene { get; set; }
	public string ArrowSpriteSheetPath { get; set; }
	private float shootCooldownLeft = 0f;

	private Sprite2D sprite;
	private Sprite2D crosshair;
	private Camera2D camera;

	private Queue<int> directionHistory = new Queue<int>();
	private const int MaxHistorySize = 8;
	private int animationFrame = 0;
	private float frameTimer = 0f;

	private bool isAiming = false;
	private Vector2 lastAttackDir = Vector2.Zero;

	private bool isDashing = false;
	private float dashTimeLeft = 0f;
	private float dashCooldownLeft = 0f;

	private int maxHp = 10;
	private int hp;
	private int maxEnergy = 10;
	private float energy;
	private Hud hud;

	[Export] public float HpRegenRate = 0.5f;
	[Export] public float EnergyRegenRate = 2f;
	private float hpRegenBuffer = 0f;

	public bool CanAttack { get; set; } = true;
	public const uint PlayerLayer = 0;

	private bool faceLocked = false;
	private Vector2 faceLockDir = Vector2.Zero;
	private Hitbox hitbox;


	public override void _Ready()
	{
		hp = maxHp;
		energy = maxEnergy;
		hud = GetTree().Root.GetNode<Hud>("World/Hud");
		hud.UpdateLife(hp);
		hud.UpdateEnergyByValue(energy, maxEnergy);
		var hitboxScene = GD.Load<PackedScene>("res://Cenas/Hitbox.tscn");
		if (hitboxScene != null)
		{
			hitbox = hitboxScene.Instantiate<Hitbox>();
			AddChild(hitbox);
			hitbox.Position = Vector2.Zero; // centraliza no player
		}

		sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
		crosshair = GetNodeOrNull<Sprite2D>("Crosshair");
		camera = GetNodeOrNull<Camera2D>("Camera2D");

		if (sprite != null && !string.IsNullOrEmpty(SpriteSheetPath))
		{
			ApplySprite();
		}

		if (crosshair != null && !string.IsNullOrEmpty(CrosshairPath))
		{
			var crossTex = GD.Load<Texture2D>(CrosshairPath);
			if (crossTex != null) crosshair.Texture = crossTex;
			crosshair.Visible = false;
		}

		Position = new Vector2(615, 345);
		colisaoOriginal = CollisionMask;

	}

	public override void _PhysicsProcess(double delta)
	{
		if (sprite == null) return;

		RegenHpEnergy(delta);

		// Bloqueia movimentação durante ataque ou tiro
		if (isAttacking || isShooting)
		{
			UpdateAttackOrShootAnimation(delta);
			return;
		}

		if (isAiming)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();

			Vector2 dir = (crosshair.GlobalPosition - GlobalPosition).Normalized();
			lastAttackDir = dir;

			if (sprite != null)
			{
				if (!string.IsNullOrEmpty(BowSpriteSheetPath))
				{
					var tex = GD.Load<Texture2D>(BowSpriteSheetPath);
					if (tex != null && sprite.Texture.ResourcePath != BowSpriteSheetPath)
					{
						sprite.Texture = tex;
						sprite.Hframes = BowHFrames;
						sprite.Vframes = BowVFrames;
					}
				}
				shootLine = GetDirectionIndex(dir);

				sprite.FrameCoords = new Vector2I(0, shootLine);
			}

			if (crosshair != null)
				crosshair.GlobalPosition = GetGlobalMousePosition();

			return;
		}


		HandleDash(delta);
		Vector2 direction = GetInputDirection();
		UpdateCameraZoom(delta, direction);
		UpdateMovement(delta, direction);

		if (shootCooldownLeft > 0)
			shootCooldownLeft -= (float)delta;
	}

	public override void _Input(InputEvent @event)
	{
		// Mira
		if (@event.IsActionPressed("aim") && !isAttacking && !isShooting)
		{
			isAiming = true;
			if (crosshair != null) crosshair.Visible = true;
		}
		if (@event.IsActionReleased("aim"))
		{
			isAiming = false;
			if (crosshair != null) crosshair.Visible = false;

			ApplySprite();
			sprite.FrameCoords = new Vector2I(0, GetDirectionIndex(lastAttackDir));
		}

		// Tiro
		if (@event.IsActionPressed("shoot") && !isShooting && isAiming && !isAttacking)
		{
			if (energy >= 1f)
			{
				StartShoot();
			}
		}

		// Ataque corpo a corpo
		if (@event.IsActionPressed("attack") && !isAttacking && !isAiming && !isShooting)
		{
			if (energy >= 2f)
			{
				energy -= 2f;
				hud.UpdateEnergyByValue(energy, maxEnergy);
				StartAttack();
			}
		}
	}

	#region Movimentação e Dash

	private Vector2 GetInputDirection()
	{
		Vector2 dir = Vector2.Zero;
		if (Input.IsKeyPressed(Key.W)) dir.Y -= 1;
		if (Input.IsKeyPressed(Key.S)) dir.Y += 1;
		if (Input.IsKeyPressed(Key.A)) dir.X -= 1;
		if (Input.IsKeyPressed(Key.D)) dir.X += 1;
		return dir.Normalized();
	}

	private void HandleDash(double delta)
	{
		if (dashTimeLeft > 0)
		{
			dashTimeLeft -= (float)delta;
			if (dashTimeLeft <= 0) isDashing = false;
		}

		if (dashCooldownLeft > 0)
			dashCooldownLeft -= (float)delta;

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
	}

	private void UpdateCameraZoom(double delta, Vector2 direction)
	{
		if (camera == null) return;

		bool isMoving = direction != Vector2.Zero;
		Vector2 targetZoom = isDashing && isMoving ? new Vector2(1.9f, 1.9f) :
							   isMoving ? new Vector2(1.7f, 1.7f) :
							   new Vector2(1.4f, 1.4f);

		camera.Zoom = camera.Zoom.Lerp(targetZoom, 5f * (float)delta);
	}

	private void UpdateMovement(double delta, Vector2 direction)
	{
		if (direction != Vector2.Zero)
		{
			faceLocked = false;
			Velocity = direction * (BaseSpeed + (isDashing ? DashBonus : 0));
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
			sprite.FrameCoords = new Vector2I(GetIdleDirection(), 0);
		}
	}

	#endregion

	#region Ataque e Tiro

	private void StartAttack()
	{
		isAttacking = true;
		attackFrame = 0;
		attackFrameTimer = 0f;
		attackDir = (GetGlobalMousePosition() - GlobalPosition).Normalized();
		attackLine = GetDirectionIndex(attackDir);

		Velocity = Vector2.Zero;
		faceLocked = true;
		faceLockDir = attackDir;

		if (!string.IsNullOrEmpty(AttackSpriteSheetPath))
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

		if (attackFrame == 2) // frame do ataque onde acerta
		{
			if (hitbox != null)
			{
				hitbox.TriggerAttack();
			}
		}
	}

	private void StartShoot()
	{
		isShooting = true;
		shootFrame = 0;
		shootFrameTimer = 0f;
		shootLine = GetDirectionIndex((crosshair.GlobalPosition - GlobalPosition).Normalized());

		if (!string.IsNullOrEmpty(BowSpriteSheetPath))
		{
			var tex = GD.Load<Texture2D>(BowSpriteSheetPath);
			if (tex != null)
			{
				sprite.Texture = tex;
				sprite.Hframes = BowHFrames;
				sprite.Vframes = BowVFrames;
			}
		}

		sprite.FrameCoords = new Vector2I(shootFrame, shootLine);
	}

	private void UpdateAttackOrShootAnimation(double delta)
	{
		if (isAttacking)
		{
			attackFrameTimer += (float)delta;
			if (attackFrameTimer >= attackFrameDuration)
			{
				attackFrameTimer = 0f;
				attackFrame++;

				if (attackFrame == 2 && hitbox != null)
					hitbox.TriggerAttack();

				if (attackFrame >= AttackHFrames)
				{
					isAttacking = false;
					ApplySprite();

					lastAttackDir = attackDir;

					sprite.FrameCoords = new Vector2I(0, GetDirectionIndex(lastAttackDir));
					faceLocked = false;
				}
				else
				{
					sprite.FrameCoords = new Vector2I(attackFrame, attackLine);
					faceLocked = true;
				}
			}
		}
		else if (isShooting)
		{
			shootFrameTimer += (float)delta;
			if (shootFrameTimer >= shootFrameDuration)
			{
				shootFrameTimer = 0f;
				shootFrame++;
				if (shootFrame >= BowHFrames)
				{
					isShooting = false;
					shootFrame = 0;
					ApplySprite();
					sprite.FrameCoords = new Vector2I(0, GetDirectionIndex(lastAttackDir));
				}
				else
				{
					sprite.FrameCoords = new Vector2I(shootFrame, shootLine);
					if (shootFrame == 2) ShootArrow();
				}
			}
		}
	}


	private void ShootArrow()
	{
		if (!CanAttack || ArrowScene == null || crosshair == null || shootCooldownLeft > 0) return;
		if (energy < 1f) return;

		energy -= 1f;
		hud.UpdateEnergyByValue(energy, maxEnergy);
		shootCooldownLeft = ShootCooldown;

		Vector2 dir = (crosshair.GlobalPosition - GlobalPosition).Normalized();
		lastAttackDir = dir;

		var arrow = ArrowScene.Instantiate<Arrow>();
		GetParent().AddChild(arrow);
		arrow.GlobalPosition = GlobalPosition + dir * 20;
		arrow.velocity = dir * 400;

		if (!string.IsNullOrEmpty(ArrowSpriteSheetPath))
		{
			arrow.SpritePath = ArrowSpriteSheetPath;
		}

		arrow.UpdateRotation();
		arrow.CollisionMask &= ~(1u << (int)PlayerLayer);
	}

	#endregion

	#region Helper Methods

	private void RegenHpEnergy(double delta)
	{
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
	}

	private int GetDirectionIndex(Vector2 dir)
	{
		if (dir == Vector2.Zero) return 0;
		float deg = dir.Angle() * 180f / MathF.PI;
		if (deg < 0) deg += 360f;

		if (deg >= 337.5f || deg < 22.5f) return 6;
		if (deg >= 22.5f && deg < 67.5f) return 7;
		if (deg >= 67.5f && deg < 112.5f) return 0;
		if (deg >= 112.5f && deg < 157.5f) return 1;
		if (deg >= 157.5f && deg < 202.5f) return 2;
		if (deg >= 202.5f && deg < 247.5f) return 3;
		if (deg >= 247.5f && deg < 292.5f) return 4;
		if (deg >= 292.5f && deg < 337.5f) return 5;

		return 0;
	}

	private int GetIdleDirection()
	{
		if (!isAttacking && lastAttackDir != Vector2.Zero)
			return GetDirectionIndex(lastAttackDir);

		if (faceLocked && lastAttackDir != Vector2.Zero)
			return GetDirectionIndex(lastAttackDir);

		return directionHistory.Count > 0 ? directionHistory.GroupBy(x => x)
								   .OrderByDescending(g => g.Count())
								   .First().Key : 0;
	}

	public void ApplySprite()
	{
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
	}

	public void TakeDamage(int amount)
	{
		hp -= amount;
		if (hp < 0) hp = 0;
		hud.UpdateLife(hp);

		if (hp <= 0)
			Die();
	}

	private void Die()
	{
		ResetScene();
	}

	public void ResetScene()
	{
		var currentScene = GetTree().CurrentScene;
		if (currentScene != null)
		{
			string scenePath = currentScene.SceneFilePath;
			GetTree().ChangeSceneToFile(scenePath);
		}
		else
		{
			GD.PrintErr("Nenhuma cena atual encontrada!");
		}
	}

	private uint colisaoOriginal;

	/// <summary>
	/// Desativa todas as colisões do player temporariamente.
	/// </summary>
	public void DesativarColisao()
	{
		CollisionMask = 0;
	}

	/// <summary>
	/// Restaura a máscara de colisão original do player.
	/// </summary>
	public void AtivarColisao()
	{
		CollisionMask = colisaoOriginal;
	}



	#endregion
}
