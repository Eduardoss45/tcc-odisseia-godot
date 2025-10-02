using Godot;
using System;

public class DashController
{
    public enum DashState { Ready, Charging, Dashing, Cooldown }
    public DashState State { get; private set; } = DashState.Ready;


    // --- Configuráveis ---
    public float DashSpeed { get; set; } = 300f;
    public float DashCooldown { get; set; } = 2f;
    public float DashChargeTime { get; set; } = 0.5f;
    public float DashDuration { get; set; } = 0.8f;
    public float StopDistance { get; set; } = 24f;

    public float Speed { get; set; } = 80f;          // AI walking speed
    public float DetectionRange { get; set; } = 200f;
    public float StopRange { get; set; } = 32f;

    private float dashTimer = 0f;
    private Vector2 dashDir = Vector2.Zero;

    public Vector2 Velocity { get; private set; } = Vector2.Zero;
    public bool IsDashing => State == DashState.Dashing;

    public void Update(float delta, Vector2 mobPos, Vector2 playerPos)
    {
        dashTimer -= delta;
        float dist = mobPos.DistanceTo(playerPos);

        switch (State)
        {
            case DashState.Ready:
                if (dist <= DetectionRange && dashTimer <= 0f)
                {
                    State = DashState.Charging;
                    dashTimer = DashChargeTime;
                    dashDir = (playerPos - mobPos).Normalized();
                }
                else
                {
                    // AI normal
                    Velocity = (playerPos - mobPos).Normalized() * Speed;
                    if (dist <= StopRange)
                        Velocity = Vector2.Zero;
                }
                break;

            case DashState.Charging:
                Velocity = Vector2.Zero;
                if (dashTimer <= 0f)
                    State = DashState.Dashing;
                break;

            case DashState.Dashing:
                Velocity = dashDir * DashSpeed;
                if (dist <= StopDistance || dashTimer <= 0f)
                {
                    State = DashState.Cooldown;
                    dashTimer = DashCooldown;
                    Velocity = Vector2.Zero;
                }
                break;

            case DashState.Cooldown:
                // AI normal durante cooldown
                Velocity = (playerPos - mobPos).Normalized() * Speed;
                if (dist <= StopRange)
                    Velocity = Vector2.Zero;

                if (dashTimer <= 0f)
                    State = DashState.Ready;
                break;
        }
    }

    public void Reset()
    {
        State = DashState.Ready;
        dashTimer = 0f;
        dashDir = Vector2.Zero;
        Velocity = Vector2.Zero;
    }
}
