using Godot;
using System;

public partial class Mob : CharacterBody2D
{
    private Sprite2D sprite;
    private Node2D player;

    [Export] public float Speed { get; set; } = 160f;
    [Export] public float DetectionRange { get; set; } = 250f;
    [Export] public float StopRange { get; set; } = 64f;
    [Export] public int SpriteSheetRows { get; set; } = 2;
    [Export] public int SpriteSheetCols { get; set; } = 4;
    [Export] public int SpriteSheetWidth { get; set; } = 1600;
    [Export] public int SpriteSheetHeight { get; set; } = 600;
    public void SetAIActive(bool active) => AiEnabled = active;

    [Export] public bool AiEnabled { get; set; } = true;

    private Vector2 velocity = Vector2.Zero;

    // Dash
    private DashController dashController;

    // Spritesheets
    [Export] public string SpriteSheetPath { get; set; } = "res://Sprites/mob_spritesheet.png"; // corrida (4x2, 1600x600)
    [Export] public string IdleSpriteSheetPath { get; set; } = "res://Sprites/javali_parado_spritesheet.png"; // idle (2x2, 320x300 cada)
    [Export] public string DeathSpriteSheetPath { get; set; } = "res://Sprites/javali_abatido_spritesheet.png";

    // Corrida: frames/linha/cols calculados
    private int moveFW; // frame width calculado = 400
    private int moveFH; // frame height calculado = 300
    private int frameX = 0;
    private float frameTimer = 0f;
    private int row = 0; // 0 = direita, 1 = esquerda (ajuste se sua planilha for o inverso)
    private const int Cols = 4; // usado pro loop de frames
    private const int Rows = 2;
    [Export] public float AnimationSpeed { get; set; } = 8f; // fps da corrida
    private int lastRow = 0;

    // Ataque
    [Export] public string AttackSpriteSheetPath { get; set; } = "res://Sprites/ataqueJavali.png";
    private Texture2D attackTex;
    private int attackFW = 400;
    private int attackFH = 300;
    private int attackFrameX = 0;
    private float attackFrameTimer = 0f;
    [Export] public float AttackAnimationSpeed { get; set; } = 12f; // fps da animação de ataque
    private bool isAttacking = false;
    private int attackCols = 11; // 11 quadros por direção
    private int attackRow = 0;   // 0 = direita, 1 = esquerda


    // Idle
    [Export] public int IdleFrameWidth { get; set; } = 320;
    [Export] public int IdleFrameHeight { get; set; } = 300;
    [Export] public float IdleAnimationSpeed { get; set; } = 1.0f; // fps do idle (lento)
    private int idleFrameX = 0;
    private float idleFrameTimer = 0f;
    private int idleCols = 2;
    private int idleRows = 2;

    // Stats
    [Export] public int MaxHealth { get; set; } = 100;
    private int currentHealth;

    private bool canAttack = true;
    private Timer attackCooldown;
    private float attackCooldownTime = 1f;
    private float attackTimer = 0f;

    // Morte
    private bool isDying = false;
    [Export] public int DeathFrameWidth { get; set; } = 400;
    [Export] public int DeathFrameHeight { get; set; } = 300;

    // Texturas em cache
    private Texture2D moveTex;
    private Texture2D idleTex;
    private Texture2D deathTex;

    // Controle de direção
    private int moveRow = 0;   // corrida (0 = direita, 1 = esquerda)
    private int idleRow = 0;   // idle (0 = direita, 1 = esquerda)
    private int lastDirection = 0; // guarda última direção vista (0 = direita, 1 = esquerda)

    // Estado de animação
    private bool wasMoving = false;
    private const float MoveDeadzone = 0.1f;




