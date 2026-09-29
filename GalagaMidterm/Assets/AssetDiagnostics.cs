using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GalagaMidterm.Rendering;

namespace GalagaMidterm.Assets;


/// <summary>
/// Runtime diagnostic that dumps a full inventory of every asset referenced
/// by <see cref="GameAssets"/> including texture dimensions, sprite sheet
/// frame layouts, and any discrepancies.
///
/// This is NOT called automatically — invoke from Game1 during development
/// or from a debug key binding.
/// </summary>
public static class AssetDiagnostics
{
    /// <summary>
    /// Runs all diagnostic checks against the GameAssets and prints a full
    /// report to Debug output.
    /// </summary>
    public static void RunFullDiagnostic(GameAssets assets)
    {
        Debug.WriteLine("");
        Debug.WriteLine("╔═══════════════════════════════════════════════════════════╗");
        Debug.WriteLine("║           GALAGA ASSET INTEGRATION DIAGNOSTIC            ║");
        Debug.WriteLine("╚═══════════════════════════════════════════════════════════╝");
        Debug.WriteLine("");

        int totalAssets = 0;
        int issues = 0;

        // ── Player Ships ───────────────────────────────────
        Debug.WriteLine("▸ PLAYER SHIPS");
        for (int i = 0; i < assets.PlayerShipCount; i++)
        {
            var tex = assets.PlayerShips[i];
            var origin = assets.PlayerShipOrigins[i];
            float scale = assets.PlayerShipNormalizedScales[i];
            float displayW = tex.Width * scale;
            float displayH = tex.Height * scale;

            Debug.WriteLine($"    [{i}] {tex.Width}×{tex.Height} px " +
                $"→ origin ({origin.X:F0},{origin.Y:F0}), " +
                $"scale {scale:F3} → display ~{displayW:F0}×{displayH:F0}");
            totalAssets++;
        }
        Debug.WriteLine("");

        // ── Enemy Sprite Sheets ────────────────────────────
        Debug.WriteLine("▸ ENEMY SPRITE SHEETS");
        for (int i = 0; i < assets.EnemyShipTypeCount; i++)
        {
            var sheet = assets.EnemyShipSheets[i];
            int expectedW = sheet.FrameWidth * sheet.FrameCount;
            string status = sheet.Texture.Width == expectedW ? "OK" : "MISMATCH";
            if (status != "OK") issues++;

            Debug.WriteLine($"    [{i}] {sheet.FrameWidth}×{sheet.FrameHeight} × {sheet.FrameCount} frames " +
                $"(tex {sheet.Texture.Width}×{sheet.Texture.Height}) [{status}]");
            totalAssets++;
        }
        Debug.WriteLine("");

        // ── Asteroids ──────────────────────────────────────
        Debug.WriteLine("▸ ASTEROID TEXTURES");
        for (int i = 0; i < assets.AsteroidTextures.Length; i++)
        {
            var tex = assets.AsteroidTextures[i];
            var origin = assets.AsteroidOrigins[i];
            float scale = assets.AsteroidNormalizedScales[i];

            Debug.WriteLine($"    [{i}] {tex.Width}×{tex.Height} px " +
                $"→ origin ({origin.X:F0},{origin.Y:F0}), scale {scale:F3}");
            totalAssets++;
        }
        Debug.WriteLine("");

        // ── Effect Sheets ──────────────────────────────────
        Debug.WriteLine("▸ EFFECT SPRITE SHEETS");
        DiagnoseSpriteSheet("ExplosionBig",    assets.ExplosionBigSheet,    ref totalAssets, ref issues);
        DiagnoseSpriteSheet("ExplosionMedium", assets.ExplosionMediumSheet, ref totalAssets, ref issues);
        DiagnoseSpriteSheet("ExplosionSmall",  assets.ExplosionSmallSheet,  ref totalAssets, ref issues);
        DiagnoseSpriteSheet("Shield",          assets.ShieldSheet,          ref totalAssets, ref issues);
        DiagnoseSpriteSheet("ProjectileBlue",  assets.ProjectileBlueSheet,  ref totalAssets, ref issues);
        DiagnoseSpriteSheet("ProjectileGreen", assets.ProjectileGreenSheet, ref totalAssets, ref issues);
        Debug.WriteLine("");

        // ── Backgrounds ────────────────────────────────────
        Debug.WriteLine("▸ BACKGROUND ASSETS");
        DiagnoseTexture("BaseTile",  assets.BgBaseTile,  ref totalAssets);
        DiagnoseTexture("Asteroid",  assets.BgAsteroid,  ref totalAssets);
        Debug.WriteLine($"    BlackHoles: {assets.BgBlackHoles.Length} loaded");
        for (int i = 0; i < assets.BgBlackHoles.Length; i++)
        {
            Debug.WriteLine($"      [{i}] {assets.BgBlackHoles[i].Width}×{assets.BgBlackHoles[i].Height}");
            totalAssets++;
        }
        Debug.WriteLine($"    Planets: {assets.BgPlanets.Length} loaded");
        for (int i = 0; i < assets.BgPlanets.Length; i++)
        {
            Debug.WriteLine($"      [{i}] {assets.BgPlanets[i].Width}×{assets.BgPlanets[i].Height} — {assets.BgPlanetPaths[i]}");
            totalAssets++;
        }
        Debug.WriteLine("");

        // ── UI Icons ───────────────────────────────────────
        Debug.WriteLine("▸ UI ICONS");
        DiagnoseTexture("Coin",   assets.IconCoin,   ref totalAssets);
        DiagnoseTexture("Shield", assets.IconShield, ref totalAssets);
        Debug.WriteLine("");

        // ── Fonts ──────────────────────────────────────────
        Debug.WriteLine("▸ FONTS");
        Debug.WriteLine($"    Small:  {(assets.FontSmall  != null ? "OK" : "MISSING")}");
        Debug.WriteLine($"    Medium: {(assets.FontMedium != null ? "OK" : "MISSING")}");
        Debug.WriteLine($"    Large:  {(assets.FontLarge  != null ? "OK" : "MISSING")}");
        if (assets.FontSmall  != null) totalAssets++;
        if (assets.FontMedium != null) totalAssets++;
        if (assets.FontLarge  != null) totalAssets++;
        Debug.WriteLine("");

        // ── Summary ────────────────────────────────────────
        Debug.WriteLine("╔═══════════════════════════════════════════════════════════╗");
        Debug.WriteLine($"║  Total assets verified: {totalAssets,-34}║");
        Debug.WriteLine($"║  Issues found: {issues,-42}║");
        Debug.WriteLine($"║  Pixel texture: {(assets.PixelTexture != null ? "OK" : "MISSING"),-41}║");
        Debug.WriteLine("╚═══════════════════════════════════════════════════════════╝");
        Debug.WriteLine("");
    }

    private static void DiagnoseSpriteSheet(string name, SpriteSheet sheet,
        ref int total, ref int issues)
    {
        if (sheet?.Texture == null)
        {
            Debug.WriteLine($"    {name}: MISSING");
            issues++;
            return;
        }

        int expectedW = sheet.FrameWidth * sheet.FrameCount;
        string status = sheet.Texture.Width == expectedW ? "OK" : "MISMATCH";
        if (status != "OK") issues++;

        Debug.WriteLine($"    {name}: {sheet.FrameWidth}×{sheet.FrameHeight} × {sheet.FrameCount} frames " +
            $"(tex {sheet.Texture.Width}×{sheet.Texture.Height}) [{status}]");
        total++;
    }

    private static void DiagnoseTexture(string name, Texture2D tex, ref int total)
    {
        if (tex == null)
        {
            Debug.WriteLine($"    {name}: MISSING");
            return;
        }

        Debug.WriteLine($"    {name}: {tex.Width}×{tex.Height}");
        total++;
    }
}
