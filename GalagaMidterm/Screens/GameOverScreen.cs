using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GalagaMidterm.Assets;
using GalagaMidterm.Core;

namespace GalagaMidterm.Screens;

/// <summary>
/// Game over screen with large "GAME OVER" text, final score display,
/// and blinking "PRESS ENTER TO CONTINUE" prompt.
/// Automatically returns to the main menu after a timeout or on input.
/// </summary>
public class GameOverScreen : IScreen
{
    private readonly InputManager _input;
    private readonly GameStateManager _stateManager;

    private SpriteFont _fontLarge;
    private SpriteFont _fontMedium;
    private SpriteFont _fontSmall;
    private Texture2D _pixelTexture;

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
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _timer += dt;

        // Allow input after a short delay (prevent accidental skip)
        if (_timer > 1.5f)
        {
            if (_input.MenuConfirm || _input.Pause)
            {
                _stateManager.SwitchTo("mainmenu");
                return;
            }
        }

        // Auto-return to menu
        _autoReturnTimer -= dt;
        if (_autoReturnTimer <= 0)
        {
            _stateManager.SwitchTo("mainmenu");
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

        // ── "PRESS ENTER TO CONTINUE" blinking ─────────────
        if (_timer > 1.5f)
        {
            float blink = MathF.Sin(_timer * 3f) > 0 ? 1f : 0.2f;
            DrawCenteredText(spriteBatch, _fontSmall, "PRESS ENTER TO CONTINUE",
                centerX, _screenHeight * 0.82f, Color.White * blink);
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
    }

    public void OnExit() { }
}
