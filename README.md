# FixAvi4cc
## Tool for fixing avi files with Xvid/Divx codec to play on modern TVs
For single file you can use [FourCC changer](https://www.fourcc.org/changer/). My tool is for batch processing.
### How to use
```
FixAvi4cc.exe [options] dirName1 dirName2 fileName1 fileName2...
FixAvi4cc.exe -backup -skipReadOnly -skipCheck -topDirectoryOnly -log logFileName dirName1 dirName2 fileName1 fileName2...
-backup                 Backup original file
-skipReadOnly           Skip read-only files
-skipCheck              Skip FourCC checking equals 'xvid' or 'divx'
-topDirectoryOnly       Search avi files in top directory only, not recursive in subdirectories
-log logFileName        Log output to logFileName
-noQuestion             Skip asking for continue
dirName1 dirName2...    Paths to directories where to search .avi files
fileName1 fileName2...  Paths to .avi files
-?,-h,-help             This help output
all arguments are not required, by default it searches all .avi files in current directory 
and subdirectories with xvid/divx FourCC and changes it to FMP4 without backup(read-only files too)
```
Based on [this thread](https://www.avforums.com/threads/a-simple-trick-to-play-avi-xvid-divx-videos-on-samsung-tv-without-re-encoding.2341579/)
