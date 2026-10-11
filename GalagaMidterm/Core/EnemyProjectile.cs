using Microsoft.Xna.Framework;

namespace GalagaMidterm.Core;

/// <summary>
/// An enemy laser projectile that travels in a specified velocity vector (aimed or spread).
/// Uses ProjectileType = 1 which renders with the green plasma projectile sprite sheet.
/// </summary>
public class EnemyProjectile : IProjectile
{
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public int ProjectileType => 1; // 1 = enemy green plasma
    public bool IsDead { get; set; }
    public float Radius => 6f;

    public EnemyProjectile(Vector2 spawnPosition, Vector2 velocity)
    {
        Position = spawnPosition;
        Velocity = velocity;
    }

    public void Update()
    {
        Position += Velocity;
        if (Position.Y > 750f || Position.Y < -50f || Position.X < -50f || Position.X > 530f)
        {
            IsDead = true;
        }
    }

    public bool Hits(Vector2 otherPos, float otherRadius)
    {
        float dx = Position.X - otherPos.X;
        float dy = Position.Y - otherPos.Y;
        float r = Radius + otherRadius;
        return dx * dx + dy * dy < r * r;
    }
}
