using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GalagaMidterm.Rendering;

namespace GalagaMidterm.Assets;

/// <summary>
/// High-level, strongly-typed asset façade that maps loaded textures and fonts
/// to their intended front-end purposes. Consumers never deal with path strings
/// or frame math — they request assets by role (e.g. "player ship 1",
/// "explosion big sheet", "HUD coin icon").
///
/// USAGE:
///   Create after AssetLoader.LoadAll() completes, then pass to any system
///   that needs visual resources (menus, HUD, gameplay renderer, effects).
///
/// DESIGN DECISIONS:
///   • Keeps all SpriteSheet frame-size knowledge in ONE place (here) so it
///     matches the actual pixel dimensions of the supplied artwork.
///   • Preserves transparency by using AlphaBlend (the default SpriteBatch
///     blend mode) and PremultipliedAlpha in the content pipeline.
///   • Preserves aspect ratio because textures are drawn at uniform scale
///     from the sprite origin, never stretched to a destination rectangle.
///   • All scaling is done via a uniform float, never distortion.
/// </summary>
public class GameAssets
{
    // ── Backing store ──────────────────────────────────────
    private readonly AssetLoader _loader;

    // ══════════════════════════════════════════════════════════
    //  Player
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Full-resolution player ship textures indexed by ship type
    /// (0 = Ship_1, 1 = Ship_2, 2 = Ship_9).
    /// Source: assests/player/ships/ — 267×278, 232×245, 251×252 px.
    /// These are single sprites, not sprite sheets.
    /// </summary>
    public Texture2D[] PlayerShips { get; private set; }

    /// <summary>Number of available player ship variants.</summary>
    public int PlayerShipCount => PlayerShips.Length;

    /// <summary>
    /// Pre-computed drawing scales that normalize the various player ship
    /// sizes to a consistent on-screen size (~50 px tall at scale 1.0).
    /// Preserves aspect ratio — each axis uses the same scale.
    /// </summary>
    public float[] PlayerShipNormalizedScales { get; private set; }

    /// <summary>
    /// Pre-computed center-origin for each player ship texture.
    /// Using center origin prevents any position offset when drawing
    /// at a world position, and avoids stretching.
    /// </summary>
    public Vector2[] PlayerShipOrigins { get; private set; }

    // ══════════════════════════════════════════════════════════
    //  Enemy Ships (animated sprite sheets)
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Enemy ship sprite sheets indexed by enemy type:
    ///   0 = EnemyShip2 (24×24, 5 frames)
    ///   1 = EnemyShip4 (32×32, 5 frames)
    ///   2 = Spaceship_4 (22×22, 5 frames)
    /// Source: assests/enemies/ships/ — horizontal strip PNGs.
    /// </summary>
    public SpriteSheet[] EnemyShipSheets { get; private set; }

    /// <summary>Number of enemy ship sprite-sheet variants.</summary>
    public int EnemyShipTypeCount => EnemyShipSheets.Length;

    // ══════════════════════════════════════════════════════════
    //  Enemy Asteroids (static textures, various sizes)
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Asteroid textures indexed 0–2 (small → large):
    ///   0 = Asteroid_1 (110×105)
    ///   1 = Asteroid_2 (153×152)
    ///   2 = Asteroid_5 (264×274)
    /// Source: assests/enemies/asteroid/
    /// </summary>
    public Texture2D[] AsteroidTextures { get; private set; }

    /// <summary>Pre-computed center origins for asteroid textures.</summary>
    public Vector2[] AsteroidOrigins { get; private set; }

    /// <summary>
    /// Recommended drawing scales to normalize asteroid display sizes.
    /// Keeps the largest asteroid from dominating the screen.
    /// </summary>
    public float[] AsteroidNormalizedScales { get; private set; }

    // ══════════════════════════════════════════════════════════
    //  Effects (animated sprite sheets — explosions, shields, projectiles)
    // ══════════════════════════════════════════════════════════

    /// <summary>Explosion Big — 60×60 px, 4 frames. Source: assests/effects/Explosion_Big.png</summary>
    public SpriteSheet ExplosionBigSheet { get; private set; }

    /// <summary>Explosion Medium — 40×40 px, 9 frames. Source: assests/effects/Explosion_Medium.png</summary>
    public SpriteSheet ExplosionMediumSheet { get; private set; }

