using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GalagaMidterm.Assets;
using GalagaMidterm.Core;
using GalagaMidterm.Rendering;
using GalagaMidterm.UI;

namespace GalagaMidterm.Screens;

/// <summary>
/// Polished arcade-style main menu screen with:
///   - Parallax scrolling starfield with drifting planets
///   - Animated title with per-letter color cycling and glow
///   - Ship showcase cycling through all 3 player ships
///   - Animated enemy formation preview
///   - Menu: START GAME / CONTROLS / QUIT
///   - Decorative star particles and scan-line overlay
///   - Classic arcade footer
///
/// Presentation only — no gameplay logic.
/// </summary>
public class MainMenuScreen : IScreen
{
    private readonly InputManager _input;
    private readonly GameStateManager _stateManager;

    // ── Fonts ──────────────────────────────────────────────
    private SpriteFont _fontLarge;
    private SpriteFont _fontMedium;
    private SpriteFont _fontSmall;

    // ── Assets ─────────────────────────────────────────────
    private Texture2D[] _playerShipTextures;
    private SpriteSheet[] _enemySheets;
    private Texture2D _pixelTexture;
    private ParallaxBackground _background;
    private MenuComponent _menu;

    // ── Screen ─────────────────────────────────────────────
    private int _screenWidth;
    private int _screenHeight;
    private float _timer;

    // ── Title animation ────────────────────────────────────
    private const string TITLE_TEXT = "GALAGA";
    private float _titleIntroTimer;              // controls letter-by-letter reveal
    private const float TITLE_LETTER_DELAY = 0.12f;
    private const float TITLE_INTRO_DURATION = 1.5f; // seconds to fully reveal


    // ── Decorative star particles ──────────────────────────
    private readonly List<StarParticle> _stars = new();
    private readonly Random _rng = new();

    // ── Scroll indicator ───────────────────────────────────
    private float _pressStartBlink;

    // ── Horizontal divider lines ───────────────────────────
    private const float DIVIDER_ALPHA = 0.15f;

    public MainMenuScreen(InputManager input, GameStateManager stateManager)
    {
        _input = input;
        _stateManager = stateManager;
    }

