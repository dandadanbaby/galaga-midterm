using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GalagaMidterm.Assets;

namespace GalagaMidterm.Core;

public enum EnemyShipState
{
    Entering,
    Formation,
    Diving,
    Strafing,
    Returning
}

/// <summary>
/// An enemy ship entity that implements IEnemy.
/// Supports 3 ship types from assets:
///   0 = EnemyShip2 (Scout / Interceptor)
///   1 = EnemyShip4 (Heavy Cruiser / Boss Flagship)
///   2 = Spaceship_4 (Agile Fighter / Dive-Bomber)
/// Features formation swaying, coordinated swoop dive-bomb attacks with rotational aiming,
/// weapon firing (aimed shots, spread shots, boss bullet spirals), and multi-phase boss mechanics.
/// </summary>
public class EnemyShipEntity : IEnemy
{
    private static readonly Random _rng = new();

    // ── IEnemy Properties ──────────────────────────────────
    public Vector2 Position { get; set; }
    public int EnemyType { get; }
    public bool IsAlive { get; set; } = true;
    public float Scale { get; set; } = 1.0f;
    public float Rotation { get; set; } = 0f;
    public bool IsBoss { get; set; } = false;

    // ── Combat Properties ──────────────────────────────────
    public int HP { get; set; } = 1;
    public int MaxHP { get; set; } = 1;
    public float Radius => 14f * Scale;
    public int ScoreValue { get; }
    public string BossName { get; set; } = "";

    // ── Movement & AI State ────────────────────────────────
    public EnemyShipState State { get; private set; } = EnemyShipState.Entering;
    public Vector2 FormationBasePos { get; set; }
    private float _diveTimer;
    private float _diveDuration = 3.5f;
    private Vector2 _diveStartPos;
    private Vector2 _diveTargetPos;
    private float _shootCooldown;
    private float _diveCooldown;
    private int _screenWidth;
    private int _screenHeight;
    private float _strafeDirection = 1f;
    private float _strafeSpeed = 1.5f;
    private float _stateTime;
    private int _stage;

    public EnemyShipEntity(int enemyType, Vector2 startPos, Vector2 formationPos, int stage, int screenWidth, int screenHeight, bool isBoss = false, string bossName = "", int customHP = 0, float customScale = 1.0f)
    {
        EnemyType = enemyType;
        Position = startPos;
        FormationBasePos = formationPos;
        _stage = stage;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        IsBoss = isBoss;
        BossName = bossName;
        Scale = customScale;

        _shootCooldown = 1.0f + (float)_rng.NextDouble() * 2.0f;
        _diveCooldown = 3.0f + (float)_rng.NextDouble() * 5.0f;

        if (isBoss)
        {
            MaxHP = customHP > 0 ? customHP : (stage >= 10 ? 130 : 45);
            HP = MaxHP;
            ScoreValue = stage >= 10 ? 6000 : 2500;
            State = EnemyShipState.Strafing;
            _strafeSpeed = 1.2f + Math.Min(stage * 0.1f, 1.0f);
        }
        else
        {
            switch (enemyType)
            {
                case 1: // Heavy Cruiser
                    MaxHP = 3;
                    ScoreValue = 250;
                    Scale = 1.2f;
                    break;
                case 2: // Dive-Bomber Fighter
                    MaxHP = 1;
                    ScoreValue = 160;
                    Scale = 1.0f;
                    break;
                default: // Scout / Interceptor
                    MaxHP = 1;
                    ScoreValue = 120;
                    Scale = 1.0f;
                    break;
            }
            HP = MaxHP;
            State = EnemyShipState.Entering;
        }
    }

    public void Update(Vector2 playerPos, float formationSwayX, List<EnemyProjectile> enemyBullets, float dt)
    {
        if (!IsAlive) return;

        _stateTime += dt;
        _shootCooldown -= dt;
        _diveCooldown -= dt;

        // Visual flash or rotation adjustments
        switch (State)
        {
            case EnemyShipState.Entering:
                UpdateEntering(formationSwayX, dt);
                break;

            case EnemyShipState.Formation:
                UpdateFormation(formationSwayX, dt);
                break;

            case EnemyShipState.Diving:
                UpdateDiving(playerPos, dt);
                break;

            case EnemyShipState.Strafing:
                UpdateStrafing(dt);
                break;

            case EnemyShipState.Returning:
                UpdateReturning(formationSwayX, dt);
                break;
        }

        // Shooting Behavior
        if (_shootCooldown <= 0f)
        {
            ExecuteShooting(playerPos, enemyBullets);
        }
    }

