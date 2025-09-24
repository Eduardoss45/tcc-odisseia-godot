using Godot;
using System;

public partial class MobAI : Node
{
    private Mob mob;
    private Node2D player;
    private bool hasStartedChasing = false;

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

        if (distance <= mob.DetectionRange && distance > mob.StopRange)
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

        mob.SetVelocity(desiredVelocity);
    }
}