    /// <summary>Explosion Small — 7×7 px, 5 frames. Source: assests/effects/Explosion_small.png</summary>
    public SpriteSheet ExplosionSmallSheet { get; private set; }

    /// <summary>Shield overlay — 16×16 px, 2 frames. Source: assests/effects/Shield_Blue.png</summary>
    public SpriteSheet ShieldSheet { get; private set; }

    /// <summary>Player projectile (blue) — 16×16 px, 2 frames. Source: assests/effects/Triple_Shot_Blue.png</summary>
    public SpriteSheet ProjectileBlueSheet { get; private set; }

    /// <summary>Enemy projectile (green) — 16×16 px, 2 frames. Source: assests/effects/Triple_Shot_Green.png</summary>
    public SpriteSheet ProjectileGreenSheet { get; private set; }

    // ══════════════════════════════════════════════════════════
    //  Backgrounds
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Tiling starfield base (180×120 px). Source: assests/background/base.png
    /// Used as Layer 0 in the parallax system, tiled via SamplerState.PointWrap.
    /// </summary>
    public Texture2D BgBaseTile { get; private set; }

    /// <summary>Background asteroid (64×64 px) for drifting debris. Source: assests/background/asteroid/</summary>
    public Texture2D BgAsteroid { get; private set; }

    /// <summary>
    /// Black hole textures (128×80, 64×64, 64×64). Source: assests/background/blackhole/
    /// Available for background events or stage-specific visuals.
    /// </summary>
    public Texture2D[] BgBlackHoles { get; private set; }

    /// <summary>
    /// All planet textures available for drifting parallax objects (14 variants, 64×64 each).
    /// Source: assests/background/planets/
    /// </summary>
    public Texture2D[] BgPlanets { get; private set; }

    /// <summary>All planet asset paths, for lookup purposes.</summary>
    public string[] BgPlanetPaths { get; private set; }

    // ══════════════════════════════════════════════════════════
    //  UI / HUD Icons
    // ══════════════════════════════════════════════════════════

    /// <summary>Coin/score icon (174×176 px). Source: assests/icons/Coin.png</summary>
    public Texture2D IconCoin { get; private set; }

    /// <summary>Shield status icon (169×173 px). Source: assests/icons/Shield.png</summary>
    public Texture2D IconShield { get; private set; }

    /// <summary>Pre-computed center origin for coin icon.</summary>
    public Vector2 IconCoinOrigin { get; private set; }

    /// <summary>Pre-computed center origin for shield icon.</summary>
    public Vector2 IconShieldOrigin { get; private set; }

    // ══════════════════════════════════════════════════════════
    //  Fonts
    // ══════════════════════════════════════════════════════════

    /// <summary>Small arcade font (body text, HUD numbers).</summary>
    public SpriteFont FontSmall { get; private set; }

    /// <summary>Medium arcade font (menu items, stage labels).</summary>
    public SpriteFont FontMedium { get; private set; }

    /// <summary>Large arcade font (title, GAME OVER).</summary>
    public SpriteFont FontLarge { get; private set; }

    // ══════════════════════════════════════════════════════════
    //  Utility
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// 1×1 white pixel texture for drawing solid rectangles, overlays, and flash effects.
    /// Created on the GPU at initialization time.
    /// </summary>
    public Texture2D PixelTexture { get; private set; }

    // ──────────────────────────────────────────────────────────
    //  Construction
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Creates the GameAssets façade.
    /// Call <see cref="Initialize"/> after construction to build everything.
    /// </summary>
    public GameAssets(AssetLoader loader)
    {
        _loader = loader;
    }

    /// <summary>
    /// Builds all typed asset references from the already-loaded AssetLoader cache.
    /// Call once, immediately after AssetLoader.LoadAll().
    /// </summary>
    public void Initialize(GraphicsDevice graphicsDevice)
    {
        BuildPlayerAssets();
        BuildEnemyShipAssets();
        BuildAsteroidAssets();
        BuildEffectSheets();
        BuildBackgroundAssets();
        BuildUIAssets();
        BuildFontAssets();
        BuildUtilityAssets(graphicsDevice);
    }

    // ──────────────────────────────────────────────────────────
    //  Builder methods — one per category
    // ──────────────────────────────────────────────────────────

