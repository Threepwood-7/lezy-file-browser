using System;
using System.IO;

namespace LezyFileBrowser
{
    // Lightweight file metadata holder used in place of FileInfo for the hot listing path.
    // When populated from Everything's index, no disk I/O is needed for Name/Length/CreationTime.
    // Directory.GetFiles() (used in a few non-hot paths) still hits disk via DirectoryInfo.
    public class FileData
    {
        public string FullName      { get; }
        public string Name          { get; }
        public long   Length        { get; }
        public DateTime CreationTime { get; }
        public string DirectoryName { get; }

        // DirectoryInfo constructed on-demand from the path; .Name and .FullName are free,
        // .GetFiles() etc. will hit the filesystem (only called outside the hot listing path).
        public DirectoryInfo Directory => new DirectoryInfo(DirectoryName);

        public FileData(string fullPath, long length, DateTime creationTime)
        {
            FullName      = fullPath;
            Name          = Path.GetFileName(fullPath);
            Length        = length;
            CreationTime  = creationTime;
            DirectoryName = Path.GetDirectoryName(fullPath);
        }

        // Wraps a real FileInfo — used for fallback enumeration and DuplicateFinder results.
        public FileData(FileInfo fi)
            : this(fi.FullName, fi.Length, fi.CreationTime) { }
    }
}
