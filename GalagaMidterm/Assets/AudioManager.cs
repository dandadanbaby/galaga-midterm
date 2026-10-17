using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace GalagaMidterm.Assets;

/// <summary>All the one-shot sound effects in the game.</summary>
public enum Sfx
{
    Shoot,
    Explosion,
    PlayerHit,
    Coin,
    Shield,
    MenuSelect,
    GameOver,
    Victory,
    WaveClear,
    BossWarning,
    ShopBuy,
    EnemyShoot,
    ShieldBreak
}

/// <summary>
/// Loads and plays all game audio. Static so any screen can call
/// AudioManager.Play(Sfx.X) without extra constructor parameters.
/// If a sound file is missing, the game keeps running (silently).
/// </summary>
public static class AudioManager
{
    // Path (no extension), volume 0..1, minimum gap between plays in ms
    private static readonly Dictionary<Sfx, (string Path, float Volume, int MinGapMs)> Config = new()
    {
        { Sfx.Shoot,      ("audio/shoot",       0.35f, 60)  },
        { Sfx.Explosion,  ("audio/explosion",   0.60f, 100) },
        { Sfx.PlayerHit,  ("audio/player_hit",  0.80f, 0)   },
        { Sfx.Coin,       ("audio/coin",        0.60f, 40)  },
        { Sfx.Shield,     ("audio/shield",      0.70f, 0)   },
        { Sfx.MenuSelect, ("audio/menu_select", 0.60f, 0)   },
        { Sfx.GameOver,   ("audio/game_over",   0.90f, 0)   },
        { Sfx.Victory,    ("audio/victory",     0.90f, 0)   },
        { Sfx.WaveClear,   ("audio/wave_clear",   0.70f, 0)   },
        { Sfx.BossWarning, ("audio/boss_warning", 0.80f, 0)   },
        { Sfx.ShopBuy,     ("audio/shop_buy",     0.70f, 0)   },
        { Sfx.EnemyShoot,  ("audio/enemy_shoot",  0.22f, 120) },
        { Sfx.ShieldBreak, ("audio/shield_break", 0.80f, 0)   },
    };

    private const string MusicPath = "audio/music_loop";

    private static readonly Dictionary<Sfx, SoundEffect> _effects = new();
    private static readonly Dictionary<Sfx, long> _lastPlayed = new();
    private static SoundEffectInstance _music;

    /// <summary>Overall volume multiplier for sound effects (0..1).</summary>
    public static float SfxVolume { get; set; } = 1.0f;

    /// <summary>Volume of the background music (0..1).</summary>
    public static float MusicVolume { get; set; } = 0.35f;

    public static void LoadAll(ContentManager content)
    {
        // Load SFX directly from .wav files using SoundEffect.FromStream
        // (avoids needing ffprobe for MGCB SoundEffectProcessor)
        string contentDir = content.RootDirectory;

        foreach (var kvp in Config)
        {
            try
            {
                string wavPath = Path.Combine(contentDir, kvp.Value.Path + ".wav");
                using var stream = TitleContainer.OpenStream(wavPath);
                _effects[kvp.Key] = SoundEffect.FromStream(stream);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AudioManager: could not load {kvp.Value.Path} ({ex.Message})");
            }
        }

        try
        {
            string musicWavPath = Path.Combine(contentDir, MusicPath + ".wav");
            using var stream = TitleContainer.OpenStream(musicWavPath);
            var music = SoundEffect.FromStream(stream);
            _music = music.CreateInstance();
            _music.IsLooped = true; // must be set before Play()
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"AudioManager: could not load {MusicPath} ({ex.Message})");
        }
    }

    public static void Play(Sfx sfx)
    {
        if (!_effects.TryGetValue(sfx, out var effect)) return;

        var cfg = Config[sfx];
        long now = Environment.TickCount64;
        if (cfg.MinGapMs > 0 && _lastPlayed.TryGetValue(sfx, out long last) && now - last < cfg.MinGapMs)
            return;
        _lastPlayed[sfx] = now;

        try
        {
            effect.Play(cfg.Volume * SfxVolume, 0f, 0f);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"AudioManager: play failed ({ex.Message})");
        }
    }

    /// <summary>Starts the music loop. Does nothing if it is already playing.</summary>
    public static void PlayMusic()
    {
        if (_music == null || _music.State == SoundState.Playing) return;
        _music.Volume = MusicVolume;
        _music.Play();
    }

    public static void PauseMusic()
    {
        if (_music?.State == SoundState.Playing) _music.Pause();
    }

    public static void ResumeMusic()
    {
        if (_music?.State == SoundState.Paused) _music.Resume();
    }

    public static void StopMusic()
    {
        _music?.Stop();
    }
}
