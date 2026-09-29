using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GalagaMidterm.Assets;
using GalagaMidterm.Core;

namespace GalagaMidterm.Screens;

/// <summary>
/// Semi-transparent overlay showing game controls and instructions.
/// Pushed on top of the main menu and dismissed with Escape or Enter.
/// Styled as a classic arcade "HOW TO PLAY" screen.
/// </summary>
public class ControlsScreen : IScreen
{
    private readonly InputManager _input;
    private readonly GameStateManager _stateManager;

    private SpriteFont _fontLarge;
    private SpriteFont _fontMedium;
    private SpriteFont _fontSmall;
    private Texture2D _pixelTexture;
    private Texture2D _playerShipTexture;

    private int _screenWidth;
    private int _screenHeight;
    private float _timer;
    private float _fadeIn;

    public ControlsScreen(InputManager input, GameStateManager stateManager)
    {
        _input = input;
        _stateManager = stateManager;
    }

    public void LoadContent(AssetLoader assets, GraphicsDevice graphicsDevice)
    {
        _screenWidth = graphicsDevice.Viewport.Width;
        _screenHeight = graphicsDevice.Viewport.Height;

        _fontLarge = assets.GetFont(AssetPaths.FontLarge);
        _fontMedium = assets.GetFont(AssetPaths.FontMedium);
        _fontSmall = assets.GetFont(AssetPaths.FontSmall);
        _playerShipTexture = assets.GetTexture(AssetPaths.PlayerShip1);

        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _timer += dt;
        _fadeIn = MathHelper.Clamp(_fadeIn + dt * 4f, 0f, 1f);

        // Dismiss after a short input delay
        if (_timer > 0.3f)
        {
            if (_input.MenuBack || _input.MenuConfirm || _input.Pause)
            {
                _stateManager.PopScreen();
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        float alpha = _fadeIn;

        // ── Dark overlay ───────────────────────────────────
        spriteBatch.Draw(_pixelTexture,
            new Rectangle(0, 0, _screenWidth, _screenHeight),
            Color.Black * (0.82f * alpha));

        // ── Border frame ───────────────────────────────────
        DrawBorderFrame(spriteBatch, alpha);

        // ── Title ──────────────────────────────────────────
        float titlePulse = 0.8f + MathF.Sin(_timer * 2.5f) * 0.2f;
        DrawCenteredText(spriteBatch, _fontLarge, "CONTROLS",
            _screenHeight * 0.1f, new Color(80, 200, 255) * (titlePulse * alpha));

        // ── Ship display ───────────────────────────────────
        float shipScale = 45f / _playerShipTexture.Height;
        Vector2 shipOrigin = new Vector2(_playerShipTexture.Width / 2f, _playerShipTexture.Height / 2f);
        float shipBob = MathF.Sin(_timer * 2f) * 4f;
        spriteBatch.Draw(_playerShipTexture,
            new Vector2(_screenWidth / 2f, _screenHeight * 0.22f + shipBob),
            null, Color.White * alpha, 0f, shipOrigin, shipScale, SpriteEffects.None, 0f);

        // ── Control entries ────────────────────────────────
        float y = _screenHeight * 0.31f;
        float lineH = 28f;
        float sectionGap = 14f;

        // Movement section
        DrawSectionHeader(spriteBatch, "MOVEMENT", y, alpha);
        y += lineH + 2;
        DrawControlEntry(spriteBatch, "LEFT / RIGHT", "A / D  OR  ARROWS", y, alpha);
        y += lineH;
        DrawControlEntry(spriteBatch, "", "L-STICK / D-PAD", y, alpha);
        y += lineH + sectionGap;

        // Combat section
        DrawSectionHeader(spriteBatch, "COMBAT", y, alpha);
        y += lineH + 2;
        DrawControlEntry(spriteBatch, "FIRE", "SPACE  OR  Z", y, alpha);
        y += lineH;
        DrawControlEntry(spriteBatch, "", "GAMEPAD A", y, alpha);
        y += lineH + sectionGap;

        // System section
        DrawSectionHeader(spriteBatch, "SYSTEM", y, alpha);
        y += lineH + 2;
        DrawControlEntry(spriteBatch, "PAUSE", "ESC / P / START", y, alpha);
        y += lineH;
        DrawControlEntry(spriteBatch, "MENU NAV", "ARROWS  +  ENTER", y, alpha);
        y += lineH + sectionGap + 6;

        // ── Divider ────────────────────────────────────────
        DrawHorizontalDivider(spriteBatch, y, alpha);
        y += 14;

        // ── Objective ──────────────────────────────────────
        DrawCenteredText(spriteBatch, _fontSmall, "OBJECTIVE",
            y, new Color(255, 255, 80) * (0.8f * alpha));
        y += lineH;
        DrawCenteredText(spriteBatch, _fontSmall, "DESTROY ALL ENEMIES",
            y, new Color(200, 200, 210) * (0.7f * alpha));
        y += lineH - 4;
        DrawCenteredText(spriteBatch, _fontSmall, "SURVIVE EACH STAGE",
            y, new Color(200, 200, 210) * (0.7f * alpha));

        // ── Back prompt ────────────────────────────────────
        float blink = MathF.Sin(_timer * 3f) > 0 ? 1f : 0.3f;
        DrawCenteredText(spriteBatch, _fontSmall, "PRESS ENTER OR ESC TO RETURN",
            _screenHeight * 0.91f, new Color(180, 180, 200) * (blink * alpha));

        spriteBatch.End();
    }

    // ── Drawing helpers ────────────────────────────────────

    private void DrawCenteredText(SpriteBatch batch, SpriteFont font, string text,
        float y, Color color)
    {
        Vector2 size = font.MeasureString(text);

        // Shadow
        batch.DrawString(font, text,
            new Vector2((_screenWidth - size.X) / 2f + 1, y + 1),
            Color.Black * (color.A / 255f * 0.4f));

        batch.DrawString(font, text,
            new Vector2((_screenWidth - size.X) / 2f, y), color);
    }

    private void DrawSectionHeader(SpriteBatch batch, string text, float y, float alpha)
    {
        Vector2 size = _fontSmall.MeasureString(text);
        float x = (_screenWidth - size.X) / 2f;

        // Decorative dashes
        string decorated = "-- " + text + " --";
        Vector2 decSize = _fontSmall.MeasureString(decorated);

        batch.DrawString(_fontSmall, decorated,
            new Vector2((_screenWidth - decSize.X) / 2f, y),
            new Color(255, 200, 80) * (0.7f * alpha));
    }

    private void DrawControlEntry(SpriteBatch batch, string action, string keys, float y, float alpha)
    {
        float centerX = _screenWidth / 2f;

        if (!string.IsNullOrEmpty(action))
        {
            // Action label on the left
            Vector2 actionSize = _fontSmall.MeasureString(action);
            batch.DrawString(_fontSmall, action,
                new Vector2(centerX - actionSize.X - 12, y),
                new Color(100, 220, 255) * (0.8f * alpha));
        }

        // Keys on the right
        batch.DrawString(_fontSmall, keys,
            new Vector2(centerX + 12, y),
            new Color(200, 200, 210) * (0.7f * alpha));
    }

    private void DrawHorizontalDivider(SpriteBatch batch, float y, float alpha)
    {
        int lineW = (int)(_screenWidth * 0.6f);
        int startX = (_screenWidth - lineW) / 2;

        batch.Draw(_pixelTexture,
            new Rectangle(startX, (int)y, lineW, 1),
            new Color(80, 180, 255) * (0.2f * alpha));
    }

    private void DrawBorderFrame(SpriteBatch batch, float alpha)
    {
        int pad = 16;
        int thick = 2;
        Color borderColor = new Color(60, 140, 220) * (0.25f * alpha);

        // Top
        batch.Draw(_pixelTexture,
            new Rectangle(pad, pad, _screenWidth - pad * 2, thick), borderColor);
        // Bottom
        batch.Draw(_pixelTexture,
            new Rectangle(pad, _screenHeight - pad, _screenWidth - pad * 2, thick), borderColor);
        // Left
        batch.Draw(_pixelTexture,
            new Rectangle(pad, pad, thick, _screenHeight - pad * 2), borderColor);
        // Right
        batch.Draw(_pixelTexture,
            new Rectangle(_screenWidth - pad - thick, pad, thick, _screenHeight - pad * 2), borderColor);

        // Corner accents (small bright squares)
        Color cornerColor = new Color(80, 200, 255) * (0.4f * alpha);
        int cornerSize = 6;
        batch.Draw(_pixelTexture, new Rectangle(pad - 1, pad - 1, cornerSize, cornerSize), cornerColor);
        batch.Draw(_pixelTexture, new Rectangle(_screenWidth - pad - cornerSize + 1, pad - 1, cornerSize, cornerSize), cornerColor);
        batch.Draw(_pixelTexture, new Rectangle(pad - 1, _screenHeight - pad - cornerSize + 1, cornerSize, cornerSize), cornerColor);
        batch.Draw(_pixelTexture, new Rectangle(_screenWidth - pad - cornerSize + 1, _screenHeight - pad - cornerSize + 1, cornerSize, cornerSize), cornerColor);
    }

    public void OnEnter()
    {
        _timer = 0f;
        _fadeIn = 0f;
    }

    public void OnExit() { }
}
