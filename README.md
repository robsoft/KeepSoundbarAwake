## KeepSoundbarAwake

(moved here from RobUttley/KeepSoundbarAwake)

Initial release.
Play a 19kHz (basically inaudible) tone, on a regular period, in an effort to keep your external audio output device (soundbar, amplifier, speakers) awake

Options;
```
--verbose, -v         = verbose output, eg a message every time a tone is produced etc
--test, -t            = test mode, plays an AUDIBLE (1kHz) tone
--period, -p [mins]   = specify period between tones, in minutes (default is 5)
--between, -b [range] = specify times to be active eg 08:30-17:30 for 8:30am to 5:30pm
```

eg
```
KeepSoundbarAwake-osx-64 --verbose --test --period 1
```
Will run with some messages, progress indicators etc going to the command line, playing an audible sound every 1 minute

```
KeepSoundbarAwake-osx-64 --between 08:30-17:30
```
Will play the inaudible sound every 5 minutes between 8:30am and 5:30pm.




tones came from 
https://www.wavtones.com/functiongenerator.php


on Mac you might need to do this to the libbass.dylib file;  
```
chmod +x libbass.dylib  
xattr -d com.apple.quarantine libbass.dylib  
```
Possibly something similar to the Linux version


todo:  
- Test Mac M3, Linux x64 and arm, Windows x86 platforms  
- Instructions for building on each platform  

