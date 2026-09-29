using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace GalagaMidterm.Core;

/// <summary>
/// Abstracts keyboard and gamepad input into game actions.
/// Tracks both current and previous frame states to detect press edges.
/// Implements IPlayerInput for future backend integration.
/// </summary>
public class InputManager : IPlayerInput
{
    private KeyboardState _currentKeyboard;
    private KeyboardState _previousKeyboard;
    private GamePadState _currentGamePad;
    private GamePadState _previousGamePad;

    // ── IPlayerInput (for backend integration) ─────────────
    public bool MoveLeft =>
        _currentKeyboard.IsKeyDown(Keys.Left) || _currentKeyboard.IsKeyDown(Keys.A) ||
        _currentGamePad.ThumbSticks.Left.X < -0.3f || _currentGamePad.DPad.Left == ButtonState.Pressed;

    public bool MoveRight =>
        _currentKeyboard.IsKeyDown(Keys.Right) || _currentKeyboard.IsKeyDown(Keys.D) ||
        _currentGamePad.ThumbSticks.Left.X > 0.3f || _currentGamePad.DPad.Right == ButtonState.Pressed;

    public bool Fire =>
        IsKeyPressed(Keys.Space) || IsKeyPressed(Keys.Z) ||
        IsButtonPressed(Buttons.A);

    public bool Pause =>
        IsKeyPressed(Keys.Escape) || IsKeyPressed(Keys.P) ||
        IsButtonPressed(Buttons.Start);

    // ── Menu navigation ────────────────────────────────────
    public bool MenuUp =>
        IsKeyPressed(Keys.Up) || IsKeyPressed(Keys.W) ||
        IsButtonPressed(Buttons.DPadUp);

    public bool MenuDown =>
        IsKeyPressed(Keys.Down) || IsKeyPressed(Keys.S) ||
        IsButtonPressed(Buttons.DPadDown);

    public bool MenuConfirm =>
        IsKeyPressed(Keys.Enter) || IsKeyPressed(Keys.Space) ||
        IsButtonPressed(Buttons.A);

    public bool MenuBack =>
        IsKeyPressed(Keys.Escape) ||
        IsButtonPressed(Buttons.B) || IsButtonPressed(Buttons.Back);

    /// <summary>
    /// Must be called at the start of every Update frame.
    /// </summary>
    public void Update()
    {
        _previousKeyboard = _currentKeyboard;
        _previousGamePad = _currentGamePad;
        _currentKeyboard = Keyboard.GetState();
        _currentGamePad = GamePad.GetState(PlayerIndex.One);
    }

    /// <summary>
    /// Returns true only on the frame the key was first pressed (edge-triggered).
    /// </summary>
    public bool IsKeyPressed(Keys key)
    {
        return _currentKeyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);
    }

    /// <summary>
    /// Returns true while the key is held down.
    /// </summary>
    public bool IsKeyHeld(Keys key)
    {
        return _currentKeyboard.IsKeyDown(key);
    }

    private bool IsButtonPressed(Buttons button)
    {
        return _currentGamePad.IsButtonDown(button) && _previousGamePad.IsButtonUp(button);
    }
}
