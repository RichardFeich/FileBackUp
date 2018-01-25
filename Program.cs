using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlServerCe;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Zrop.FileBackup.Console.Utilities;

namespace Zrop.FileBackup.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("*** START FILE BACKUP ***");

            Assembly ass = Assembly.GetExecutingAssembly();
            string path = System.IO.Path.GetDirectoryName(ass.Location);

            string excludeFile = path + @"\exclude.txt";
            string extensionsFile = path + @"\extensions.txt";
            var sourceNode = ConfigurationManager.AppSettings["SourceNode"];
            var destinationNode = ConfigurationManager.AppSettings["DestinationNode"];
            var useMetadata = ConfigurationManager.AppSettings["UseMetadata"] == "true" ? true : false;
            var logFile = destinationNode + @"\log.txt";
            var errorLogFile = destinationNode + @"\error.txt";

            var excludeList = new List<string>();
            if (File.Exists(excludeFile))
            {
                var fileLines = File.ReadAllLines(excludeFile);
                excludeList = new List<string>(fileLines);
            }

            var extensionsList = new List<string>();
            if (File.Exists(extensionsFile))
            {
                var fileLines = File.ReadAllLines(extensionsFile);
                extensionsList = new List<string>(fileLines);
            }

            OrganizationFormat organizationFormat = (OrganizationFormat)Enum.Parse(typeof(OrganizationFormat), ConfigurationManager.AppSettings["OrganizationFormat"]);

            DirectoryInfo di = new DirectoryInfo(sourceNode);
            FileSystemScan.FullDirList(di, "*", excludeList);

            System.Console.WriteLine("Total File Count: {0}", FileSystemScan.files.Count);
            System.Console.WriteLine("Directory Count: {0}", FileSystemScan.DirectoryCount);
            System.Console.WriteLine("Folder Count: {0}", FileSystemScan.folders.Count);
            System.Console.WriteLine("Inaccessible Folder Count: {0}", FileSystemScan.inaccessfolders.Count);

            var fileList = FileSystemScan.files.Where(s => extensionsList.Contains(s.Extension.ToLower())).ToList();

            System.Console.WriteLine("Copy File Count: {0}", fileList.Count);

            foreach (var file in fileList)
            {

                var destinationPath = CreateDestinationPath(file, destinationNode, useMetadata, organizationFormat);

                CopyFileExactly(file.FullName, destinationPath, errorLogFile);
                //System.Console.WriteLine("File {0} {1} {2}", info.Name, info.CreationTime, fullDestPath);
            }
            System.Console.WriteLine("*** END FILE BACKUP ***");
            System.Console.Read();
        }

        public static string CreateDestinationPath(FileInfo sourcfileInfo, string destinationNode, bool useMetatData, OrganizationFormat orgFormat)
        {

            DateTime fileDate;

            if (useMetatData)
            {
                var takenDate = GetDateTakenFromImage(sourcfileInfo.FullName);

                if (takenDate != DateTime.MinValue)
                {
                    fileDate = takenDate;
                }
                else
                {
                    fileDate = sourcfileInfo.CreationTime > sourcfileInfo.LastWriteTime
                        ? sourcfileInfo.LastWriteTime
                        : sourcfileInfo.CreationTime;
                }
            }
            else
            {
                fileDate = sourcfileInfo.CreationTime > sourcfileInfo.LastWriteTime
                    ? sourcfileInfo.LastWriteTime
                    : sourcfileInfo.CreationTime;
            }

            switch (orgFormat)
            {
                case OrganizationFormat.ByYear:
                    {
                        var year = fileDate.Year.ToString();
                        return destinationNode + "\\" + year + "\\" + sourcfileInfo.Name;
                    }
                case OrganizationFormat.ByMonth:
                    {
                        var month = fileDate.ToString("MMM");
                        var monthNumber = fileDate.ToString("MM");
                        var year = fileDate.Year.ToString();
                        return destinationNode + "\\" + year + "\\" + monthNumber + "_" + month + "\\" + sourcfileInfo.Name;
                    }
                case OrganizationFormat.ByDay:
                    {
                        var month = fileDate.ToString("MMM");
                        var monthNumber = fileDate.ToString("MM");
                        var year = fileDate.Year.ToString();
                        var dayOfMonth = fileDate.Day.ToString().PadLeft(2, '0');
                        return destinationNode + "\\" + year + "\\" + monthNumber + "_" + month + "\\" + dayOfMonth + "\\" + sourcfileInfo.Name;
                    }
                case OrganizationFormat.Flat:
                default:
                    {
                        return destinationNode + "\\" + sourcfileInfo.Name;
                    }
            }
        }

        public static string CreatePath(DateTime dateStamp, string topNode, OrganizationFormat orgFormat)
        {
            switch (orgFormat)
            {
                case OrganizationFormat.ByYear:
                    {
                        var year = dateStamp.Year.ToString();
                        return topNode + "\\" + year + "\\";
                    }
                case OrganizationFormat.ByMonth:
                    {
                        var month = dateStamp.ToString("MMM");
                        var monthNumber = dateStamp.ToString("MM");
                        var year = dateStamp.Year.ToString();
                        return topNode + "\\" + year + "\\" + monthNumber + "_" + month + "\\";
                    }
                case OrganizationFormat.ByDay:
                    {
                        var month = dateStamp.ToString("MMM");
                        var monthNumber = dateStamp.ToString("MM");
                        var year = dateStamp.Year.ToString();
                        var dayOfMonth = dateStamp.Day.ToString().PadLeft(2, '0');
                        return topNode + "\\" + year + "\\" + monthNumber + "_" + month + "\\" + dayOfMonth + "\\";
                    }
                case OrganizationFormat.Flat:
                default:
                    {
                        return topNode + "\\";
                    }
            }
        }

        public static void CopyFileExactly(string copyFromPath, string copyToPath, string errorLogPath)
        {

            //var errorPath = Path.Combine(copyToPath, @"..\..\..");
            var toPath = Path.GetDirectoryName(copyToPath);

            try
            {
                if (!Directory.Exists(toPath))
                {
                    Directory.CreateDirectory(toPath);
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(errorLogPath + "//error.txt", copyFromPath + Environment.NewLine);
                System.Console.WriteLine("File {0} {1} {2}", "EXCEPTION-1", ex.Message, "");
                return;
            }

            try
            {
                var originFile = new FileInfo(copyFromPath);
                var destinationFile = new FileInfo(copyToPath);

                if (File.Exists(copyToPath))
                {
                    File.AppendAllText(toPath + "//dup.txt", copyFromPath + Environment.NewLine);
                    System.Console.WriteLine("File {0} {1} {2}", "DUP", copyFromPath, copyToPath);
                    return;
                }
                System.Console.WriteLine("File {0} {1} {2}", "COPY", copyFromPath, copyToPath);
                originFile.CopyTo(copyToPath, true);

                File.AppendAllText(toPath + "//index.txt", copyFromPath + Environment.NewLine);

                destinationFile.CreationTime = originFile.CreationTime;
                destinationFile.LastWriteTime = originFile.LastWriteTime;
                destinationFile.LastAccessTime = originFile.LastAccessTime;
            }
            catch (Exception ex)
            {
                File.AppendAllText(errorLogPath + "//error.txt", copyFromPath + Environment.NewLine);
                System.Console.WriteLine("File {0} {1} {2}", "EXCEPTION-2", ex.Message, "");
                return;
            }
            return;
        }

        private static Regex r = new Regex(":");

        public static DateTime GetDateTakenFromImage(string path)
        {
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                using (Image myImage = Image.FromStream(fs, false, false))
                {
                    if (myImage.PropertyIdList.Any(x => x == 36867))
                    {
                        PropertyItem propItem = myImage.GetPropertyItem(36867);
                        string dateTaken = r.Replace(Encoding.UTF8.GetString(propItem.Value), "-", 2);
                        return DateTime.Parse(dateTaken);
                    }
                    return DateTime.MinValue;
                }
            }
            catch (Exception ex)
            {
                return DateTime.MinValue;
            }

        }

        public static string CreateMd5ForFolder(string path)
        {
            // assuming you want to include nested folders
            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                                 .OrderBy(p => p).ToList();

            MD5 md5 = MD5.Create();

            for (int i = 0; i < files.Count; i++)
            {
                string file = files[i];

                // hash path
                string relativePath = file.Substring(path.Length + 1);
                byte[] pathBytes = Encoding.UTF8.GetBytes(relativePath.ToLower());
                md5.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);

                // hash contents
                byte[] contentBytes = File.ReadAllBytes(file);
                if (i == files.Count - 1)
                    md5.TransformFinalBlock(contentBytes, 0, contentBytes.Length);
                else
                    md5.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);
            }

            return BitConverter.ToString(md5.Hash).Replace("-", "").ToLower();
        }

    }
}
