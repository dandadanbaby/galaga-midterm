using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework.Graphics;

namespace GalagaMidterm.Assets;

/// <summary>
/// Validates that all expected assets are present and loadable.
/// Run after <see cref="AssetLoader.LoadAll"/> to catch missing/broken assets
/// early instead of crashing mid-game.
///
/// Reports results via <see cref="ValidationResult"/> — the caller decides
/// whether to log, throw, or display.
/// </summary>
public static class AssetManifest
{
    /// <summary>
    /// Result of a full asset validation pass.
    /// </summary>
    public class ValidationResult
    {
        public List<string> LoadedAssets { get; } = new();
        public List<string> MissingAssets { get; } = new();
        public List<string> Warnings { get; } = new();
        public bool AllAssetsPresent => MissingAssets.Count == 0;
    }

    /// <summary>
    /// Validates all expected assets against the loaded cache.
    /// Call after AssetLoader.LoadAll() and GameAssets.Initialize().
    /// </summary>
    public static ValidationResult Validate(AssetLoader loader, GameAssets assets)
    {
        var result = new ValidationResult();

        // ── Fonts ──────────────────────────────────────────
        ValidateFont(result, assets.FontSmall, "FontSmall", AssetPaths.FontSmall);
        ValidateFont(result, assets.FontMedium, "FontMedium", AssetPaths.FontMedium);
        ValidateFont(result, assets.FontLarge, "FontLarge", AssetPaths.FontLarge);

        // ── Player ships ───────────────────────────────────
        ValidateTextureArray(result, assets.PlayerShips, "PlayerShips",
            AssetPaths.PlayerShips);

        // ── Enemy ship sprite sheets ───────────────────────
        string[] enemyShipPaths = { AssetPaths.EnemyShip2, AssetPaths.EnemyShip4, AssetPaths.Spaceship4 };
        for (int i = 0; i < enemyShipPaths.Length; i++)
        {
            if (i < assets.EnemyShipSheets.Length && assets.EnemyShipSheets[i]?.Texture != null)
            {
                var sheet = assets.EnemyShipSheets[i];
                result.LoadedAssets.Add($"EnemySheet[{i}] ({enemyShipPaths[i]}) — {sheet.FrameWidth}×{sheet.FrameHeight}, {sheet.FrameCount} frames");

                // Verify frame dimensions make sense
                int expectedWidth = sheet.FrameWidth * sheet.FrameCount;
                if (sheet.Texture.Width != expectedWidth)
                {
                    result.Warnings.Add(
                        $"EnemySheet[{i}]: texture width {sheet.Texture.Width} ≠ expected {expectedWidth} " +
                        $"(frameW={sheet.FrameWidth} × count={sheet.FrameCount}). Possible frame mismatch.");
                }
            }
            else
            {
                result.MissingAssets.Add($"EnemySheet[{i}] ({enemyShipPaths[i]})");
            }
        }

        // ── Asteroid textures ──────────────────────────────
        string[] asteroidPaths = { AssetPaths.Asteroid1, AssetPaths.Asteroid2, AssetPaths.Asteroid5 };
        ValidateTextureArray(result, assets.AsteroidTextures, "AsteroidTextures", asteroidPaths);

        // ── Effect sprite sheets ───────────────────────────
        ValidateSpriteSheet(result, assets.ExplosionBigSheet, "ExplosionBigSheet", AssetPaths.ExplosionBig);
        ValidateSpriteSheet(result, assets.ExplosionMediumSheet, "ExplosionMediumSheet", AssetPaths.ExplosionMedium);
        ValidateSpriteSheet(result, assets.ExplosionSmallSheet, "ExplosionSmallSheet", AssetPaths.ExplosionSmall);
        ValidateSpriteSheet(result, assets.ShieldSheet, "ShieldSheet", AssetPaths.ShieldBlue);
        ValidateSpriteSheet(result, assets.ProjectileBlueSheet, "ProjectileBlueSheet", AssetPaths.TripleShotBlue);
        ValidateSpriteSheet(result, assets.ProjectileGreenSheet, "ProjectileGreenSheet", AssetPaths.TripleShotGreen);

        // ── Backgrounds ────────────────────────────────────
        ValidateTexture(result, assets.BgBaseTile, "BgBaseTile", AssetPaths.BgBase);
        ValidateTexture(result, assets.BgAsteroid, "BgAsteroid", AssetPaths.BgAsteroid);
        ValidateTextureArray(result, assets.BgBlackHoles, "BgBlackHoles",
            new[] { AssetPaths.BgBlackHole1, AssetPaths.BgBlackHole2, AssetPaths.BgBlackHole3 });
        ValidateTextureArray(result, assets.BgPlanets, "BgPlanets", AssetPaths.AllPlanets);

        // ── UI Icons ───────────────────────────────────────
        ValidateTexture(result, assets.IconCoin, "IconCoin", AssetPaths.IconCoin);
        ValidateTexture(result, assets.IconShield, "IconShield", AssetPaths.IconShield);

        // ── Utility ────────────────────────────────────────
        ValidateTexture(result, assets.PixelTexture, "PixelTexture", "(generated 1×1)");

        return result;
    }