    public override void _Ready()
    {
        sprite = GetNode<Sprite2D>("Sprite2D");

        // Carrega texturas 1x
        moveTex = GD.Load<Texture2D>(SpriteSheetPath);
        idleTex = GD.Load<Texture2D>(IdleSpriteSheetPath);
        deathTex = GD.Load<Texture2D>(DeathSpriteSheetPath);

        if (moveTex == null) { GD.PrintErr($"Falha ao carregar: {SpriteSheetPath}"); return; }
        if (idleTex == null) { GD.PrintErr($"Falha ao carregar: {IdleSpriteSheetPath}"); return; }

        // Calcula tamanho correto de cada frame da corrida (com base nos exports)
        moveFW = SpriteSheetWidth / SpriteSheetCols; // 1600 / 4 = 400
        moveFH = SpriteSheetHeight / SpriteSheetRows; // 600  / 2 = 300

        // Config inicial
        sprite.Texture = idleTex;
        sprite.RegionEnabled = true;
        sprite.RegionRect = new Rect2(0, 0, IdleFrameWidth, IdleFrameHeight);
        sprite.Scale = new Vector2(0.3f, 0.3f);

        //ataque
        attackTex = GD.Load<Texture2D>(AttackSpriteSheetPath);
        if (attackTex == null) { GD.PrintErr($"Falha ao carregar: {AttackSpriteSheetPath}"); }


        // Dash
        dashController = new DashController
        {
            DashSpeed = 300f,
            DashChargeTime = 0.5f,
            DashCooldown = 2f,
            StopDistance = 24f,
            DashDuration = 0.8f
        };

        // Hitbox
        var hitbox = GetNode<Area2D>("Hitbox");
        hitbox.BodyEntered += OnHitPlayer;

        // Timer de cooldown de ataque
        attackCooldown = new Timer
        {
            WaitTime = 1f,
            OneShot = true,
            Autostart = false
        };
        AddChild(attackCooldown);
        attackCooldown.Timeout += () => { canAttack = true; };

        currentHealth = MaxHealth;

        // Começa virado para a direita (linha 0). Ajuste se sua sheet for o contrário.
        row = 0;
        lastRow = 0;

        CollisionMask &= ~(uint)1;

        base._Ready();
    }

    private void UpdateAttackAnimation(double delta)
    {
        attackRow = lastDirection; // segue a última direção vista

        attackFrameTimer += (float)delta;
        if (attackFrameTimer >= 1.0f / AttackAnimationSpeed)
        {
            attackFrameTimer -= 1.0f / AttackAnimationSpeed;
            attackFrameX++;

            if (attackFrameX >= attackCols)
            {
                // fim da animação
                attackFrameX = 0;
                isAttacking = false;
                ResetIdleAnimation(); // volta pro idle
                return;
            }
        }

        sprite.Texture = attackTex;
        sprite.RegionEnabled = true;
        sprite.RegionRect = new Rect2(
            attackFrameX * attackFW,
            attackRow * attackFH,
            attackFW,
            attackFH
        );
        sprite.Scale = new Vector2(0.3f, 0.3f);
    }


    private void OnHitPlayer(Node body)
    {
        if (body is Player player && canAttack)
        {
            player.TakeDamage(1);
            GD.Print("Dano aplicado ao Player!");

            canAttack = false;
            attackCooldown.Start();

            // dispara animação de ataque
            isAttacking = true;
            attackFrameX = 0;
            attackFrameTimer = 0f;
        }
    }


    public override void _PhysicsProcess(double delta)
    {
        if (isDying) return;

        if (isAttacking)
        {
            UpdateAttackAnimation(delta);
            return; // prioridade absoluta
        }

        if (player == null)
            player = GetTree().Root.GetNodeOrNull<Node2D>("/root/World/Player");

        if (!AiEnabled || player == null)
        {
            velocity = Vector2.Zero;
            Velocity = velocity;
            MoveAndSlide();

            // transição para idle se for o caso
            bool isMovingNow = false;
            if (isMovingNow != wasMoving)
            {
                if (isMovingNow) ResetMovementAnimation();
                else ResetIdleAnimation();
                wasMoving = isMovingNow;
            }

            UpdateIdleAnimation(delta);
            return;
        }

        attackTimer -= (float)delta;

        var hitbox = GetNode<Area2D>("Hitbox");
        foreach (var body in hitbox.GetOverlappingBodies())
        {
            if (body is Player player && attackTimer <= 0f)
            {
                player.TakeDamage(1);
                GD.Print("Dano aplicado ao Player!");
                attackTimer = attackCooldownTime;
            }
        }

        float distance = Position.DistanceTo(player.Position);

        if (distance <= DetectionRange && distance > StopRange)
            velocity = (player.Position - Position).Normalized() * Speed;
        else
            velocity = Vector2.Zero;

        dashController.Update((float)delta, Position, player.Position);
        if (dashController.IsDashing)
            velocity = dashController.Velocity;

        Velocity = velocity;
        MoveAndSlide();

        // Atualiza a última direção SOMENTE quando há movimento real
        if (velocity.X > MoveDeadzone)
            lastDirection = 1; // direita
        else if (velocity.X < -MoveDeadzone)
            lastDirection = 0; // esquerda

        // Detecta transição parado <-> andando e reseta os contadores corretos
        bool isMoving = velocity.Length() > MoveDeadzone || dashController.IsDashing;
        if (isMoving != wasMoving)
        {
            if (isMoving) ResetMovementAnimation();
            else ResetIdleAnimation();
            wasMoving = isMoving;
        }

        // Chama apenas a animação do estado atual
        if (isMoving)
            UpdateMovementAnimation(delta);
        else
            UpdateIdleAnimation(delta);

    }

