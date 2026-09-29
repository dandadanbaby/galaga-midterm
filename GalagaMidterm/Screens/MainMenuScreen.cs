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
/// Title / main menu screen with scrolling starfield background,
/// title text, and menu options. Classic arcade presentation.
/// </summary>
public class MainMenuScreen : IScreen
{
    private readonly InputManager _input;
    private readonly GameStateManager _stateManager;

    private SpriteFont _fontLarge;
    private SpriteFont _fontMedium;
    private SpriteFont _fontSmall;
    private Texture2D _playerShipTexture;
    private ParallaxBackground _background;
    private MenuComponent _menu;

    private int _screenWidth;
    private int _screenHeight;
    private float _timer;
    private float _titleGlow;
    private float _shipBob;

    // ── Title animation ────────────────────────────────────
    private const string TITLE_TEXT = "GALAGA";
    private const string SUBTITLE_TEXT = "MIDTERM EDITION";

    public MainMenuScreen(InputManager input, GameStateManager stateManager)
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

        _background = new ParallaxBackground();
        _background.LoadContent(assets, _screenWidth, _screenHeight);

        _menu = new MenuComponent(new List<string> { "START GAME", "QUIT" });
        _menu.OnSelect += OnMenuSelect;
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _timer += dt;

        _background.Update(gameTime);
        _menu.Update(gameTime, _input);

        // Title glow pulse
        _titleGlow = 0.6f + MathF.Sin(_timer * 2.5f) * 0.4f;

        // Ship bob animation
        _shipBob = MathF.Sin(_timer * 1.8f) * 8f;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // ── Background ─────────────────────────────────────
        spriteBatch.Begin(samplerState: SamplerState.PointWrap);
        _background.Draw(spriteBatch);
        spriteBatch.End();

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        // ── Title text ─────────────────────────────────────
        Vector2 titleSize = _fontLarge.MeasureString(TITLE_TEXT);
        Vector2 titlePos = new Vector2(
            (_screenWidth - titleSize.X) / 2f,
            _screenHeight * 0.15f
        );

        // Title shadow
        spriteBatch.DrawString(_fontLarge, TITLE_TEXT,
            titlePos + new Vector2(3, 3), Color.Black * 0.6f);

        // Title with glow
        Color titleColor = Color.Lerp(new Color(255, 255, 80), Color.White, _titleGlow * 0.3f);
        spriteBatch.DrawString(_fontLarge, TITLE_TEXT, titlePos, titleColor);

        // Subtitle
        Vector2 subtitleSize = _fontSmall.MeasureString(SUBTITLE_TEXT);
        Vector2 subtitlePos = new Vector2(
            (_screenWidth - subtitleSize.X) / 2f,
            titlePos.Y + titleSize.Y + 12
        );
        spriteBatch.DrawString(_fontSmall, SUBTITLE_TEXT,
            subtitlePos, new Color(100, 220, 255) * 0.8f);

        // ── Player ship showcase ───────────────────────────
        float shipScale = 0.22f;
        Vector2 shipPos = new Vector2(
            _screenWidth / 2f,
            _screenHeight * 0.42f + _shipBob
        );
        Vector2 shipOrigin = new Vector2(
            _playerShipTexture.Width / 2f,
            _playerShipTexture.Height / 2f
        );
        spriteBatch.Draw(_playerShipTexture, shipPos, null, Color.White,
            0f, shipOrigin, shipScale, SpriteEffects.None, 0f);

        // ── Menu ───────────────────────────────────────────
        _menu.Draw(spriteBatch, _fontMedium,
            new Vector2(_screenWidth / 2f, _screenHeight * 0.68f));

        // ── Footer ─────────────────────────────────────────
        string footer = "2026 MIDTERM PROJECT";
        Vector2 footerSize = _fontSmall.MeasureString(footer);
        float footerAlpha = 0.3f + MathF.Sin(_timer * 1.2f) * 0.1f;
        spriteBatch.DrawString(_fontSmall, footer,
            new Vector2((_screenWidth - footerSize.X) / 2f, _screenHeight - 30),
            Color.Gray * footerAlpha);

        spriteBatch.End();
    }

    public void OnEnter()
    {
        _timer = 0f;
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
            case 1: // Quit
                Environment.Exit(0);
                break;
        }
    }
}
