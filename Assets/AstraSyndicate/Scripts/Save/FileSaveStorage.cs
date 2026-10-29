using System;
using System.IO;

namespace AstraSyndicate.Save
{
    /// <summary>
    /// Stores each key as "{key}.sav" in one directory. A write goes to a temp file first and the
    /// previous file is kept as "{key}.bak", so an interrupted write never destroys both copies.
    /// </summary>
    public sealed class FileSaveStorage : ISaveStorage
    {
        private const string PrimaryExtension = ".sav";
        private const string BackupExtension = ".bak";
        private const string TempExtension = ".tmp";

        public FileSaveStorage(string rootDirectory)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
                throw new ArgumentException("A save directory is required.", nameof(rootDirectory));
            RootDirectory = rootDirectory;
        }

        public string RootDirectory { get; }

        public void Write(string key, byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));

            Directory.CreateDirectory(RootDirectory);
            string primary = PathFor(key, SaveCopy.Primary);
            string temp = PathFor(key, TempExtension);

            File.WriteAllBytes(temp, bytes);
            if (File.Exists(primary))
            {
                File.Copy(primary, PathFor(key, SaveCopy.Backup), overwrite: true);
                File.Delete(primary);
            }
            File.Move(temp, primary);
        }

        public bool TryRead(string key, SaveCopy copy, out byte[] bytes)
        {
            string path = PathFor(key, copy);
            bytes = File.Exists(path) ? File.ReadAllBytes(path) : null;
            return bytes != null;
        }

        public bool Exists(string key)
        {
            return File.Exists(PathFor(key, SaveCopy.Primary)) || File.Exists(PathFor(key, SaveCopy.Backup));
        }

        public void Delete(string key)
        {
            DeleteCopy(key, SaveCopy.Primary);
            DeleteCopy(key, SaveCopy.Backup);
            DeleteFile(PathFor(key, TempExtension));
        }

        public void DeleteCopy(string key, SaveCopy copy)
        {
            DeleteFile(PathFor(key, copy));
        }

        private static void DeleteFile(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }

        private string PathFor(string key, SaveCopy copy)
        {
            return PathFor(key, copy == SaveCopy.Primary ? PrimaryExtension : BackupExtension);
        }

        private string PathFor(string key, string extension)
        {
            ValidateKey(key);
            return Path.Combine(RootDirectory, key + extension);
        }

        // Keys become file names, so anything that could escape the save directory is rejected.
        private static void ValidateKey(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("A save key is required.", nameof(key));

            foreach (char c in key)
            {
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-')
                    throw new ArgumentException($"Save key '{key}' may only contain letters, digits, '_' and '-'.", nameof(key));
            }
        }
    }
}
