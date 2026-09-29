using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GalagaMidterm.Core;

public enum TransitionStyle
{
    Instant,
    FadeBlack,
    HorizontalWipe,
    VerticalWipe,
    SquareIris
}

/// <summary>
/// Manages the screen state machine with transition effects.
/// Supports push/pop (for overlays like Pause) and direct switch transitions.
/// </summary>
public class GameStateManager
{
    private readonly Dictionary<string, Screens.IScreen> _screens = new();
    private readonly Stack<string> _screenStack = new();
    private Texture2D _pixelTexture;

    // ── Transition state ───────────────────────────────────
    private enum TransitionPhase { None, FadeOut, FadeIn }
    private TransitionPhase _phase = TransitionPhase.None;
    private TransitionStyle _currentStyle;
    private float _transitionTimer;
    private float _transitionDuration = 0.4f; // seconds per half
    private float _transitionProgress; // 0.0 to 1.0 (1.0 is fully obscured)
    private string _pendingScreen;

    /// <summary>The currently active (topmost) screen name.</summary>
    public string CurrentScreenName => _screenStack.Count > 0 ? _screenStack.Peek() : null;

    /// <summary>The currently active screen instance.</summary>
    public Screens.IScreen CurrentScreen =>
        CurrentScreenName != null && _screens.ContainsKey(CurrentScreenName)
            ? _screens[CurrentScreenName]
            : null;

    /// <summary>True while a transition animation is playing.</summary>
    public bool IsTransitioning => _phase != TransitionPhase.None;

    public void Initialize(GraphicsDevice device)
    {
        _pixelTexture = new Texture2D(device, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    /// <summary>
    /// Registers a screen by name. Must be called before the screen can be activated.
    /// </summary>
    public void RegisterScreen(string name, Screens.IScreen screen)
    {
        _screens[name] = screen;
    }

    /// <summary>
    /// Switches to a new screen with a visual transition, replacing the current screen.
    /// </summary>
    public void SwitchTo(string screenName, TransitionStyle style = TransitionStyle.FadeBlack, float transitionDuration = 0.4f)
    {
        if (_phase != TransitionPhase.None) return;

        _pendingScreen = screenName;
        _currentStyle = style;
        _transitionDuration = transitionDuration;

        if (_screenStack.Count == 0 || style == TransitionStyle.Instant)
        {
            // No current screen or instant transition — go directly
            PerformSwitch();
            _phase = TransitionPhase.None;
        }
        else
        {
            // Start fade-out
            _phase = TransitionPhase.FadeOut;
            _transitionTimer = 0f;
            _transitionProgress = 0f;
        }
    }

    /// <summary>
    /// Retrieves a registered screen by name, allowing pre-configuration before transitioning.
    /// </summary>
    public Screens.IScreen GetScreen(string name)
    {
        return _screens.TryGetValue(name, out var screen) ? screen : null;
    }

    /// <summary>
    /// Pushes a screen on top (e.g. pause overlay). Uses a fast darken, no full fade.
    /// </summary>
    public void PushScreen(string screenName)
    {
        if (!_screens.ContainsKey(screenName)) return;

        _screenStack.Push(screenName);
        _screens[screenName].OnEnter();
    }

    /// <summary>
    /// Pops the topmost screen (e.g. un-pausing).
    /// </summary>
    public void PopScreen()
    {
        if (_screenStack.Count <= 1) return; // Don't pop the last screen

        var popped = _screenStack.Pop();
        _screens[popped].OnExit();
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Handle transition
        if (_phase != TransitionPhase.None)
        {
            _transitionTimer += dt;
            float rawProgress = MathHelper.Clamp(_transitionTimer / _transitionDuration, 0f, 1f);

            switch (_phase)
            {
                case TransitionPhase.FadeOut:
                    _transitionProgress = rawProgress;
                    if (rawProgress >= 1f)
                    {
                        PerformSwitch();
                        _phase = TransitionPhase.FadeIn;
                        _transitionTimer = 0f;
                    }
                    break;

                case TransitionPhase.FadeIn:
                    _transitionProgress = 1f - rawProgress;
                    if (rawProgress >= 1f)
                    {
                        _phase = TransitionPhase.None;
                        _transitionProgress = 0f;
                    }
                    break;
            }
        }

        // Update the current screen
        CurrentScreen?.Update(gameTime);
    }

    public void Draw(SpriteBatch spriteBatch, int screenWidth, int screenHeight)
    {
        // Draw the current screen
        CurrentScreen?.Draw(spriteBatch);

        // Draw transition overlay
        if (_phase != TransitionPhase.None && _transitionProgress > 0.001f)
        {
            spriteBatch.Begin();
            DrawTransitionEffect(spriteBatch, screenWidth, screenHeight);
            spriteBatch.End();
        }
    }

    private void DrawTransitionEffect(SpriteBatch spriteBatch, int width, int height)
    {
        switch (_currentStyle)
        {
            case TransitionStyle.FadeBlack:
                spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, width, height), Color.Black * _transitionProgress);
                break;

            case TransitionStyle.HorizontalWipe:
                int wipeW = (int)(width / 2f * _transitionProgress);
                // Left curtain
                spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, wipeW, height), Color.Black);
                // Right curtain
                spriteBatch.Draw(_pixelTexture, new Rectangle(width - wipeW, 0, wipeW, height), Color.Black);
                break;

            case TransitionStyle.VerticalWipe:
                int wipeH = (int)(height / 2f * _transitionProgress);
                // Top curtain
                spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, width, wipeH), Color.Black);
                // Bottom curtain
                spriteBatch.Draw(_pixelTexture, new Rectangle(0, height - wipeH, width, wipeH), Color.Black);
                break;

            case TransitionStyle.SquareIris:
                float maxRadius = MathF.Max(width, height) / 2f;
                float currentHoleRadius = maxRadius * (1f - _transitionProgress);
                
                int rectSize = (int)(maxRadius * 2);
                int cx = width / 2;
                int cy = height / 2;
                int ir = (int)currentHoleRadius;

                // 4 rects forming a square hole
                // Top
                spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, width, cy - ir), Color.Black);
                // Bottom
                spriteBatch.Draw(_pixelTexture, new Rectangle(0, cy + ir, width, height - (cy + ir)), Color.Black);
                // Left
                spriteBatch.Draw(_pixelTexture, new Rectangle(0, cy - ir, cx - ir, ir * 2), Color.Black);
                // Right
                spriteBatch.Draw(_pixelTexture, new Rectangle(cx + ir, cy - ir, width - (cx + ir), ir * 2), Color.Black);
                break;
        }
    }

    private void PerformSwitch()
    {
        // Exit all current screens
        while (_screenStack.Count > 0)
        {
            var name = _screenStack.Pop();
            _screens[name].OnExit();
        }

        // Enter new screen
        if (_screens.ContainsKey(_pendingScreen))
        {
            _screenStack.Push(_pendingScreen);
            _screens[_pendingScreen].OnEnter();
        }
    }
}
