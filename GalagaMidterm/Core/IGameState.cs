using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace GalagaMidterm.Core;

/// <summary>
/// Read-only interface that the front-end reads to know what to render.
/// The front-end NEVER writes to this — all state flows one-way from the provider.
///
/// BACKEND INTEGRATION:
///   Currently implemented by MockGameState (in MockState/).
///   When the backend team delivers the real gameplay system, they implement
///   this same interface. The front-end rendering code requires zero changes.
/// </summary>
public interface IGameState
{
    // ── Player ─────────────────────────────────────────────
    Vector2 PlayerPosition { get; }
    int Lives { get; }
    bool IsPlayerAlive { get; }
    bool HasShield { get; }
    int PlayerShipType { get; } // 0 = Ship_1, 1 = Ship_2, 2 = Ship_9

    // ── Enemies ────────────────────────────────────────────
    IReadOnlyList<IEnemy> Enemies { get; }

    // ── Projectiles ────────────────────────────────────────
    IReadOnlyList<IProjectile> PlayerProjectiles { get; }
    IReadOnlyList<IProjectile> EnemyProjectiles { get; }

    // ── Score & Stage ──────────────────────────────────────
    int Score { get; }
    int HighScore { get; }
    int Stage { get; }

    // ── Game flow ──────────────────────────────────────────
    bool IsPaused { get; }
    bool IsGameOver { get; }

    // ── Events the front-end subscribes to ─────────────────
    /// <summary>Fired when an explosion should be rendered at a position.</summary>
    event Action<Vector2> OnExplosion;
    /// <summary>Fired when the player is hit (triggers screen shake / flash).</summary>
    event Action OnPlayerHit;
    /// <summary>Fired when the score changes (triggers HUD pop animation).</summary>
    event Action<int> OnScoreChanged;
    /// <summary>Fired when a stage is cleared (triggers transition).</summary>
    event Action OnStageCleared;
    /// <summary>Fired when the game is over (triggers game-over screen).</summary>
    event Action OnGameOver;
}

/// <summary>
/// Represents an enemy entity the renderer needs to draw.
/// </summary>
public interface IEnemy
{
    Vector2 Position { get; }
    int EnemyType { get; }  // 0 = EnemyShip2, 1 = EnemyShip4, 2 = Spaceship_4, 3+ = Asteroid
    bool IsAlive { get; }
}

/// <summary>
/// Represents a projectile the renderer needs to draw.
/// </summary>
public interface IProjectile
{
    Vector2 Position { get; }
    int ProjectileType { get; } // 0 = player blue, 1 = enemy green
}