    private void UpdateMovementAnimation(double delta)
    {
        // Sem decidir direção aqui — só usa a já decidida
        moveRow = lastDirection;

        frameTimer += (float)delta;
        if (frameTimer >= 1.0f / AnimationSpeed)
        {
            frameTimer -= 1.0f / AnimationSpeed;
            frameX = (frameX + 1) % Cols;
        }

        sprite.Texture = moveTex;
        sprite.RegionEnabled = true;
        sprite.RegionRect = new Rect2(
            frameX * moveFW,
            moveRow * moveFH,
            moveFW,
            moveFH
        );
        sprite.Scale = new Vector2(1.1f, 1.1f);
    }


    private void UpdateIdleAnimation(double delta)
    {
        if (currentHealth <= 0 || isDying) return;

        idleRow = lastDirection; // sempre pega a última direção válida

        idleFrameTimer += (float)delta;
        if (idleFrameTimer >= 1.0f / IdleAnimationSpeed)
        {
            idleFrameTimer -= 1.0f / IdleAnimationSpeed;
            idleFrameX = (idleFrameX + 1) % idleCols;
        }

        sprite.Texture = idleTex;
        sprite.RegionEnabled = true;
        sprite.RegionRect = new Rect2(
            idleFrameX * IdleFrameWidth,
            idleRow * IdleFrameHeight,
            IdleFrameWidth,
            IdleFrameHeight
        );
        sprite.Scale = new Vector2(0.3f, 0.3f);
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        GD.Print($"Mob levou {amount} de dano! HP: {currentHealth}");
        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        if (isDying) return;
        isDying = true;

        if (deathTex == null)
            deathTex = GD.Load<Texture2D>(DeathSpriteSheetPath);

        sprite.Texture = deathTex;
        sprite.RegionEnabled = true;

        // Coluna = direção
        int deathCol = (lastDirection == 0) ? 1 : 0; // lastDirection 0=direita,1=esquerda
        int deathRow = 0; // só tem uma linha

        sprite.RegionRect = new Rect2(
            deathCol * DeathFrameWidth,
            deathRow * DeathFrameHeight,
            DeathFrameWidth,
            DeathFrameHeight
        );
        sprite.Scale = new Vector2(0.3f, 0.3f);

        AiEnabled = false;

        var hitbox = GetNode<Area2D>("Hitbox");
        hitbox.SetDeferred("monitoring", false);

        // 🔥 Dispara a timeline ao morrer
        var dialogicNode = GetNodeOrNull<Node>("/root/Dialogic");
        if (dialogicNode != null)
        {
            GD.Print("Mob morreu — iniciando timeline npc_dialogo_2.dtl");
            dialogicNode.Call("start", "res://Chars/npc_dialogo_2.dtl");
        }
        else
        {
            GD.PrintErr("Dialogic não encontrado em /root.");
        }
    }



    private void ResetMovementAnimation()
    {
        frameX = 0;
        frameTimer = 0f;
        // opcional: garantir textura para evitar “frame sujo”
        // sprite.Texture = moveTex;
    }

    private void ResetIdleAnimation()
    {
        idleFrameX = 0;
        idleFrameTimer = 0f;
        // opcional: garantir textura para evitar “frame sujo”
        // sprite.Texture = idleTex;
    }


}
