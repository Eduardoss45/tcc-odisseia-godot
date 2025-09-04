using Godot;
using System;

public partial class MobAI : Node
{
    private Mob mob;
    private Node2D player;

    public override void _Ready()
    {
        // Encontra o Mob pai
        mob = GetParent<Mob>();
        if (mob == null)
        {
            GD.PrintErr("MobAI precisa ser filho de um Node do tipo Mob");
            return;
        }

        // Tenta cachear o player (se for removido, recarrega no _PhysicsProcess)
        player = GetTree().Root.GetNodeOrNull<Node2D>("/root/World/Player");
    }

    public override void _PhysicsProcess(double delta)
    {
        // Sai se não houver Mob ou IA desativada
        if (mob == null || !mob.AiEnabled)
            return;

        // Tenta recarregar o player caso seja null
        if (player == null)
            player = GetTree().Root.GetNodeOrNull<Node2D>("/root/World/Player");
        if (player == null)
            return;

        // Calcula distância e velocidade desejada
        float distance = mob.Position.DistanceTo(player.Position);
        Vector2 desiredVelocity = Vector2.Zero;

        if (distance <= mob.DetectionRange && distance > mob.StopRange)
            desiredVelocity = (player.Position - mob.Position)
                              .Normalized() * mob.Speed;

        // Atualiza somente a velocidade; Movement e MoveAndSlide ficam no Mob
        mob.SetVelocity(desiredVelocity);
    }
}
