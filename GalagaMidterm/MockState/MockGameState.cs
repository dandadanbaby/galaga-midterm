using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GalagaMidterm.Core;

namespace GalagaMidterm.MockState;

/// <summary>
/// MOCK DATA LAYER — Provides fake presentation data for front-end testing.
///
/// PURPOSE:
///   Allows the front-end to demonstrate all visual features (rendering, HUD,
///   animations, effects, screen transitions) before the real backend exists.
///
/// DOES:
///   - Provides demo values: score, lives, stage, positions
///   - Animates entities for visual testing (player sway, enemy bob, projectile drift)
///   - Fires demo events (explosions, score ticks) to test UI reactions
///
/// DOES NOT:
///   - Implement gameplay rules
///   - Handle collision detection
///   - Calculate real scores
///   - Manage real game state
///
/// REPLACEMENT:
///   When the backend team delivers a real IGameState implementation,
///   swap the constructor parameter in GalagaGame.LoadContent() — zero
///   rendering code changes required.
/// </summary>
public class MockGameState : IGameState
{
    // ── Player ─────────────────────────────────────────────
    public Vector2 PlayerPosition { get; set; }
    public int Lives { get; set; } = 3;
    public bool IsPlayerAlive { get; set; } = true;
    public bool HasShield { get; set; } = false;
    public int PlayerShipType { get; set; } = 0;

    // ── Enemies ────────────────────────────────────────────
    private readonly List<MockEnemy> _enemies = new();
    public IReadOnlyList<IEnemy> Enemies => _enemies;

    // ── Projectiles ────────────────────────────────────────
    private readonly List<MockProjectile> _playerProjectiles = new();
    private readonly List<MockProjectile> _enemyProjectiles = new();
    public IReadOnlyList<IProjectile> PlayerProjectiles => _playerProjectiles;
    public IReadOnlyList<IProjectile> EnemyProjectiles => _enemyProjectiles;

    // ── Score & Stage ──────────────────────────────────────
    public int Score { get; set; } = 0;
    public int HighScore { get; set; } = 99999;
    public int Stage { get; set; } = 1;

    // ── Game flow ──────────────────────────────────────────
    public bool IsPaused { get; set; }
    public bool IsGameOver { get; set; }

    // ── Events ─────────────────────────────────────────────
    public event Action<Vector2> OnExplosion;
    public event Action OnPlayerHit;
    public event Action<int> OnScoreChanged;
#pragma warning disable CS0067 // Event is never used — reserved for backend integration
    public event Action OnStageCleared;
#pragma warning restore CS0067
    public event Action OnGameOver;

    // ── Mock animation timers (presentation only) ──────────
    private float _mockTimer;
    private float _projectileSpawnTimer;
    private int _mockScoreAccumulator;
    private int _screenWidth;
    private int _screenHeight;

    /// <summary>
    /// Initializes mock state with a test formation and player position.
    /// </summary>
    public void Initialize(int screenWidth, int screenHeight)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;

        // Reset state
        Score = 0;
        Lives = 3;
        Stage = 1;
        IsPlayerAlive = true;
        IsGameOver = false;
        HasShield = false;
        _mockTimer = 0f;
        _projectileSpawnTimer = 0f;
        _mockScoreAccumulator = 0;

        PlayerPosition = new Vector2(screenWidth / 2f, screenHeight - 70);

        _enemies.Clear();
        CreateTestFormation(screenWidth);

