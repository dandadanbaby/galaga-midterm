using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GalagaMidterm.Rendering;

/// <summary>
/// Defines a horizontal sprite sheet's frame layout and provides
/// source rectangles for each frame. All sprite sheets in this project
/// are horizontal strips (frames laid out left-to-right).
/// </summary>
public class SpriteSheet
{
    public Texture2D Texture { get; }
    public int FrameWidth { get; }
    public int FrameHeight { get; }
    public int FrameCount { get; }
    public Rectangle[] Frames { get; }

    /// <summary>
    /// Creates a sprite sheet from a texture and frame dimensions.
    /// </summary>
    /// <param name="texture">The sprite sheet texture.</param>
    /// <param name="frameWidth">Width of each frame in pixels.</param>
    /// <param name="frameHeight">Height of each frame in pixels.</param>
    /// <param name="frameCount">Number of frames. If 0, auto-calculated from texture width.</param>
    public SpriteSheet(Texture2D texture, int frameWidth, int frameHeight, int frameCount = 0)
    {
        Texture = texture;
        FrameWidth = frameWidth;
        FrameHeight = frameHeight;
        FrameCount = frameCount > 0 ? frameCount : texture.Width / frameWidth;
        Frames = new Rectangle[FrameCount];

        for (int i = 0; i < FrameCount; i++)
        {
            Frames[i] = new Rectangle(i * frameWidth, 0, frameWidth, frameHeight);
        }
    }

    /// <summary>
    /// Gets the source rectangle for a specific frame index.
    /// </summary>
    public Rectangle GetFrame(int frameIndex)
    {
        return Frames[frameIndex % FrameCount];
    }

    /// <summary>
    /// Draws a specific frame at the given position.
    /// </summary>
    public void DrawFrame(SpriteBatch batch, int frameIndex, Vector2 position,
        float scale = 1f, Color? color = null, float rotation = 0f,
        SpriteEffects effects = SpriteEffects.None, float layerDepth = 0f)
    {
        var frame = GetFrame(frameIndex);
        var origin = new Vector2(FrameWidth / 2f, FrameHeight / 2f);

        batch.Draw(
            Texture,
            position,
            frame,
            color ?? Color.White,
            rotation,
            origin,
            scale,
            effects,
            layerDepth
        );
    }
}
