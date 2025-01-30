using System.Diagnostics;
using NAudio.Wave;

namespace Speaking_Clock;

internal class OnlineRadioPlayer
{
    private static BufferedWaveProvider _bufferedWaveProvider;
    internal static IWavePlayer WaveOut;
    private static IMp3FrameDecompressor _decompressor;

    internal static PlaybackState _playbackState = PlaybackState.Stopped;
    private static float _volume = 0.5f; // Default volume (50%)

    internal static async Task PlayStreamAsync(string url, float _volume = 0.05f)
    {
        try
        {
            if (_playbackState == PlaybackState.Paused && WaveOut != null)
            {
                Resume();
                return;
            }

            Stop(); // Ensure no existing resources are leaking

            using (var httpClient = new HttpClient())
            using (var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            using (var responseStream = await response.Content.ReadAsStreamAsync())
            using (var mp3DataBuffer = new MemoryStream())
            {
                response.EnsureSuccessStatusCode();

                WaveOut = new WaveOutEvent();
                WaveOut.Volume = _volume; // Set initial volume
                var buffer = new byte[65536]; // Buffer for incoming data
                int bytesRead;

                Mp3Frame frame = null;
                BufferedWaveProvider waveProvider = null;

                const int minBytesForFrame = 1000;

                _playbackState = PlaybackState.Playing;

                while (
                    (bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length)) > 0 &&
                    _playbackState != PlaybackState.Stopped)
                {
                    if (_playbackState == PlaybackState.Paused)
                    {
                        await Task.Delay(100); // Wait while paused
                        continue;
                    }

                    mp3DataBuffer.Write(buffer, 0, bytesRead);

                    while (mp3DataBuffer.Length >= minBytesForFrame)
                    {
                        mp3DataBuffer.Position = 0;

                        try
                        {
                            frame = Mp3Frame.LoadFromStream(mp3DataBuffer);
                        }
                        catch (EndOfStreamException)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Invalid MP3 frame: {ex.Message}, skipping...");
                            break;
                        }

                        if (_decompressor == null)
                        {
                            var mp3WaveFormat = new Mp3WaveFormat(
                                frame.SampleRate,
                                frame.ChannelMode == ChannelMode.Mono ? 1 : 2,
                                frame.FrameLength,
                                frame.BitRate);

                            _decompressor = new AcmMp3FrameDecompressor(mp3WaveFormat);

                            waveProvider = new BufferedWaveProvider(_decompressor.OutputFormat)
                            {
                                BufferDuration = TimeSpan.FromSeconds(20),
                                DiscardOnBufferOverflow = true
                            };

                            WaveOut.Init(waveProvider);
                            WaveOut.Play();
                        }

                        var decompressedBuffer = new byte[_decompressor.OutputFormat.AverageBytesPerSecond];
                        var decompressedBytes = _decompressor.DecompressFrame(frame, decompressedBuffer, 0);
                        waveProvider.AddSamples(decompressedBuffer, 0, decompressedBytes);

                        var remainingData = mp3DataBuffer.Length - mp3DataBuffer.Position;
                        var remainingBytes = new byte[remainingData];
                        mp3DataBuffer.Read(remainingBytes, 0, remainingBytes.Length);
                        mp3DataBuffer.SetLength(0);
                        mp3DataBuffer.Write(remainingBytes, 0, remainingBytes.Length);
                        mp3DataBuffer.Position = mp3DataBuffer.Length;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error playing stream: {ex.Message}");
        }
    }

    public static void Pause()
    {
        if (_playbackState == PlaybackState.Playing && WaveOut != null)
        {
            WaveOut.Pause();
            _playbackState = PlaybackState.Paused;
            Debug.WriteLine("Playback paused.");
        }
    }

    public static void Resume()
    {
        if (_playbackState == PlaybackState.Paused && WaveOut != null)
        {
            WaveOut.Play();
            _playbackState = PlaybackState.Playing;
            Debug.WriteLine("Playback resumed.");
        }
    }

    public static void Stop()
    {
        try
        {
            if (_playbackState == PlaybackState.Stopped)
                return;

            WaveOut?.Stop();
            WaveOut?.Dispose();
            WaveOut = null;

            _bufferedWaveProvider?.ClearBuffer();
            _bufferedWaveProvider = null;

            _decompressor?.Dispose();
            _decompressor = null;

            _playbackState = PlaybackState.Stopped;

            Debug.WriteLine("Playback stopped and resources cleaned up successfully.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error stopping playback: {ex.Message}");
        }
    }

    public static void SetVolume(float volume)
    {
        if (WaveOut != null && volume >= 0.00f && volume <= 1.0f)
        {
            WaveOut.Volume = volume;
            _volume = volume; // Save volume for future sessions
            Debug.WriteLine($"Volume set to: {_volume * 100}%");
        }
        else
        {
            Debug.WriteLine($"Invalid volume level: {volume}. Must be between 0.0 and 1.0.");
        }
    }

    internal enum PlaybackState
    {
        Stopped,
        Playing,
        Paused
    }
}