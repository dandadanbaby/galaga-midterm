using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GalagaMidterm.Assets;
using GalagaMidterm.Core;
using GalagaMidterm.Rendering;
using GalagaMidterm.UI;

namespace GalagaMidterm.Screens;

/// <summary>
/// Main gameplay screen. Renders all game entities (player, enemies, projectiles,
/// effects) by reading from an IGameState, plus the HUD overlay.
///
/// SEPARATION OF CONCERNS:
///   This class does ONLY rendering and visual effects.
///   All entity positions, scores, and states come from the injected IGameState.
///   It never modifies game state — it only reads and presents.
///
/// BACKEND INTEGRATION:
///   The IGameState is injected via SetGameState(). Currently a MockGameState
///   is injected by GalagaGame. When the backend delivers a real IGameState,
///   change only the injection site — this class needs zero modifications.
/// </summary>
public class GameplayScreen : IScreen
{
    private readonly InputManager _input;
    private readonly GameStateManager _stateManager;
    private readonly Camera2D _camera;

    // ── Data source (interface, NOT concrete type) ─────────
    private IGameState _gameState;

    // ── Rendering systems ──────────────────────────────────
    private ParallaxBackground _background;
    private EffectsRenderer _effects;
    private HUD _hud;

    // ── Player rendering ───────────────────────────────────
    private Texture2D[] _playerShipTextures;
    private const float PLAYER_SCALE = 0.2f;

    // ── Enemy rendering ────────────────────────────────────
    private SpriteSheet[] _enemySheets;
    private Texture2D[] _asteroidTextures;
    private float _enemyAnimTimer;
    private int _enemyAnimFrame;
    private const float ENEMY_ANIM_RATE = 150f; // ms per frame

    // ── Projectile rendering ───────────────────────────────
    private SpriteSheet _projBlueSheet;
    private SpriteSheet _projGreenSheet;
    private float _projAnimTimer;
    private int _projAnimFrame;

    // ── Shield rendering ───────────────────────────────────
    private SpriteSheet _shieldSheet;
    private float _shieldAnimTimer;
    private int _shieldAnimFrame;

    // ── Screen dimensions ──────────────────────────────────
    private int _screenWidth;
    private int _screenHeight;

    // ── Hit flash effect ───────────────────────────────────
    private float _hitFlashTimer;
    private Texture2D _pixelTexture;

    // ── Overlay text ───────────────────────────────────────
    private SpriteFont _fontMedium;
    private SpriteFont _fontSmall;

    // ── Drop rendering ─────────────────────────────────────
    private Texture2D _coinTexture;
    private Texture2D _shieldIconTexture;

    public GameplayScreen(InputManager input, GameStateManager stateManager, Camera2D camera)
    {
        _input = input;
        _stateManager = stateManager;
        _camera = camera;
    }

    /// <summary>
    /// Injects the game state data source. Call this before the screen is used.
    /// The front-end reads from this interface — it never writes game state.
    /// </summary>
    public void SetGameState(IGameState gameState)
    {
        // Unsubscribe from old state events if swapping
        if (_gameState != null)
        {
            _gameState.OnExplosion -= HandleExplosion;
            _gameState.OnPlayerHit -= HandlePlayerHit;
            _gameState.OnScoreChanged -= HandleScoreChanged;
            _gameState.OnGameOver -= HandleGameOver;
            _gameState.OnStageCleared -= HandleStageCleared;
        }

        _gameState = gameState;

        // Subscribe to new state events
        if (_gameState != null)
        {
            _gameState.OnExplosion += HandleExplosion;
            _gameState.OnPlayerHit += HandlePlayerHit;
            _gameState.OnScoreChanged += HandleScoreChanged;
            _gameState.OnGameOver += HandleGameOver;
            _gameState.OnStageCleared += HandleStageCleared;
        }
    }

