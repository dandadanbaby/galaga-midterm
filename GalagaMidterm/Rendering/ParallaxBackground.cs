using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GalagaMidterm.Assets;

namespace GalagaMidterm.Rendering;

/// <summary>
/// Multi-layer parallax scrolling background with a tiled starfield base,
/// drifting planets, and floating debris. All layers scroll vertically downward
/// at different speeds to create depth.
/// </summary>
public class ParallaxBackground
{
    // ── Starfield base layer ───────────────────────────────
    private Texture2D _baseTile;
    private float _baseScrollOffset;
    private readonly float _baseScrollSpeed = 30f; // px/s

    // ── Drifting objects (planets, asteroids, black holes) ─
    private readonly List<DriftingObject> _driftingObjects = new();
    private readonly List<Texture2D> _planetTextures = new();
    private Texture2D _bgAsteroidTexture;
    private readonly Random _rng = new();

    private int _screenWidth;
    private int _screenHeight;
    private float _spawnTimer;
    private readonly float _spawnInterval = 4f; // seconds between spawns

    /// <summary>
    /// A background object that drifts slowly downward with parallax depth.
    /// </summary>
    private class DriftingObject
    {
        public Texture2D Texture;
        public Vector2 Position;
        public float Speed;
        public float Scale;
        public float Opacity;
        public float Rotation;
        public float RotationSpeed;
    }

    public void LoadContent(AssetLoader assets, int screenWidth, int screenHeight)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;

        _baseTile = assets.GetTexture(AssetPaths.BgBase);
        _bgAsteroidTexture = assets.GetTexture(AssetPaths.BgAsteroid);

        // Load all planet textures for random spawning
        foreach (var path in AssetPaths.AllPlanets)
        {
            _planetTextures.Add(assets.GetTexture(path));
        }

        // Seed some initial drifting objects so the screen isn't empty at start
        for (int i = 0; i < 3; i++)
        {
            SpawnDriftingObject(randomizeY: true);
        }
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Scroll the starfield tile
        _baseScrollOffset += _baseScrollSpeed * dt;
        if (_baseScrollOffset >= _baseTile.Height)
            _baseScrollOffset -= _baseTile.Height;

        // Spawn new drifting objects periodically
        _spawnTimer += dt;
        if (_spawnTimer >= _spawnInterval)
        {
            _spawnTimer = 0f;
            SpawnDriftingObject(randomizeY: false);
        }

        // Update drifting objects
        for (int i = _driftingObjects.Count - 1; i >= 0; i--)
        {
            var obj = _driftingObjects[i];
            obj.Position.Y += obj.Speed * dt;
            obj.Rotation += obj.RotationSpeed * dt;

            // Remove if off-screen bottom
            if (obj.Position.Y > _screenHeight + 100)
            {
                _driftingObjects.RemoveAt(i);
            }
        }
    }

    public void Draw(SpriteBatch batch)
    {
        // ── Layer 0: Tiled starfield ───────────────────────
        DrawTiledBackground(batch);

        // ── Layer 1–3: Drifting objects ────────────────────
        foreach (var obj in _driftingObjects)
        {
            var origin = new Vector2(obj.Texture.Width / 2f, obj.Texture.Height / 2f);
            batch.Draw(
                obj.Texture,
                obj.Position,
                null,
                Color.White * obj.Opacity,
                obj.Rotation,
                origin,
                obj.Scale,
                SpriteEffects.None,
                0f
            );
        }
    }

    private void DrawTiledBackground(SpriteBatch batch)
    {
        int tileW = _baseTile.Width;
        int tileH = _baseTile.Height;
        int tilesX = (_screenWidth / tileW) + 2;
        int tilesY = (_screenHeight / tileH) + 2;

        for (int x = 0; x < tilesX; x++)
        {
            for (int y = -1; y < tilesY; y++)
            {
                var pos = new Vector2(x * tileW, y * tileH + _baseScrollOffset);
                batch.Draw(_baseTile, pos, Color.White);
            }
        }
    }

    private void SpawnDriftingObject(bool randomizeY)
    {
        // Randomly choose: planet (70%) or asteroid (30%)
        bool isPlanet = _rng.NextDouble() < 0.7 && _planetTextures.Count > 0;

        Texture2D tex;
        float scale;
        float speed;
        float opacity;
        float rotSpeed;

        if (isPlanet)
        {
            tex = _planetTextures[_rng.Next(_planetTextures.Count)];
            scale = 0.4f + (float)_rng.NextDouble() * 0.4f;   // 0.4–0.8
            speed = 15f + (float)_rng.NextDouble() * 25f;       // 15–40 px/s
            opacity = 0.4f + (float)_rng.NextDouble() * 0.35f;  // 0.4–0.75
            rotSpeed = 0f; // planets don't rotate
        }
        else
        {
            tex = _bgAsteroidTexture;
            scale = 0.3f + (float)_rng.NextDouble() * 0.5f;   // 0.3–0.8
            speed = 25f + (float)_rng.NextDouble() * 35f;       // 25–60 px/s
            opacity = 0.5f + (float)_rng.NextDouble() * 0.3f;   // 0.5–0.8
            rotSpeed = -0.3f + (float)_rng.NextDouble() * 0.6f; // slow tumble
        }

        float x = (float)_rng.NextDouble() * _screenWidth;
        float y = randomizeY
            ? (float)_rng.NextDouble() * _screenHeight
            : -80f; // spawn above screen

        _driftingObjects.Add(new DriftingObject
        {
            Texture = tex,
            Position = new Vector2(x, y),
            Speed = speed,
            Scale = scale,
            Opacity = opacity,
            Rotation = (float)_rng.NextDouble() * MathF.Tau,
            RotationSpeed = rotSpeed
        });
    }
}
