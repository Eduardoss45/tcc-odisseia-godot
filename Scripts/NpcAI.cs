using Godot;
using System;

public partial class NpcAI : Node
{
    [Export] public float Speed = 80f;
    [Export] public float DetectionRange = 200f;
    [Export] public float StopRange = 32f;

    private Npc npc;
    private Node2D player;

    public override void _Ready()
    {
        // Assumir que o script está dentro do NPC
        npc = GetParent<Npc>();
        if (npc == null)
        {
            GD.PrintErr("NpcAI precisa ser filho de um Node Npc");
            return;
        }

        player = GetTree().Root.GetNodeOrNull<Node2D>("/root/World/Player");
        if (player == null)
            GD.PrintErr("Player não encontrado em /root/World/Player");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (npc == null || player == null || !npc.AiEnabled)
            return;

        float distanceToPlayer = npc.Position.DistanceTo(player.Position);
        Vector2 velocity = Vector2.Zero;

        if (distanceToPlayer <= DetectionRange && distanceToPlayer > StopRange)
        {
            velocity = (player.Position - npc.Position).Normalized() * Speed;
        }

        npc.Velocity = velocity;
        npc.MoveAndSlide();
    }
}