    /// <summary>
    /// Writes the validation results to the Debug output window.
    /// </summary>
    public static void LogResults(ValidationResult result)
    {
        Debug.WriteLine("═══════════════════════════════════════════════");
        Debug.WriteLine("  ASSET MANIFEST VALIDATION");
        Debug.WriteLine("═══════════════════════════════════════════════");

        Debug.WriteLine($"  Loaded: {result.LoadedAssets.Count}");
        foreach (var a in result.LoadedAssets)
            Debug.WriteLine($"    ✓ {a}");

        if (result.Warnings.Count > 0)
        {
            Debug.WriteLine($"\n  Warnings: {result.Warnings.Count}");
            foreach (var w in result.Warnings)
                Debug.WriteLine($"    ⚠ {w}");
        }

        if (result.MissingAssets.Count > 0)
        {
            Debug.WriteLine($"\n  MISSING: {result.MissingAssets.Count}");
            foreach (var m in result.MissingAssets)
                Debug.WriteLine($"    ✗ {m}");
        }

        Debug.WriteLine("═══════════════════════════════════════════════");
        Debug.WriteLine(result.AllAssetsPresent
            ? "  RESULT: ALL ASSETS LOADED SUCCESSFULLY"
            : $"  RESULT: {result.MissingAssets.Count} ASSET(S) MISSING");
        Debug.WriteLine("═══════════════════════════════════════════════");
    }

    // ── Private helpers ────────────────────────────────────

    private static void ValidateTexture(ValidationResult result, Texture2D tex, string name, string path)
    {
        if (tex != null)
            result.LoadedAssets.Add($"{name} ({path}) — {tex.Width}×{tex.Height}");
        else
            result.MissingAssets.Add($"{name} ({path})");
    }

    private static void ValidateFont(ValidationResult result, SpriteFont font, string name, string path)
    {
        if (font != null)
            result.LoadedAssets.Add($"{name} ({path})");
        else
            result.MissingAssets.Add($"{name} ({path})");
    }

    private static void ValidateTextureArray(ValidationResult result, Texture2D[] textures,
        string baseName, string[] paths)
    {
        if (textures == null || textures.Length == 0)
        {
            result.MissingAssets.Add($"{baseName} (entire array missing)");
            return;
        }

        for (int i = 0; i < paths.Length; i++)
        {
            string itemName = $"{baseName}[{i}]";
            if (i < textures.Length)
                ValidateTexture(result, textures[i], itemName, paths[i]);
            else
                result.MissingAssets.Add($"{itemName} ({paths[i]})");
        }
    }

    private static void ValidateSpriteSheet(ValidationResult result, Rendering.SpriteSheet sheet,
        string name, string path)
    {
        if (sheet?.Texture != null)
        {
            result.LoadedAssets.Add(
                $"{name} ({path}) — {sheet.FrameWidth}×{sheet.FrameHeight}, {sheet.FrameCount} frames");

            int expectedWidth = sheet.FrameWidth * sheet.FrameCount;
            if (sheet.Texture.Width != expectedWidth)
            {
                result.Warnings.Add(
                    $"{name}: texture width {sheet.Texture.Width} ≠ expected {expectedWidth}. Check frame layout.");
            }
        }
        else
        {
            result.MissingAssets.Add($"{name} ({path})");
        }
    }
}
