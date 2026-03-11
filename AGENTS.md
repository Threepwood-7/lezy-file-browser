# LezyFileBrowserNet10 — Project Summary

## Overview

A Windows desktop file manager application built with C# / WinForms on .NET 10.0, designed specifically for managing downloaded content (torrents, Usenet). It provides a streamlined workflow for reviewing, playing, moving, or deleting downloaded files.

## Technology Stack

- **Runtime:** .NET 10.0 for Windows (`net10.0-windows`)
- **UI:** Windows Forms (WinForms)
- **Language:** C# (nullable disabled, implicit usings disabled)
- **Output:** WinExe desktop application
- **Dependencies:** No external NuGet packages — only BCL and Windows APIs via P/Invoke

## Project Structure

```
LezyFileBrowserNet10/
├── LezyFileBrowserNet10.sln
└── LezyFileBrowserNet10/
    ├── LezyFileBrowserNet10.csproj
    ├── Program.cs              # Entry point, high-DPI setup
    ├── MainForm.cs             # Core UI logic and event handlers
    ├── MainForm.Designer.cs    # Auto-generated designer code
    ├── FileOperations.cs       # Async directory move/copy, history tracking
    ├── FileActions.cs          # Media file launching, player window focus, WoD detection
    ├── DuplicateFinder.cs      # Duplicate detection: by size + head/tail byte comparison
    ├── Util.cs                 # Shared utilities, dangerous extension detection, disk space (P/Invoke)
    ├── EverythingSearch.cs     # Everything SDK integration for fast directory listing (P/Invoke)
    ├── Caching.cs              # In-memory directory size cache
    ├── ListItemDir.cs          # Data model: ListItemTag (FileInfo + WorkOnFileOrDir enum)
    ├── FileData.cs             # Lightweight file metadata holder (no extra I/O)
    ├── App.config              # All configurable settings with documentation
    └── app.manifest            # Windows manifest: long paths, Win10/11 compat
```

## Core Features

1. **File/Directory Listing** — Scans a configurable input directory; supports size filters, sort by date/name/random, and result count limits.
2. **Move/Copy to OK Directory** — Moves or copies selected items to a destination directory; supports both `Directory.Move` and robocopy (configurable).
3. **Deletion with Undo** — 60-second grace period before actual deletion; Ctrl+Z cancels pending deletes.
4. **Media Player Integration** — Launches video files in MPlayer/MPC-HC/SMPlayer; auto-focuses player window; force fullscreen; kill player shortcut.
5. **Torrent Search** — Opens two configurable torrent search URLs in Firefox.
6. **Total Commander Integration** — `F3` runs a TC-Here script.
7. **SABnzbd/Usenet Integration** — `F4`/`Shift+F4` triggers a lookup script with optional suffix.
8. **Save for Later** — Appends item name to a txt file in Documents.
9. **In-Place Browsing Mode** — Source equals destination for local organization.
10. **Duplicate Detection** — Identifies duplicate video files by size + head/tail byte comparison:
    - Intra-inputDir dupes highlighted **amber**; okDir matches highlighted **red**
    - Delete one → all content-identical dupes are soft-deleted with same 60s undo window
    - Save one → dangling dupes in inputDir are immediately hard-deleted
    - okDir already-saved files: logged on each refresh; "Show dupez only" checkbox filters list to dupes only
    - Selecting a dupe entry logs all peer paths/sizes in the log panel
11. **Similar-name Detection** — Entries sharing ≥ `SIMILAR_NAME_MIN_TOKENS` leading dot-tokens are highlighted **pale orange** (e.g. same show, different quality/group/tag). Selecting one logs all similar peers. Included in "Show dupez only" filter. Colour priority: red > amber > pale orange.

