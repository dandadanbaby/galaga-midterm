using Microsoft.Xna.Framework;

namespace GalagaMidterm.Core;

/// <summary>
/// A player bullet that implements IProjectile.
/// Travels straight upward (Galaga-style) and has a limited lifetime.
/// </summary>
public class BulletEntity : IProjectile
{
    public Vector2 Position { get; set; }
    public int ProjectileType { get; } = 0; // 0 = player blue
    public bool IsDead { get; set; }

    public float Radius { get; } = 5f;
    private int _lifeFrames = 120; // ~2 seconds at 60fps
    private const float Speed = 7f;

    public Vector2 Velocity { get; set; }

    public BulletEntity(Vector2 spawnPosition, Vector2? velocity = null)
    {
        Position = spawnPosition + new Vector2(0, -20f);
        Velocity = velocity ?? new Vector2(0, -Speed);
    }

    public void Update()
    {
        Position += Velocity;
        _lifeFrames--;
        if (_lifeFrames <= 0 || Position.Y < -20f || Position.X < -20f || Position.X > 500f)
            IsDead = true;
    }

    /// <summary>
    /// Circle-circle collision check.
    /// </summary>
    public bool Hits(Vector2 otherPos, float otherRadius)
    {
        float dx = Position.X - otherPos.X;
        float dy = Position.Y - otherPos.Y;
        float r = Radius + otherRadius;
        return dx * dx + dy * dy < r * r;
    }
}
