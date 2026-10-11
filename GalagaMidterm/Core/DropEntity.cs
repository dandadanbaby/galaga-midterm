using System;
using Microsoft.Xna.Framework;

namespace GalagaMidterm.Core;

/// <summary>
/// A collectible drop that falls downward after an asteroid is destroyed.
/// DropType 0 = Coin (bonus score), DropType 1 = Shield (temporary protection).
/// </summary>
public class DropEntity
{
    private static readonly Random _rng = new();

    public Vector2 Position { get; set; }
    public int DropType { get; }
    public bool IsAlive { get; set; } = true;
    public float Radius => 10f;

    private readonly float _fallSpeed;
    private readonly float _swayAmplitude;
    private readonly float _swayOffset;
    private readonly int _screenHeight;
    private float _time;

    public DropEntity(float x, float y, int dropType, int screenHeight)
    {
        Position = new Vector2(x, y);
        DropType = dropType;
        _screenHeight = screenHeight;
        _fallSpeed = 1.2f + (float)_rng.NextDouble() * 0.6f;
        _swayAmplitude = 8f + (float)_rng.NextDouble() * 6f;
        _swayOffset = (float)(_rng.NextDouble() * Math.PI * 2);
    }

    public void Update()
    {
        _time += 0.05f;
        var pos = Position;
        pos.Y += _fallSpeed;
        pos.X += MathF.Sin(_time * 2f + _swayOffset) * 0.5f;
        Position = pos;

        if (pos.Y > _screenHeight + 20f)
            IsAlive = false;
    }

    public bool Hits(Vector2 otherPos, float otherRadius)
    {
        float dx = Position.X - otherPos.X;
        float dy = Position.Y - otherPos.Y;
        float r = Radius + otherRadius;
        return dx * dx + dy * dy < r * r;
    }
}
