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
    private const string SUBTITLE_TEXT = "MIDTERM EDITION";
    private float _titleIntroTimer;              // controls letter-by-letter reveal
    private const float TITLE_LETTER_DELAY = 0.12f;
    private const float TITLE_INTRO_DURATION = 1.5f; // seconds to fully reveal

    // ── Ship showcase ──────────────────────────────────────
    private int _showcaseShipIndex;
    private float _showcaseTimer;
    private float _showcaseTransition;           // 0 = showing, 1 = transitioning
    private const float SHOWCASE_DISPLAY_TIME = 3.5f;
    private const float SHOWCASE_FADE_TIME = 0.6f;

    // ── Enemy formation display ────────────────────────────
    private float _enemyAnimTimer;
    private int _enemyAnimFrame;

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

        // Ship showcase cycling
        UpdateShowcase(dt);

        // Enemy animation
        _enemyAnimTimer += dt * 1000f;
        if (_enemyAnimTimer >= 150f)
        {
            _enemyAnimTimer -= 150f;
            _enemyAnimFrame = (_enemyAnimFrame + 1) % 5;
        }

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
        DrawSubtitle(spriteBatch);
        DrawDividerLine(spriteBatch, _screenHeight * 0.28f);
        DrawShipShowcase(spriteBatch);
        DrawEnemyFormation(spriteBatch);
        DrawDividerLine(spriteBatch, _screenHeight * 0.595f);
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

    private void DrawSubtitle(SpriteBatch batch)
    {
        // Fade in after title reveal
        float subtitleDelay = TITLE_TEXT.Length * TITLE_LETTER_DELAY + 0.3f;
        float subtitleAlpha = MathHelper.Clamp((_titleIntroTimer - subtitleDelay) / 0.5f, 0f, 1f);
        if (subtitleAlpha <= 0) return;

        Vector2 titleSize = _fontLarge.MeasureString(TITLE_TEXT);
        float titleY = _screenHeight * 0.08f;

        Vector2 subtitleSize = _fontSmall.MeasureString(SUBTITLE_TEXT);
        Vector2 subtitlePos = new Vector2(
            (_screenWidth - subtitleSize.X) / 2f,
            titleY + titleSize.Y + 8
        );

        // Subtle slide up
        subtitlePos.Y += (1f - subtitleAlpha) * 10f;

        // Cyan with slight pulse
        float pulse = 0.7f + MathF.Sin(_timer * 2f) * 0.3f;
        Color subtitleColor = new Color(80, 200, 255) * (pulse * subtitleAlpha);

        batch.DrawString(_fontSmall, SUBTITLE_TEXT,
            subtitlePos + new Vector2(1, 1), Color.Black * (0.4f * subtitleAlpha));
        batch.DrawString(_fontSmall, SUBTITLE_TEXT, subtitlePos, subtitleColor);
    }

    // ═══════════════════════════════════════════════════════
    //  Ship showcase — cycles through all 3 player ships
    // ═══════════════════════════════════════════════════════

    private void UpdateShowcase(float dt)
    {
        _showcaseTimer += dt;

        if (_showcaseTimer >= SHOWCASE_DISPLAY_TIME)
        {
            // Start transition
            _showcaseTransition += dt / SHOWCASE_FADE_TIME;
            if (_showcaseTransition >= 1f)
            {
                // Switch ship
                _showcaseShipIndex = (_showcaseShipIndex + 1) % _playerShipTextures.Length;
                _showcaseTimer = 0f;
                _showcaseTransition = 0f;
            }
        }
        else
        {
            _showcaseTransition = 0f;
        }
    }

    private void DrawShipShowcase(SpriteBatch batch)
    {
        float showcaseDelay = TITLE_TEXT.Length * TITLE_LETTER_DELAY + 0.8f;
        float showcaseAlpha = MathHelper.Clamp((_titleIntroTimer - showcaseDelay) / 0.5f, 0f, 1f);
        if (showcaseAlpha <= 0) return;

        float centerX = _screenWidth / 2f;
        float shipY = _screenHeight * 0.38f;

        // Draw current ship
        int currentIdx = _showcaseShipIndex;
        var currentTex = _playerShipTextures[currentIdx];
        float currentAlpha = showcaseAlpha * (1f - _showcaseTransition);

        // Ship bob
        float bob = MathF.Sin(_timer * 1.8f) * 6f;
        // Thruster glow bob
        float thrusterBob = MathF.Sin(_timer * 8f) * 2f;

        // Ship name labels
        string[] shipNames = { "FIGHTER I", "FIGHTER II", "FIGHTER IX" };

        if (currentAlpha > 0.01f)
        {
            DrawShip(batch, currentTex, centerX, shipY + bob, currentAlpha);
            DrawThrusterGlow(batch, centerX, shipY + bob + thrusterBob, currentTex, currentAlpha);

            // Ship name under the ship
            string shipName = shipNames[currentIdx];
            Vector2 nameSize = _fontSmall.MeasureString(shipName);
            float nameY = shipY + 40f;
            batch.DrawString(_fontSmall, shipName,
                new Vector2(centerX - nameSize.X / 2f, nameY),
                new Color(200, 200, 200) * (0.7f * currentAlpha));
        }

        // Draw next ship fading in during transition
        if (_showcaseTransition > 0.01f)
        {
            int nextIdx = (currentIdx + 1) % _playerShipTextures.Length;
            var nextTex = _playerShipTextures[nextIdx];
            float nextAlpha = showcaseAlpha * _showcaseTransition;

            DrawShip(batch, nextTex, centerX, shipY + bob, nextAlpha);
            DrawThrusterGlow(batch, centerX, shipY + bob + thrusterBob, nextTex, nextAlpha);

            string nextName = shipNames[nextIdx];
            Vector2 nextNameSize = _fontSmall.MeasureString(nextName);
            float nextNameY = shipY + 40f;
            batch.DrawString(_fontSmall, nextName,
                new Vector2(centerX - nextNameSize.X / 2f, nextNameY),
                new Color(200, 200, 200) * (0.7f * nextAlpha));
        }

        // Ship indicator dots
        DrawShipIndicatorDots(batch, centerX, shipY + 58f, showcaseAlpha);
    }

    private void DrawShip(SpriteBatch batch, Texture2D tex, float x, float y, float alpha)
    {
        // Normalize to ~55 px height, preserving aspect ratio
        float scale = 55f / tex.Height;
        Vector2 origin = new Vector2(tex.Width / 2f, tex.Height / 2f);

        // Shadow
        batch.Draw(tex, new Vector2(x + 2, y + 2), null,
            Color.Black * (0.3f * alpha), 0f, origin, scale, SpriteEffects.None, 0f);

        // Ship
        batch.Draw(tex, new Vector2(x, y), null,
            Color.White * alpha, 0f, origin, scale, SpriteEffects.None, 0f);
    }

    private void DrawThrusterGlow(SpriteBatch batch, float x, float y, Texture2D shipTex, float alpha)
    {
        // Small glowing rectangle below the ship to simulate thruster
        float shipScale = 55f / shipTex.Height;
        float shipHalfH = shipTex.Height * shipScale / 2f;

        float glowIntensity = 0.3f + MathF.Sin(_timer * 12f) * 0.15f;
        float glowW = 8f + MathF.Sin(_timer * 6f) * 3f;
        float glowH = 6f + MathF.Sin(_timer * 10f) * 4f;

        batch.Draw(_pixelTexture,
            new Rectangle(
                (int)(x - glowW / 2f),
                (int)(y + shipHalfH - 2),
                (int)glowW,
                (int)glowH),
            new Color(80, 160, 255) * (glowIntensity * alpha));

        // Brighter core
        batch.Draw(_pixelTexture,
            new Rectangle(
                (int)(x - glowW / 4f),
                (int)(y + shipHalfH - 1),
                (int)(glowW / 2f),
                (int)(glowH * 0.6f)),
            Color.White * (glowIntensity * 0.6f * alpha));
    }

    private void DrawShipIndicatorDots(SpriteBatch batch, float x, float y, float alpha)
    {
        int count = _playerShipTextures.Length;
        float dotSpacing = 12f;
        float startX = x - (count - 1) * dotSpacing / 2f;

        for (int i = 0; i < count; i++)
        {
            bool isActive = (i == _showcaseShipIndex);
            float dotSize = isActive ? 4f : 2f;
            Color dotColor = isActive
                ? new Color(255, 255, 80) * alpha
                : new Color(100, 100, 120) * (0.5f * alpha);

            batch.Draw(_pixelTexture,
                new Rectangle(
                    (int)(startX + i * dotSpacing - dotSize / 2f),
                    (int)(y - dotSize / 2f),
                    (int)dotSize,
                    (int)dotSize),
                dotColor);
        }
    }

    // ═══════════════════════════════════════════════════════
    //  Enemy formation preview
    // ═══════════════════════════════════════════════════════

    private void DrawEnemyFormation(SpriteBatch batch)
    {
        float formationDelay = TITLE_TEXT.Length * TITLE_LETTER_DELAY + 1.2f;
        float formationAlpha = MathHelper.Clamp((_titleIntroTimer - formationDelay) / 0.6f, 0f, 1f);
        if (formationAlpha <= 0) return;

        float centerX = _screenWidth / 2f;
        float startY = _screenHeight * 0.49f;
        float spacing = 28f;

        // "ENEMY FORCES" label
        string label = "- ENEMY FORCES -";
        Vector2 labelSize = _fontSmall.MeasureString(label);
        batch.DrawString(_fontSmall, label,
            new Vector2(centerX - labelSize.X / 2f, startY - 16),
            new Color(255, 80, 80) * (0.6f * formationAlpha));

        // 3 rows of enemies, one type per row
        for (int row = 0; row < 3; row++)
        {
            int cols = 5 - row; // 5, 4, 3 per row
            float rowY = startY + 6 + row * spacing;
            float rowStartX = centerX - (cols - 1) * spacing / 2f;
            float rowScale = 1.2f + row * 0.15f;

            for (int col = 0; col < cols; col++)
            {
                float enemyX = rowStartX + col * spacing;

                // Individual enemy bob
                float bob = MathF.Sin(_timer * 2.5f + row * 0.8f + col * 0.4f) * 3f;
                float bobX = MathF.Cos(_timer * 1.8f + row + col * 0.6f) * 2f;

                _enemySheets[row].DrawFrame(
                    batch, _enemyAnimFrame,
                    new Vector2(enemyX + bobX, rowY + bob),
                    rowScale,
                    Color.White * formationAlpha);
            }
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

        Vector2 menuCenter = new Vector2(_screenWidth / 2f, _screenHeight * 0.72f);
        _menu.Draw(batch, _fontMedium, menuCenter);
    }

    // ═══════════════════════════════════════════════════════
    //  Decorative elements
    // ═══════════════════════════════════════════════════════

    private void DrawDividerLine(SpriteBatch batch, float y)
    {
        // Gradient horizontal line — fades at edges
        int lineW = (int)(_screenWidth * 0.7f);
        int startX = (_screenWidth - lineW) / 2;

        for (int i = 0; i < lineW; i++)
        {
            float t = (float)i / lineW;
            // Smooth fade at both ends
            float edgeFade = MathF.Min(t * 5f, 1f) * MathF.Min((1f - t) * 5f, 1f);
            float pulse = 0.6f + MathF.Sin(_timer * 1.5f + t * 3f) * 0.4f;

            batch.Draw(_pixelTexture,
                new Rectangle(startX + i, (int)y, 1, 1),
                new Color(80, 180, 255) * (DIVIDER_ALPHA * edgeFade * pulse));
        }
    }

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
        _showcaseTimer = 0f;
        _showcaseShipIndex = 0;
        _showcaseTransition = 0f;
        _pressStartBlink = 0f;
        _menu?.Reset();
    }

    public void OnExit() { }

    private void OnMenuSelect(int index)
    {
        switch (index)
        {
            case 0: // Start Game
                _stateManager.SwitchTo("gameplay");
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
