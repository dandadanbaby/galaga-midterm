using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GalagaMidterm.Core;

namespace GalagaMidterm.UI;

/// <summary>
/// Reusable vertical menu list with cursor navigation, selection highlighting,
/// and blinking cursor arrow. Used by MainMenu, PauseScreen, etc.
/// </summary>
public class MenuComponent
{
    private readonly List<string> _options;
    private int _selectedIndex;
    private float _cursorBlinkTimer;
    private bool _cursorVisible = true;

    // ── Visual settings ────────────────────────────────────
    public Color NormalColor { get; set; } = Color.White;
    public Color SelectedColor { get; set; } = new Color(255, 255, 80);   // Yellow
    public Color HighlightGlow { get; set; } = new Color(100, 220, 255);  // Cyan
    public float LineSpacing { get; set; } = 36f;
    public float CursorBlinkRate { get; set; } = 0.4f; // seconds per blink
    public string CursorChar { get; set; } = "> ";

    /// <summary>
    /// Fires when the user confirms a selection. Provides the selected index.
    /// </summary>
    public event Action<int> OnSelect;

    public int SelectedIndex => _selectedIndex;

    public MenuComponent(List<string> options)
    {
        _options = options;
        _selectedIndex = 0;
    }

    public void Update(GameTime gameTime, InputManager input)
    {
        // Cursor blink
        _cursorBlinkTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_cursorBlinkTimer >= CursorBlinkRate)
        {
            _cursorBlinkTimer = 0f;
            _cursorVisible = !_cursorVisible;
        }

        // Navigation
        if (input.MenuUp)
        {
            _selectedIndex--;
            if (_selectedIndex < 0) _selectedIndex = _options.Count - 1;
            _cursorBlinkTimer = 0f;
            _cursorVisible = true;
        }

        if (input.MenuDown)
        {
            _selectedIndex++;
            if (_selectedIndex >= _options.Count) _selectedIndex = 0;
            _cursorBlinkTimer = 0f;
            _cursorVisible = true;
        }

        if (input.MenuConfirm)
        {
            OnSelect?.Invoke(_selectedIndex);
        }
    }

    /// <summary>
    /// Draws the menu list centered at the given position.
    /// </summary>
    public void Draw(SpriteBatch batch, SpriteFont font, Vector2 centerPosition)
    {
        float totalHeight = _options.Count * LineSpacing;
        float startY = centerPosition.Y - totalHeight / 2f;

        for (int i = 0; i < _options.Count; i++)
        {
            bool isSelected = (i == _selectedIndex);
            string text = _options[i];
            string displayText = isSelected && _cursorVisible
                ? CursorChar + text
                : "  " + text;

            Vector2 textSize = font.MeasureString(displayText);
            Vector2 pos = new Vector2(
                centerPosition.X - textSize.X / 2f,
                startY + i * LineSpacing
            );

            // Draw shadow for depth
            batch.DrawString(font, displayText,
                pos + new Vector2(2, 2), Color.Black * 0.5f);

            // Draw text
            Color color = isSelected ? SelectedColor : NormalColor;
            batch.DrawString(font, displayText, pos, color);

            // Subtle glow line under selected item
            if (isSelected)
            {
                float glowAlpha = 0.3f + MathF.Sin(_cursorBlinkTimer * MathF.PI * 2f / CursorBlinkRate) * 0.15f;
                batch.DrawString(font, displayText, pos, HighlightGlow * glowAlpha);
            }
        }
    }

    /// <summary>
    /// Resets the selection to the first item.
    /// </summary>
    public void Reset()
    {
        _selectedIndex = 0;
        _cursorBlinkTimer = 0f;
        _cursorVisible = true;
    }
}
