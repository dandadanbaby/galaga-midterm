using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GalagaMidterm.Assets;

namespace GalagaMidterm.Core;

/// <summary>
/// Dynamic wave-based game state featuring varied stages:
/// - Stage 1: Asteroid belt field
/// - Stage 2: Scout fleet (EnemyShip2)
/// - Stage 3: Asteroid storm + Fighters (Spaceship_4)
/// - Stage 4: Coordinated dive-bombers & scout pincer fleet
/// - Stage 5: MINI-BOSS: Sentinel Crusher (Scale 2.5x, 45 HP, 3-way cannon)
/// - Stage 6: Heavy Gunship armada (EnemyShip4)
/// - Stage 7: Synchronized pincer incursion (rapid cross-swoops)
/// - Stage 8: Meteor storm + heavy gunships
/// - Stage 9: TWIN MINI-BOSSES: Phantom Corvettes (Twin 35 HP bosses)
/// - Stage 10: MAIN BOSS: Mothership Goliath (Scale 3.8x, 130 HP, 3 combat phases)
/// - Stage 11+: Escalating elite space warfare
///
/// Supports enemy shooting (green plasma projectiles), multi-shot weapons,
/// coin economy, and integrated Hangar & Upgrade Shop.
/// </summary>
public class AsteroidsGameState : IGameState
{
    // ── Player ─────────────────────────────────────────────
    public Vector2 PlayerPosition { get; private set; }
    public int Lives { get; private set; } = 3;
    public bool IsPlayerAlive { get; private set; } = true;
    public bool HasShield => _shieldTimer > 0;
    public int PlayerShipType => Shop.SelectedShip;

    // ── Shop & Currency ────────────────────────────────────
    public ShopState Shop { get; } = new();
    public int Coins => Shop.Coins;

    // ── Enemies ────────────────────────────────────────────
    private readonly List<AsteroidEntity> _asteroids = new();
    private readonly List<EnemyShipEntity> _enemyShips = new();
    private readonly List<IEnemy> _combinedEnemies = new();
    public IReadOnlyList<IEnemy> Enemies => _combinedEnemies;

    // ── Projectiles ────────────────────────────────────────
    private readonly List<BulletEntity> _playerBullets = new();
    private readonly List<EnemyProjectile> _enemyBullets = new();
    public IReadOnlyList<IProjectile> PlayerProjectiles => _playerBullets;
    public IReadOnlyList<IProjectile> EnemyProjectiles => _enemyBullets;

    // ── Drops ──────────────────────────────────────────────
    private readonly List<DropEntity> _drops = new();
    public IReadOnlyList<DropEntity> Drops => _drops;

    // ── Score & Stage ──────────────────────────────────────
    public int Score { get; private set; }
    public int HighScore { get; private set; }
    public int Stage { get; private set; } = 1;

    // ── Boss UI Properties ─────────────────────────────────
    public string BossName { get; private set; } = "";
    public float BossHealthPercent { get; private set; } = -1f;

    // ── Game flow ──────────────────────────────────────────
    public bool IsPaused => false;
    public bool IsGameOver { get; private set; }
    public int MaxStages => 99;
    public bool IsVictory { get; private set; }
    public bool IsIntro { get; private set; }
    public float InvulnerableTimer => _invulnerableFrames / 60f;
    public string WaveAnnounceText { get; private set; } = "";
    public float ShieldTimer => _shieldTimer / 60f;

    // ── Events ─────────────────────────────────────────────
    public event Action<Vector2> OnExplosion;
    public event Action OnPlayerHit;
    public event Action<int> OnScoreChanged;
    public event Action OnStageCleared;
    public event Action OnGameOver;

    // ── Internal Timers & Systems ──────────────────────────
    private int _screenWidth;
    private int _screenHeight;
    private float _playerSpeed = 4.5f;
    private int _fireCooldown;
    private int _invulnerableFrames;
    private int _respawnTimer;
    private int _introTimer;
    private float _shieldTimer;
    private IPlayerInput _input;
    private static readonly Random _rng = new();