## Key Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `Enter` | Launch item |
| `Space` | Save (move to OK dir) |
| `Del` | Soft-delete (60s undo window) |
| `Shift+Del` | Delete parent directory |
| `Ctrl+Z` | Undo pending delete |
| `F3` | TC Here (Total Commander) |
| `F4` / `Shift+F4` | SABnzbd lookup |
| `F5` / `Shift+F5` | Refresh / force refresh (bypass all caches) |
| `F6` | Torrent search |
| `F7` | Toggle sort mode (DAT → NAM → RND); logs new mode |
| `F9` | Launch with alternative player |
| `F11` | Save for later |

## Configuration

All settings are in `App.config` and can be overridden by environment variables (`X_*` prefix). Key settings:

| Setting | Default | Description |
|---------|---------|-------------|
| `X_INPUT_DIR` | *(required)* | Source directory to browse |
| `X_OK_DIR` | *(required)* | Destination for saved items |
| `X_MIN_FILE_SIZE_MB` | `99` | Minimum file size to show |
| `X_SORT_DIR` | `1` (date desc) | Sort: 1=date, 3=name, 7=random |
| `X_INPLACE_BROWSING` | `false` | Source = destination mode |
| `X_NO_CACHE` | `false` | Always re-read from disk |
| `X_MOVE_WITH_ROBOCOPY` | `true` | Use robocopy vs `Directory.Move` |
| `X_MAX_LIST_ITEMS` | unlimited | Cap list to N items |
| `PLAYER_PROCESS_NAMES` | mplayer/mpc-hc64/smplayer | Processes to focus/kill |
| `VIDEO_EXTENSIONS` | avi/mkv/mp4/... | Extensions treated as video |
| `DUPE_CHECK_HEAD_MB` | `10` | MB to compare at file start for duplicate detection |
| `DUPE_CHECK_TAIL_MB` | `10` | MB to compare at file end for duplicate detection |
| `SIMILAR_NAME_MIN_TOKENS` | `5` | Leading dot-tokens that must match to flag similar names |

## Architecture Notes

- **Event-driven:** `MainForm` coordinates UI events; background threads handle file operations.
- **Async refresh:** `RefreshItemsListingAsync` offloads all I/O (`ComputeListData`) to `Task.Run`; UI stays responsive. A marquee `ProgressBar` shows loading state.
- **Thread safety:** `FileOperations` uses locking for history; background thread for long-running moves. `IsInHistory` is lock-protected and safe to call from background tasks.
- **P/Invoke:** Used for disk space queries (`GetDiskFreeSpaceEx`) and player window focus (`SetForegroundWindow`).
- **Robocopy wrapper:** Generates temporary `.cmd` files for robocopy-based moves.
- **File info cache:** `_fileInfoCache` (`FileInfo[]`) populated via `DirectoryInfo.EnumerateFiles()` (attributes pre-populated from OS buffer, zero extra stats). Invalidated by Shift+F5.
- **Dir-files cache:** `dirFilesCache` in `ComputeListData` — reduces `ShouldWorkOnFileOrDir` dir scans from O(files) to O(unique dirs).
- **okDir cache:** `_okDirCache` — okDir scan + byte comparisons run once per session, invalidated by Shift+F5.
- **Duplicate state:** `_dupeGroups` (`Dictionary<string, List<FileInfo>>`) and `_alreadyInOkPaths` (`HashSet<string>`) are fields updated each refresh; used by `ListContent` to log peer details on selection.
- **Profile UI state:** Main-window checkbox state for `On Top`, `Keep Focus`, `Autoplay`, and `Show dupez only` is persisted per profile in `HKCU\Software\LezyFileBrowser\Profiles\<ProfileName>`. `Full Screen` remains time-driven and is not persisted.
- **Pending delete count:** Title bar shows `DEL pending N` when soft-deletes are queued; updated by timer tick.

## Build

```
dotnet build LezyFileBrowserNet10.sln          # Debug (default)
dotnet build LezyFileBrowserNet10.sln -c Release  # Release → c:\bin\toolz\
```

Always launch `.\rebuild.cmd` too when changes are made.
Release output goes directly to `c:\bin\toolz\` (no framework subfolder).

## Git Conventions

- No AI/Claude attribution in commit messages.
- Commit after each logical change; keep AGENTS.md up to date.

