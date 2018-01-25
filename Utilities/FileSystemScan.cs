using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Zrop.FileBackup.Console.Utilities
{
    public static class FileSystemScan
    {

        public static List<FileInfo> files = new List<FileInfo>();  // List that will hold the files and subfiles in path
        public static List<DirectoryInfo> folders = new List<DirectoryInfo>(); // List that hold direcotries that cannot be accessed
        public static List<DirectoryInfo> inaccessfolders = new List<DirectoryInfo>();
        public static int DirectoryCount;

        public static void FullDirList(DirectoryInfo dir, string searchPattern, List<string> excludeList)
        {
            //System.Console.WriteLine("Directory {0}", dir.FullName);
            // list the files
            //List<string> excluded = new List<string>() { @"F:\dir1\dir2", @"F:\dir1\dir3" };
            //bool containsItem = false;

            DirectoryCount++;

            if (DirectoryCount % 1000 == 0 && DirectoryCount != 0)
            {
                System.Console.WriteLine("Directories: {0} -- {1}", DirectoryCount, DateTime.Now);
            }

                if (excludeList.Count > 0)
            {
                var containsItem = excludeList.Any(item => item == dir.FullName);
                if (containsItem)
                {
                    return;
                }                
            }

            try
            {
                foreach (FileInfo f in dir.GetFiles(searchPattern))
                {
                    //System.Console.WriteLine("File {0} {1}", f.FullName, f.CreationTime);
                    files.Add(f);

                }
            }
            catch
            {
                inaccessfolders.Add(dir);
                //System.Console.WriteLine("Directory {0}  \n could not be accessed!!!!", dir.FullName);
                return;  // We alredy got an error trying to access dir so dont try to access it again
            }

            // process each directory
            // If I have been able to see the files in the directory I should also be able 
            // to look at its directories so I dont think I should place this in a try catch block
            foreach (DirectoryInfo d in dir.GetDirectories())
            {
                folders.Add(d);
                FullDirList(d, searchPattern, excludeList);
            }

        }
    }
}
