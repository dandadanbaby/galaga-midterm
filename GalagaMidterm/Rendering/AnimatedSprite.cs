using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GalagaMidterm.Rendering;

/// <summary>
/// A frame-based animation player that cycles through sprite sheet frames.
/// Supports looping and one-shot animations (e.g. explosions).
/// </summary>
public class AnimatedSprite
{
    private readonly SpriteSheet _sheet;
    private float _frameTimer;
    private readonly float _frameDuration;

    public int CurrentFrame { get; private set; }
    public bool IsLooping { get; set; }
    public bool IsFinished { get; private set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Position in world space (center of the sprite).</summary>
    public Vector2 Position { get; set; }
    public float Scale { get; set; } = 1f;
    public float Rotation { get; set; }
    public Color Tint { get; set; } = Color.White;

    /// <summary>
    /// Creates an animated sprite from a sprite sheet definition.
    /// </summary>
    /// <param name="sheet">The sprite sheet containing animation frames.</param>
    /// <param name="frameDurationMs">Milliseconds per frame.</param>
    /// <param name="isLooping">Whether the animation loops continuously.</param>
    public AnimatedSprite(SpriteSheet sheet, float frameDurationMs, bool isLooping = true)
    {
        _sheet = sheet;
        _frameDuration = frameDurationMs / 1000f; // Convert to seconds
        IsLooping = isLooping;
        CurrentFrame = 0;
        _frameTimer = 0f;
        IsFinished = false;
    }

    public void Update(GameTime gameTime)
    {
        if (!IsActive || IsFinished) return;

        _frameTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_frameTimer >= _frameDuration)
        {
            _frameTimer -= _frameDuration;
            CurrentFrame++;

            if (CurrentFrame >= _sheet.FrameCount)
            {
                if (IsLooping)
                {
                    CurrentFrame = 0;
                }
                else
                {
                    CurrentFrame = _sheet.FrameCount - 1;
                    IsFinished = true;
                }
            }
        }
    }

    public void Draw(SpriteBatch batch)
    {
        if (!IsActive) return;

        _sheet.DrawFrame(batch, CurrentFrame, Position, Scale, Tint, Rotation);
    }

    /// <summary>
    /// Draw at an explicit position (overrides Position property for this call).
    /// </summary>
    public void Draw(SpriteBatch batch, Vector2 position, float scale = 1f)
    {
        if (!IsActive) return;

        _sheet.DrawFrame(batch, CurrentFrame, position, scale, Tint, Rotation);
    }

    /// <summary>
    /// Resets the animation to its first frame.
    /// </summary>
    public void Reset()
    {
        CurrentFrame = 0;
        _frameTimer = 0f;
        IsFinished = false;
        IsActive = true;
    }

    /// <summary>
    /// Restarts at a given position (useful for pooled effects).
    /// </summary>
    public void Restart(Vector2 position)
    {
        Position = position;
        Reset();
    }
}
