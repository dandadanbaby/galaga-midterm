using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GalagaMidterm.Assets;
using GalagaMidterm.Core;

namespace GalagaMidterm.UI;

/// <summary>
/// Renders the in-game heads-up display: score, lives, stage, and power-up indicators.
/// Layout follows classic Galaga arcade conventions.
/// </summary>
public class HUD
{
    private SpriteFont _fontSmall;
    private Texture2D _playerShipTexture;  // Mini ship icon for lives display
    private Texture2D _shieldIcon;
    private Texture2D _coinIcon;
    private int _screenWidth;

    // ── Score pop animation ────────────────────────────────
    private float _scorePopTimer;
    private float _scorePopScale = 1f;
    private int _displayedScore;

    // ── Layout constants ───────────────────────────────────
    private const int PADDING = 12;
    private const int LIVES_Y = 30;
    private const float LIFE_ICON_SCALE = 0.08f;  // Scale down player ship for lives
    private const float ICON_SCALE = 0.12f;

    public void LoadContent(AssetLoader assets, int screenWidth)
    {
        _screenWidth = screenWidth;
        _fontSmall = assets.GetFont(AssetPaths.FontSmall);
        _playerShipTexture = assets.GetTexture(AssetPaths.PlayerShip1);
        _shieldIcon = assets.GetTexture(AssetPaths.IconShield);
        _coinIcon = assets.GetTexture(AssetPaths.IconCoin);
    }

    /// <summary>
    /// Call when the player ship type changes to update the lives icon.
    /// </summary>
    public void SetPlayerShipTexture(Texture2D shipTexture)
    {
        _playerShipTexture = shipTexture;
    }

    /// <summary>
    /// Triggers a score pop animation (scale bounce) when score changes.
    /// </summary>
    public void OnScoreChanged(int newScore)
    {
        _scorePopTimer = 0.2f;
        _displayedScore = newScore;
    }

    public void Update(GameTime gameTime, IGameState state)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Smooth score counter
        if (_displayedScore < state.Score)
        {
            int diff = state.Score - _displayedScore;
            _displayedScore += System.Math.Max(1, diff / 10);
            if (_displayedScore > state.Score)
                _displayedScore = state.Score;
        }

        // Score pop animation
        if (_scorePopTimer > 0)
        {
            _scorePopTimer -= dt;
            _scorePopScale = 1f + _scorePopTimer * 2f; // 1.4 → 1.0
        }
        else
        {
            _scorePopScale = 1f;
        }
    }

    public void Draw(SpriteBatch batch, IGameState state)
    {
        // ── Top Bar Layout ─────────────────────────────────
        // 1UP (Left) | HIGH SCORE (Center)
        
        string oneUpLabel = "1UP";
        string scoreValue = _displayedScore.ToString("D6");
        
        string highScoreLabel = "HIGH SCORE";
        string highScoreValue = state.HighScore.ToString("D6");

        // 1. Draw "1UP" text
        Vector2 oneUpSize = _fontSmall.MeasureString(oneUpLabel);
        float scoreMarginX = PADDING * 3f;
        batch.DrawString(_fontSmall, oneUpLabel, new Vector2(scoreMarginX, PADDING), Color.Red);

        // 2. Draw Score Value (with animation pop)
        Vector2 scorePos = new Vector2(scoreMarginX, PADDING + 20);
        Vector2 scoreOrigin = _scorePopScale > 1.01f ? _fontSmall.MeasureString(scoreValue) * 0.5f : Vector2.Zero;
        Vector2 drawPos = _scorePopScale > 1.01f ? scorePos + scoreOrigin : scorePos;
        batch.DrawString(_fontSmall, scoreValue, drawPos, Color.White, 0f, scoreOrigin, _scorePopScale, SpriteEffects.None, 0f);

        // 3. Draw HIGH SCORE text
        Vector2 hiLabelSize = _fontSmall.MeasureString(highScoreLabel);
        float centerX = _screenWidth / 2f;
        batch.DrawString(_fontSmall, highScoreLabel, new Vector2(centerX - hiLabelSize.X / 2f, PADDING), Color.Red);

        // 4. Draw High Score Value
        Vector2 hiValueSize = _fontSmall.MeasureString(highScoreValue);
        batch.DrawString(_fontSmall, highScoreValue, new Vector2(centerX - hiValueSize.X / 2f, PADDING + 20), Color.White);

        // ── Bottom Bar Layout ──────────────────────────────
        // Lives (Bottom Left) | Stage (Bottom Right)
        
        float bottomY = 720f - PADDING - 24f; // 720 is SCREEN_HEIGHT

        // 5. Lives
        if (_playerShipTexture != null)
        {
            for (int i = 0; i < state.Lives; i++)
            {
                float x = PADDING + i * (_playerShipTexture.Width * LIFE_ICON_SCALE + 6);
                batch.Draw(_playerShipTexture,
                    new Vector2(x, bottomY),
                    null,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    LIFE_ICON_SCALE,
                    SpriteEffects.None,
                    0f);
            }
        }

        // 6. Stage (Right aligned)
        string stageText = $"STAGE {state.Stage}";
        Vector2 stageSize = _fontSmall.MeasureString(stageText);
        batch.DrawString(_fontSmall, stageText,
            new Vector2(_screenWidth - stageSize.X - PADDING * 2f, bottomY + 4),
            new Color(100, 220, 255));

        // 7. Shield Powerup Icon (Right aligned, above stage)
        if (state.HasShield)
        {
            float iconSize = _shieldIcon.Width * ICON_SCALE;
            batch.Draw(_shieldIcon,
                new Vector2(_screenWidth - iconSize - PADDING * 2f, bottomY - 24),
                null,
                Color.White,
                0f,
                Vector2.Zero,
                ICON_SCALE,
                SpriteEffects.None,
                0f);
        }
    }
}
