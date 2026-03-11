using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LezyFileBrowser
{
    public class DateTimeSortDesc : Comparer<ListViewItem>
    {
        public override int Compare(ListViewItem x, ListViewItem y)
        {
            return y.SubItems[1].Text.CompareTo(x.SubItems[1].Text);
        }
    }

    public class Util
    {
        public bool IsDangerousExtension(string _s)
        {
            string s = _s.ToLower().Trim();

            return
                s.EndsWith("ahk") ||
                s.EndsWith("apk") ||
                s.EndsWith("apkx") ||
                s.EndsWith("application") ||
                s.EndsWith("bat") ||
                s.EndsWith("cmd") ||
                s.EndsWith("com") ||
                s.EndsWith("cpl") ||
                s.EndsWith("csproj") ||
                s.EndsWith("doc") ||
                s.EndsWith("docm") ||
                s.EndsWith("docx") ||
                s.EndsWith("dotm") ||
                s.EndsWith("eml") ||
                s.EndsWith("exe") ||
                s.EndsWith("gadget") ||
                s.EndsWith("help") ||
                s.EndsWith("hlp") ||
                s.EndsWith("hta") ||
                s.EndsWith("htm") ||
                s.EndsWith("html") ||
                s.EndsWith("inf") ||
                s.EndsWith("jar") ||
                s.EndsWith("js ") ||
                s.EndsWith("js") ||
                s.EndsWith("jse") ||
                s.EndsWith("lnk") ||
                s.EndsWith("mht") ||
                s.EndsWith("msc") ||
                s.EndsWith("msh") ||
                s.EndsWith("msh1") ||
                s.EndsWith("msh1xml") ||
                s.EndsWith("msh2") ||
                s.EndsWith("msh2xml") ||
                s.EndsWith("mshxml") ||
                s.EndsWith("msi") ||
                s.EndsWith("msp") ||
                s.EndsWith("nzb") ||
                s.EndsWith("pif") ||
                s.EndsWith("potm") ||
                s.EndsWith("ppam") ||
                s.EndsWith("ppsm") ||
                s.EndsWith("ppt") ||
                s.EndsWith("pptm") ||
                s.EndsWith("pptx") ||
                s.EndsWith("ps1") ||
                s.EndsWith("ps1xml") ||
                s.EndsWith("ps2") ||
                s.EndsWith("ps2xml") ||
                s.EndsWith("psc1") ||
                s.EndsWith("psc2") ||
                s.EndsWith("reg") ||
                s.EndsWith("rtf") ||
                s.EndsWith("scf") ||
                s.EndsWith("scr") ||
                s.EndsWith("sh") ||
                s.EndsWith("sldm") ||
                s.EndsWith("sln") ||
                s.EndsWith("url") ||
                s.EndsWith("vb,") ||
                s.EndsWith("vbe") ||
                s.EndsWith("vbproj") ||
                s.EndsWith("vbs") ||
                s.EndsWith("ws") ||
                s.EndsWith("wsc") ||
                s.EndsWith("wsf") ||
                s.EndsWith("wsh") ||
                s.EndsWith("xlam") ||
                s.EndsWith("xls") ||
                s.EndsWith("xlsm") ||
                s.EndsWith("xlsx") ||
                s.EndsWith("xltm") ||
                s.EndsWith("zzz")
                ;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetDiskFreeSpaceEx(string lpDirectoryName,
        out ulong lpFreeBytesAvailable,
        out ulong lpTotalNumberOfBytes,
        out ulong lpTotalNumberOfFreeBytes);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetVolumePathName(string lpszFileName, StringBuilder lpszVolumePathName, uint cchBufferLength);

        // Returns the volume mount point for the given path (e.g. "C:\", "D:\Mounts\Data\").
        // Handles NTFS mount points correctly, not just drive letters.
        public static string GetVolumeMountPoint(string path)
        {
            var sb = new StringBuilder(261);
            if (GetVolumePathName(path, sb, (uint)sb.Capacity))
                return sb.ToString();
            // Fallback: use Path.GetPathRoot (drive-letter only, no mount point awareness)
            return Path.GetPathRoot(path);
        }

        public static bool DriveFreeBytes(string folderName, out ulong freespace, out ulong totalspace)
        {
            freespace = 0;
            totalspace = 0;

            if (string.IsNullOrEmpty(folderName))
            {
                throw new ArgumentNullException("folderName");
            }

            if (!folderName.EndsWith("\\"))
            {
                folderName += '\\';
            }

            ulong free = 0, total = 0, dummy2 = 0;

            if (GetDiskFreeSpaceEx(folderName, out free, out total, out dummy2))
            {
                freespace = free;
                totalspace = total;
                return true;
            }
            else
            {
                return false;
            }
        }

        public string GetDiskFreeSpace()
        {
            DriveInfo[] allDrives = DriveInfo.GetDrives();

            foreach (DriveInfo d in allDrives)
            {
                if (d.IsReady == true)
                {
                    return string.Format("{0} / {1}", d.AvailableFreeSpace.ToString("N0"), d.TotalSize.ToString("N0"));
                }
            }

            return string.Empty;
        }
    }
}
