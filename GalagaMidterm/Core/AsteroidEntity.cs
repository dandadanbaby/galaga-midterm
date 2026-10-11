using System;
using Microsoft.Xna.Framework;
using GalagaMidterm.Core;

namespace GalagaMidterm.Core;

/// <summary>
/// An asteroid entity that implements IEnemy for the renderer.
/// Sizes: 1 = small, 2 = medium, 3 = large.
/// EnemyType is mapped to: 3 = small (Asteroid_1), 4 = medium (Asteroid_2), 5 = large (Asteroid_5).
/// Boss asteroids are size 3 with IsBoss = true and higher HP.
/// </summary>
public class AsteroidEntity : IEnemy
{
    private static readonly Random _rng = new();

    // ── IEnemy properties ──────────────────────────────────
    public Vector2 Position { get; set; }
    public int EnemyType { get; }
    public bool IsAlive { get; set; } = true;
    public float Scale { get; }
    public float Rotation { get; set; }
    public bool IsBoss { get; }

    // ── Asteroid-specific ──────────────────────────────────
    public int Size { get; }
    public int HP { get; set; }
    public float Radius { get; }
    public Vector2 Velocity { get; set; }
    public float SpinRate { get; set; }

    // ── Screen bounds for wrapping ─────────────────────────
    private readonly int _screenWidth;
    private readonly int _screenHeight;

    /// <summary>
    /// Creates a regular asteroid at the given position with random velocity.
    /// </summary>
    public AsteroidEntity(float x, float y, int size, float speed, int screenWidth, int screenHeight)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;

        Size = size;
        HP = 1;
        IsBoss = false;

        // Size → radius, EnemyType, scale
        switch (size)
        {
            case 3: // Large
                Radius = 42f;
                EnemyType = 5; // Asteroid_5 texture
                Scale = 0.55f;
                break;
            case 2: // Medium
                Radius = 24f;
                EnemyType = 4; // Asteroid_2 texture
                Scale = 0.35f;
                break;
            default: // Small
                Radius = 12f;
                EnemyType = 3; // Asteroid_1 texture
                Scale = 0.2f;
                break;
        }

        Position = new Vector2(x, y);

        // Downward drift with slight horizontal sway (Galaga/Invaders style)
        float horizontalDrift = ((float)_rng.NextDouble() - 0.5f) * speed * 0.6f;
        float downSpeed = speed * (0.5f + (float)_rng.NextDouble() * 0.5f) * (4 - size) * 0.5f;
        Velocity = new Vector2(horizontalDrift, downSpeed);

        SpinRate = ((float)_rng.NextDouble() - 0.5f) * 0.04f;
        Rotation = (float)(_rng.NextDouble() * Math.PI * 2);
    }

    /// <summary>
    /// Creates a boss asteroid ("Mother Asteroid") for level 5.
    /// </summary>
    private AsteroidEntity(int screenWidth, int screenHeight, float speed)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;

        Size = 3;
        HP = 30;
        IsBoss = true;
        Radius = 80f;
        EnemyType = 5; // Uses same large asteroid texture
        Scale = 0.9f;  // Rendered bigger

        // Spawn on left or right side near the top
        float x = _rng.Next(2) == 0 ? 60f : screenWidth - 60f;
        Position = new Vector2(x, 60f);

        // Slow horizontal drift, stays near top
        float drift = ((float)_rng.NextDouble() - 0.5f) * 0.8f;
        Velocity = new Vector2(drift, 0.3f);
        SpinRate = 0.006f;
        Rotation = 0f;
    }

    /// <summary>
    /// Factory method for boss asteroid.
    /// </summary>
    public static AsteroidEntity CreateBoss(int screenWidth, int screenHeight, float speed)
    {
        return new AsteroidEntity(screenWidth, screenHeight, speed);
    }

    /// <summary>
    /// Creates an asteroid that spawns away from the player at the bottom-center.
    /// </summary>
    public static AsteroidEntity SpawnAwayFromPlayer(int size, float speed, int screenWidth, int screenHeight)
    {
        // Spawn along the top edge, random X position
        float x = _rng.Next(40, screenWidth - 40);
        float y = -_rng.Next(10, 80); // Just above the screen

        return new AsteroidEntity(x, y, size, speed, screenWidth, screenHeight);
    }

    /// <summary>
    /// Creates a child asteroid at the parent's position (from splitting).
    /// </summary>
    public static AsteroidEntity SpawnChild(float x, float y, int size, float speed, int screenWidth, int screenHeight)
    {
        return new AsteroidEntity(x, y, size, speed, screenWidth, screenHeight);
    }

    /// <summary>
    /// Updates position with wrapping and spin.
    /// </summary>
    public void Update()
    {
        Position += Velocity;
        Rotation += SpinRate;

        var pos = Position;

        // Horizontal wrapping only
        if (pos.X < -Radius) pos.X = _screenWidth + Radius;
        if (pos.X > _screenWidth + Radius) pos.X = -Radius;

        // Boss bounces at top/bottom of upper region instead of leaving
        if (IsBoss)
        {
            if (pos.Y < 30f) { pos.Y = 30f; Velocity = new Vector2(Velocity.X, MathF.Abs(Velocity.Y)); }
            if (pos.Y > _screenHeight * 0.35f) { pos.Y = _screenHeight * 0.35f; Velocity = new Vector2(Velocity.X, -MathF.Abs(Velocity.Y)); }
        }
        else
        {
            // Non-boss asteroids die when they pass the bottom
            if (pos.Y > _screenHeight + Radius + 20f)
                IsAlive = false;
        }

        Position = pos;
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
