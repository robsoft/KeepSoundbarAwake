using System.Reflection;
using ManagedBass;
using System.Runtime.InteropServices;

public class KeepAwakeApp
{
    private static string _appName = "Keep Soundbar Awake Tool (ManagedBass, Cross-Platform Edition)";
    private static string _version = "v0.2";

    private static Timer? _timer;
    private static CancellationTokenSource _cts = new CancellationTokenSource();
    private static TimeSpan _workingHoursStart = TimeSpan.Zero; // Default to 00:00
    private static TimeSpan _workingHoursEnd = TimeSpan.FromHours(24); // Default to 24:00 (full day)
    private static bool _useWorkingHours = false;
    private static string _embeddedSilentResourceName = "KeepSoundbarAwake.keep-awake-19khz_3s.wav";
    private static string _embeddedTestResourceName = "KeepSoundbarAwake.keep-awake-1khz_3s.wav";
    private static bool _useEmbeddedTest = false;
    private static int _defaultIntervalMinutes = 5;
    private static bool _verboseMode = false;


    public static async Task Main(string[] args)
    {
        // handle the fact that we have platform-specific libraries that the Bass library will link to
        NativeLibrary.SetDllImportResolver(typeof(Bass).Assembly, (libraryName, assembly, searchPath) =>
        {
            // The libraryName here will be "bass" (what Bass.dllimport points to)
            if (libraryName == "bass")
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return NativeLibrary.Load("bass.dll", assembly, searchPath);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return NativeLibrary.Load("libbass.dylib", assembly, searchPath);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // For Linux, dynamically determine which one based on architecture
                    if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                    {
                        return NativeLibrary.Load("libbass_aarch64.so", assembly, searchPath);
                    }
                    else if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
                    {
                        return NativeLibrary.Load("libbass_x86_64.so", assembly, searchPath);
                    }
                }
            }
            // Let the default resolver handle other libraries if not "bass"
            return IntPtr.Zero;
        });

        // set defaults and then parse arguments the old-school way
        int periodMinutes = _defaultIntervalMinutes;
        string? betweenHoursString = null;

        // Loop through the arguments
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--help":
                case "--info":
                case "-h":
                case "-?":
                    ShowHelp();
                    return;

                case "--verbose":
                case "-v":
                    _verboseMode = true;
                    break;

                case "--test":
                case "-t":
                    _useEmbeddedTest = true;
                    break;

                case "--period":
                case "-p":
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out int parsedPeriod))
                    {
                        periodMinutes = parsedPeriod;
                        i++; // Consume the next argument as the value
                    }
                    else
                    {
                        Console.WriteLine("Error: --period requires an integer value.");
                        return; // Exit if invalid
                    }
                    break;

                case "--between":
                case "-b":
                    if (i + 1 < args.Length)
                    {
                        betweenHoursString = args[i + 1];
                        i++; // Consume the next argument as the value
                    }
                    else
                    {
                        Console.WriteLine("Error: --between requires a value in HH:mm-HH:mm format.");
                        return; // Exit if invalid
                    }
                    break;

                default:
                    Console.WriteLine($"Warning: Unknown argument '{args[i]}'.");
                    ShowHelp();
                    return;
            }
        }

        // Validate and parse 'between' string
        if (!string.IsNullOrWhiteSpace(betweenHoursString))
        {
            if (TryParseWorkingHours(betweenHoursString, out TimeSpan start, out TimeSpan end))
            {
                _workingHoursStart = start;
                _workingHoursEnd = end;
                _useWorkingHours = true;
            }
            else
            {
                Console.WriteLine($"Invalid --between format '{betweenHoursString}'. Using default (24 hours).");
            }
        }

        // Check we have the resource we've chosen to use
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();
        string resource = _useEmbeddedTest ? _embeddedTestResourceName : _embeddedSilentResourceName;
        if (!resourceNames.Contains(resource))
        {
            Console.WriteLine($"Error: Embedded resource '{resource}' not found.");
            _cts.Cancel(); // Use cancellation to exit Main loop
            return;
        }


        Output(_appName);
        Output(_version);

        // reflect any non-default parameters
        if (_useWorkingHours)
        {
            Output($"Operating between {_workingHoursStart:hh\\:mm} and {_workingHoursEnd:hh\\:mm}.");
        }
        if (periodMinutes != _defaultIntervalMinutes)
        {
            Output($"Using a period of {periodMinutes} minutes");
        }
        if (_useEmbeddedTest)
        {
            Output($"Using audible 1Khz test tone");
        }

        Output("Press Ctrl+C to stop.");


        if (!Bass.Init(-1, 44100)) // Use -1 ato say 'whatever, just use the fallback driver'
        {
            Console.WriteLine($"Error initializing BASS: {Bass.LastError}");
            _cts.Cancel(); // Use cancellation to exit Main loop
            return;
        }

        // Set up a timer to play the sound periodically
        _timer = new Timer(PlayEmbeddedSoundWithBass, null,
            TimeSpan.FromMinutes(periodMinutes),
            TimeSpan.FromMinutes(periodMinutes));

        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            Output("\nStopping...");
            _cts.Cancel();
            eventArgs.Cancel = true;
        };

        // Keep the application running until cancelled
        await Task.Delay(-1, _cts.Token).ContinueWith(task => { }, TaskContinuationOptions.OnlyOnCanceled);

        // Cleanup
        Bass.Free();

        Output("Stopped.");
    }


    // Helper method to parse working hours string
    private static bool TryParseWorkingHours(string input, out TimeSpan start, out TimeSpan end)
    {
        start = TimeSpan.Zero;
        end = TimeSpan.Zero;

        var parts = input.Split('-');
        if (parts.Length != 2) return false;

        if (!TimeSpan.TryParse(parts[0], out start)) return false;
        if (!TimeSpan.TryParse(parts[1], out end)) return false;

        if (start < TimeSpan.Zero || start >= TimeSpan.FromHours(24) ||
            end < TimeSpan.Zero || end > TimeSpan.FromHours(24)) // End can be 24:00
        {
            return false;
        }

        return true;
    }


    // Get BASS library to play the desired resource
    private static void PlayEmbeddedSoundWithBass(object? state)
    {
        // Check if current time is within working hours
        if (_useWorkingHours)
        {
            TimeSpan now = DateTime.Now.TimeOfDay;
            if (now < _workingHoursStart || now > _workingHoursEnd)
            {
                Output($"Outside working hours ({_workingHoursStart:hh\\:mm}-{_workingHoursEnd:hh\\:mm}). Skipping tone.");
                return; // Bail, having not played sound
            }
        }

        int stream = 0;
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resource = _useEmbeddedTest ? _embeddedTestResourceName : _embeddedSilentResourceName;

            using (Stream? audioStream = assembly.GetManifestResourceStream(resource))
            {
                if (audioStream == null)
                {
                    Console.WriteLine($"Error: Embedded resource '{resource}' not found during playback.");
                    return;
                }

                var memoryStream = new MemoryStream();
                audioStream.CopyTo(memoryStream);
                memoryStream.Position = 0;

                stream = Bass.CreateStream(memoryStream.ToArray(), 0, memoryStream.Length, BassFlags.AutoFree);

                if (stream == 0)
                {
                    Console.WriteLine($"Error creating BASS stream: {Bass.LastError}");
                    return;
                }

                Bass.ChannelSetAttribute(stream, ChannelAttribute.Volume, 0.5f); // Play at half volume

                if (!Bass.ChannelPlay(stream))
                {
                    Console.WriteLine($"Error playing BASS stream: {Bass.LastError}");
                    Bass.StreamFree(stream);
                    return;
                }
            }
            Output($"Played tone at {DateTime.Now:HH:mm:ss}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error playing embedded sound with ManagedBass: {ex.Message}");
        }
    }


    private static void Output(string output)
    {
        if (!_verboseMode) return;
        Console.WriteLine(output);
    }


    private static void ShowHelp()
    {
        Console.WriteLine(_appName);
        Console.WriteLine(_version);
        Console.WriteLine("Play a 19kHz (basically inaudible) tone, on a regular period, in an effort to keep");
        Console.WriteLine("your external audio output device (soundbar, amplifier, speakers) awake");
        Console.WriteLine("\nOptions;");
        Console.WriteLine("--verbose, -v         = verbose output, eg a message every time a tone is produced etc");
        Console.WriteLine("--test, -t            = test mode, plays an AUDIBLE (1kHz) tone");
        Console.WriteLine("--period, -p [mins]   = specify period between tones, in minutes (default is 5)");
        Console.WriteLine("--between, -b [range] = specify times to be active eg 08:30-17:30 for 8:30am to 5:30pm");
        Console.WriteLine("\n");
    }
}