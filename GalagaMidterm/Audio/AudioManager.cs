using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace GalagaMidterm.Audio;

/// <summary>
/// Centralized audio playback manager for music and sound effects.
/// Gracefully handles missing audio files (no-op/silent) since no audio assets exist yet.
/// </summary>
public class AudioManager
{
    private ContentManager _content;
    private bool _audioAvailable;
    private float _masterVolume = 0.8f;
    private float _sfxVolume = 1.0f;
    private float _musicVolume = 0.6f;

    public void Initialize(ContentManager content)
    {
        _content = content;
        _audioAvailable = true; // Will be set to false if loading fails
    }

    /// <summary>
    /// Plays a music track by content path. No-op if file doesn't exist.
    /// </summary>
    public void PlayMusic(string trackPath, bool loop = true)
    {
        if (!_audioAvailable) return;

        try
        {
            var song = _content.Load<Song>(trackPath);
            MediaPlayer.IsRepeating = loop;
            MediaPlayer.Volume = _musicVolume * _masterVolume;
            MediaPlayer.Play(song);
        }
        catch
        {
            // Audio file doesn't exist yet — silent no-op
        }
    }

    /// <summary>
    /// Stops the currently playing music with an optional fade-out.
    /// </summary>
    public void StopMusic()
    {
        try { MediaPlayer.Stop(); } catch { }
    }

    /// <summary>
    /// Plays a sound effect by content path. No-op if file doesn't exist.
    /// </summary>
    public void PlaySFX(string sfxPath, float volume = 1.0f, float pitch = 0.0f)
    {
        if (!_audioAvailable) return;

        try
        {
            var sfx = _content.Load<SoundEffect>(sfxPath);
            sfx.Play(volume * _sfxVolume * _masterVolume, pitch, 0f);
        }
        catch
        {
            // Audio file doesn't exist yet — silent no-op
        }
    }

    public void SetMasterVolume(float volume)
    {
        _masterVolume = System.Math.Clamp(volume, 0f, 1f);
    }

    public void PauseAll()
    {
        try { MediaPlayer.Pause(); } catch { }
    }

    public void ResumeAll()
    {
        try { MediaPlayer.Resume(); } catch { }
    }
}
