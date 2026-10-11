using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GalagaMidterm.Assets;
using GalagaMidterm.Core;

namespace GalagaMidterm.Screens;

/// <summary>
/// Displays "LEVEL X" intro text with a countdown before gameplay starts.
/// For the boss level (5), shows a "BOSS INCOMING!" warning.
/// Rendered as a transparent overlay so the game world is visible behind it.
/// </summary>
public class IntroScreen : IScreen
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
    private int _level;
    private bool _isBossLevel;

    /// <summary>
    /// Set these before transitioning to this screen.
    /// </summary>
    public int Level { get => _level; set { _level = value; _isBossLevel = value >= 5; } }

    public IntroScreen(InputManager input, GameStateManager stateManager)
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

        // Auto-transition to gameplay after 2.5 seconds
        if (_timer >= 2.5f)
        {
            _stateManager.SwitchTo("gameplay", TransitionStyle.FadeBlack, 0.3f);
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        // ── Dark semi-transparent background ───────────────
        spriteBatch.Draw(_pixelTexture,
            new Rectangle(0, 0, _screenWidth, _screenHeight),
            Color.Black * 0.7f);

        float centerX = _screenWidth / 2f;

        // ── "LEVEL X" text ─────────────────────────────────
        string levelText = $"LEVEL {_level}";
        Vector2 levelSize = _fontLarge.MeasureString(levelText);
        Vector2 levelPos = new Vector2(centerX - levelSize.X / 2f, _screenHeight * 0.35f);

        // Fade-in animation
        float alpha = MathHelper.Clamp(_timer / 0.5f, 0f, 1f);

        // Scale-in animation
        float scale = 0.8f + MathHelper.Clamp(_timer / 0.3f, 0f, 1f) * 0.2f;

        // Shadow
        spriteBatch.DrawString(_fontLarge, levelText,
            levelPos + new Vector2(3, 3), Color.Black * 0.5f * alpha,
            0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        // Main text
        spriteBatch.DrawString(_fontLarge, levelText,
            levelPos, Color.White * alpha,
            0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

        // ── Sub-text ───────────────────────────────────────
        float subAlpha = MathHelper.Clamp((_timer - 0.5f) / 0.5f, 0f, 1f);
        if (_isBossLevel)
        {
            string bossText = "BOSS INCOMING!";
            Vector2 bossSize = _fontMedium.MeasureString(bossText);
            float pulse = 0.7f + MathF.Sin(_timer * 4f) * 0.3f;
            spriteBatch.DrawString(_fontMedium, bossText,
                new Vector2(centerX - bossSize.X / 2f, _screenHeight * 0.5f),
                new Color(255, 90, 70) * subAlpha * pulse);
        }
        else
        {
            string readyText = "Get ready...";
            Vector2 readySize = _fontMedium.MeasureString(readyText);
            spriteBatch.DrawString(_fontMedium, readyText,
                new Vector2(centerX - readySize.X / 2f, _screenHeight * 0.5f),
                Color.LightGray * subAlpha);
        }

        // ── Level/Max indicator ────────────────────────────
        string stageInfo = $"STAGE {_level} / 5";
        Vector2 stageSize = _fontSmall.MeasureString(stageInfo);
        spriteBatch.DrawString(_fontSmall, stageInfo,
            new Vector2(centerX - stageSize.X / 2f, _screenHeight * 0.62f),
            new Color(100, 220, 255) * subAlpha);

        spriteBatch.End();
    }

    public void OnEnter()
    {
        _timer = 0f;
    }

    public void OnExit() { }
}
