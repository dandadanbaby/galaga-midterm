using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GalagaMidterm.Assets;
using GalagaMidterm.Core;
using GalagaMidterm.UI;

namespace GalagaMidterm.Screens;

/// <summary>
/// Victory screen shown when the player beats all 5 levels and defeats the boss.
/// Displays final score, congratulations, and options to play again or return to menu.
/// </summary>
public class VictoryScreen : IScreen
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
    private float _autoReturnTimer = 20f;

    // ── Score display (set before transition) ──────────────
    public int FinalScore { get; set; }
    public int HighScore { get; set; }

    public VictoryScreen(InputManager input, GameStateManager stateManager)
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

        _menu = new MenuComponent(new List<string> { "PLAY AGAIN", "MAIN MENU" });
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

        // Allow input after delay
        if (_timer > 2.0f)
        {
            _menu.Update(gameTime, _input);
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        // ── Dark background with a subtle green tint ───────
        spriteBatch.Draw(_pixelTexture,
            new Rectangle(0, 0, _screenWidth, _screenHeight),
            new Color(5, 15, 10));

        float centerX = _screenWidth / 2f;

        // ── "VICTORY!" ─────────────────────────────────────
        string victoryText = "VICTORY!";
        Vector2 vSize = _fontLarge.MeasureString(victoryText);
        Vector2 vPos = new Vector2(centerX - vSize.X / 2f, _screenHeight * 0.15f);

        float pulse = 0.7f + MathF.Sin(_timer * 2f) * 0.3f;
        Color victoryColor = Color.Lerp(new Color(120, 255, 140), Color.White, pulse * 0.4f);

        // Shadow
        spriteBatch.DrawString(_fontLarge, victoryText,
            vPos + new Vector2(3, 3), new Color(0, 80, 20) * 0.5f);
        // Main text
        spriteBatch.DrawString(_fontLarge, victoryText,
            vPos, victoryColor);

        // ── Congratulations message ────────────────────────
        float y = _screenHeight * 0.38f;

        DrawCenteredText(spriteBatch, _fontSmall,
            "You cleared the asteroid field!",
            centerX, y, Color.LightGray);
        y += 28;

        DrawCenteredText(spriteBatch, _fontSmall,
            "The Mother Asteroid is destroyed.",
            centerX, y, Color.LightGray);
        y += 42;

        // ── Score display ──────────────────────────────────
        DrawCenteredText(spriteBatch, _fontMedium, "FINAL SCORE",
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

        // ── All stages cleared ─────────────────────────────
        DrawCenteredText(spriteBatch, _fontSmall, "ALL 5 STAGES CLEARED",
            centerX, y, new Color(100, 220, 255));

        // ── Menu ───────────────────────────────────────────
        if (_timer > 2.0f)
        {
            _menu.Draw(spriteBatch, _fontSmall, new Vector2(centerX, _screenHeight * 0.85f));
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
        _autoReturnTimer = 20f;
        _menu?.Reset();
    }

    public void OnExit() { }

    private void OnMenuSelect(int index)
    {
        switch (index)
        {
            case 0: // Play Again
                _stateManager.SwitchTo("gameplay", TransitionStyle.SquareIris, 0.6f);
                break;
            case 1: // Main Menu
                _stateManager.SwitchTo("mainmenu", TransitionStyle.VerticalWipe, 0.6f);
                break;
        }
    }
}
