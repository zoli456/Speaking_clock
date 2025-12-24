using System.Diagnostics;
using ManagedBass;
using ManagedBass.Mix;

namespace Speaking_Clock;

internal class OnlineRadioPlayer
{
    private static int _streamHandle;
    private static int _mixerHandle;
    private static SyncProcedure _endSync;
    private static readonly object _lock = new();

    internal static RadioPlaybackState CurrentState { get; private set; } = RadioPlaybackState.Stopped;
    internal static float Volume { get; private set; } = 0.5f;

    internal static async Task PlayStreamAsync(string url, float volume = 0.05f)
    {
        Debug.WriteLine($"{url} started playing");

        try
        {
            if (CurrentState == RadioPlaybackState.Playing)
                Stop();

            lock (_lock)
            {
                // Create stream without AutoFree to prevent premature disposal
                _streamHandle = Bass.CreateStream(url, 0, BassFlags.Decode | BassFlags.StreamStatus, DownloadProc,
                    IntPtr.Zero);

                if (_streamHandle == 0)
                {
                    Debug.WriteLine("BASS stream creation failed: " + Bass.LastError);
                    TryAlternativeStreamMethod(url);
                    if (_streamHandle == 0)
                    {
                        Debug.WriteLine("All stream creation attempts failed.");
                        return;
                    }
                }

                // Retrieve stream info to configure mixer accordingly
                if (!Bass.ChannelGetInfo(_streamHandle, out var info))
                {
                    Debug.WriteLine("Failed to get stream info: " + Bass.LastError);
                    Stop();
                    return;
                }

                // Create mixer matching the stream's format
                _mixerHandle = BassMix.CreateMixerStream(info.Frequency, info.Channels, BassFlags.Default);
                if (_mixerHandle == 0)
                {
                    Debug.WriteLine("Mixer creation failed: " + Bass.LastError);
                    Stop();
                    return;
                }

                if (!BassMix.MixerAddChannel(_mixerHandle, _streamHandle, BassFlags.Default))
                {
                    Debug.WriteLine("Mixer error: " + Bass.LastError);
                    Stop();
                    return;
                }

                // Set sync for stream end
                _endSync = (h, ch, data, user) => Stop();
                Bass.ChannelSetSync(_mixerHandle, SyncFlags.End, 0, _endSync);

                // Set volume and start playback
                Volume = volume;
                Bass.ChannelSetAttribute(_mixerHandle, ChannelAttribute.Volume, Volume);

                if (Bass.ChannelPlay(_mixerHandle))
                {
                    CurrentState = RadioPlaybackState.Playing;
                    Debug.WriteLine("Playback started successfully");
                }
                else
                {
                    Debug.WriteLine("Playback failed to start: " + Bass.LastError);
                    Stop();
                }
            }

            // Keep alive while playing
            while (CurrentState != RadioPlaybackState.Stopped)
            {
                await Task.Delay(500);
                if (CurrentState == RadioPlaybackState.Paused)
                    await Task.Delay(1000);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Play error: {ex.Message}");
            Stop();
        }
    }

    private static void TryAlternativeStreamMethod(string url)
    {
        _streamHandle = Bass.CreateStream(url, 0, 0, DownloadProc, IntPtr.Zero);
        if (_streamHandle != 0) return;

        if (Environment.GetEnvironmentVariable("HTTP_PROXY") != null)
            _streamHandle = Bass.CreateStream(url, 0, BassFlags.StreamStatus, DownloadProc, IntPtr.Zero);
    }

    private static void DownloadProc(IntPtr buffer, int length, IntPtr user)
    {
    }

    public static void Pause()
    {
        lock (_lock)
        {
            if (CurrentState != RadioPlaybackState.Playing || _mixerHandle == 0) return;
            if (Bass.ChannelPause(_mixerHandle))
            {
                CurrentState = RadioPlaybackState.Paused;
                Debug.WriteLine("Playback paused");
            }
        }
    }

    public static void Resume()
    {
        lock (_lock)
        {
            if (CurrentState != RadioPlaybackState.Paused || _mixerHandle == 0) return;
            if (Bass.ChannelPlay(_mixerHandle))
            {
                CurrentState = RadioPlaybackState.Playing;
                Debug.WriteLine("Playback resumed");
            }
        }
    }

    public static void Stop()
    {
        lock (_lock)
        {
            if (CurrentState == RadioPlaybackState.Stopped) return;

            if (_mixerHandle != 0)
            {
                Bass.ChannelStop(_mixerHandle);
                Bass.StreamFree(_mixerHandle);
                _mixerHandle = 0;
            }

            if (_streamHandle != 0)
            {
                Bass.StreamFree(_streamHandle);
                _streamHandle = 0;
            }

            CurrentState = RadioPlaybackState.Stopped;
            Debug.WriteLine("Playback stopped");
        }
    }

    internal static void SetVolume(float volume)
    {
        lock (_lock)
        {
            Volume = Math.Clamp(volume, 0f, 1f);
            if (_mixerHandle != 0)
                Bass.ChannelSetAttribute(_mixerHandle, ChannelAttribute.Volume, Volume);
            Debug.WriteLine($"Volume set to {Volume * 100}%");
        }
    }

    internal enum RadioPlaybackState
    {
        Stopped,
        Playing,
        Paused
    }
}