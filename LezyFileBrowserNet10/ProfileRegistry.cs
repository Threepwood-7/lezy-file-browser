using Microsoft.Win32;
using System;
using System.Collections.Generic;

namespace LezyFileBrowser
{
    public static class ProfileRegistry
    {
        private const string BaseKey     = @"Software\LezyFileBrowser";
        private const string ProfilesKey = @"Software\LezyFileBrowser\Profiles";

        // ── Read ──────────────────────────────────────────────────────────────

        public static List<Profile> LoadAll()
        {
            var result = new List<Profile>();
            using var root = Registry.CurrentUser.OpenSubKey(ProfilesKey);
            if (root == null) return result;

            foreach (var name in root.GetSubKeyNames())
            {
                var p = Load(name);
                if (p != null) result.Add(p);
            }
            return result;
        }

        public static Profile Load(string name)
        {
            using var key = Registry.CurrentUser.OpenSubKey($@"{ProfilesKey}\{name}");
            if (key == null) return null;

            return new Profile
            {
                Name             = name,
                Description      = key.GetValue("Description",      "") as string ?? "",
                InputDir         = key.GetValue("InputDir",         "") as string ?? "",
                OkDir            = key.GetValue("OkDir",            "") as string ?? "",
                SortDir          = (int)(key.GetValue("SortDir",          7)  ),
                InplaceBrowsing  = (int)(key.GetValue("InplaceBrowsing",  0)  ) != 0,
                MinFileSizeMb    = (int)(key.GetValue("MinFileSizeMb",    99) ),
                MaxListItems     = (int)(key.GetValue("MaxListItems",     int.MaxValue)),
                MoveWithRobocopy = key.GetValue("MoveWithRobocopy", "") as string ?? "",
                NoCache          = (int)(key.GetValue("NoCache",          0)  ) != 0,
                NoDupeCheck      = (int)(key.GetValue("NoDupeCheck",      0)  ) != 0,
                ProfileLog       = (int)(key.GetValue("ProfileLog",       0)  ) != 0,
                OnTop            = (int)(key.GetValue("OnTop",            0)  ) != 0,
                KeepFocus        = (int)(key.GetValue("KeepFocus",        1)  ) != 0,
                Autoplay         = (int)(key.GetValue("Autoplay",         1)  ) != 0,
                ShowDupesOnly    = (int)(key.GetValue("ShowDupesOnly",    0)  ) != 0,
            };
        }

        // ── Write ─────────────────────────────────────────────────────────────

        // oldName != null && oldName != p.Name  →  rename (delete old key, create new)
        public static void Save(Profile p, string oldName = null)
        {
            if (oldName != null && !string.Equals(oldName, p.Name, StringComparison.OrdinalIgnoreCase))
                Delete(oldName);

            using var key = Registry.CurrentUser.CreateSubKey($@"{ProfilesKey}\{p.Name}");
            key.SetValue("Description",      p.Description      ?? "",   RegistryValueKind.String);
            key.SetValue("InputDir",         p.InputDir         ?? "",   RegistryValueKind.String);
            key.SetValue("OkDir",            p.OkDir            ?? "",   RegistryValueKind.String);
            key.SetValue("SortDir",          p.SortDir,                   RegistryValueKind.DWord);
            key.SetValue("InplaceBrowsing",  p.InplaceBrowsing ? 1 : 0,  RegistryValueKind.DWord);
            key.SetValue("MinFileSizeMb",    (int)p.MinFileSizeMb,        RegistryValueKind.DWord);
            key.SetValue("MaxListItems",     p.MaxListItems,               RegistryValueKind.DWord);
            key.SetValue("MoveWithRobocopy", p.MoveWithRobocopy ?? "",   RegistryValueKind.String);
            key.SetValue("NoCache",          p.NoCache     ? 1 : 0,       RegistryValueKind.DWord);
            key.SetValue("NoDupeCheck",      p.NoDupeCheck ? 1 : 0,       RegistryValueKind.DWord);
            key.SetValue("ProfileLog",       p.ProfileLog  ? 1 : 0,       RegistryValueKind.DWord);
            key.SetValue("OnTop",            p.OnTop       ? 1 : 0,       RegistryValueKind.DWord);
            key.SetValue("KeepFocus",        p.KeepFocus   ? 1 : 0,       RegistryValueKind.DWord);
            key.SetValue("Autoplay",         p.Autoplay    ? 1 : 0,       RegistryValueKind.DWord);
            key.SetValue("ShowDupesOnly",    p.ShowDupesOnly ? 1 : 0,     RegistryValueKind.DWord);
        }

        public static void Delete(string name) =>
            Registry.CurrentUser.DeleteSubKeyTree($@"{ProfilesKey}\{name}", throwOnMissingSubKey: false);

        // ── Last profile ──────────────────────────────────────────────────────

        public static string GetLastProfile()
        {
            using var key = Registry.CurrentUser.OpenSubKey(BaseKey);
            return key?.GetValue("LastProfile") as string;
        }

        public static void SetLastProfile(string name)
        {
            using var key = Registry.CurrentUser.CreateSubKey(BaseKey);
            key.SetValue("LastProfile", name ?? "", RegistryValueKind.String);
        }

        // ── Mutex ─────────────────────────────────────────────────────────────

        // Named mutex used to detect whether a profile is already running.
        public static string MutexName(string profileName)
        {
            var safe = System.Text.RegularExpressions.Regex.Replace(profileName ?? "", @"[^\w]", "_");
            return $"Local\\LezyFileBrowser_{safe}";
        }
    }
}

