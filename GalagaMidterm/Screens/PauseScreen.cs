using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GalagaMidterm.Assets;
using GalagaMidterm.Core;
using GalagaMidterm.UI;

namespace GalagaMidterm.Screens;

/// <summary>
/// Semi-transparent pause overlay rendered on top of the frozen gameplay screen.
/// Shows "PAUSED" text and resume/quit options.
/// </summary>
public class PauseScreen : IScreen
{
    private readonly InputManager _input;
    private readonly GameStateManager _stateManager;

    private SpriteFont _fontLarge;
    private SpriteFont _fontMedium;
    private Texture2D _pixelTexture;
    private MenuComponent _menu;

    private int _screenWidth;
    private int _screenHeight;
    private float _timer;

    public PauseScreen(InputManager input, GameStateManager stateManager)
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

        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });

        _menu = new MenuComponent(new List<string> { "RESUME", "RESTART", "MAIN MENU" });
        _menu.OnSelect += OnMenuSelect;
    }

    public void Update(GameTime gameTime)
    {
        _timer += (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Quick unpause with Escape
        if (_input.Pause)
        {
            _stateManager.PopScreen();
            return;
        }

        _menu.Update(gameTime, _input);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        // ── Dark overlay ───────────────────────────────────
        spriteBatch.Draw(_pixelTexture,
            new Rectangle(0, 0, _screenWidth, _screenHeight),
            Color.Black * 0.65f);

        // ── "PAUSED" title ─────────────────────────────────
        string pauseText = "PAUSED";
        Vector2 pauseSize = _fontLarge.MeasureString(pauseText);
        Vector2 pausePos = new Vector2(
            (_screenWidth - pauseSize.X) / 2f,
            _screenHeight * 0.3f
        );

        // Pulsing glow
        float pulse = 0.7f + MathF.Sin(_timer * 3f) * 0.3f;

        // Shadow
        spriteBatch.DrawString(_fontLarge, pauseText,
            pausePos + new Vector2(3, 3), Color.Black * 0.5f);

        // Main text
        spriteBatch.DrawString(_fontLarge, pauseText,
            pausePos, Color.White * pulse);

        // ── Menu ───────────────────────────────────────────
        _menu.Draw(spriteBatch, _fontMedium,
            new Vector2(_screenWidth / 2f, _screenHeight * 0.55f));

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
            case 0: // Resume
                _stateManager.PopScreen();
                break;
            case 1: // Restart (Mock)
                _stateManager.PopScreen(); // Remove pause overlay
                _stateManager.SwitchTo("gameplay", TransitionStyle.SquareIris, 0.6f);
                break;
            case 2: // Quit to Menu
                _stateManager.PopScreen(); // Remove pause overlay
                _stateManager.SwitchTo("mainmenu", TransitionStyle.HorizontalWipe, 0.5f);
                break;
        }
    }
}
