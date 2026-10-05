using System.IO;
using System.Text.Json;
using System.Windows.Media;
using CaroGame.Core.Enums;
using CaroGame.Core.Models;

namespace CaroGame.Wpf.Services;

public interface ISoundService
{
    AudioProfile Profile { get; }
    void Play(SoundEffectType type);
    void SetCustomFile(SoundEffectType type, string filePath);
    void ResetToDefault(SoundEffectType type);
    void SetSoundEnabled(SoundEffectType type, bool enabled);
    void SetVolume(double volume);
    void SaveSettings();
}

public class SoundService : ISoundService
{
    private readonly AudioProfile _profile;
    private readonly string _settingsFilePath;
    private readonly Dictionary<SoundEffectType, MediaPlayer> _players = new();
    private readonly string _defaultAudioDir;

    public AudioProfile Profile => _profile;

    public SoundService()
    {
        string appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CaroGame");
        Directory.CreateDirectory(appDataDir);
        _settingsFilePath = Path.Combine(appDataDir, "audio_settings.json");
        _defaultAudioDir = Path.Combine(appDataDir, "Assets", "Audio");

        _profile = LoadSettings();
        EnsureDefaultSoundFiles();
        InitializePlayers();
    }

    private AudioProfile LoadSettings()
    {
        if (File.Exists(_settingsFilePath))
        {
            try
            {
                string json = File.ReadAllText(_settingsFilePath);
                AudioProfile? loaded = JsonSerializer.Deserialize<AudioProfile>(json);
                if (loaded != null) return loaded;
            }
            catch
            {
                // fallback to default
            }
        }
        return new AudioProfile();
    }

    public void SaveSettings()
    {
        try
        {
            string json = JsonSerializer.Serialize(_profile, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFilePath, json);
        }
        catch
        {
            // ignore save error
        }
    }

    private void InitializePlayers()
    {
        foreach (SoundEffectType type in Enum.GetValues<SoundEffectType>())
        {
            var player = new MediaPlayer();
            player.Volume = _profile.MasterVolume;
            _players[type] = player;
            UpdatePlayerSource(type);
        }
    }

    private void UpdatePlayerSource(SoundEffectType type)
    {
        if (!_players.TryGetValue(type, out MediaPlayer? player)) return;

        string path = GetAudioFilePath(type);
        if (File.Exists(path))
        {
            player.Open(new Uri(path, UriKind.Absolute));
        }
    }

    private string GetAudioFilePath(SoundEffectType type)
    {
        if (_profile.CustomFilePaths.TryGetValue(type, out string? customPath) && File.Exists(customPath))
        {
            return customPath;
        }

        return Path.Combine(_defaultAudioDir, $"{type}.wav");
    }

    public void Play(SoundEffectType type)
    {
        if (!_profile.EnabledSounds.TryGetValue(type, out bool enabled) || !enabled)
            return;

        try
        {
            if (_players.TryGetValue(type, out MediaPlayer? player))
            {
                player.Volume = _profile.MasterVolume;
                player.Stop();
                player.Position = TimeSpan.Zero;
                player.Play();
            }
        }
        catch
        {
            // fallback gracefully
        }
    }

    public void SetCustomFile(SoundEffectType type, string filePath)
    {
        if (File.Exists(filePath))
        {
            _profile.CustomFilePaths[type] = filePath;
            UpdatePlayerSource(type);
            SaveSettings();
        }
    }

    public void ResetToDefault(SoundEffectType type)
    {
        _profile.CustomFilePaths.Remove(type);
        UpdatePlayerSource(type);
        SaveSettings();
    }

    public void SetSoundEnabled(SoundEffectType type, bool enabled)
    {
        _profile.EnabledSounds[type] = enabled;
        SaveSettings();
    }

    public void SetVolume(double volume)
    {
        _profile.MasterVolume = Math.Clamp(volume, 0.0, 1.0);
        foreach (var player in _players.Values)
        {
            player.Volume = _profile.MasterVolume;
        }
        SaveSettings();
    }

    private void EnsureDefaultSoundFiles()
    {
        Directory.CreateDirectory(_defaultAudioDir);

        // Sinh các file âm thanh WAV chuẩn trong trường hợp chưa có file
        GenerateWavIfMissing(SoundEffectType.MovePlaced, 880, 60, 0.4);       // Tiếng click cờ đanh
        GenerateWavIfMissing(SoundEffectType.WarningFour, 440, 150, 0.5);      // Tiếng cảnh báo nguy hiểm
        GenerateWavIfMissing(SoundEffectType.GameWon, 1046, 300, 0.6);        // Âm thanh thắng
        GenerateWavIfMissing(SoundEffectType.GameLost, 330, 400, 0.5);        // Âm thanh thua
        GenerateWavIfMissing(SoundEffectType.UndoMove, 523, 70, 0.3);         // Tiếng undo
        GenerateWavIfMissing(SoundEffectType.TimerTick, 1200, 40, 0.3);       // Tiếng tick đồng hồ
        GenerateWavIfMissing(SoundEffectType.ChatMessage, 950, 90, 0.4);      // Tiếng chuông tin nhắn mới
    }

    private void GenerateWavIfMissing(SoundEffectType type, double frequency, int durationMs, double volume)
    {
        string path = Path.Combine(_defaultAudioDir, $"{type}.wav");
        if (File.Exists(path)) return;

        try
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            int sampleRate = 44100;
            short channels = 1;
            short bitsPerSample = 16;
            int totalSamples = (int)(sampleRate * (durationMs / 1000.0));
            int subChunk2Size = totalSamples * channels * (bitsPerSample / 8);
            int chunkSize = 36 + subChunk2Size;

            // RIFF header
            writer.Write("RIFF"u8.ToArray());
            writer.Write(chunkSize);
            writer.Write("WAVE"u8.ToArray());

            // fmt subchunk
            writer.Write("fmt "u8.ToArray());
            writer.Write(16); // subchunk1size (16 for PCM)
            writer.Write((short)1); // audio format (1 = PCM)
            writer.Write(channels);
            writer.Write(sampleRate);
            writer.Write(sampleRate * channels * (bitsPerSample / 8)); // byte rate
            writer.Write((short)(channels * (bitsPerSample / 8))); // block align
            writer.Write(bitsPerSample);

            // data subchunk
            writer.Write("data"u8.ToArray());
            writer.Write(subChunk2Size);

            // Generate Sine wave with rapid decay
            for (int i = 0; i < totalSamples; i++)
            {
                double t = (double)i / sampleRate;
                double decay = Math.Exp(-5.0 * t); // smooth fade out
                double sample = Math.Sin(2.0 * Math.PI * frequency * t) * decay * volume;
                short intSample = (short)(sample * short.MaxValue);
                writer.Write(intSample);
            }

            File.WriteAllBytes(path, ms.ToArray());
        }
        catch
        {
            // Ignore error if can't write file
        }
    }
}
