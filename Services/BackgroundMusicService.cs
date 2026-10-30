using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;
using ETBTool.Models;
using ETBTool.Utils;

namespace ETBTool.Services
{
    public enum MusicPlaybackState { None, Loading, Playing, Paused, Error }

    public sealed class BackgroundMusicService : IDisposable
    {
        private sealed class MusicSettings
        {
            public string FileName { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public double Volume { get; set; } = 0.35;
        }

        public static BackgroundMusicService Instance { get; } = new();
        private readonly MediaPlayer _player = new();
        private readonly DispatcherTimer _volumeTimer;
        private MusicSettings _settings = new();
        private bool _initialized;
        private bool _pauseRequested;
        private bool _disposed;
        private string ConfigFile => Path.Combine(GamePaths.AppConfigPath, "music.json");
        private string MusicDirectory => Path.Combine(GamePaths.AppConfigPath, "Music");
        public string TrackName => _settings.DisplayName;
        public double Volume => _settings.Volume;
        public bool HasTrack => !string.IsNullOrEmpty(_settings.FileName);
        public bool IsImporting { get; private set; }
        public MusicPlaybackState State { get; private set; }
        public string? ErrorMessage { get; private set; }
        public event Action? Changed;

        private BackgroundMusicService()
        {
            _player.MediaOpened += (_, _) =>
            {
                if (_disposed || !HasTrack) return;
                if (!_player.HasAudio)
                {
                    FailPlayback("The selected file does not contain supported audio.");
                    return;
                }
                try
                {
                    if (_pauseRequested) _player.Pause(); else _player.Play();
                    State = _pauseRequested ? MusicPlaybackState.Paused : MusicPlaybackState.Playing;
                    Changed?.Invoke();
                }
                catch (Exception ex) { FailPlayback(ex.Message); }
            };
            _player.MediaEnded += (_, _) =>
            {
                if (_disposed || State != MusicPlaybackState.Playing) return;
                try
                {
                    _player.Position = TimeSpan.Zero;
                    _player.Play();
                }
                catch (Exception ex) { FailPlayback(ex.Message); }
            };
            _player.MediaFailed += (_, e) => FailPlayback(e.ErrorException.Message);
            _volumeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            _volumeTimer.Tick += (_, _) => FlushVolume();
        }

        public void Initialize()
        {
            if (_initialized || _disposed) return;
            _initialized = true;
            try
            {
                _settings = ConfigPersistence.Read<MusicSettings>(ConfigFile);
                _settings.Volume = double.IsFinite(_settings.Volume) ? Math.Clamp(_settings.Volume, 0, 1) : 0.35;
                _player.Volume = _settings.Volume;
                if (HasTrack) OpenTrack();
            }
            catch (Exception ex) { FailPlayback(ex.Message); }
        }

        public async Task ImportAsync(string source)
        {
            if (IsImporting || _disposed) return;
            IsImporting = true;
            ErrorMessage = null;
            Changed?.Invoke();
            string? destination = null;
            bool saved = false;
            try
            {
                var extension = Path.GetExtension(source).ToLowerInvariant();
                if (extension is not (".mp3" or ".wav" or ".wma" or ".aac" or ".m4a" or ".flac"))
                    throw new NotSupportedException("Unsupported audio file extension.");
                Directory.CreateDirectory(MusicDirectory);
                var fileName = Guid.NewGuid().ToString("N") + extension;
                destination = Path.Combine(MusicDirectory, fileName);
                await Task.Run(() => File.Copy(source, destination, false));
                if (_disposed) return;
                var next = new MusicSettings
                {
                    FileName = fileName,
                    DisplayName = Path.GetFileName(source),
                    Volume = _settings.Volume
                };
                ConfigPersistence.Write(ConfigFile, next);
                saved = true;
                var previous = _settings.FileName;
                _settings = next;
                _player.Close();
                DeleteImportedFile(previous);
                OpenTrack();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                Logger.Log($"Background music import failed: {ex.Message}");
            }
            finally
            {
                if (!saved && destination != null) DeleteImportedFile(Path.GetFileName(destination));
                IsImporting = false;
                Changed?.Invoke();
            }
        }

        private string GetImportedPath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || fileName != Path.GetFileName(fileName) ||
                !Guid.TryParseExact(Path.GetFileNameWithoutExtension(fileName), "N", out _))
                throw new IOException("Invalid imported music path.");
            return Path.Combine(MusicDirectory, fileName);
        }

        private void OpenTrack()
        {
            try
            {
                _player.Close();
                var path = GetImportedPath(_settings.FileName);
                if (!File.Exists(path)) throw new FileNotFoundException("Imported music file is unavailable.");
                ErrorMessage = null;
                _pauseRequested = false;
                State = MusicPlaybackState.Loading;
                _player.Volume = _settings.Volume;
                _player.Open(new Uri(path, UriKind.Absolute));
                _player.Play();
                Changed?.Invoke();
            }
            catch (Exception ex) { FailPlayback(ex.Message); }
        }

        public void TogglePlayback()
        {
            if (!HasTrack || IsImporting || _disposed) return;
            try
            {
                if (State == MusicPlaybackState.Error) { OpenTrack(); return; }
                _pauseRequested = State is MusicPlaybackState.Playing or MusicPlaybackState.Loading;
                if (_pauseRequested) _player.Pause(); else _player.Play();
                State = _pauseRequested ? MusicPlaybackState.Paused : MusicPlaybackState.Playing;
                Changed?.Invoke();
            }
            catch (Exception ex) { FailPlayback(ex.Message); }
        }

        public void Clear()
        {
            if (IsImporting || _disposed) return;
            try
            {
                var next = new MusicSettings { Volume = _settings.Volume };
                ConfigPersistence.Write(ConfigFile, next);
                _player.Close();
                var previous = _settings.FileName;
                _settings = next;
                State = MusicPlaybackState.None;
                ErrorMessage = null;
                DeleteImportedFile(previous);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                Logger.Log($"Background music clear failed: {ex.Message}");
            }
            Changed?.Invoke();
        }

        public void SetVolume(double value)
        {
            if (_disposed || !double.IsFinite(value)) return;
            _settings.Volume = Math.Clamp(value, 0, 1);
            _player.Volume = _settings.Volume;
            _volumeTimer.Stop();
            _volumeTimer.Start();
        }

        private void FlushVolume()
        {
            _volumeTimer.Stop();
            try { ConfigPersistence.Write(ConfigFile, _settings); }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                Logger.Log($"Background music volume could not be saved: {ex.Message}");
                Changed?.Invoke();
            }
        }

        private void FailPlayback(string message)
        {
            if (_disposed) return;
            try { _player.Close(); } catch { }
            State = MusicPlaybackState.Error;
            ErrorMessage = message;
            Logger.Log($"Background music playback failed: {message}");
            Changed?.Invoke();
        }

        private void DeleteImportedFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;
            try
            {
                var path = GetImportedPath(fileName);
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception ex) { Logger.Log($"Background music cleanup failed: {ex.Message}"); }
        }

        public void Dispose()
        {
            if (_disposed) return;
            if (_volumeTimer.IsEnabled) FlushVolume();
            _disposed = true;
            _volumeTimer.Stop();
            _player.Close();
        }
    }
}