    private void BuildPlayerAssets()
    {
        PlayerShips = new Texture2D[]
        {
            _loader.GetTexture(AssetPaths.PlayerShip1),   // 267×278
            _loader.GetTexture(AssetPaths.PlayerShip2),   // 232×245
            _loader.GetTexture(AssetPaths.PlayerShip9),   // 251×252
        };

        // Compute center origins (preserves draw position = center of sprite)
        PlayerShipOrigins = new Vector2[PlayerShips.Length];
        for (int i = 0; i < PlayerShips.Length; i++)
        {
            PlayerShipOrigins[i] = new Vector2(
                PlayerShips[i].Width / 2f,
                PlayerShips[i].Height / 2f);
        }

        // Normalize to ~50 px tall for consistent on-screen size
        const float targetHeight = 50f;
        PlayerShipNormalizedScales = new float[PlayerShips.Length];
        for (int i = 0; i < PlayerShips.Length; i++)
        {
            PlayerShipNormalizedScales[i] = targetHeight / PlayerShips[i].Height;
        }
    }

    private void BuildEnemyShipAssets()
    {
        // Frame definitions match actual pixel measurements of the supplied PNGs:
        //   EnemyShip2.png  = 120×24  → 5 frames of 24×24
        //   EnemyShip4.png  = 160×32  → 5 frames of 32×32
        //   Spaceship_4.png = 110×22  → 5 frames of 22×22
        EnemyShipSheets = new SpriteSheet[]
        {
            new SpriteSheet(_loader.GetTexture(AssetPaths.EnemyShip2),  24, 24, 5),
            new SpriteSheet(_loader.GetTexture(AssetPaths.EnemyShip4),  32, 32, 5),
            new SpriteSheet(_loader.GetTexture(AssetPaths.Spaceship4),  22, 22, 5),
        };
    }

    private void BuildAsteroidAssets()
    {
        AsteroidTextures = new Texture2D[]
        {
            _loader.GetTexture(AssetPaths.Asteroid1),   // 110×105
            _loader.GetTexture(AssetPaths.Asteroid2),   // 153×152
            _loader.GetTexture(AssetPaths.Asteroid5),   // 264×274
        };

        AsteroidOrigins = new Vector2[AsteroidTextures.Length];
        for (int i = 0; i < AsteroidTextures.Length; i++)
        {
            AsteroidOrigins[i] = new Vector2(
                AsteroidTextures[i].Width / 2f,
                AsteroidTextures[i].Height / 2f);
        }

        // Normalize so largest asteroid renders at ~40 px tall
        const float targetSize = 40f;
        AsteroidNormalizedScales = new float[AsteroidTextures.Length];
        for (int i = 0; i < AsteroidTextures.Length; i++)
        {
            float maxDim = Math.Max(AsteroidTextures[i].Width, AsteroidTextures[i].Height);
            AsteroidNormalizedScales[i] = targetSize / maxDim;
        }
    }

    private void BuildEffectSheets()
    {
        // All frame sizes match the actual pixel measurements of the supplied PNGs
        ExplosionBigSheet    = new SpriteSheet(_loader.GetTexture(AssetPaths.ExplosionBig),    60, 60, 4);
        ExplosionMediumSheet = new SpriteSheet(_loader.GetTexture(AssetPaths.ExplosionMedium), 40, 40, 9);
        ExplosionSmallSheet  = new SpriteSheet(_loader.GetTexture(AssetPaths.ExplosionSmall),   7,  7, 5);
        ShieldSheet          = new SpriteSheet(_loader.GetTexture(AssetPaths.ShieldBlue),      16, 16, 2);
        ProjectileBlueSheet  = new SpriteSheet(_loader.GetTexture(AssetPaths.TripleShotBlue),  16, 16, 2);
        ProjectileGreenSheet = new SpriteSheet(_loader.GetTexture(AssetPaths.TripleShotGreen), 16, 16, 2);
    }

