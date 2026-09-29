using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GalagaMidterm.Assets;

namespace GalagaMidterm.Rendering;

/// <summary>
/// Manages an object pool of explosion/effect animations.
/// Explosions are spawned at a world position and play once, then return to the pool.
/// Uses tagged sprites to route returns to the correct pool.
/// </summary>
public class EffectsRenderer
{
    /// <summary>Tags a pooled sprite so it returns to the correct pool.</summary>
    private class PooledEffect
    {
        public AnimatedSprite Sprite;
        public ExplosionSize Size;
    }

    private readonly List<PooledEffect> _activeEffects = new();
    private readonly Queue<PooledEffect> _bigPool = new();
    private readonly Queue<PooledEffect> _mediumPool = new();
    private readonly Queue<PooledEffect> _smallPool = new();

    private SpriteSheet _bigSheet;
    private SpriteSheet _mediumSheet;
    private SpriteSheet _smallSheet;
    private SpriteSheet _shieldSheet;
    private SpriteSheet _projectileBlueSheet;
    private SpriteSheet _projectileGreenSheet;

    private const int POOL_SIZE = 15;

    public void LoadContent(AssetLoader assets)
    {
        // Create sprite sheet definitions based on analyzed frame data
        _bigSheet = new SpriteSheet(assets.GetTexture(AssetPaths.ExplosionBig), 60, 60, 4);
        _mediumSheet = new SpriteSheet(assets.GetTexture(AssetPaths.ExplosionMedium), 40, 40, 9);
        _smallSheet = new SpriteSheet(assets.GetTexture(AssetPaths.ExplosionSmall), 7, 7, 5);
        _shieldSheet = new SpriteSheet(assets.GetTexture(AssetPaths.ShieldBlue), 16, 16, 2);
        _projectileBlueSheet = new SpriteSheet(assets.GetTexture(AssetPaths.TripleShotBlue), 16, 16, 2);
        _projectileGreenSheet = new SpriteSheet(assets.GetTexture(AssetPaths.TripleShotGreen), 16, 16, 2);

        // Pre-populate pools
        for (int i = 0; i < POOL_SIZE; i++)
        {
            _bigPool.Enqueue(CreatePooled(_bigSheet, 100f, ExplosionSize.Big));
            _mediumPool.Enqueue(CreatePooled(_mediumSheet, 80f, ExplosionSize.Medium));
            _smallPool.Enqueue(CreatePooled(_smallSheet, 60f, ExplosionSize.Small));
        }
    }

    /// <summary>
    /// Spawns an explosion effect at the given position.
    /// </summary>
    public void SpawnExplosion(Vector2 position, ExplosionSize size, float scale = 1f)
    {
        PooledEffect pooled;

        switch (size)
        {
            case ExplosionSize.Big:
                pooled = _bigPool.Count > 0
                    ? _bigPool.Dequeue()
                    : CreatePooled(_bigSheet, 100f, ExplosionSize.Big);
                break;
            case ExplosionSize.Medium:
                pooled = _mediumPool.Count > 0
                    ? _mediumPool.Dequeue()
                    : CreatePooled(_mediumSheet, 80f, ExplosionSize.Medium);
                break;
            default:
                pooled = _smallPool.Count > 0
                    ? _smallPool.Dequeue()
                    : CreatePooled(_smallSheet, 60f, ExplosionSize.Small);
                break;
        }

        pooled.Sprite.Scale = scale;
        pooled.Sprite.Restart(position);
        _activeEffects.Add(pooled);
    }

    public void Update(GameTime gameTime)
    {
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            _activeEffects[i].Sprite.Update(gameTime);

            if (_activeEffects[i].Sprite.IsFinished)
            {
                var finished = _activeEffects[i];
                _activeEffects.RemoveAt(i);
                ReturnToPool(finished);
            }
        }
    }

    public void Draw(SpriteBatch batch)
    {
        foreach (var effect in _activeEffects)
        {
            effect.Sprite.Draw(batch);
        }
    }

    /// <summary>Gets the shield sprite sheet for rendering shield overlays.</summary>
    public SpriteSheet GetShieldSheet() => _shieldSheet;

    /// <summary>Gets the projectile sprite sheet for rendering player bullets.</summary>
    public SpriteSheet GetProjectileBlueSheet() => _projectileBlueSheet;

    /// <summary>Gets the projectile sprite sheet for rendering enemy bullets.</summary>
    public SpriteSheet GetProjectileGreenSheet() => _projectileGreenSheet;

    private PooledEffect CreatePooled(SpriteSheet sheet, float frameDurationMs, ExplosionSize size)
    {
        return new PooledEffect
        {
            Sprite = new AnimatedSprite(sheet, frameDurationMs, isLooping: false),
            Size = size
        };
    }

    /// <summary>
    /// Returns a finished effect to its correct pool based on the Size tag.
    /// </summary>
    private void ReturnToPool(PooledEffect effect)
    {
        effect.Sprite.IsActive = false;

        switch (effect.Size)
        {
            case ExplosionSize.Big:    _bigPool.Enqueue(effect); break;
            case ExplosionSize.Medium: _mediumPool.Enqueue(effect); break;
            case ExplosionSize.Small:  _smallPool.Enqueue(effect); break;
        }
    }
}

public enum ExplosionSize
{
    Small,
    Medium,
    Big
}
