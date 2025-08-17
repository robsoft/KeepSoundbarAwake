using System;
using System.IO;
using System.Media; // For playing WAV files
using System.Threading;
using System.Threading.Tasks;

public class KeepAwakeApp
{
    private static SoundPlayer? _soundPlayer;
    private static Timer? _timer;
    private static CancellationTokenSource _cts = new CancellationTokenSource();
    // https://www.wavtones.com/functiongenerator.php

    // Configuration values (to come from cmd line)
    private static string _audioFilePath = "keep-awake-19khz_3s.wav";
    private static int _intervalMinutes = 5; // Play every 5 minutes

    public static async Task Main(string[] args)
    {
        Console.WriteLine("Soundbar Keep-Awake Utility");
        Console.WriteLine("Press Ctrl+C to stop.");

        if (!File.Exists(_audioFilePath))
        {
            Console.WriteLine($"Error: Audio file not found at '{_audioFilePath}'");
            Console.WriteLine("Please provide/generate a 1-2 second WAV file (e.g., 19kHz) and place it here.");
            return;
        }

        try
        {
            _soundPlayer = new SoundPlayer(_audioFilePath);
            _soundPlayer.Load(); // Pre-load the WAV to avoid delay on first play

            // Set up a timer to play the sound periodically
            // Timer interval is in milliseconds
            _timer = new Timer(PlaySound, null, TimeSpan.Zero, TimeSpan.FromMinutes(_intervalMinutes));

            // Handle Ctrl+C to gracefully stop the application
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                Console.WriteLine("\nStopping...");
                _cts.Cancel();
                eventArgs.Cancel = true; // Prevent the process from terminating immediately
            };

            // Keep the application running until cancelled
            await Task.Delay(-1, _cts.Token).ContinueWith(task => { }, TaskContinuationOptions.OnlyOnCanceled);
        }
        catch (OperationCanceledException)
        {
            // Expected when Ctrl+C is pressed
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
        finally
        {
            // Clean up resources
            _timer?.Dispose();
            _soundPlayer?.Dispose();
            Console.WriteLine("Application stopped.");
        }
    }

    private static void PlaySound(object? state)
    {
        try
        {
            // This is synchronous, which is fine for a very short sound
            _soundPlayer?.Play();
            Console.WriteLine($"Played tone at {DateTime.Now:HH:mm:ss}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error playing sound: {ex.Message}");
        }
    }
}