    private void UpdateEntering(float formationSwayX, float dt)
    {
        Vector2 target = FormationBasePos + new Vector2(formationSwayX, 0);
        Vector2 toTarget = target - Position;
        float dist = toTarget.Length();

        if (dist < 4f)
        {
            Position = target;
            Rotation = 0f;
            State = EnemyType == 1 ? EnemyShipState.Strafing : EnemyShipState.Formation;
        }
        else
        {
            toTarget.Normalize();
            float speed = 200f * dt;
            Position += toTarget * Math.Min(speed, dist);
            Rotation = MathF.Atan2(toTarget.Y, toTarget.X) - MathF.PI / 2f;
        }
    }

    private void UpdateFormation(float formationSwayX, float dt)
    {
        Position = FormationBasePos + new Vector2(formationSwayX, 0);
        Rotation = 0f;

        // Check if ready to dive
        if (!IsBoss && _diveCooldown <= 0f)
        {
            StartDive();
        }
    }

    public void StartDive(Vector2? forcedTarget = null)
    {
        State = EnemyShipState.Diving;
        _diveTimer = 0f;
        _diveStartPos = Position;
        _diveTargetPos = forcedTarget ?? new Vector2(Position.X + ((float)_rng.NextDouble() - 0.5f) * 160f, _screenHeight + 40f);
        _diveDuration = 2.4f - Math.Min(_stage * 0.05f, 0.8f);
    }

    private void UpdateDiving(Vector2 playerPos, float dt)
    {
        _diveTimer += dt;
        float t = _diveTimer / _diveDuration;

        if (t >= 1.0f || Position.Y > _screenHeight + 30f)
        {
            // Wrap to top and return to formation
            Position = new Vector2(Position.X, -30f);
            State = EnemyShipState.Returning;
            _diveCooldown = 5.0f + (float)_rng.NextDouble() * 4.0f;
            return;
        }

        // Smooth cubic dive with horizontal swooping arc
        float sway = MathF.Sin(t * MathF.PI * 2f) * 70f;
        float x = MathHelper.Lerp(_diveStartPos.X, _diveTargetPos.X, t) + sway;
        float y = MathHelper.Lerp(_diveStartPos.Y, _diveTargetPos.Y, t);

        Vector2 prevPos = Position;
        Position = new Vector2(x, y);

        Vector2 moveDir = Position - prevPos;
        if (moveDir.LengthSquared() > 0.001f)
        {
            Rotation = MathF.Atan2(moveDir.Y, moveDir.X) - MathF.PI / 2f;
        }
    }

    private void UpdateReturning(float formationSwayX, float dt)
    {
        Vector2 target = FormationBasePos + new Vector2(formationSwayX, 0);
        Vector2 toTarget = target - Position;
        float dist = toTarget.Length();

        if (dist < 6f)
        {
            Position = target;
            Rotation = 0f;
            State = EnemyShipState.Formation;
        }
        else
        {
            toTarget.Normalize();
            Position += toTarget * (180f * dt);
            Rotation = MathF.Atan2(toTarget.Y, toTarget.X) - MathF.PI / 2f;
        }
    }

    private void UpdateStrafing(float dt)
    {
        var pos = Position;
        pos.X += _strafeDirection * _strafeSpeed * 60f * dt;

        float minX = 40f * Scale;
        float maxX = _screenWidth - 40f * Scale;

        if (pos.X <= minX)
        {
            pos.X = minX;
            _strafeDirection = 1f;
        }
        else if (pos.X >= maxX)
        {
            pos.X = maxX;
            _strafeDirection = -1f;
        }

        // Gentle vertical wave for bosses
        if (IsBoss)
        {
            pos.Y = FormationBasePos.Y + MathF.Sin(_stateTime * 1.5f) * 18f;
        }

        Position = pos;
        Rotation = MathF.Sin(_stateTime * 2f) * 0.08f;
    }

