using System.Diagnostics;
using NAudio.Wave;
using Vanara.PInvoke;
using Vosk;

namespace Speaking_Clock;

internal class SpeechRecognition
{
    private const User32.SetWindowPosFlags TopmostFlags = User32.SetWindowPosFlags.SWP_NOMOVE |
                                                          User32.SetWindowPosFlags.SWP_NOSIZE |
                                                          User32.SetWindowPosFlags.SWP_SHOWWINDOW;

    internal static bool IsRunning;
    internal static WaveInEvent WaveIn;
    private static VoskRecognizer _recognizer;
    private static Model _voskModel;
    internal static string[] Temp;

    private static readonly HWND HwndTopmost = new(new IntPtr(-1)); // Constant for making the window top-most

    // Activate recording and recognition
    internal static void ActivateRecognition(string modelPath)
    {
        if (IsRunning) return;

        if (!Directory.Exists(modelPath))
        {
            Beallitasok.HangfelismerésSection["Bekapcsolva"].BoolValue = false;
            return;
        }

        _voskModel = new Model(modelPath);
        _recognizer = new VoskRecognizer(_voskModel, 16000.0f);

        IsRunning = true;

        WaveIn = new WaveInEvent { WaveFormat = new WaveFormat(16000, 1) };

        WaveIn.DataAvailable += (s, e) =>
        {
            if (!IsRunning) return;

            if (Beallitasok.HangfelismerésSection["Zajszűrés"].BoolValue)
            {
                /* if (_recognizer.AcceptWaveform(AudioProcessor.ApplyWienerFilter(e.Buffer, WaveIn.WaveFormat),
                         e.BytesRecorded))
                 {
                     Temp = _recognizer.FinalResult().Split("\"");
                     if (Temp[3].Length > 0)
                         ProcessResult(Temp[3]);
                 }*/
            }
            else
            {
                if (_recognizer.AcceptWaveform(e.Buffer, e.BytesRecorded))
                {
                    Temp = _recognizer.FinalResult().Split("\"");
                    if (Temp[3].Length > 0)
                        ProcessResult(Temp[3]);
                }
            }
        };

        WaveIn.StartRecording();
    }

    // Deactivate and clean up resources
    internal static void DeactivateRecognition()
    {
        if (IsRunning)
            WaveIn?.StopRecording();
        IsRunning = false;
        WaveIn?.Dispose();
        _recognizer?.Dispose();
        _voskModel?.Dispose();
    }

    // Process the recognition result and check for keywords
    private static void ProcessResult(string result)
    {
        Debug.WriteLine(result);
        // Process the recognized result
        switch (result)
        {
            case "time":
            {
                Debug.WriteLine("Jelenlegi idő felolvasás");
                Beallitasok.Most_mod();
                return;
            }
            case "today":
            {
                Debug.WriteLine("Mostani időjárás felolvasás");
                Beallitasok.AnnounceWeather();
                return;
            }
            case "tomorrow":
            {
                Debug.WriteLine("Holnapi időjárás felolvasás");
                Beallitasok.AnnounceForecast();
                return;
            }
            case "green":
            {
                Debug.WriteLine("Mai névnap felolvasás");
                Beallitasok.AnnounceNameDay();
                return;
            }
            case "browser":
            {
                if (!FullScreenChecker.IsAppInFullScreen())
                    Task.Run(() =>
                    {
                        Debug.WriteLine("Böngésző megnyitás");
                        var browserProcess = Process.Start(Utils.GetDefaultBrowser());
                        browserProcess.WaitForInputIdle();
                        var hWnd = new HWND(browserProcess.MainWindowHandle);
                        User32.SetForegroundWindow(hWnd);
                        User32.SetWindowPos(hWnd, HwndTopmost, 0, 0, 0, 0, TopmostFlags);
                    });
                return;
            }
            case "calculator":
            {
                if (!FullScreenChecker.IsAppInFullScreen())
                    Task.Run(() =>
                    {
                        Debug.WriteLine("Számológép megnyitás");
                        var calculatorProcess = Process.Start("calc.exe");
                        calculatorProcess.WaitForInputIdle();
                        var hWnd = new HWND(calculatorProcess.MainWindowHandle);
                        User32.SetForegroundWindow(hWnd);
                        User32.SetWindowPos(hWnd, HwndTopmost, 0, 0, 0, 0, TopmostFlags);
                    });

                return;
            }
        }

        if (result == Beallitasok.HangfelismerésSection["Kiváltó_Szó"].StringValue)
            Beallitasok.SimulateRealKeyPress(
                Utils.CharToHexKeyCode(Beallitasok.HangfelismerésSection["Eredmény_billentyű"].StringValue));
    }

    internal static void EnableVoiceRecognition()
    {
        if (Beallitasok.HangfelismerésSection["Bekapcsolva"].BoolValue && !IsRunning)
        {
            IsRunning = true;
            WaveIn.StartRecording();
        }
    }

    internal static void DisableVoiceRecognition()
    {
        if (Beallitasok.HangfelismerésSection["Bekapcsolva"].BoolValue && IsRunning)
        {
            IsRunning = false;
            WaveIn.StopRecording();
        }
    }
}