    private void BuildBackgroundAssets()
    {
        BgBaseTile = _loader.GetTexture(AssetPaths.BgBase);
        BgAsteroid = _loader.GetTexture(AssetPaths.BgAsteroid);

        BgBlackHoles = new Texture2D[]
        {
            _loader.GetTexture(AssetPaths.BgBlackHole1),
            _loader.GetTexture(AssetPaths.BgBlackHole2),
            _loader.GetTexture(AssetPaths.BgBlackHole3),
        };

        // Build planet array in the same order as AssetPaths.AllPlanets
        BgPlanetPaths = AssetPaths.AllPlanets;
        BgPlanets = new Texture2D[AssetPaths.AllPlanets.Length];
        for (int i = 0; i < AssetPaths.AllPlanets.Length; i++)
        {
            BgPlanets[i] = _loader.GetTexture(AssetPaths.AllPlanets[i]);
        }
    }

    private void BuildUIAssets()
    {
        IconCoin   = _loader.GetTexture(AssetPaths.IconCoin);
        IconShield = _loader.GetTexture(AssetPaths.IconShield);

        IconCoinOrigin   = new Vector2(IconCoin.Width / 2f, IconCoin.Height / 2f);
        IconShieldOrigin = new Vector2(IconShield.Width / 2f, IconShield.Height / 2f);
    }

    private void BuildFontAssets()
    {
        FontSmall  = _loader.GetFont(AssetPaths.FontSmall);
        FontMedium = _loader.GetFont(AssetPaths.FontMedium);
        FontLarge  = _loader.GetFont(AssetPaths.FontLarge);
    }

    private void BuildUtilityAssets(GraphicsDevice device)
    {
        PixelTexture = new Texture2D(device, 1, 1);
        PixelTexture.SetData(new[] { Color.White });
    }

    // ──────────────────────────────────────────────────────────
    //  Convenience accessors
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Gets the player ship texture for the given ship type index,
    /// clamped to valid range.
    /// </summary>
    public Texture2D GetPlayerShip(int shipType)
    {
        int idx = Math.Clamp(shipType, 0, PlayerShips.Length - 1);
        return PlayerShips[idx];
    }

    /// <summary>
    /// Gets the normalized scale for a player ship type.
    /// </summary>
    public float GetPlayerShipScale(int shipType)
    {
        int idx = Math.Clamp(shipType, 0, PlayerShipNormalizedScales.Length - 1);
        return PlayerShipNormalizedScales[idx];
    }

    /// <summary>
    /// Gets the center origin for a player ship type.
    /// </summary>
    public Vector2 GetPlayerShipOrigin(int shipType)
    {
        int idx = Math.Clamp(shipType, 0, PlayerShipOrigins.Length - 1);
        return PlayerShipOrigins[idx];
    }

    /// <summary>
    /// Gets the enemy sprite sheet for the given enemy type.
    /// Returns null if the type represents an asteroid (type >= EnemyShipTypeCount).
    /// </summary>
    public SpriteSheet GetEnemySheet(int enemyType)
    {
        if (enemyType < 0 || enemyType >= EnemyShipSheets.Length) return null;
        return EnemyShipSheets[enemyType];
    }

    /// <summary>
    /// Gets the asteroid texture for an enemy type that is an asteroid
    /// (type index relative to the first asteroid type).
    /// </summary>
    public Texture2D GetAsteroidTexture(int asteroidIndex)
    {
        int idx = Math.Clamp(asteroidIndex, 0, AsteroidTextures.Length - 1);
        return AsteroidTextures[idx];
    }

    /// <summary>
    /// Gets the asteroid center origin for an asteroid index.
    /// </summary>
    public Vector2 GetAsteroidOrigin(int asteroidIndex)
    {
        int idx = Math.Clamp(asteroidIndex, 0, AsteroidOrigins.Length - 1);
        return AsteroidOrigins[idx];
    }

    /// <summary>
    /// Gets the normalized drawing scale for an asteroid index.
    /// </summary>
    public float GetAsteroidScale(int asteroidIndex)
    {
        int idx = Math.Clamp(asteroidIndex, 0, AsteroidNormalizedScales.Length - 1);
        return AsteroidNormalizedScales[idx];
    }

    /// <summary>
    /// Gets a random planet texture for background spawning.
    /// </summary>
    public Texture2D GetRandomPlanet(Random rng)
    {
        if (BgPlanets.Length == 0) return null;
        return BgPlanets[rng.Next(BgPlanets.Length)];
    }

    /// <summary>
    /// Provides direct access to the underlying AssetLoader for edge cases
    /// where raw content path access is needed. Prefer typed properties above.
    /// </summary>
    public AssetLoader RawLoader => _loader;
}
