using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zrop.FileBackup.Console.Utilities
{

    public static class SafefWalk
    {
        public static IEnumerable<string> GetDirectoryFiles(string rootPath, string patternMatch, SearchOption searchOption)
        {
            var foundFiles = Enumerable.Empty<string>();

            if (searchOption == SearchOption.AllDirectories)
            {
                try
                {
                    IEnumerable<string> subDirs = Directory.EnumerateDirectories(rootPath);
                    foreach (string dir in subDirs)
                    {
                        foundFiles = foundFiles.Concat(GetDirectoryFiles(dir, patternMatch, searchOption));
                        // Add files in subdirectories recursively to the list
                    }
                }
                catch (DirectoryNotFoundException e)
                {
                    //Console.Write("DirectoryNotFoundException");
                }
                catch (UnauthorizedAccessException e)
                {
                    //Console.Write("UnauthorizedAccessException");
                }
                catch (PathTooLongException e)
                {
                    //Console.Write("PathTooLongException");
                }
            }

            try
            {
                foundFiles = foundFiles.Concat(Directory.EnumerateFiles(rootPath, patternMatch)); // Add files from the current directory
            }
            catch (UnauthorizedAccessException) { }

            return foundFiles;
        }
    }
}
