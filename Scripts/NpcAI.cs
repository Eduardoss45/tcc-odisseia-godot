// using Godot;
// using System;
// 
// public partial class NpcAI : Node
// {
//     [Export] public float Speed = 80f;
//     [Export] public float DetectionRange = 200f;
//     [Export] public float StopRange = 32f;
// 
//     private Npc npc;
//     private Node2D player;
// 
//     public override void _Ready()
//     {
//         npc = GetParent<Npc>();
//         if (npc == null)
//         {
//             GD.PrintErr("NpcAI precisa ser filho de um Node Npc");
//             return;
//         }
// 
//         player = GetTree().Root.GetNodeOrNull<Node2D>("/root/World/Player");
//         if (player == null)
//         {
//             player = GetTree().Root.GetNodeOrNull<Node2D>("/root/World/Player");
//             if (player == null)
//                 return;
//         }
//     }
// 
//     public override void _PhysicsProcess(double delta)
//     {
//         if (npc == null || !npc.AiEnabled)
//             return;
// 
//         if (player == null)
//         {
//             player = GetTree().Root.GetNodeOrNull<Node2D>("/root/World/Player");
//             if (player == null) return;
//         }
// 
//         float distanceToPlayer = npc.Position.DistanceTo(player.Position);
//         Vector2 vel = Vector2.Zero;
// 
//         if (distanceToPlayer <= DetectionRange && distanceToPlayer > StopRange)
//             vel = (player.Position - npc.Position).Normalized() * Speed;
//             
//         npc.SetVelocity(vel);
//     }
// }