    public void LoadContent(AssetLoader assets, GraphicsDevice graphicsDevice)
    {
        _screenWidth = graphicsDevice.Viewport.Width;
        _screenHeight = graphicsDevice.Viewport.Height;

        // Fonts
        _fontLarge = assets.GetFont(AssetPaths.FontLarge);
        _fontMedium = assets.GetFont(AssetPaths.FontMedium);
        _fontSmall = assets.GetFont(AssetPaths.FontSmall);

        // Pixel for drawing lines/overlays
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });

        // Player ships for showcase
        _playerShipTextures = new Texture2D[]
        {
            assets.GetTexture(AssetPaths.PlayerShip1),
            assets.GetTexture(AssetPaths.PlayerShip2),
            assets.GetTexture(AssetPaths.PlayerShip9),
        };

        // Enemy sprite sheets for formation preview
        _enemySheets = new SpriteSheet[]
        {
            new SpriteSheet(assets.GetTexture(AssetPaths.EnemyShip2),  24, 24, 5),
            new SpriteSheet(assets.GetTexture(AssetPaths.EnemyShip4),  32, 32, 5),
            new SpriteSheet(assets.GetTexture(AssetPaths.Spaceship4),  22, 22, 5),
        };

        // Parallax background
        _background = new ParallaxBackground();
        _background.LoadContent(assets, _screenWidth, _screenHeight);

        // Menu
        _menu = new MenuComponent(new List<string>
        {
            "START GAME",
            "CONTROLS",
            "QUIT"
        });
        _menu.LineSpacing = 38f;
        _menu.OnSelect += OnMenuSelect;

        // Initialize star particles
        SpawnInitialStars();
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _timer += dt;

        // Title intro animation
        if (_titleIntroTimer < TITLE_INTRO_DURATION + 1f)
            _titleIntroTimer += dt;

        // Background
        _background.Update(gameTime);

        // Menu
        _menu.Update(gameTime, _input);



        // Stars
        UpdateStars(dt);

        // Press-start blink
        _pressStartBlink += dt;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // ── Layer 0: Background ────────────────────────────
        spriteBatch.Begin(samplerState: SamplerState.PointWrap);
        _background.Draw(spriteBatch);
        spriteBatch.End();

        // ── Layer 1: Star particles (additive blend) ───────
        spriteBatch.Begin(blendState: BlendState.Additive, samplerState: SamplerState.PointClamp);
        DrawStars(spriteBatch);
        spriteBatch.End();

        // ── Layer 2: Main UI content ───────────────────────
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        DrawTitle(spriteBatch);
        DrawMenu(spriteBatch);
        DrawFooter(spriteBatch);
        DrawScanlineOverlay(spriteBatch);

        spriteBatch.End();
    }

    // ═══════════════════════════════════════════════════════
    //  Title rendering
    // ═══════════════════════════════════════════════════════

    private void DrawTitle(SpriteBatch batch)
    {
        float centerX = _screenWidth / 2f;
        float titleY = _screenHeight * 0.08f;

        // Measure full title for centering
        Vector2 fullSize = _fontLarge.MeasureString(TITLE_TEXT);

        // Draw each letter individually with staggered color cycling
        float charX = centerX - fullSize.X / 2f;

        for (int i = 0; i < TITLE_TEXT.Length; i++)
        {
            string ch = TITLE_TEXT[i].ToString();
            Vector2 charSize = _fontLarge.MeasureString(ch);

            // Letter reveal timing
            float letterAppearTime = i * TITLE_LETTER_DELAY;
            float letterProgress = MathHelper.Clamp(
                (_titleIntroTimer - letterAppearTime) / 0.3f, 0f, 1f);

            if (letterProgress <= 0) continue;

            // Per-letter color cycling — offset by letter index for wave effect
            float hueOffset = i * 0.4f;
            float hue = (_timer * 1.5f + hueOffset) % 1f;
            Color letterColor = HueToArcadeColor(hue);

            // Scale bounce on appear
            float scale = 1f;
            if (letterProgress < 1f)
            {
                scale = 0.5f + letterProgress * 0.5f + MathF.Sin(letterProgress * MathF.PI) * 0.2f;
            }

            // Subtle vertical float per letter
            float letterFloat = MathF.Sin(_timer * 2f + i * 0.5f) * 2f;
            Vector2 pos = new Vector2(charX + charSize.X / 2f, titleY + fullSize.Y / 2f + letterFloat);
            Vector2 origin = charSize / 2f;

            float alpha = letterProgress;

            // Deep shadow
            batch.DrawString(_fontLarge, ch,
                pos + new Vector2(4, 4), Color.Black * (0.5f * alpha),
                0f, origin, scale, SpriteEffects.None, 0f);

            // Color shadow (matching hue, darker)
            batch.DrawString(_fontLarge, ch,
                pos + new Vector2(2, 2), letterColor * (0.3f * alpha),
                0f, origin, scale, SpriteEffects.None, 0f);

            // Main letter
            batch.DrawString(_fontLarge, ch,
                pos, letterColor * alpha,
                0f, origin, scale, SpriteEffects.None, 0f);

            // White highlight glow pulse
            float glowPulse = 0.1f + MathF.Sin(_timer * 3f + i) * 0.08f;
            batch.DrawString(_fontLarge, ch,
                pos, Color.White * (glowPulse * alpha),
                0f, origin, scale, SpriteEffects.None, 0f);

            charX += charSize.X;
        }
    }



    // ═══════════════════════════════════════════════════════
    //  Menu
    // ═══════════════════════════════════════════════════════

    private void DrawMenu(SpriteBatch batch)
    {
        float menuDelay = TITLE_TEXT.Length * TITLE_LETTER_DELAY + 1.5f;
        float menuAlpha = MathHelper.Clamp((_titleIntroTimer - menuDelay) / 0.4f, 0f, 1f);
        if (menuAlpha <= 0) return;

        Vector2 menuCenter = new Vector2(_screenWidth / 2f, _screenHeight * 0.50f);
        _menu.Draw(batch, _fontMedium, menuCenter);
    }

    // ═══════════════════════════════════════════════════════
    //  Decorative elements
    // ═══════════════════════════════════════════════════════


    private void DrawFooter(SpriteBatch batch)
    {
        // Credits line
        string footer = "2026 MIDTERM PROJECT";
        Vector2 footerSize = _fontSmall.MeasureString(footer);
        float footerAlpha = 0.25f + MathF.Sin(_timer * 1.2f) * 0.08f;
        batch.DrawString(_fontSmall, footer,
            new Vector2((_screenWidth - footerSize.X) / 2f, _screenHeight - 52),
            new Color(100, 100, 120) * footerAlpha);

        // "PRESS START" blinking prompt
        float blinkAlpha = MathF.Sin(_pressStartBlink * 2.5f);
        if (blinkAlpha > 0)
        {
            string pressStart = "PRESS ENTER";
            Vector2 psSize = _fontSmall.MeasureString(pressStart);
            batch.DrawString(_fontSmall, pressStart,
                new Vector2((_screenWidth - psSize.X) / 2f, _screenHeight - 30),
                new Color(255, 255, 100) * (blinkAlpha * 0.5f));
        }
    }

    private void DrawScanlineOverlay(SpriteBatch batch)
    {
        // Very subtle scanline effect — every other 2px row gets darkened
        for (int y = 0; y < _screenHeight; y += 3)
        {
            batch.Draw(_pixelTexture,
                new Rectangle(0, y, _screenWidth, 1),
                Color.Black * 0.04f);
        }
    }

    // ═══════════════════════════════════════════════════════
    //  Star particles
    // ═══════════════════════════════════════════════════════

    private class StarParticle
    {
        public Vector2 Position;
        public float Speed;
        public float Size;
        public float Brightness;
        public float Twinkle;
        public float TwinkleSpeed;
    }

    private void SpawnInitialStars()
    {
        for (int i = 0; i < 60; i++)
        {
            _stars.Add(CreateStar(randomY: true));
        }
    }

    private StarParticle CreateStar(bool randomY)
    {
        return new StarParticle
        {
            Position = new Vector2(
                _rng.Next(_screenWidth),
                randomY ? _rng.Next(_screenHeight) : -2),
            Speed = 20f + (float)_rng.NextDouble() * 60f,
            Size = 1f + (float)_rng.NextDouble() * 1.5f,
            Brightness = 0.3f + (float)_rng.NextDouble() * 0.7f,
            Twinkle = (float)_rng.NextDouble() * MathF.Tau,
            TwinkleSpeed = 2f + (float)_rng.NextDouble() * 4f,
        };
    }

    private void UpdateStars(float dt)
    {
        for (int i = _stars.Count - 1; i >= 0; i--)
        {
            _stars[i].Position.Y += _stars[i].Speed * dt;
            _stars[i].Twinkle += _stars[i].TwinkleSpeed * dt;

            if (_stars[i].Position.Y > _screenHeight + 5)
            {
                _stars[i] = CreateStar(randomY: false);
            }
        }
    }

    private void DrawStars(SpriteBatch batch)
    {
        foreach (var star in _stars)
        {
            float twinkle = 0.5f + MathF.Sin(star.Twinkle) * 0.5f;
            float alpha = star.Brightness * twinkle;
            int size = Math.Max(1, (int)star.Size);

            Color starColor = star.Speed > 50f
                ? new Color(180, 200, 255) // Blue-white for fast stars
                : new Color(255, 240, 200); // Warm white for slow stars

            batch.Draw(_pixelTexture,
                new Rectangle(
                    (int)star.Position.X,
                    (int)star.Position.Y,
                    size, size),
                starColor * alpha);
        }
    }

    // ═══════════════════════════════════════════════════════
    //  Color utilities
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Converts a hue value (0–1) to an arcade-style color.
    /// Cycles through warm yellow, red, cyan, and back.
    /// </summary>
    private static Color HueToArcadeColor(float hue)
    {
        // Arcade palette: Yellow → Red → Magenta → Cyan → White → Yellow
        hue = hue % 1f;
        if (hue < 0.2f)
            return Color.Lerp(new Color(255, 255, 80), new Color(255, 100, 60), hue / 0.2f);
        if (hue < 0.4f)
            return Color.Lerp(new Color(255, 100, 60), new Color(255, 80, 200), (hue - 0.2f) / 0.2f);
        if (hue < 0.6f)
            return Color.Lerp(new Color(255, 80, 200), new Color(80, 200, 255), (hue - 0.4f) / 0.2f);
        if (hue < 0.8f)
            return Color.Lerp(new Color(80, 200, 255), Color.White, (hue - 0.6f) / 0.2f);
        return Color.Lerp(Color.White, new Color(255, 255, 80), (hue - 0.8f) / 0.2f);
    }

    // ═══════════════════════════════════════════════════════
    //  Screen lifecycle
    // ═══════════════════════════════════════════════════════

    public void OnEnter()
    {
        _timer = 0f;
        _titleIntroTimer = 0f;
        _pressStartBlink = 0f;
        _menu?.Reset();
    }

    public void OnExit() { }

    private void OnMenuSelect(int index)
    {
        switch (index)
        {
            case 0: // Start Game
                _stateManager.SwitchTo("gameplay", TransitionStyle.SquareIris, 0.6f);
                break;
            case 1: // Controls
                _stateManager.PushScreen("controls");
                break;
            case 2: // Quit
                Environment.Exit(0);
                break;
        }
    }
}
