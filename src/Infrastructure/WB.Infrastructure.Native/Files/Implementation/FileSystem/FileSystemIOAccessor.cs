﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WB.Core.Infrastructure.FileSystem;

namespace WB.Infrastructure.Native.Files.Implementation.FileSystem
{
    internal class FileSystemIOAccessor : FileSystemAccessorBase, IFileSystemAccessor
    {
        public string ChangeExtension(string path1, string newExtension) => Path.ChangeExtension(path1, newExtension);
        public void MoveFile(string pathToFile, string newPathToFile)
        {
            if (File.Exists(newPathToFile))
                File.Delete(newPathToFile);
            File.Move(pathToFile, newPathToFile);
        }

        public void MoveDirectory(string pathToDir, string newPathToDir)
        {
            Directory.Move(pathToDir, newPathToDir);
        }


        public long GetFileSize(string filePath) => this.IsFileExists(filePath) ? new FileInfo(filePath).Length : -1;

        public DateTime GetCreationTime(string filePath)
            => this.IsFileExists(filePath) ? new FileInfo(filePath).CreationTime : DateTime.MinValue;

        public DateTime GetModificationTime(string filePath)
            => this.IsFileExists(filePath) ? new FileInfo(filePath).LastWriteTime : DateTime.MinValue;

        public bool IsDirectoryExists(string pathToDirectory) => Directory.Exists(pathToDirectory);

        public void DeleteDirectory(string path) => Directory.Delete(path, true);

        public string GetDirectory(string path) => Path.GetDirectoryName(path);
        public string GetDirectoryName(string path)
        {
            return new DirectoryInfo(path).Name;
        }

        public bool IsFileExists(string pathToFile) => File.Exists(pathToFile);

        public void DeleteFile(string pathToFile) => File.Delete(pathToFile);

        public string[] GetDirectoriesInDirectory(string pathToDirectory)
            => this.IsDirectoryExists(pathToDirectory) ? Directory.GetDirectories(pathToDirectory) : new string[0];

        public string[] GetFilesInDirectory(string pathToDirectory, bool searchInSubdirectories = false)
            => this.IsDirectoryExists(pathToDirectory)
                ? Directory.GetFiles(pathToDirectory, "*.*", searchInSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                : new string[0];

        public string[] GetFilesInDirectory(string pathToDirectory, string pattern)
            => this.IsDirectoryExists(pathToDirectory) ? Directory.GetFiles(pathToDirectory, pattern) : new string[0];
        

        public void MarkFileAsReadonly(string pathToFile) => File.SetAttributes(pathToFile, FileAttributes.ReadOnly);

        public long GetDirectorySize(string path)
        {
            long size = 0;
            // Add file sizes.
            var filesInDirectory = this.GetFilesInDirectory(path);
            foreach (var file in filesInDirectory)
                size += this.GetFileSize(file);
            // Add subdirectory sizes.
            var nestedDirectories = this.GetDirectoriesInDirectory(path);
            foreach (var nestedDirectory in nestedDirectories)
                size += this.GetDirectorySize(nestedDirectory);
            return size;
        }

        public void CreateDirectory(string path)
        {
            var intermediateDirectories = this.GetAllIntermediateDirectories(path);

            foreach (var directory in intermediateDirectories)
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
        }

        public Stream OpenOrCreateFile(string pathToFile, bool append)
        {
            var stream = File.OpenWrite(pathToFile);

            if (append && stream.CanSeek)
                stream.Seek(0, SeekOrigin.End);

            return stream;
        }


        public Stream ReadFile(string pathToFile) => File.OpenRead(pathToFile);

        public string MakeStataCompatibleFileName(string name)
        {
            var result = this.RemoveNonAscii(this.MakeValidFileName(name)).Trim();
            var clippedResult = result.Length < 128 ? result : result.Substring(0, 128);
            return string.IsNullOrWhiteSpace(clippedResult) ? "_" : clippedResult;
        }

        public string MakeValidFileName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            var invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            var invalidReStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);
            var fileNameWithReplaceInvalidChars = Regex.Replace(name, invalidReStr, "_");
            return fileNameWithReplaceInvalidChars.Substring(0, Math.Min(fileNameWithReplaceInvalidChars.Length, 128));
        }

        public void CopyFileOrDirectory(string sourceDir, string targetDir, bool overrideAll = false, string[] fileExtentionsFilter = null)
        {
            var attr = File.GetAttributes(sourceDir);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                var sourceDirectoryName = this.GetFileName(sourceDir);
                if (sourceDirectoryName == null)
                    return;
                var destDir = this.CombinePath(targetDir, sourceDirectoryName);
                this.CreateDirectory(destDir);

                foreach (var file in this.GetFilesInDirectory(sourceDir))
                    this.CopyFile(file, targetDir, overrideAll, fileExtentionsFilter);


                foreach (var directory in this.GetDirectoriesInDirectory(sourceDir))
                    this.CopyFileOrDirectory(directory, this.CombinePath(destDir, sourceDirectoryName), overrideAll);
            }
            else
            {
                this.CopyFile(sourceDir, targetDir, overrideAll, fileExtentionsFilter);
            }
        }

        private void CopyFile(string sourcePath, string backupFolderPath, bool overrideAll, string[] fileExtentionsFilter)
        {
            var sourceFileName = this.GetFileName(sourcePath);
            if (sourceFileName == null)
                return;

            if (fileExtentionsFilter != null)
            {
                if (!fileExtentionsFilter.Contains(GetFileExtension(sourcePath), StringComparer.InvariantCultureIgnoreCase))
                    return;
            }

            File.Copy(sourcePath, this.CombinePath(backupFolderPath, sourceFileName), overrideAll);
        }

        private IEnumerable<string> GetAllIntermediateDirectories(string path)
        {
            var pathChunks = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var allChunksCount = pathChunks.Length;

            for (var countOfChunksToTake = 1; countOfChunksToTake <= allChunksCount; countOfChunksToTake++)
            {
                var pathToIntermediateDirectory = string.Join(Path.DirectorySeparatorChar.ToString(),
                    pathChunks.Take(countOfChunksToTake));

                if (!string.IsNullOrEmpty(pathToIntermediateDirectory))
                    yield return pathToIntermediateDirectory;
            }
        }

        private string RemoveNonAscii(string s) => Regex.Replace(s, @"[^\u0000-\u007F]", string.Empty);
    }
}
