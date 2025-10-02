using Godot;
using System;

public partial class MobAI : Node
{
    private Mob mob;
    private Node2D player;
    private bool hasStartedChasing = false;

    // Distância fixa que o mob deve manter do jogador
    [Export] public float DesiredDistance { get; set; } = 20f;

    public override void _Ready()
    {
        mob = GetParent<Mob>();
        if (mob == null)
        {
            GD.PrintErr("MobAI precisa ser filho de um Node do tipo Mob");
            return;
        }

        player = GetTree().Root.GetNodeOrNull<Node2D>("/root/World/Player");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (mob == null || !mob.AiEnabled)
            return;

        if (player == null)
            player = GetTree().Root.GetNodeOrNull<Node2D>("/root/World/Player");
        if (player == null)
            return;

        float distance = mob.Position.DistanceTo(player.Position);
        Vector2 desiredVelocity = Vector2.Zero;

        // Se o mob estiver mais longe do que a distância desejada → se aproxima
        if (distance > DesiredDistance + 15f)
        {
            desiredVelocity = (player.Position - mob.Position).Normalized() * mob.Speed;

            if (!hasStartedChasing)
            {
                hasStartedChasing = true;
                GD.Print("Mob começou a perseguir o jogador!");

                var dialogicNode = GetNodeOrNull<Node>("/root/Dialogic");
                if (dialogicNode != null)
                {
                    dialogicNode.Call("start", "res://Chars/npc_dialogo_dicas.dtl");
                }
                else
                {
                    GD.PrintErr("Dialogic não encontrado em /root.");
                }
            }
        }
        // Se o mob estiver muito perto → se afasta
        else if (distance < DesiredDistance - 2f) // -2 para evitar jitter
        {
            desiredVelocity = (mob.Position - player.Position).Normalized() * mob.Speed;
        }
        // Se já está na faixa desejada → para
        else
        {
            desiredVelocity = Vector2.Zero;
        }

        mob.SetVelocity(desiredVelocity);
    }
}
