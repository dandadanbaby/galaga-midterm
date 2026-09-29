namespace GalagaMidterm.Assets;

/// <summary>
/// Centralized content pipeline asset path constants.
/// All paths are relative to Content/ and use forward slashes (MonoGame convention).
/// </summary>
public static class AssetPaths
{
    // ── Fonts ──────────────────────────────────────────────
    public const string FontSmall  = "fonts/ArcadeFont_Small";
    public const string FontMedium = "fonts/ArcadeFont_Medium";
    public const string FontLarge  = "fonts/ArcadeFont_Large";

    // ── Player Ships ───────────────────────────────────────
    public const string PlayerShip1 = "sprites/player/Ship_1";
    public const string PlayerShip2 = "sprites/player/Ship_2";
    public const string PlayerShip9 = "sprites/player/Ship_9";

    public static readonly string[] PlayerShips = { PlayerShip1, PlayerShip2, PlayerShip9 };

    // ── Enemy Ships (sprite sheets) ────────────────────────
    public const string EnemyShip2   = "sprites/enemies/EnemyShip2";
    public const string EnemyShip4   = "sprites/enemies/EnemyShip4";
    public const string Spaceship4   = "sprites/enemies/Spaceship_4";

    // ── Enemy Asteroids ────────────────────────────────────
    public const string Asteroid1 = "sprites/enemies/Asteroid_1";
    public const string Asteroid2 = "sprites/enemies/Asteroid_2";
    public const string Asteroid5 = "sprites/enemies/Asteroid_5";

    // ── Effects (sprite sheets) ────────────────────────────
    public const string ExplosionBig    = "sprites/effects/Explosion_Big";
    public const string ExplosionMedium = "sprites/effects/Explosion_Medium";
    public const string ExplosionSmall  = "sprites/effects/Explosion_small";
    public const string ShieldBlue      = "sprites/effects/Shield_Blue";
    public const string TripleShotBlue  = "sprites/effects/Triple_Shot_Blue";
    public const string TripleShotGreen = "sprites/effects/Triple_Shot_Green";

    // ── Backgrounds ────────────────────────────────────────
    public const string BgBase      = "backgrounds/base";
    public const string BgAsteroid  = "backgrounds/asteroid/Asteroid";
    public const string BgBlackHole1 = "backgrounds/blackhole/BlackHole1";
    public const string BgBlackHole2 = "backgrounds/blackhole/BlackHole2";
    public const string BgBlackHole3 = "backgrounds/blackhole/BlackHole3";

    // Planets
    public const string BgEarth            = "backgrounds/planets/Earth";
    public const string BgMoon             = "backgrounds/planets/Moon";
    public const string BgPlanet           = "backgrounds/planets/Planet";
    public const string BgPlanet1          = "backgrounds/planets/Planet1";
    public const string BgPlanet2          = "backgrounds/planets/Planet2";
    public const string BgPlanet3          = "backgrounds/planets/Planet3";
    public const string BgPlanet4          = "backgrounds/planets/Planet4";
    public const string BgPlanet5          = "backgrounds/planets/Planet5";
    public const string BgShatteredMoon    = "backgrounds/planets/ShatteredMoon";
    public const string BgShatteredPlanet5 = "backgrounds/planets/ShatteredPlanet5";
    public const string BgShatteredPlanet7 = "backgrounds/planets/ShatteredPlanet7";
    public const string BgShatteredPlanet8 = "backgrounds/planets/ShatteredPlanet8";
    public const string BgShatteredPlanet9 = "backgrounds/planets/ShatteredPlanet9";
    public const string BgSun2             = "backgrounds/planets/Sun2";

    public static readonly string[] AllPlanets =
    {
        BgEarth, BgMoon, BgPlanet, BgPlanet1, BgPlanet2, BgPlanet3,
        BgPlanet4, BgPlanet5, BgShatteredMoon, BgShatteredPlanet5,
        BgShatteredPlanet7, BgShatteredPlanet8, BgShatteredPlanet9, BgSun2
    };

    // ── UI Icons ───────────────────────────────────────────
    public const string IconCoin   = "sprites/icons/Coin";
    public const string IconShield = "sprites/icons/Shield";
}