    // ── Wave Tracking ──────────────────────────────────────
    private int _asteroidsSpawned;
    private int _asteroidsForWave;
    private int _spawnTimer;
    private int _waveAnnounceTimer;
    private bool _waveComplete;
    private int _wavePauseTimer;
    private float _formationSwayTime;
    private float _formationSwayX;
    private float _coordinatedAttackTimer;

    // ── Constants ──────────────────────────────────────────
    private const int MaxLives = 5;
    private const int InvulnerabilityFrames = 180;
    private const int RespawnDelayFrames = 90;
    private const int IntroDurationFrames = 120;
    private const int WaveAnnounceDuration = 120;
    private const int WavePauseDuration = 90;
    private const int PlayerMarginX = 14;
    private const int PlayerMarginTop = 80;
    private const int PlayerMarginBottom = 30;
    private const float DropChance = 0.08f;
    private const int ShieldDurationFrames = 540; // 9 seconds

    public void Initialize(int screenWidth, int screenHeight, IPlayerInput input)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        _input = input;
        Restart();
    }

    public void Restart()
    {
        Score = 0;
        Lives = 3;
        Stage = 1;
        IsPlayerAlive = true;
        IsGameOver = false;
        IsVictory = false;
        _fireCooldown = 0;
        _invulnerableFrames = InvulnerabilityFrames;
        _respawnTimer = 0;
        _shieldTimer = 0;
        _waveAnnounceTimer = 0;
        WaveAnnounceText = "";

        _playerBullets.Clear();
        _enemyBullets.Clear();
        _asteroids.Clear();
        _enemyShips.Clear();
        _drops.Clear();
        _combinedEnemies.Clear();

        PlayerPosition = new Vector2(_screenWidth / 2f, _screenHeight - 70f);

        StartWave();

        _introTimer = IntroDurationFrames;
        IsIntro = true;
    }

    private void StartWave()
    {
        _asteroids.Clear();
        _enemyShips.Clear();
        _enemyBullets.Clear();
        _combinedEnemies.Clear();

        _asteroidsSpawned = 0;
        _spawnTimer = 0;
        _waveComplete = false;
        _wavePauseTimer = 0;
        _coordinatedAttackTimer = 3.0f;
        BossHealthPercent = -1f;
        BossName = "";

        // Titan Dreadnought free shield bonus
        if (Shop.SelectedShip == 2)
        {
            _shieldTimer = ShieldDurationFrames;
        }

        SetupWaveComposition();
    }

    private void SetupWaveComposition()
    {
        // ── Stage 1: Asteroid belt field ───────────────────
        if (Stage == 1)
        {
            _asteroidsForWave = 8;
        }
        // ── Stage 2: Scout fleet (EnemyShip2) ──────────────
        else if (Stage == 2)
        {
            _asteroidsForWave = 0;
            SpawnShipFormation(0, count: 6, rows: 2, cols: 3, topY: 90f);
        }
        // ── Stage 3: Asteroid storm + Fighters ─────────────
        else if (Stage == 3)
        {
            _asteroidsForWave = 8;
            SpawnShipFormation(2, count: 4, rows: 1, cols: 4, topY: 80f);
        }
        // ── Stage 4: Coordinated dive-bombers & scouts ─────
        else if (Stage == 4)
        {
            _asteroidsForWave = 3;
            SpawnShipFormation(0, count: 4, rows: 1, cols: 4, topY: 80f);
            SpawnShipFormation(2, count: 4, rows: 1, cols: 4, topY: 130f);
        }
        // ── Stage 5: MINI-BOSS: Sentinel Crusher ───────────
        else if (Stage == 5)
        {
            _asteroidsForWave = 0;
            BossName = "SENTINEL CRUSHER";
            var boss = new EnemyShipEntity(1,
                startPos: new Vector2(_screenWidth / 2f, -50f),
                formationPos: new Vector2(_screenWidth / 2f, 100f),
                stage: Stage,
                screenWidth: _screenWidth,
                screenHeight: _screenHeight,
                isBoss: true,
                bossName: BossName,
                customHP: 45,
                customScale: 2.5f);
            _enemyShips.Add(boss);

            // 2 escort scouts
            _enemyShips.Add(new EnemyShipEntity(0, new Vector2(80f, -40f), new Vector2(80f, 110f), Stage, _screenWidth, _screenHeight));
            _enemyShips.Add(new EnemyShipEntity(0, new Vector2(_screenWidth - 80f, -40f), new Vector2(_screenWidth - 80f, 110f), Stage, _screenWidth, _screenHeight));
        }
        // ── Stage 6: Heavy Gunship Armada ──────────────────
        else if (Stage == 6)
        {
            _asteroidsForWave = 0;
            SpawnShipFormation(1, count: 4, rows: 1, cols: 4, topY: 90f); // Gunships
            SpawnShipFormation(2, count: 4, rows: 1, cols: 4, topY: 145f); // Escort fighters
        }
        // ── Stage 7: Synchronized Pincer Incursion ─────────
        else if (Stage == 7)
        {
            _asteroidsForWave = 4;
            SpawnShipFormation(0, count: 4, rows: 1, cols: 4, topY: 85f);
            SpawnShipFormation(2, count: 4, rows: 1, cols: 4, topY: 135f);
        }
        // ── Stage 8: Meteor Tempest & Gunships ─────────────
        else if (Stage == 8)
        {
            _asteroidsForWave = 10;
            SpawnShipFormation(1, count: 4, rows: 1, cols: 4, topY: 90f);
        }
        // ── Stage 9: TWIN MINI-BOSSES: Phantom Corvettes ───
        else if (Stage == 9)
        {
            _asteroidsForWave = 0;
            BossName = "TWIN PHANTOM CORVETTES";

            var boss1 = new EnemyShipEntity(0,
                startPos: new Vector2(100f, -50f),
                formationPos: new Vector2(120f, 100f),
                stage: Stage,
                screenWidth: _screenWidth,
                screenHeight: _screenHeight,
                isBoss: true,
                bossName: "PHANTOM ALPHA",
                customHP: 35,
                customScale: 2.2f);

            var boss2 = new EnemyShipEntity(2,
                startPos: new Vector2(_screenWidth - 100f, -50f),
                formationPos: new Vector2(_screenWidth - 120f, 100f),
                stage: Stage,
                screenWidth: _screenWidth,
                screenHeight: _screenHeight,
                isBoss: true,
                bossName: "PHANTOM BETA",
                customHP: 35,
                customScale: 2.2f);

            _enemyShips.Add(boss1);
            _enemyShips.Add(boss2);
        }
        // ── Stage 10: MAIN BOSS: Mothership Goliath ────────
        else if (Stage == 10)
        {
            _asteroidsForWave = 0;
            BossName = "MOTHERSHIP GOLIATH";

            var boss = new EnemyShipEntity(1,
                startPos: new Vector2(_screenWidth / 2f, -70f),
                formationPos: new Vector2(_screenWidth / 2f, 110f),
                stage: Stage,
                screenWidth: _screenWidth,
                screenHeight: _screenHeight,
                isBoss: true,
                bossName: BossName,
                customHP: 130,
                customScale: 3.8f);

            _enemyShips.Add(boss);
        }
        // ── Stage 11+: Endless Escalation ──────────────────
        else
        {
            bool isBossWave = Stage % 5 == 0;
            if (isBossWave)
            {
                BossName = $"DREADNOUGHT TITAN MK-{Stage / 5}";
                int bossHP = 60 + Stage * 6;
                var boss = new EnemyShipEntity(1,
                    startPos: new Vector2(_screenWidth / 2f, -60f),
                    formationPos: new Vector2(_screenWidth / 2f, 100f),
                    stage: Stage,
                    screenWidth: _screenWidth,
                    screenHeight: _screenHeight,
                    isBoss: true,
                    bossName: BossName,
                    customHP: bossHP,
                    customScale: 3.0f);
                _enemyShips.Add(boss);
                _asteroidsForWave = 4;
            }
            else
            {
                _asteroidsForWave = Math.Min(12, 4 + (Stage % 5) * 2);
                int shipCount = Math.Min(10, 6 + (Stage % 5));
                SpawnShipFormation(Stage % 3, shipCount, rows: 2, cols: (shipCount + 1) / 2, topY: 85f);
            }
        }
    }

    private void SpawnShipFormation(int enemyType, int count, int rows, int cols, float topY)
    {
        float startX = 60f;
        float spacingX = (_screenWidth - 120f) / Math.Max(1, cols - 1);
        float spacingY = 46f;

        int spawned = 0;
        for (int r = 0; r < rows && spawned < count; r++)
        {
            for (int c = 0; c < cols && spawned < count; c++)
            {
                float formX = startX + c * spacingX;
                float formY = topY + r * spacingY;
                float spawnX = formX + ((float)_rng.NextDouble() - 0.5f) * 200f;
                float spawnY = -40f - (r * 30f);

                _enemyShips.Add(new EnemyShipEntity(
                    enemyType,
                    new Vector2(spawnX, spawnY),
                    new Vector2(formX, formY),
                    Stage,
                    _screenWidth,
                    _screenHeight));

                spawned++;
            }
        }
    }

    public void Update(GameTime gameTime)
    {
        if (IsGameOver) return;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (IsIntro)
        {
            _introTimer--;
            if (_introTimer <= 0)
                IsIntro = false;
            return;
        }

        // Wave announcement timer
        if (_waveAnnounceTimer > 0)
        {
            _waveAnnounceTimer--;
            if (_waveAnnounceTimer <= 0)
                WaveAnnounceText = "";
        }

        // Pause between waves
        if (_waveComplete)
        {
            _wavePauseTimer--;
            if (_wavePauseTimer <= 0)
            {
                Stage++;
                WaveAnnounceText = Stage == 10 ? "WARNING: BOSS DETECTED!" : $"WAVE {Stage}";
                _waveAnnounceTimer = WaveAnnounceDuration;
                if (Stage == 10 || Stage == 5 || Stage == 9 || (Stage > 10 && Stage % 5 == 0))
                    AudioManager.Play(Sfx.BossWarning);
                OnStageCleared?.Invoke();
                StartWave();
            }
            else
            {
                UpdateBullets();
                UpdateDrops();
                return;
            }
        }

        // Formation sway
        _formationSwayTime += dt;
        _formationSwayX = MathF.Sin(_formationSwayTime * 1.8f) * 36f;

        // Coordinated attack trigger
        _coordinatedAttackTimer -= dt;
        if (_coordinatedAttackTimer <= 0f)
        {
            TriggerCoordinatedAttack();
            _coordinatedAttackTimer = 3.2f + (float)_rng.NextDouble() * 3.0f;
        }

        UpdatePlayer();
        UpdateBullets();
        UpdateAsteroids();
        UpdateEnemyShips(dt);
        UpdateDrops();
        SpawnAsteroids();
        CheckCollisions();
        CheckDropCollection();
        UpdateBossHealth();
        RefreshCombinedEnemies();
        CheckWaveComplete();
    }

    private void TriggerCoordinatedAttack()
    {
        // Find formation ships that can peel off in a coordinated pincer attack
        var formationShips = _enemyShips.FindAll(s => s.IsAlive && !s.IsBoss && s.State == EnemyShipState.Formation);
        if (formationShips.Count >= 2)
        {
            // Pick a pair for synchronized pincer swoop
            int idx1 = _rng.Next(formationShips.Count);
            int idx2 = (idx1 + 1) % formationShips.Count;

            formationShips[idx1].StartDive(new Vector2(_screenWidth * 0.25f, _screenHeight + 40f));
            formationShips[idx2].StartDive(new Vector2(_screenWidth * 0.75f, _screenHeight + 40f));
        }
        else if (formationShips.Count == 1)
        {
            formationShips[0].StartDive();
        }

        // Main boss escort summon in Stage 10
        if (Stage == 10 && _enemyShips.Exists(s => s.IsAlive && s.IsBoss))
        {
            if (_enemyShips.FindAll(s => s.IsAlive && !s.IsBoss).Count < 3)
            {
                float x = _rng.Next(40, _screenWidth - 40);
                var escort = new EnemyShipEntity(2, new Vector2(x, -30f), new Vector2(x, 170f), Stage, _screenWidth, _screenHeight);
                _enemyShips.Add(escort);
            }
        }
    }

    private void UpdatePlayer()
    {
        if (_input == null) return;

        // Player speed adjusted by selected ship model
        _playerSpeed = Shop.SelectedShip switch
        {
            1 => 5.8f, // Interceptor: fast and agile
            2 => 4.0f, // Titan: heavier hull
            _ => 4.6f  // Vanguard: balanced
        };

        if (!IsPlayerAlive)
        {
            _respawnTimer--;
            if (_respawnTimer <= 0)
            {
                if (Lives > 0)
                {
                    IsPlayerAlive = true;
                    _invulnerableFrames = InvulnerabilityFrames;
                    PlayerPosition = new Vector2(_screenWidth / 2f, _screenHeight - 70f);
                }
                else
                {
                    IsGameOver = true;
                    HighScore = Math.Max(HighScore, Score);
                    OnGameOver?.Invoke();
                }
            }
            return;
        }

        // Free 4-directional movement
        var pos = PlayerPosition;
        if (_input.MoveLeft) pos.X -= _playerSpeed;
        if (_input.MoveRight) pos.X += _playerSpeed;
        if (_input.MoveUp) pos.Y -= _playerSpeed;
        if (_input.MoveDown) pos.Y += _playerSpeed;

        pos.X = MathHelper.Clamp(pos.X, PlayerMarginX, _screenWidth - PlayerMarginX);
        pos.Y = MathHelper.Clamp(pos.Y, PlayerMarginTop, _screenHeight - PlayerMarginBottom);
        PlayerPosition = pos;

        if (_invulnerableFrames > 0)
            _invulnerableFrames--;

        if (_shieldTimer > 0)
            _shieldTimer--;

        // Calculate fire cooldown from Shop upgrades and ship perks
        int baseCooldown = Shop.FireRateLevel switch
        {
            4 => 4,  // Gatling laser
            3 => 6,  // Hyper cannon
            2 => 8,  // Rapid blaster
            _ => 10  // Standard
        };
        if (Shop.SelectedShip == 1) baseCooldown = Math.Max(3, baseCooldown - 1); // Interceptor bonus

        if (_fireCooldown > 0) _fireCooldown--;

        if (_input.Fire && _fireCooldown == 0)
        {
            FirePlayerWeapons();
            _fireCooldown = baseCooldown;
            AudioManager.Play(Sfx.Shoot);
        }
    }

    private void FirePlayerWeapons()
    {
        int multiShot = Shop.MultiShotLevel;

        switch (multiShot)
        {
            case 2: // Dual parallel blasters
                _playerBullets.Add(new BulletEntity(PlayerPosition + new Vector2(-10f, 0f), new Vector2(0, -7.5f)));
                _playerBullets.Add(new BulletEntity(PlayerPosition + new Vector2(10f, 0f), new Vector2(0, -7.5f)));
                break;

            case 3: // Triple spread cannon
                _playerBullets.Add(new BulletEntity(PlayerPosition, new Vector2(0, -7.5f)));
                _playerBullets.Add(new BulletEntity(PlayerPosition + new Vector2(-12f, 0f), new Vector2(-1.8f, -7.2f)));
                _playerBullets.Add(new BulletEntity(PlayerPosition + new Vector2(12f, 0f), new Vector2(1.8f, -7.2f)));
                break;

            case 4: // Quad heavy barrage
                _playerBullets.Add(new BulletEntity(PlayerPosition + new Vector2(-6f, 0f), new Vector2(0, -7.5f)));
                _playerBullets.Add(new BulletEntity(PlayerPosition + new Vector2(6f, 0f), new Vector2(0, -7.5f)));
                _playerBullets.Add(new BulletEntity(PlayerPosition + new Vector2(-18f, 0f), new Vector2(-2.5f, -7.0f)));
                _playerBullets.Add(new BulletEntity(PlayerPosition + new Vector2(18f, 0f), new Vector2(2.5f, -7.0f)));
                break;

            default: // Single center laser
                _playerBullets.Add(new BulletEntity(PlayerPosition, new Vector2(0, -7.5f)));
                break;
        }
    }

    private void UpdateBullets()
    {
        // Player bullets
        for (int i = _playerBullets.Count - 1; i >= 0; i--)
        {
            _playerBullets[i].Update();
            if (_playerBullets[i].IsDead)
                _playerBullets.RemoveAt(i);
        }

        // Enemy bullets
        for (int i = _enemyBullets.Count - 1; i >= 0; i--)
        {
            _enemyBullets[i].Update();
            if (_enemyBullets[i].IsDead)
                _enemyBullets.RemoveAt(i);
        }
    }

    private void UpdateAsteroids()
    {
        for (int i = _asteroids.Count - 1; i >= 0; i--)
        {
            if (_asteroids[i].IsAlive)
                _asteroids[i].Update();

            if (!_asteroids[i].IsAlive)
                _asteroids.RemoveAt(i);
        }
    }

    private void UpdateEnemyShips(float dt)
    {
        for (int i = _enemyShips.Count - 1; i >= 0; i--)
        {
            var ship = _enemyShips[i];
            if (ship.IsAlive)
            {
                ship.Update(PlayerPosition, _formationSwayX, _enemyBullets, dt);
            }
            else
            {
                _enemyShips.RemoveAt(i);
            }
        }
    }

    private void UpdateDrops()
    {
        for (int i = _drops.Count - 1; i >= 0; i--)
        {
            var drop = _drops[i];

            // Coin Magnet Perk
            if (Shop.HasMagnet && IsPlayerAlive)
            {
                float dx = PlayerPosition.X - drop.Position.X;
                float dy = PlayerPosition.Y - drop.Position.Y;
                float distSq = dx * dx + dy * dy;

                if (distSq < 170f * 170f && distSq > 1f)
                {
                    float dist = MathF.Sqrt(distSq);
                    var pos = drop.Position;
                    pos.X += (dx / dist) * 4.5f;
                    pos.Y += (dy / dist) * 4.5f;
                    drop.Position = pos;
                }
            }

            drop.Update();
            if (!drop.IsAlive)
                _drops.RemoveAt(i);
        }
    }

    private void SpawnAsteroids()
    {
        if (_asteroidsSpawned >= _asteroidsForWave) return;

        _spawnTimer++;
        if (_spawnTimer >= 45)
        {
            _spawnTimer = 0;
            int size = GetAsteroidSize();
            float speed = 0.8f + (Stage - 1) * 0.12f;
            _asteroids.Add(AsteroidEntity.SpawnAwayFromPlayer(size, speed, _screenWidth, _screenHeight));
            _asteroidsSpawned++;
        }
    }

    private int GetAsteroidSize()
    {
        if (Stage <= 2) return 3;
        int roll = _rng.Next(100);
        if (roll < 45) return 3;
        if (roll < 80) return 2;
        return 1;
    }

    private void CheckCollisions()
    {
        var spawnedAsteroids = new List<AsteroidEntity>();

        // ── 1. Player Bullets vs Asteroids ─────────────────
        for (int b = _playerBullets.Count - 1; b >= 0; b--)
        {
            var bullet = _playerBullets[b];
            if (bullet.IsDead) continue;

            for (int a = _asteroids.Count - 1; a >= 0; a--)
            {
                var asteroid = _asteroids[a];
                if (!asteroid.IsAlive) continue;

                if (bullet.Hits(asteroid.Position, asteroid.Radius))
                {
                    bullet.IsDead = true;
                    HitAsteroid(asteroid, spawnedAsteroids);
                    break;
                }
            }
        }
        _asteroids.AddRange(spawnedAsteroids);

        // ── 2. Player Bullets vs Enemy Ships & Bosses ──────
        for (int b = _playerBullets.Count - 1; b >= 0; b--)
        {
            var bullet = _playerBullets[b];
            if (bullet.IsDead) continue;

            for (int s = _enemyShips.Count - 1; s >= 0; s--)
            {
                var ship = _enemyShips[s];
                if (!ship.IsAlive) continue;

                if (bullet.Hits(ship.Position, ship.Radius))
                {
                    bullet.IsDead = true;
                    ship.HP--;

                    if (ship.HP <= 0)
                    {
                        ship.IsAlive = false;
                        AddScore(ship.ScoreValue);
                        OnExplosion?.Invoke(ship.Position);

                        // Chance for collectible drop
                        TrySpawnDrop(ship.Position, isBoss: ship.IsBoss);
                    }
                    else
                    {
                        // Spark hit effect
                        OnExplosion?.Invoke(bullet.Position);
                    }
                    break;
                }
            }
        }

        // ── 3. Enemy Bullets vs Player ─────────────────────
        if (IsPlayerAlive && _invulnerableFrames <= 0)
        {
            for (int eb = _enemyBullets.Count - 1; eb >= 0; eb--)
            {
                var eBullet = _enemyBullets[eb];
                if (eBullet.IsDead) continue;

                if (eBullet.Hits(PlayerPosition, 14f))
                {
                    eBullet.IsDead = true;

                    if (HasShield)
                    {
                        _shieldTimer = 0;
                        _invulnerableFrames = 60;
                        OnExplosion?.Invoke(PlayerPosition);
                        AudioManager.Play(Sfx.ShieldBreak);
                    }
                    else
                    {
                        KillPlayer();
                        break;
                    }
                }
            }
        }

        // ── 4. Enemy Ships vs Player Collision ─────────────
        if (IsPlayerAlive && _invulnerableFrames <= 0)
        {
            foreach (var ship in _enemyShips)
            {
                if (!ship.IsAlive) continue;

                if (ship.Hits(PlayerPosition, 14f))
                {
                    if (HasShield)
                    {
                        _shieldTimer = 0;
                        _invulnerableFrames = 60;
                        if (!ship.IsBoss) ship.IsAlive = false;
                        OnExplosion?.Invoke(ship.Position);
                        AudioManager.Play(Sfx.ShieldBreak);
                    }
                    else
                    {
                        KillPlayer();
                    }
                    break;
                }
            }
        }

        // ── 5. Asteroids vs Player Collision ───────────────
        if (IsPlayerAlive && _invulnerableFrames <= 0)
        {
            foreach (var asteroid in _asteroids)
            {
                if (!asteroid.IsAlive) continue;

                float dx = PlayerPosition.X - asteroid.Position.X;
                float dy = PlayerPosition.Y - asteroid.Position.Y;
                float r = 14f + asteroid.Radius;
                if (dx * dx + dy * dy < r * r)
                {
                    if (HasShield)
                    {
                        _shieldTimer = 0;
                        _invulnerableFrames = 60;
                        asteroid.IsAlive = false;
                        OnExplosion?.Invoke(asteroid.Position);
                        AudioManager.Play(Sfx.ShieldBreak);
                    }
                    else
                    {
                        KillPlayer();
                    }
                    break;
                }
            }
        }

        _playerBullets.RemoveAll(b => b.IsDead);
        _enemyBullets.RemoveAll(b => b.IsDead);
        _asteroids.RemoveAll(a => !a.IsAlive);
        _enemyShips.RemoveAll(s => !s.IsAlive);
    }

    private void HitAsteroid(AsteroidEntity asteroid, List<AsteroidEntity> spawned)
    {
        asteroid.IsAlive = false;
        int points = asteroid.Size switch
        {
            3 => 20,
            2 => 50,
            _ => 100
        };
        AddScore(points);
        OnExplosion?.Invoke(asteroid.Position);

        TrySpawnDrop(asteroid.Position);

        // Split large asteroids into 2 smaller ones
        if (asteroid.Size > 1)
        {
            float speed = (0.8f + (Stage - 1) * 0.12f) * 1.2f;
            for (int i = 0; i < 2; i++)
            {
                spawned.Add(AsteroidEntity.SpawnChild(
                    asteroid.Position.X, asteroid.Position.Y,
                    asteroid.Size - 1, speed,
                    _screenWidth, _screenHeight));
            }
        }
    }

    private void TrySpawnDrop(Vector2 pos, bool isBoss = false)
    {
        if (isBoss)
        {
            // Bosses always drop rich loot: coins + guaranteed shield
            for (int i = 0; i < 4; i++)
            {
                _drops.Add(new DropEntity(pos.X + (i - 1.5f) * 20f, pos.Y, 0, _screenHeight));
            }
            _drops.Add(new DropEntity(pos.X, pos.Y - 10f, 1, _screenHeight));
            return;
        }

        if (_rng.NextDouble() < DropChance)
        {
            int dropType = _rng.NextDouble() < 0.85 ? 0 : 1; // 85% Coin, 15% Shield

            // Shield cap: if already shielded or shield on screen, give coin
            if (dropType == 1 && (HasShield || _drops.Exists(d => d.IsAlive && d.DropType == 1)))
            {
                dropType = 0;
            }

            _drops.Add(new DropEntity(pos.X, pos.Y, dropType, _screenHeight));
        }
    }

    private void CheckDropCollection()
    {
        if (!IsPlayerAlive) return;

        for (int i = _drops.Count - 1; i >= 0; i--)
        {
            var drop = _drops[i];
            if (!drop.IsAlive) continue;

            if (drop.Hits(PlayerPosition, 18f))
            {
                drop.IsAlive = false;

                if (drop.DropType == 0) // Coin
                {
                    Shop.Coins++;
                    AddScore(200);
                    AudioManager.Play(Sfx.Coin);
                }
                else // Shield
                {
                    _shieldTimer = ShieldDurationFrames;
                    AudioManager.Play(Sfx.Shield);
                }
            }
        }
    }

    private void UpdateBossHealth()
    {
        var boss = _enemyShips.Find(s => s.IsAlive && s.IsBoss);
        if (boss != null)
        {
            BossHealthPercent = Math.Clamp((float)boss.HP / boss.MaxHP, 0f, 1f);
            BossName = boss.BossName;
        }
        else
        {
            BossHealthPercent = -1f;
        }
    }

    private void RefreshCombinedEnemies()
    {
        _combinedEnemies.Clear();
        foreach (var ship in _enemyShips)
        {
            if (ship.IsAlive) _combinedEnemies.Add(ship);
        }
        foreach (var asteroid in _asteroids)
        {
            if (asteroid.IsAlive) _combinedEnemies.Add(asteroid);
        }
    }

    private void CheckWaveComplete()
    {
        if (_asteroidsSpawned < _asteroidsForWave) return;

        bool anyAsteroid = _asteroids.Exists(a => a.IsAlive);
        bool anyShip = _enemyShips.Exists(s => s.IsAlive);

        if (!anyAsteroid && !anyShip)
        {
            _waveComplete = true;
            _wavePauseTimer = WavePauseDuration;
        }
    }

    private void KillPlayer()
    {
        IsPlayerAlive = false;
        Lives--;
        _respawnTimer = RespawnDelayFrames;
        _shieldTimer = 0;
        OnPlayerHit?.Invoke();
        OnExplosion?.Invoke(PlayerPosition);
    }

    private void AddScore(int points)
    {
        Score += points;
        OnScoreChanged?.Invoke(Score);
    }

    // ── Methods for Shop interaction ───────────────────────
    public void DeployShopShield()
    {
        _shieldTimer = ShieldDurationFrames;
    }

    public bool AddLifeFromShop()
    {
        if (Lives < MaxLives)
        {
            Lives++;
            return true;
        }
        return false;
    }
}
