using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GalagaMidterm.Core;

/// <summary>
/// Camera with screen shake support and viewport transformation.
/// Apply the TransformMatrix to SpriteBatch.Begin() to enable shake effects.
/// </summary>
public class Camera2D
{
    private Vector2 _shakeOffset;
    private float _shakeDuration;
    private float _shakeTimer;
    private float _shakeIntensity;
    private readonly Random _rng = new();

    public Matrix TransformMatrix { get; private set; } = Matrix.Identity;

    /// <summary>
    /// Triggers a screen shake effect.
    /// </summary>
    /// <param name="intensity">Maximum pixel displacement.</param>
    /// <param name="durationSeconds">How long the shake lasts.</param>
    public void Shake(float intensity = 4f, float durationSeconds = 0.3f)
    {
        _shakeIntensity = intensity;
        _shakeDuration = durationSeconds;
        _shakeTimer = durationSeconds;
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_shakeTimer > 0)
        {
            _shakeTimer -= dt;
            float progress = _shakeTimer / _shakeDuration; // 1.0 → 0.0
            float currentIntensity = _shakeIntensity * progress;

            _shakeOffset = new Vector2(
                ((float)_rng.NextDouble() * 2f - 1f) * currentIntensity,
                ((float)_rng.NextDouble() * 2f - 1f) * currentIntensity
            );
        }
        else
        {
            _shakeOffset = Vector2.Zero;
        }

        TransformMatrix = Matrix.CreateTranslation(_shakeOffset.X, _shakeOffset.Y, 0);
    }

    /// <summary>
    /// Returns true if the camera is currently shaking.
    /// </summary>
    public bool IsShaking => _shakeTimer > 0;
}
