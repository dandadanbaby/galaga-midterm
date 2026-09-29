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
        // ── Top-left: Score ────────────────────────────────
        string scoreLabel = "SCORE";
        string scoreValue = _displayedScore.ToString("D6");

        batch.DrawString(_fontSmall, scoreLabel,
            new Vector2(PADDING, PADDING), new Color(180, 180, 180));

        Vector2 scoreValuePos = new Vector2(PADDING, PADDING + 14);
        Vector2 scoreOrigin = _scorePopScale > 1.01f
            ? _fontSmall.MeasureString(scoreValue) * 0.5f
            : Vector2.Zero;
        Vector2 drawPos = _scorePopScale > 1.01f
            ? scoreValuePos + scoreOrigin
            : scoreValuePos;

        batch.DrawString(_fontSmall, scoreValue,
            drawPos, Color.Yellow,
            0f, scoreOrigin, _scorePopScale, SpriteEffects.None, 0f);

        // ── Top-right: Stage ───────────────────────────────
        string stageText = $"STAGE {state.Stage:D2}";
        Vector2 stageSize = _fontSmall.MeasureString(stageText);
        batch.DrawString(_fontSmall, stageText,
            new Vector2(_screenWidth - stageSize.X - PADDING, PADDING),
            new Color(100, 220, 255));

        // ── Lives (below score): mini ship icons ───────────
        if (_playerShipTexture != null)
        {
            for (int i = 0; i < state.Lives; i++)
            {
                float x = PADDING + i * (_playerShipTexture.Width * LIFE_ICON_SCALE + 4);
                batch.Draw(_playerShipTexture,
                    new Vector2(x, LIVES_Y),
                    null,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    LIFE_ICON_SCALE,
                    SpriteEffects.None,
                    0f);
            }
        }

        // ── Top-right below stage: Shield indicator ────────
        if (state.HasShield)
        {
            float iconSize = _shieldIcon.Width * ICON_SCALE;
            batch.Draw(_shieldIcon,
                new Vector2(_screenWidth - iconSize - PADDING, LIVES_Y),
                null,
                Color.White,
                0f,
                Vector2.Zero,
                ICON_SCALE,
                SpriteEffects.None,
                0f);
        }

        // ── Bottom-center: High Score ──────────────────────
        string highScoreText = $"HI {state.HighScore:D6}";
        Vector2 hiSize = _fontSmall.MeasureString(highScoreText);
        // Draw it semi-transparent at the top center
        batch.DrawString(_fontSmall, highScoreText,
            new Vector2((_screenWidth - hiSize.X) / 2f, PADDING),
            new Color(120, 120, 120) * 0.6f);
    }
}
