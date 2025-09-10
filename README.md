# 🔊 KeepSoundbarAwake
(moved here from RobUttley/KeepSoundbarAwake)

A simple cross-platform utility to prevent soundbars from entering standby mode during periods of inactivity.

## 💡 The Problem

Many soundbars and audio devices feature an "auto-standby" function. If no audio signal is detected for a few minutes, they power down to save energy. While this is useful, it can be incredibly frustrating during typical work-from-home scenarios (e.g., connected to a laptop via an aux cable for Teams calls). When a new sound *does* come through, there's a noticeable delay (often a couple of seconds) as the soundbar "wakes up," cutting off the beginning of speech or notifications.

This utility was born out of that exact frustration with a Goodmans soundbar!

## ✨ The Solution

`KeepSoundbarAwake` sends an inaudible, high-frequency audio tone (19 kHz) through your system's default audio output at regular intervals. This subtle "ping" tricks the soundbar into thinking there's continuous audio activity, keeping it awake and ready to play sound instantly.

### Why 19 kHz?

*   **Inaudible to Humans:** Most human hearing tops out around 18-20 kHz, especially with age. 19 kHz is generally beyond comfortable human perception.
*   **Inaudible to Dogs:** While dogs can hear much higher frequencies (up to 45-65 kHz), 19 kHz is typically too low to bother them, unlike truly ultrasonic tones.
*   **Soundbar Compatible:** Most consumer soundbars and speakers can still reproduce or at least detect a signal at 19 kHz, even if the fidelity is limited.
*   **Teams Friendly:** Because the tone is outside the human speech frequency range, it's highly unlikely to interfere with video conferencing software like Microsoft Teams, which primarily focuses its noise suppression and voice activity detection on the human vocal spectrum.

## 🚀 Features

*   **Cross-Platform:** Available for Windows, macOS, and Linux (x64/ARM64).
*   **Lightweight:** A single, self-contained executable for each platform.
*   **Configurable Interval:** Set how frequently the tone is played.
*   **Working Hours Control:** Define a time window (e.g., 09:00-17:00) during which the utility operates, allowing your soundbar to genuinely sleep outside of these hours.
*   **Verbose Output:** Suppress regular output for silent background operation, or enable it for debugging.

## ⬇️ Download

Grab the latest release binaries from the [GitHub Releases page](https://github.com/robsoft/KeepSoundbarAwake/releases).

Each platform's release is packaged in a `.zip` file containing the single executable and its required native BASS audio library.

## ⚙️ Usage

Once you've downloaded the appropriate `.zip` file for your operating system and extracted its contents, you can run `KeepSoundbarAwake` (or `KeepSoundbarAwake.exe` on Windows) from your terminal/command prompt.

```bash
# Basic usage (defaults to 5-minute interval, 24-hour operation)
./KeepSoundbarAwake

# Specify a 10-minute interval
./KeepSoundbarAwake --period 10
./KeepSoundbarAwake -p 10

# Operate only between 9 AM and 5 PM (09:00-17:00)
./KeepSoundbarAwake --between 09:00-17:00
./KeepSoundbarAwake -b 09:00-17:00

# Combine options and enable verbose output
./KeepSoundbarAwake -p 15 -b 08:30-18:30 --verbose
./KeepSoundbarAwake -p 15 -b 08:30-18:30 -v
```

### Command-Line Options:

*   `-p`, `--period <minutes>`: Sets the interval (in minutes) between tone plays. Default: `5` minutes.
*   `-b`, `--between <HH:mm-HH:mm>`: Sets the working hours. The utility will only play the tone within this window. Example: `09:00-17:00`. Default: `00:00-24:00` (always active).
*   `-v`, `--verbose`: Enables detailed output messages, useful for debugging. Default: Disabled (silent operation).
*   `-t`, `--test`: Test mode, plays an AUDIBLE (1kHz) tone instead of he INAUDIBLE (19Khz) one  


## 🖥️ Platform Specifics

`KeepSoundbarAwake` is a .NET console application that leverages the powerful **BASS Audio Library** (via `ManagedBass`) for cross-platform audio playback. The specific native BASS library (`bass.dll`, `libbass.dylib`, `libbass_x64.so`, `libbass_aarch64.so`) is bundled alongside the executable in each platform's `.zip` release.

### Windows (x64)

*   **Executable:** `KeepSoundbarAwake.exe`
*   **Native BASS Library:** `bass.dll`
*   **Audio Backend:** BASS utilizes standard Windows audio APIs like DirectSound or WASAPI to output sound.
*   **Usage Notes:** Run directly from Command Prompt or PowerShell.

### macOS (Intel `osx-x64` & Apple Silicon `osx-arm64`)

*   **Executable:** `KeepSoundbarAwake`  
*   **Native BASS Library:** `libbass.dylib`  
on Mac you might need to do this to the libbass.dylib file;  
```
chmod +x libbass.dylib  
xattr -d com.apple.quarantine libbass.dylib  
```

### Linux (x64)  
* I've had this running on Omarchy (!) and Ubuntu 2024
  

## Tones came from  
https://www.wavtones.com/functiongenerator.php

## Todo (Sept 2025):    
- [ ] Test release on each of the Mac M3, Linux x64 and arm, Windows x86 platforms  
- [ ] Instructions for actually building in VSCode on each platform  

