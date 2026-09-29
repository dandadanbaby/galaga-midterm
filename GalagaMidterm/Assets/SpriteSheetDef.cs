namespace GalagaMidterm.Assets;

/// <summary>
/// Declares the frame layout for a horizontal sprite sheet asset.
/// Used by <see cref="GameAssets"/> to build <see cref="Rendering.SpriteSheet"/>
/// instances with correct frame dimensions after the texture is loaded.
/// </summary>
public readonly struct SpriteSheetDef
{
    /// <summary>Content pipeline path (relative to Content/).</summary>
    public string Path { get; }

    /// <summary>Width of a single frame in pixels.</summary>
    public int FrameWidth { get; }

    /// <summary>Height of a single frame in pixels.</summary>
    public int FrameHeight { get; }

    /// <summary>
    /// Number of frames. When 0, auto-calculated from texture width / frame width.
    /// </summary>
    public int FrameCount { get; }

    public SpriteSheetDef(string path, int frameWidth, int frameHeight, int frameCount = 0)
    {
        Path = path;
        FrameWidth = frameWidth;
        FrameHeight = frameHeight;
        FrameCount = frameCount;
    }
}
