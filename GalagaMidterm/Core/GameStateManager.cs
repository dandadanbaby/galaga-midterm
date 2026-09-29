using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GalagaMidterm.Core;

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
    private float _transitionTimer;
    private float _transitionDuration = 0.4f; // seconds per half
    private float _transitionAlpha;
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

    /// <summary>Current fade overlay alpha (0 = transparent, 1 = fully black).</summary>
    public float TransitionAlpha => _transitionAlpha;

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
    /// Switches to a new screen with a fade transition, replacing the current screen.
    /// </summary>
    public void SwitchTo(string screenName, float transitionDuration = 0.4f)
    {
        if (_phase != TransitionPhase.None) return;

        _pendingScreen = screenName;
        _transitionDuration = transitionDuration;

        if (_screenStack.Count == 0)
        {
            // No current screen — go directly
            PerformSwitch();
        }
        else
        {
            // Start fade-out
            _phase = TransitionPhase.FadeOut;
            _transitionTimer = 0f;
            _transitionAlpha = 0f;
        }
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
            float progress = MathHelper.Clamp(_transitionTimer / _transitionDuration, 0f, 1f);

            switch (_phase)
            {
                case TransitionPhase.FadeOut:
                    _transitionAlpha = progress;
                    if (progress >= 1f)
                    {
                        PerformSwitch();
                        _phase = TransitionPhase.FadeIn;
                        _transitionTimer = 0f;
                    }
                    break;

                case TransitionPhase.FadeIn:
                    _transitionAlpha = 1f - progress;
                    if (progress >= 1f)
                    {
                        _phase = TransitionPhase.None;
                        _transitionAlpha = 0f;
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
        if (_transitionAlpha > 0.001f)
        {
            spriteBatch.Begin();
            spriteBatch.Draw(
                _pixelTexture,
                new Rectangle(0, 0, screenWidth, screenHeight),
                Color.Black * _transitionAlpha
            );
            spriteBatch.End();
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
