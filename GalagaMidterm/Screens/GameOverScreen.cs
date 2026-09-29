using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GalagaMidterm.Assets;
using GalagaMidterm.Core;
using GalagaMidterm.UI;

namespace GalagaMidterm.Screens;

/// <summary>
/// Game over screen with large "GAME OVER" text, final score display,
/// and interactive options to restart or return to menu.
/// Automatically returns to the main menu after a timeout.
/// </summary>
public class GameOverScreen : IScreen
{
    private readonly InputManager _input;
    private readonly GameStateManager _stateManager;

    private SpriteFont _fontLarge;
    private SpriteFont _fontMedium;
    private SpriteFont _fontSmall;
    private Texture2D _pixelTexture;
    private MenuComponent _menu;

    private int _screenWidth;
    private int _screenHeight;
    private float _timer;
    private float _autoReturnTimer = 15f; // Return to menu after 15 seconds

    // ── Score display (set by the gameplay screen before transition) ──
    public int FinalScore { get; set; }
    public int HighScore { get; set; }
    public int StageReached { get; set; }

    public GameOverScreen(InputManager input, GameStateManager stateManager)
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

        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });

        _menu = new MenuComponent(new List<string> { "RESTART", "MAIN MENU" });
        _menu.LineSpacing = 35f;
        _menu.OnSelect += OnMenuSelect;
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _timer += dt;

        // Auto-return to menu
        _autoReturnTimer -= dt;
        if (_autoReturnTimer <= 0)
        {
            _stateManager.SwitchTo("mainmenu", TransitionStyle.FadeBlack, 0.8f);
            return;
        }

        // Allow input after a short delay (prevent accidental skip)
        if (_timer > 1.5f)
        {
            _menu.Update(gameTime, _input);
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        // ── Dark background ────────────────────────────────
        spriteBatch.Draw(_pixelTexture,
            new Rectangle(0, 0, _screenWidth, _screenHeight),
            new Color(10, 5, 15));

        // ── "GAME OVER" ────────────────────────────────────
        string gameOverText = "GAME OVER";
        Vector2 goSize = _fontLarge.MeasureString(gameOverText);
        Vector2 goPos = new Vector2((_screenWidth - goSize.X) / 2f, _screenHeight * 0.2f);

        // Red glow pulse
        float pulse = 0.7f + MathF.Sin(_timer * 2f) * 0.3f;

        // Shadow
        spriteBatch.DrawString(_fontLarge, gameOverText,
            goPos + new Vector2(3, 3), Color.DarkRed * 0.5f);
        // Main text
        spriteBatch.DrawString(_fontLarge, gameOverText,
            goPos, Color.Red * pulse);

        // ── Score summary ──────────────────────────────────
        float centerX = _screenWidth / 2f;
        float y = _screenHeight * 0.42f;

        DrawCenteredText(spriteBatch, _fontSmall, $"STAGE REACHED: {StageReached}",
            centerX, y, new Color(100, 220, 255));
        y += 32;

        DrawCenteredText(spriteBatch, _fontMedium, $"SCORE",
            centerX, y, Color.Gray);
        y += 28;

        DrawCenteredText(spriteBatch, _fontMedium, FinalScore.ToString("D6"),
            centerX, y, Color.Yellow);
        y += 40;

        if (FinalScore >= HighScore && FinalScore > 0)
        {
            float newHighPulse = MathF.Abs(MathF.Sin(_timer * 4f));
            DrawCenteredText(spriteBatch, _fontSmall, "NEW HIGH SCORE!",
                centerX, y, Color.Lerp(Color.Yellow, Color.White, newHighPulse));
            y += 32;
        }
        else
        {
            DrawCenteredText(spriteBatch, _fontSmall, $"HIGH SCORE: {HighScore:D6}",
                centerX, y, Color.Gray * 0.6f);
            y += 32;
        }

        // ── Interactive Menu ───────────────────────────────
        if (_timer > 1.5f)
        {
            _menu.Draw(spriteBatch, _fontSmall, new Vector2(centerX, _screenHeight * 0.82f));
        }

        spriteBatch.End();
    }

    private void DrawCenteredText(SpriteBatch batch, SpriteFont font, string text,
        float centerX, float y, Color color)
    {
        Vector2 size = font.MeasureString(text);
        batch.DrawString(font, text,
            new Vector2(centerX - size.X / 2f, y), color);
    }

    public void OnEnter()
    {
        _timer = 0f;
        _autoReturnTimer = 15f;
        _menu?.Reset();
    }

    public void OnExit() { }

    private void OnMenuSelect(int index)
    {
        switch (index)
        {
            case 0: // Restart
                _stateManager.SwitchTo("gameplay", TransitionStyle.SquareIris, 0.6f);
                break;
            case 1: // Main Menu
                _stateManager.SwitchTo("mainmenu", TransitionStyle.VerticalWipe, 0.6f);
                break;
        }
    }
}
