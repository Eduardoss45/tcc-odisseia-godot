using Godot;
using System;

public partial class MobAI : Node
{
    [Export] public float Speed = 80f;
    [Export] public float DetectionRange = 200f;
    [Export] public float StopRange = 32f;

    private Mob mob;
    private Node2D player;

    public override void _Ready()
    {
        mob = GetParent<Mob>();
        if (mob == null)
        {
            GD.PrintErr("MobAI precisa ser filho de um Node Mob");
            return;
        }

        player = GetTree().Root.GetNodeOrNull<Node2D>("/root/World/Player");
        if (player == null)
        {
            player = GetTree().Root.GetNodeOrNull<Node2D>("/root/World/Player");
            if (player == null)
                return;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (mob == null || !mob.AiEnabled)
            return;

        if (player == null)
        {
            player = GetTree().Root.GetNodeOrNull<Node2D>("/root/World/Player");
            if (player == null)
                return;
        }

        float distanceToPlayer = mob.Position.DistanceTo(player.Position);
        Vector2 velocity = Vector2.Zero;

        if (distanceToPlayer <= DetectionRange && distanceToPlayer > StopRange)
            velocity = (player.Position - mob.Position).Normalized() * Speed;

        mob.SetVelocity(velocity);
        mob.MoveAndSlide();
    }

}
