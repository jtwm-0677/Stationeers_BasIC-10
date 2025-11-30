using System.Media;
using System.Runtime.InteropServices;

namespace BasicToMips.Editor.RetroEffects;

/// <summary>
/// Plays classic BASIC startup sounds
/// </summary>
public static class StartupBeepManager
{
    [DllImport("kernel32.dll")]
    private static extern bool Beep(int frequency, int duration);

    /// <summary>
    /// Plays a classic BASIC "READY" beep sequence
    /// </summary>
    public static void PlayStartupBeep()
    {
        try
        {
            // Classic computer startup beep - short high tone
            // Frequency: 800Hz, Duration: 150ms
            Task.Run(() =>
            {
                Beep(800, 100);
                Thread.Sleep(50);
                Beep(1000, 80);
            });
        }
        catch
        {
            // Fallback to system beep if Beep() fails
            try
            {
                SystemSounds.Beep.Play();
            }
            catch
            {
                // Silently ignore if no audio available
            }
        }
    }

    /// <summary>
    /// Plays a simple single beep
    /// </summary>
    public static void PlaySimpleBeep()
    {
        try
        {
            Task.Run(() => Beep(880, 100)); // A5 note, 100ms
        }
        catch
        {
            // Silently ignore
        }
    }
}
