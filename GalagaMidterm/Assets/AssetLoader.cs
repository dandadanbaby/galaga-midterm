using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace GalagaMidterm.Assets;

/// <summary>
/// Loads and caches all game textures, fonts, and sprite sheet definitions.
/// Single point of access for content so assets are loaded once and shared.
/// </summary>
public class AssetLoader
{
    private readonly ContentManager _content;
    private readonly Dictionary<string, Texture2D> _textures = new();
    private readonly Dictionary<string, SpriteFont> _fonts = new();

    public AssetLoader(ContentManager content)
    {
        _content = content;
    }

    /// <summary>
    /// Loads all game assets from the content pipeline.
    /// Call once during LoadContent.
    /// </summary>
    public void LoadAll()
    {
        // Fonts
        LoadFont(AssetPaths.FontSmall);
        LoadFont(AssetPaths.FontMedium);
        LoadFont(AssetPaths.FontLarge);

        // Player ships
        foreach (var path in AssetPaths.PlayerShips)
            LoadTexture(path);

        // Enemy ships
        LoadTexture(AssetPaths.EnemyShip2);
        LoadTexture(AssetPaths.EnemyShip4);
        LoadTexture(AssetPaths.Spaceship4);

        // Enemy asteroids
        LoadTexture(AssetPaths.Asteroid1);
        LoadTexture(AssetPaths.Asteroid2);
        LoadTexture(AssetPaths.Asteroid5);

        // Effects
        LoadTexture(AssetPaths.ExplosionBig);
        LoadTexture(AssetPaths.ExplosionMedium);
        LoadTexture(AssetPaths.ExplosionSmall);
        LoadTexture(AssetPaths.ShieldBlue);
        LoadTexture(AssetPaths.TripleShotBlue);
        LoadTexture(AssetPaths.TripleShotGreen);

        // Backgrounds
        LoadTexture(AssetPaths.BgBase);
        LoadTexture(AssetPaths.BgAsteroid);
        LoadTexture(AssetPaths.BgBlackHole1);
        LoadTexture(AssetPaths.BgBlackHole2);
        LoadTexture(AssetPaths.BgBlackHole3);

        // Planets
        foreach (var path in AssetPaths.AllPlanets)
            LoadTexture(path);

        // UI Icons
        LoadTexture(AssetPaths.IconCoin);
        LoadTexture(AssetPaths.IconShield);
    }

    public Texture2D GetTexture(string assetPath)
    {
        return _textures[assetPath];
    }

    public SpriteFont GetFont(string assetPath)
    {
        return _fonts[assetPath];
    }

    public bool TryGetTexture(string assetPath, out Texture2D texture)
    {
        return _textures.TryGetValue(assetPath, out texture);
    }

    private void LoadTexture(string path)
    {
        if (!_textures.ContainsKey(path))
        {
            _textures[path] = _content.Load<Texture2D>(path);
        }
    }

    private void LoadFont(string path)
    {
        if (!_fonts.ContainsKey(path))
        {
            _fonts[path] = _content.Load<SpriteFont>(path);
        }
    }

    /// <summary>
    /// Creates a simple 1×1 white pixel texture for drawing rectangles/overlays.
    /// </summary>
    public Texture2D CreatePixelTexture(GraphicsDevice device)
    {
        var pixel = new Texture2D(device, 1, 1);
        pixel.SetData(new[] { Color.White });
        return pixel;
    }
}