        _playerProjectiles.Clear();
        _enemyProjectiles.Clear();
    }

    /// <summary>
    /// Updates the mock with simple demo animations for visual testing.
    /// This is NOT gameplay — it only produces visual data.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _mockTimer += dt;

        if (!IsPlayerAlive || IsGameOver || IsPaused) return;

        // Player sways gently (visual demo only)
        float swayX = MathF.Sin(_mockTimer * 1.2f) * 60f;
        PlayerPosition = new Vector2(_screenWidth / 2f + swayX, _screenHeight - 70);

        // Enemy idle bobbing (visual demo only)
        foreach (var enemy in _enemies)
        {
            if (!enemy.IsAlive) continue;
            enemy.Position = new Vector2(
                enemy.BasePosition.X + MathF.Sin(_mockTimer * 2f + enemy.Phase) * 8f,
                enemy.BasePosition.Y + MathF.Cos(_mockTimer * 1.5f + enemy.Phase) * 4f
            );
        }

        // Spawn mock projectiles periodically (visual demo only)
        _projectileSpawnTimer += dt;
        if (_projectileSpawnTimer >= 0.6f)
        {
            _projectileSpawnTimer = 0f;
            _playerProjectiles.Add(new MockProjectile
            {
                Position = PlayerPosition + new Vector2(0, -25),
                ProjectileType = 0
            });

            if (_enemies.Count > 0 && _mockTimer % 2f < 1f)
            {
                var shooter = _enemies[((int)(_mockTimer * 3)) % _enemies.Count];
                if (shooter.IsAlive)
                {
                    _enemyProjectiles.Add(new MockProjectile
                    {
                        Position = shooter.Position + new Vector2(0, 15),
                        ProjectileType = 1
                    });
                }
            }
        }

        // Drift projectiles (visual demo only — NOT physics)
        for (int i = _playerProjectiles.Count - 1; i >= 0; i--)
        {
            _playerProjectiles[i].Position += new Vector2(0, -300 * dt);
            if (_playerProjectiles[i].Position.Y < -20)
                _playerProjectiles.RemoveAt(i);
        }

        for (int i = _enemyProjectiles.Count - 1; i >= 0; i--)
        {
            _enemyProjectiles[i].Position += new Vector2(0, 200 * dt);
            if (_enemyProjectiles[i].Position.Y > _screenHeight + 20)
                _enemyProjectiles.RemoveAt(i);
        }

        // Mock score ticking (presentation counter, NOT real scoring)
        _mockScoreAccumulator++;
        if (_mockScoreAccumulator % 60 == 0)
        {
            Score += 100;
            OnScoreChanged?.Invoke(Score);
        }

        // Periodic mock explosion (visual demo of effects system)
        if (_mockTimer % 5f < dt && _enemies.Count > 0)
        {
            int idx = ((int)(_mockTimer * 7)) % _enemies.Count;
            if (_enemies[idx].IsAlive)
            {
                OnExplosion?.Invoke(_enemies[idx].Position);
            }
        }
    }

    /// <summary>
    /// Creates a grid formation of enemies for visual testing.
    /// This is layout data, NOT AI or spawning logic.
    /// </summary>
    private void CreateTestFormation(int screenWidth)
    {
        int cols = 8;
        int rows = 4;
        float spacingX = 45f;
        float spacingY = 40f;
        float startX = (screenWidth - (cols - 1) * spacingX) / 2f;
        float startY = 80f;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                int enemyType = row switch
                {
                    0 => 0, // EnemyShip2
                    1 => 0, // EnemyShip2
                    2 => 1, // EnemyShip4
                    3 => 2, // Spaceship_4
                    _ => 0
                };

                var basePos = new Vector2(
                    startX + col * spacingX,
                    startY + row * spacingY
                );

                _enemies.Add(new MockEnemy
                {
                    BasePosition = basePos,
                    Position = basePos,
                    EnemyType = enemyType,
                    IsAlive = true,
                    Phase = (row * cols + col) * 0.5f
                });
            }
        }
    }

    // ── Test trigger methods (for manual front-end testing) ─
    public void TriggerPlayerHit() => OnPlayerHit?.Invoke();
    public void TriggerGameOver()
    {
        IsGameOver = true;
        OnGameOver?.Invoke();
    }
}

/// <summary>Mock enemy — presentation data only.</summary>
public class MockEnemy : IEnemy
{
    public Vector2 Position { get; set; }
    public Vector2 BasePosition { get; set; }
    public int EnemyType { get; set; }
    public bool IsAlive { get; set; }
    public float Phase { get; set; }
}

/// <summary>Mock projectile — presentation data only.</summary>
public class MockProjectile : IProjectile
{
    public Vector2 Position { get; set; }
    public int ProjectileType { get; set; }
}
