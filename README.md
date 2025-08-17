## KeepSoundbarAwake

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

