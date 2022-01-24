using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace FixAvi4cc
{
    public class Fixer
    {
        /*
         * So, I started experimenting, and I found out that simply changing codec ID (also known as FourCC)
         * in the AVI file from XVID (or, less common DIVX or DX50) into FMP4 fixes the problem!
         * (For videos encoded with the oldest DivX version, the FourCC is DIV3, and has to be changed to MP43).
         */

        private readonly int[] _offsets = {112, 188};

        private readonly string _new4Cc = "FMP4";
        private readonly byte[] _new4CcLower;
        private readonly byte[] _new4CcUpper;
        private readonly bool _canRun;

        private readonly string _newDivx4Cc = "MP43";
        private readonly byte[] _newDivx4CcLower;
        private readonly byte[] _newDivx4CcUpper;

        private readonly string[] _allowedFourCC = {"xvid", "divx", "div3" };

        private bool _skipReadOnly;
        private bool _skipCheck;
        private bool _backup;

        private bool _logToFile;
        private string _logFileName;
        private StringBuilder _log;

        private SearchOption _searchOption = SearchOption.AllDirectories;
        private IEnumerable<string> _filePaths;
        

        public Fixer(string[] args)
        {
            _new4CcLower = Encoding.ASCII.GetBytes(_new4Cc.ToLower());
            _new4CcUpper = Encoding.ASCII.GetBytes(_new4Cc.ToUpper());

            _newDivx4CcLower = Encoding.ASCII.GetBytes(_newDivx4Cc.ToLower());
            _newDivx4CcUpper = Encoding.ASCII.GetBytes(_newDivx4Cc.ToUpper());

            _canRun = Initialize(args);
        }

        public void Run()
        {
            if (!_canRun)
                return;

            try
            {
                foreach (var filePath in _filePaths)
                {
                    ProcessFile(filePath);
                }
            }
            catch (Exception ex)
            {
                LogLine(ex.ToString());
            }

            if (_logToFile)
            {
                File.WriteAllText(_logFileName, _log.ToString());
            }
        }
        
        private bool Initialize(string[] args)
        {
            if (args?.Length > 0)
            {
                if (args.Contains("?") || args.Contains("-?") || args.Contains("-h") || args.Contains("-help"))
                {
                    ShowHelp();
                    return false;
                }

                var argsList = new List<string>(args);

                _skipReadOnly = argsList.Remove("-skipReadOnly");
                _skipCheck = argsList.Remove("-skipCheck");
                _backup = argsList.Remove("-backup");

                _searchOption = argsList.Remove("-topDirectoryOnly") ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories;

                _logToFile = argsList.Remove("-log");
                if (_logToFile)
                {
                    _logFileName = GetValueByKeyFromArgs(args, "-log");

                    if (string.IsNullOrWhiteSpace(_logFileName))
                    {
                        ShowHelp();
                        return false;
                    }

                    argsList.Remove(_logFileName);
                }

                if (argsList.Any(u => u.StartsWith("-")))
                {
                    ShowHelp();
                    return false;
                }

                _filePaths = GetFilePathsFromArray(argsList.ToArray());
                if (_filePaths == null)
                {
                    ShowHelp();
                    return false;
                }
            }

            if (_filePaths?.Any() != true)
            {
                _filePaths = GetDirectoryFiles(Directory.GetCurrentDirectory(), "*.avi", _searchOption);
            }

            if (_filePaths?.Any() != true)
            {
                LogLine("0 .avi files found to process");

                return false;
            }
            
            return true;
        }

        private void ShowHelp()
        {
            Console.WriteLine("FixAvi4cc.exe [options] dirName1 dirName2 fileName1 fileName2...");
            Console.WriteLine("FixAvi4cc.exe -backup -skipReadOnly -skipCheck -topDirectoryOnly -log logFileName dirName1 dirName2 fileName1 fileName2...");
            Console.WriteLine("-backup\t\t\tBackup original file");
            Console.WriteLine("-skipReadOnly\t\tSkip read-only files");
            Console.WriteLine("-skipCheck\t\tSkip FourCC checking equals 'xvid' or 'divx'");
            Console.WriteLine("-topDirectoryOnly\tSearch avi files in top directory only, not recursive");
            Console.WriteLine("-log logFileName\tLog output to logFileName");
            Console.WriteLine("dirName1 dirName2...\tPaths to directories where to search .avi files");
            Console.WriteLine("fileName1 fileName2...\tPaths to .avi files");
            Console.WriteLine("-?,-h,-help\t\tThis help output");
            Console.WriteLine("all arguments are not required, by default it searches all .avi files in current directory and subdirectories with xvid/divx FourCC and changes it to FMP4 without backup(read-only files too)");
        }

        private string GetValueByKeyFromArgs(string[] args, string key)
        {
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == key)
                {
                    if (!args[i + 1].StartsWith("-"))
                        return args[i + 1];
                }
            }

            return null;
        }

        private IEnumerable<string> GetFilePathsFromArray(string[] array)
        {
            var result = new List<string>();
            for (var i = array.Length - 1; i >= 0; i--)
            {
                var path = array[i];
                
                if (Directory.Exists(path))
                    result.AddRange(GetDirectoryFiles(path, "*.avi", _searchOption));
                else if (File.Exists(path) && Path.GetExtension(path).ToLower() == "avi")
                    result.Add(path);
                else
                {
                    LogLine($"Invalid file/dir path: {path}");

                    return null;
                }
            }

            return result.ToArray();
        }
        
        private void ProcessFile(string filePath)
        {
            try
            {
                LogLine($"{filePath}");

                var fileInfo = new FileInfo(filePath);

                if (fileInfo.IsReadOnly)
                {
                    if (_skipReadOnly)
                    {
                        LogLine("Read-only, skipping");
                        LogLine();

                        return;
                    }

                    LogLine("Turning off read-only flag");

                    fileInfo.IsReadOnly = false;
                }

                FixFile(filePath);
            }
            catch (Exception ex)
            {
                LogLine(ex.ToString());
                LogLine();
            }
        }

        /// <summary>
        /// A safe way to get all the files in a directory and sub directory without crashing on UnauthorizedException or PathTooLongException
        /// </summary>
        /// <param name="rootPath">Starting directory</param>
        /// <param name="patternMatch">Filename pattern match</param>
        /// <param name="searchOption">Search subdirectories or only top level directory for files</param>
        /// <returns>List of files</returns>
        private static IEnumerable<string> GetDirectoryFiles(string rootPath, string patternMatch, SearchOption searchOption)
        {
            var foundFiles = Enumerable.Empty<string>();

            if (searchOption == SearchOption.AllDirectories)
            {
                try
                {
                    var subDirs = Directory.EnumerateDirectories(rootPath);
                    foreach (string dir in subDirs)
                    {
                        // Add files in subdirectories recursively to the list
                        foundFiles = foundFiles.Concat(GetDirectoryFiles(dir, patternMatch, searchOption)); 
                    }
                }
                catch (UnauthorizedAccessException) { }
                catch (PathTooLongException) { }
            }

            try
            {
                // Add files from the current directory
                foundFiles = foundFiles.Concat(Directory.EnumerateFiles(rootPath, patternMatch, SearchOption.TopDirectoryOnly));
            }
            catch (UnauthorizedAccessException) { }

            return foundFiles;
        }

        private void FixFile(string filePath)
        {
            using (var fs = new FileStream(filePath,
                FileMode.Open,
                FileAccess.ReadWrite))
            {
                var buffer = new byte[4];

                fs.Position = _offsets[0];

                if (fs.Read(buffer, 0, 4) == 4)
                {
                    var codec = Encoding.ASCII.GetString(buffer).ToLower();

                    LogLine($"Used FourCC: {codec}");
                    if (!_skipCheck && !_allowedFourCC.Contains(codec))
                    {
                        LogLine("Not divx/xvid/div3 FourCC, skipping");
                        LogLine();
                        return;
                    }

                    var oldDivx = codec == "div3";

                    if (_backup)
                        File.Copy(filePath, $"{filePath}.backup");

                    if (oldDivx)
                    {
                        fs.Position = _offsets[0];
                        fs.Write(_newDivx4CcLower, 0, _newDivx4CcLower.Length);

                        fs.Position = _offsets[1];
                        fs.Write(_newDivx4CcUpper, 0, _newDivx4CcUpper.Length);

                        LogLine($"Changed FourCC: {_newDivx4Cc}");
                        LogLine();
                    }
                    else
                    {
                        fs.Position = _offsets[0];
                        fs.Write(_new4CcLower, 0, _new4CcLower.Length);

                        fs.Position = _offsets[1];
                        fs.Write(_new4CcUpper, 0, _new4CcUpper.Length);

                        LogLine($"Changed FourCC: {_new4Cc}");
                        LogLine();
                    }
                }
            }
        }
        
        private void LogLine(string s = "")
        {
            if (_logToFile)
            {
                _log.AppendLine(s);
            }
            else
            {
                Console.WriteLine(s);
            }
        }
    }
}