    public void LoadContent(AssetLoader assets, GraphicsDevice graphicsDevice)
    {
        _screenWidth = graphicsDevice.Viewport.Width;
        _screenHeight = graphicsDevice.Viewport.Height;

        _fontMedium = assets.GetFont(AssetPaths.FontMedium);
        _fontSmall = assets.GetFont(AssetPaths.FontSmall);

        // Pixel texture for flash effects
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });

        // ── Player ships ───────────────────────────────────
        _playerShipTextures = new Texture2D[]
        {
            assets.GetTexture(AssetPaths.PlayerShip1),
            assets.GetTexture(AssetPaths.PlayerShip2),
            assets.GetTexture(AssetPaths.PlayerShip9)
        };

        // ── Enemy sprite sheets ────────────────────────────
        _enemySheets = new SpriteSheet[]
        {
            new SpriteSheet(assets.GetTexture(AssetPaths.EnemyShip2), 24, 24, 5),
            new SpriteSheet(assets.GetTexture(AssetPaths.EnemyShip4), 32, 32, 5),
            new SpriteSheet(assets.GetTexture(AssetPaths.Spaceship4), 22, 22, 5),
        };

        _asteroidTextures = new Texture2D[]
        {
            assets.GetTexture(AssetPaths.Asteroid1),
            assets.GetTexture(AssetPaths.Asteroid2),
            assets.GetTexture(AssetPaths.Asteroid5)
        };

        // ── Effects ────────────────────────────────────────
        _effects = new EffectsRenderer();
        _effects.LoadContent(assets);

        _projBlueSheet = _effects.GetProjectileBlueSheet();
        _projGreenSheet = _effects.GetProjectileGreenSheet();
        _shieldSheet = _effects.GetShieldSheet();

        // ── Background ─────────────────────────────────────
        _background = new ParallaxBackground();
        _background.LoadContent(assets, _screenWidth, _screenHeight);

        // ── HUD ────────────────────────────────────────────
        _hud = new HUD();
        _hud.LoadContent(assets, _screenWidth);

        // ── Drop icons ───────────────────────────────────────
        _coinTexture = assets.GetTexture(AssetPaths.IconCoin);
        _shieldIconTexture = assets.GetTexture(AssetPaths.IconShield);
    }

    public void Update(GameTime gameTime)
    {
        if (_gameState == null) return;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Drive game logic — called here so pause overlay naturally freezes gameplay
        _gameState.Update(gameTime);

        // Pause check
        if (_input.Pause && !_gameState.IsIntro)
        {
            _stateManager.PushScreen("pause");
            return;
        }

        // Shop check
        if (_input.OpenShop && !_gameState.IsIntro)
        {
            _stateManager.PushScreen("shop");
            return;
        }

        // Update rendering systems (purely visual)
        _background.Update(gameTime);
        _effects.Update(gameTime);
        _camera.Update(gameTime);
        _hud.Update(gameTime, _gameState);

        // Animate enemy sprites
        _enemyAnimTimer += dt * 1000f;
        if (_enemyAnimTimer >= ENEMY_ANIM_RATE)
        {
            _enemyAnimTimer -= ENEMY_ANIM_RATE;
            _enemyAnimFrame = (_enemyAnimFrame + 1) % 5;
        }

        // Animate projectiles
        _projAnimTimer += dt * 1000f;
        if (_projAnimTimer >= 100f)
        {
            _projAnimTimer -= 100f;
            _projAnimFrame = (_projAnimFrame + 1) % 2;
        }

        // Animate shield
        _shieldAnimTimer += dt * 1000f;
        if (_shieldAnimTimer >= 200f)
        {
            _shieldAnimTimer -= 200f;
            _shieldAnimFrame = (_shieldAnimFrame + 1) % 2;
        }

        // Hit flash decay
        if (_hitFlashTimer > 0) _hitFlashTimer -= dt;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (_gameState == null) return;

        // ── Layer 0: Background (no camera shake) ──────────
        spriteBatch.Begin(samplerState: SamplerState.PointWrap);
        _background.Draw(spriteBatch);
        spriteBatch.End();

        // ── Layers 1–6: Game entities (with camera shake) ──
        spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.PointClamp,
            transformMatrix: _camera.TransformMatrix);

        DrawEnemyProjectiles(spriteBatch);
        DrawEnemies(spriteBatch);
        DrawPlayerProjectiles(spriteBatch);
        DrawPlayer(spriteBatch);
        DrawDrops(spriteBatch);
        _effects.Draw(spriteBatch);

        spriteBatch.End();

        // ── Hit flash overlay ──────────────────────────────
        if (_hitFlashTimer > 0)
        {
            spriteBatch.Begin();
            float flashAlpha = _hitFlashTimer / 0.15f * 0.3f;
            spriteBatch.Draw(_pixelTexture,
                new Rectangle(0, 0, _screenWidth, _screenHeight),
                Color.Red * flashAlpha);
            spriteBatch.End();
        }

        // ── Layer 7: HUD (no camera shake) ─────────────────
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _hud.Draw(spriteBatch, _gameState);

        // Wave announcement text & Shop prompt
        if (!string.IsNullOrEmpty(_gameState.WaveAnnounceText))
        {
            string waveText = _gameState.WaveAnnounceText;
            Vector2 waveSize = _fontMedium.MeasureString(waveText);
            Color alertColor = waveText.Contains("WARNING") || waveText.Contains("BOSS") ? Color.Red : Color.Yellow;
            spriteBatch.DrawString(_fontMedium, waveText,
                new Vector2((_screenWidth - waveSize.X) / 2f, _screenHeight * 0.38f),
                alertColor);

            string shopPrompt = "PRESS [B] OR [TAB] FOR UPGRADE SHOP";
            Vector2 promptSize = _fontSmall.MeasureString(shopPrompt);
            spriteBatch.DrawString(_fontSmall, shopPrompt,
                new Vector2((_screenWidth - promptSize.X) / 2f, _screenHeight * 0.38f + waveSize.Y + 10f),
                Color.Cyan);
        }

        // Boss health bar overlay
        if (_gameState.BossHealthPercent >= 0f)
        {
            DrawBossHealthBar(spriteBatch);
        }

        // Intro overlay
        if (_gameState.IsIntro)
        {
            DrawIntroOverlay(spriteBatch);
        }

        spriteBatch.End();
    }

    // ── Private draw helpers (read-only from IGameState) ────

    private void DrawPlayer(SpriteBatch batch)
    {
        if (!_gameState.IsPlayerAlive) return;

        // Invulnerability blink: skip drawing every other 6-frame cycle
        if (_gameState.InvulnerableTimer > 0f)
        {
            int frame = (int)(_gameState.InvulnerableTimer * 60f);
            if ((frame / 6) % 2 == 0) return; // Blink off
        }

        int shipType = Math.Clamp(_gameState.PlayerShipType, 0, _playerShipTextures.Length - 1);
        var tex = _playerShipTextures[shipType];
        var origin = new Vector2(tex.Width / 2f, tex.Height / 2f);

        batch.Draw(tex, _gameState.PlayerPosition, null, Color.White,
            0f, origin, PLAYER_SCALE, SpriteEffects.None, 0f);

        // Shield overlay
        if (_gameState.HasShield)
        {
            _shieldSheet.DrawFrame(batch, _shieldAnimFrame,
                _gameState.PlayerPosition, 3.5f, new Color(100, 200, 255, 150));
        }
    }

    private void DrawEnemies(SpriteBatch batch)
    {
        foreach (var enemy in _gameState.Enemies)
        {
            if (!enemy.IsAlive) continue;

            if (enemy.EnemyType < _enemySheets.Length)
            {
                // Sprite-sheet enemy (ship types) with rotation and scaling
                float scale = enemy.Scale > 0 ? enemy.Scale * 1.5f : 1.5f;
                Color tint = enemy.IsBoss ? new Color(255, 120, 120) : Color.White;
                _enemySheets[enemy.EnemyType].DrawFrame(
                    batch, _enemyAnimFrame, enemy.Position, scale, tint, enemy.Rotation);
            }
            else
            {
                // Asteroid enemy — uses Scale and Rotation from IEnemy
                int asteroidIdx = Math.Clamp(enemy.EnemyType - _enemySheets.Length, 0, _asteroidTextures.Length - 1);
                var tex = _asteroidTextures[asteroidIdx];
                var origin = new Vector2(tex.Width / 2f, tex.Height / 2f);

                float scale = enemy.Scale;
                float rotation = enemy.Rotation;

                // Boss gets a red tint
                Color tint = enemy.IsBoss ? new Color(255, 150, 130) : Color.White;

                batch.Draw(tex, enemy.Position, null, tint,
                    rotation, origin, scale, SpriteEffects.None, 0f);
            }
        }
    }

    private void DrawPlayerProjectiles(SpriteBatch batch)
    {
        foreach (var proj in _gameState.PlayerProjectiles)
        {
            _projBlueSheet.DrawFrame(batch, _projAnimFrame, proj.Position, 1.5f);
        }
    }

    private void DrawEnemyProjectiles(SpriteBatch batch)
    {
        foreach (var proj in _gameState.EnemyProjectiles)
        {
            _projGreenSheet.DrawFrame(batch, _projAnimFrame, proj.Position, 1.5f);
        }
    }

    private void DrawDrops(SpriteBatch batch)
    {
        float time = (float)System.Environment.TickCount64 / 1000f;

        foreach (var drop in _gameState.Drops)
        {
            if (!drop.IsAlive) continue;

            Texture2D tex = drop.DropType == 0 ? _coinTexture : _shieldIconTexture;
            var origin = new Vector2(tex.Width / 2f, tex.Height / 2f);
            float scale = 0.12f;

            // Small floating bob
            float bob = MathF.Sin(time * 3f + drop.Position.X * 0.1f) * 3f;
            var drawPos = new Vector2(drop.Position.X, drop.Position.Y + bob);

            Color tint = drop.DropType == 0
                ? new Color(255, 220, 50)
                : new Color(100, 200, 255);

            batch.Draw(tex, drawPos, null, tint,
                0f, origin, scale, SpriteEffects.None, 0f);
        }
    }

    private void DrawBossHealthBar(SpriteBatch batch)
    {
        int barWidth = 300;
        int barHeight = 14;
        int barX = (_screenWidth - barWidth) / 2;
        int barY = _screenHeight - 36;

        // Background
        batch.Draw(_pixelTexture, new Rectangle(barX, barY, barWidth, barHeight),
            new Color(60, 0, 0));

        // Health fill
        int fillWidth = (int)(barWidth * _gameState.BossHealthPercent);
        batch.Draw(_pixelTexture, new Rectangle(barX, barY, fillWidth, barHeight),
            new Color(220, 50, 40));

        // Border (drawn as 4 thin rects)
        Color borderColor = Color.White;
        batch.Draw(_pixelTexture, new Rectangle(barX, barY, barWidth, 1), borderColor);
        batch.Draw(_pixelTexture, new Rectangle(barX, barY + barHeight - 1, barWidth, 1), borderColor);
        batch.Draw(_pixelTexture, new Rectangle(barX, barY, 1, barHeight), borderColor);
        batch.Draw(_pixelTexture, new Rectangle(barX + barWidth - 1, barY, 1, barHeight), borderColor);

        // Label
        string bossLabel = !string.IsNullOrEmpty(_gameState.BossName) ? _gameState.BossName : "BOSS";
        Vector2 labelSize = _fontSmall.MeasureString(bossLabel);
        batch.DrawString(_fontSmall, bossLabel,
            new Vector2(barX + (barWidth - labelSize.X) / 2f, barY - labelSize.Y - 4),
            Color.White);
    }

    private void DrawIntroOverlay(SpriteBatch batch)
    {
        // Semi-transparent overlay
        batch.Draw(_pixelTexture,
            new Rectangle(0, 0, _screenWidth, _screenHeight),
            Color.Black * 0.5f);

        string readyText = "GET READY!";
        Vector2 readySize = _fontMedium.MeasureString(readyText);
        batch.DrawString(_fontMedium, readyText,
            new Vector2((_screenWidth - readySize.X) / 2f, _screenHeight * 0.4f),
            Color.White);
    }

    // ── Event handlers (visual reactions to state events) ───

    private void HandleExplosion(Vector2 position)
    {
        _effects.SpawnExplosion(position, ExplosionSize.Medium, 1.5f);
        AudioManager.Play(Sfx.Explosion);
    }

    private void HandlePlayerHit()
    {
        _camera.Shake(6f, 0.4f);
        _hitFlashTimer = 0.15f;
        AudioManager.Play(Sfx.PlayerHit);
    }

    private void HandleScoreChanged(int score)
    {
        _hud.OnScoreChanged(score);
    }

    private void HandleStageCleared()
    {
        AudioManager.Play(Sfx.WaveClear);
    }

    private void HandleGameOver()
    {
        AudioManager.Play(Sfx.GameOver);
        AudioManager.StopMusic();
        var gameOver = _stateManager.GetScreen("gameover") as GameOverScreen;
        if (gameOver != null)
        {
            gameOver.FinalScore = _gameState.Score;
            gameOver.HighScore = _gameState.HighScore;
            gameOver.StageReached = _gameState.Stage;
        }
        _stateManager.SwitchTo("gameover", TransitionStyle.FadeBlack, 1.0f);
    }

    public void OnEnter()
    {
        _gameState?.Restart();
        AudioManager.PlayMusic();
    }

    public void OnExit()
    {
        AudioManager.StopMusic();
    }
}
