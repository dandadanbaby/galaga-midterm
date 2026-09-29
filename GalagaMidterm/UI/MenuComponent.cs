using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GalagaMidterm.Core;

namespace GalagaMidterm.UI;

/// <summary>
/// Reusable vertical menu list with cursor navigation, animated selection states,
/// smooth scale transitions, and pulsing cursor glow.
/// Used by MainMenuScreen, PauseScreen, etc.
/// </summary>
public class MenuComponent
{
    private readonly List<string> _options;
    private int _selectedIndex;
    private float _cursorBlinkTimer;
    private bool _cursorVisible = true;
    private float _selectionTime;          // time spent on current selection
    private float _totalTime;              // total elapsed time

    // Per-item animation state
    private float[] _itemScales;           // smooth scale targets
    private float[] _itemSlideOffsets;     // slide-in X offsets
    private float[] _itemAppearTimers;     // staggered appear timing

    // ── Visual settings ────────────────────────────────────
    public Color NormalColor { get; set; } = new Color(180, 180, 200);
    public Color SelectedColor { get; set; } = new Color(255, 255, 80);   // Yellow
    public Color HighlightGlow { get; set; } = new Color(100, 220, 255);  // Cyan
    public float LineSpacing { get; set; } = 40f;
    public float CursorBlinkRate { get; set; } = 0.4f;
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
        _itemScales = new float[options.Count];
        _itemSlideOffsets = new float[options.Count];
        _itemAppearTimers = new float[options.Count];

        for (int i = 0; i < options.Count; i++)
        {
            _itemScales[i] = 1f;
            _itemSlideOffsets[i] = 80f + i * 20f; // staggered start offset
            _itemAppearTimers[i] = 0f;
        }
    }

    public void Update(GameTime gameTime, InputManager input)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _totalTime += dt;
        _selectionTime += dt;

        // Cursor blink
        _cursorBlinkTimer += dt;
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
            _selectionTime = 0f;
        }

        if (input.MenuDown)
        {
            _selectedIndex++;
            if (_selectedIndex >= _options.Count) _selectedIndex = 0;
            _cursorBlinkTimer = 0f;
            _cursorVisible = true;
            _selectionTime = 0f;
        }

        if (input.MenuConfirm)
        {
            OnSelect?.Invoke(_selectedIndex);
        }

        // Smooth scale animation — selected item scales up, others scale down
        for (int i = 0; i < _options.Count; i++)
        {
            float targetScale = (i == _selectedIndex) ? 1.08f : 1f;
            _itemScales[i] = MathHelper.Lerp(_itemScales[i], targetScale, dt * 10f);

            // Slide-in animation (decays to 0)
            _itemSlideOffsets[i] = MathHelper.Lerp(_itemSlideOffsets[i], 0f, dt * 6f);
            if (MathF.Abs(_itemSlideOffsets[i]) < 0.5f) _itemSlideOffsets[i] = 0f;
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

            // Build cursor prefix
            string cursorPrefix;
            if (isSelected && _cursorVisible)
                cursorPrefix = CursorChar;
            else if (isSelected)
                cursorPrefix = "  "; // maintain spacing when cursor blinks off
            else
                cursorPrefix = "  ";

            string displayText = cursorPrefix + text;

            Vector2 textSize = font.MeasureString(displayText);
            float scale = _itemScales[i];
            Vector2 origin = textSize * 0.5f;
            Vector2 pos = new Vector2(
                centerPosition.X + _itemSlideOffsets[i],
                startY + i * LineSpacing + textSize.Y * 0.5f
            );

            // ── Shadow layers (deeper shadow + near shadow) ──
            batch.DrawString(font, displayText,
                pos + new Vector2(3, 3), Color.Black * 0.4f,
                0f, origin, scale, SpriteEffects.None, 0f);

            batch.DrawString(font, displayText,
                pos + new Vector2(1, 1), Color.Black * 0.6f,
                0f, origin, scale, SpriteEffects.None, 0f);

            // ── Main text ────────────────────────────────────
            Color textColor;
            if (isSelected)
            {
                // Warm pulsing yellow-white for selected item
                float pulse = 0.5f + MathF.Sin(_selectionTime * 4f) * 0.5f;
                textColor = Color.Lerp(SelectedColor, Color.White, pulse * 0.3f);
            }
            else
            {
                textColor = NormalColor;
            }

            batch.DrawString(font, displayText,
                pos, textColor,
                0f, origin, scale, SpriteEffects.None, 0f);

            // ── Cyan glow overlay on selected ────────────────
            if (isSelected)
            {
                float glowPulse = 0.15f + MathF.Sin(_totalTime * 5f) * 0.1f;
                batch.DrawString(font, displayText,
                    pos, HighlightGlow * glowPulse,
                    0f, origin, scale, SpriteEffects.None, 0f);
            }
        }
    }

    /// <summary>
    /// Draws a single-line sub-label under a given menu item (e.g. description text).
    /// </summary>
    public void DrawSubLabel(SpriteBatch batch, SpriteFont font, int itemIndex,
        string text, Vector2 centerPosition, Color color)
    {
        float totalHeight = _options.Count * LineSpacing;
        float startY = centerPosition.Y - totalHeight / 2f;
        float itemY = startY + itemIndex * LineSpacing;

        Vector2 labelSize = font.MeasureString(text);
        Vector2 labelPos = new Vector2(
            centerPosition.X - labelSize.X / 2f,
            itemY + LineSpacing * 0.7f
        );

        batch.DrawString(font, text, labelPos, color);
    }

    /// <summary>
    /// Resets the selection to the first item and re-triggers slide-in animations.
    /// </summary>
    public void Reset()
    {
        _selectedIndex = 0;
        _cursorBlinkTimer = 0f;
        _cursorVisible = true;
        _selectionTime = 0f;

        for (int i = 0; i < _options.Count; i++)
        {
            _itemScales[i] = 0.8f;
            _itemSlideOffsets[i] = 60f + i * 15f; // staggered slide-in
        }
    }
}