    private void ExecuteShooting(Vector2 playerPos, List<EnemyProjectile> enemyBullets)
    {
        if (Position.Y < 10f || Position.Y > _screenHeight - 40f) return;

        float bulletSpeed = 3.5f + Math.Min(_stage * 0.15f, 2.5f);
        AudioManager.Play(Sfx.EnemyShoot);

        if (IsBoss)
        {
            ExecuteBossShooting(playerPos, enemyBullets, bulletSpeed);
            return;
        }

        // Regular ship shooting patterns
        if (EnemyType == 0) // Scout: single aimed shot
        {
            Vector2 dir = playerPos - Position;
            if (dir.LengthSquared() > 1f) dir.Normalize();
            enemyBullets.Add(new EnemyProjectile(Position, dir * bulletSpeed));
            _shootCooldown = 2.5f + (float)_rng.NextDouble() * 2.0f;
        }
        else if (EnemyType == 1) // Heavy Cruiser: 3-way spread shot
        {
            Vector2 baseDir = new Vector2(0, 1f);
            enemyBullets.Add(new EnemyProjectile(Position, baseDir * bulletSpeed));
            enemyBullets.Add(new EnemyProjectile(Position + new Vector2(-12, 0), new Vector2(-0.4f, 0.9f) * bulletSpeed));
            enemyBullets.Add(new EnemyProjectile(Position + new Vector2(12, 0), new Vector2(0.4f, 0.9f) * bulletSpeed));
            _shootCooldown = 2.2f + (float)_rng.NextDouble() * 1.5f;
        }
        else if (EnemyType == 2) // Dive-Bomber: fires when diving toward player
        {
            if (State == EnemyShipState.Diving)
            {
                Vector2 dir = playerPos - Position;
                if (dir.LengthSquared() > 1f) dir.Normalize();
                enemyBullets.Add(new EnemyProjectile(Position, dir * (bulletSpeed * 1.15f)));
                _shootCooldown = 1.0f;
            }
            else
            {
                _shootCooldown = 3.0f;
            }
        }
    }

    private void ExecuteBossShooting(Vector2 playerPos, List<EnemyProjectile> enemyBullets, float bulletSpeed)
    {
        float healthPct = (float)HP / MaxHP;

        if (_stage >= 10) // Main Boss: Mothership Goliath
        {
            if (healthPct > 0.65f) // Phase 1: Alternating twin cannons
            {
                enemyBullets.Add(new EnemyProjectile(Position + new Vector2(-28f * Scale, 20f), new Vector2(0, bulletSpeed)));
                enemyBullets.Add(new EnemyProjectile(Position + new Vector2(28f * Scale, 20f), new Vector2(0, bulletSpeed)));
                _shootCooldown = 1.2f;
            }
            else if (healthPct > 0.30f) // Phase 2: 5-way green plasma spread
            {
                for (int i = -2; i <= 2; i++)
                {
                    float angle = MathF.PI / 2f + (i * 0.22f);
                    Vector2 vel = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * bulletSpeed;
                    enemyBullets.Add(new EnemyProjectile(Position, vel));
                }
                _shootCooldown = 1.4f;
            }
            else // Phase 3: ENRAGED! 8-way rotating spiral barrage
            {
                float baseAngle = _stateTime * 4f;
                for (int i = 0; i < 8; i++)
                {
                    float angle = baseAngle + (i * MathF.PI / 4f);
                    Vector2 vel = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (bulletSpeed * 0.85f);
                    enemyBullets.Add(new EnemyProjectile(Position, vel));
                }
                _shootCooldown = 0.8f;
            }
        }
        else // Mini-Boss (Stage 5 or 9): 3-way spread aimed volley
        {
            Vector2 toPlayer = playerPos - Position;
            if (toPlayer.LengthSquared() > 1f) toPlayer.Normalize();
            float centerAngle = MathF.Atan2(toPlayer.Y, toPlayer.X);

            for (int i = -1; i <= 1; i++)
            {
                float angle = centerAngle + (i * 0.25f);
                Vector2 vel = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * bulletSpeed;
                enemyBullets.Add(new EnemyProjectile(Position, vel));
            }
            _shootCooldown = 1.5f;
        }
    }

    public bool Hits(Vector2 otherPos, float otherRadius)
    {
        float dx = Position.X - otherPos.X;
        float dy = Position.Y - otherPos.Y;
        float r = Radius + otherRadius;
        return dx * dx + dy * dy < r * r;
    }
}
