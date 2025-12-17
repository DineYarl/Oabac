using Oabac.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace Oabac.Services
{
    public enum SyncMode
    {
        Interval,
        Realtime
    }

    public class SyncService
    {
        public event EventHandler<string> StatusChanged;
        public Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue { get; set; }
        private ObservableCollection<Mapping> _mappings = new ObservableCollection<Mapping>();
        
        // Configuration
        public SyncMode Mode { get; set; } = SyncMode.Interval;
        public int IntervalMinutes { get; set; } = 60;
        public bool MirrorDeletions { get; set; } = false;
        public bool MinimizeToTray { get; set; } = false;
        public bool UseRecycleBin { get; set; } = false;
        public bool RunAtStartup { get; set; } = false;
        public bool IsFirstRun { get; set; } = true;

        private Timer _timer;
        private List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();
        private readonly string _settingsFilePath;

        public SyncService()
        {
            var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appDir = Path.Combine(localData, "Oabac");
            Directory.CreateDirectory(appDir);
            _settingsFilePath = Path.Combine(appDir, "settings.json");
        }

        public void Initialize()
        {
            LoadSettings();
            ApplyConfiguration();
            Log("Sync Service Initialized.");
        }

        public ObservableCollection<Mapping> GetMappings()
        {
            return _mappings;
        }

        public void AddMapping(string source, string dest, List<string> exclusions = null, SyncDirection direction = SyncDirection.OneWay)
        {
            _mappings.Add(new Mapping 
            { 
                SourcePath = source, 
                DestinationPath = dest,
                Exclusions = exclusions ?? new List<string>(),
                Direction = direction
            });
            Log($"Added mapping: {source} -> {dest} ({direction})");
            SaveSettings();
            ApplyConfiguration();
        }

        public void RemoveMapping(Mapping mapping)
        {
            _mappings.Remove(mapping);
            Log($"Removed mapping: {mapping.SourcePath} -> {mapping.DestinationPath}");
            SaveSettings();
            ApplyConfiguration();
        }

        public void UpdateSettings(SyncMode mode, int intervalMinutes, bool mirrorDeletions, bool minimizeToTray, bool useRecycleBin, bool runAtStartup)
        {
            Mode = mode;
            IntervalMinutes = intervalMinutes;
            MirrorDeletions = mirrorDeletions;
            MinimizeToTray = minimizeToTray;
            UseRecycleBin = useRecycleBin;
            RunAtStartup = runAtStartup;
            SaveSettings();
            ApplyConfiguration();
        }

        public void CompleteFirstRun()
        {
            IsFirstRun = false;
            SaveSettings();
        }

        private void ApplyConfiguration()
        {
            // Stop existing
            _timer?.Dispose();
            _timer = null;
            foreach (var w in _watchers) w.Dispose();
            _watchers.Clear();

            SetRunAtStartupRegistry(RunAtStartup);

            if (Mode == SyncMode.Interval)
            {
                _timer = new Timer(TimerCallback, null, TimeSpan.Zero, TimeSpan.FromMinutes(IntervalMinutes));
                Log($"Timer started with interval: {IntervalMinutes} min");
            }
            else if (Mode == SyncMode.Realtime)
            {
                foreach (var mapping in _mappings)
                {
                    if (Directory.Exists(mapping.SourcePath))
                    {
                        try
                        {
                            var watcher = new FileSystemWatcher(mapping.SourcePath);
                            watcher.IncludeSubdirectories = true;
                            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                            
                            // Debounce logic
                            CancellationTokenSource cts = null;
                            FileSystemEventHandler handler = (s, e) => 
                            {
                                cts?.Cancel();
                                cts = new CancellationTokenSource();
                                var token = cts.Token;
                                Task.Delay(500, token).ContinueWith(t => 
                                {
                                    if (!t.IsCanceled) Task.Run(() => SyncFolder(mapping));
                                }, TaskScheduler.Default);
                            };

                            watcher.Changed += handler;
                            watcher.Created += handler;
                            watcher.Deleted += handler;
                            watcher.Renamed += (s, e) => handler(s, e);
                            
                            watcher.EnableRaisingEvents = true;
                            _watchers.Add(watcher);
                        }
                        catch (Exception ex)
                        {
                            Log($"Failed to start watcher for {mapping.SourcePath}: {ex.Message}");
                        }
                    }
                }
                Log("Realtime watchers started.");
            }
        }

        private void SetRunAtStartupRegistry(bool enable)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (enable)
                    {
                        var exePath = Environment.ProcessPath;
                        if (exePath != null && exePath.EndsWith(".dll")) 
                        {
                            exePath = exePath.Replace(".dll", ".exe");
                        }
                        key?.SetValue("Oabac", $"\"{exePath}\" --background");
                    }
                    else
                    {
                        if (key?.GetValue("Oabac") != null)
                            key?.DeleteValue("Oabac", false);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Failed to set startup registry key: {ex.Message}");
            }
        }

        private void TimerCallback(object state)
        {
            SyncNow();
        }

        public void SyncNow()
        {
            Task.Run(async () =>
            {
                Log("Starting sync...");
                var tasks = _mappings.Select(mapping => Task.Run(() => SyncFolder(mapping)));
                await Task.WhenAll(tasks);
                Log("Sync completed.");
            });
        }

        private void SyncFolder(Mapping mapping)
        {
            string source = mapping.SourcePath;
            string dest = mapping.DestinationPath;

            lock (mapping.SyncLock)
            {
                try
                {
                    UpdateMappingStatus(mapping, "Analyzing...", 0, "Calculating...");

                    if (!Directory.Exists(source))
                    {
                        Log($"Source directory not found: {source}");
                        UpdateMappingStatus(mapping, "Source not found", 0, "");
                        SendNotification("Sync Failed", $"Source not found: {source}");
                        return;
                    }

                    if (!Directory.Exists(dest))
                    {
                        Directory.CreateDirectory(dest);
                    }

                    // Sync Source -> Dest
                    SyncDirectional(source, dest, mapping, true);

                    // If Two-Way, Sync Dest -> Source
                    if (mapping.Direction == SyncDirection.TwoWay)
                    {
                        SyncDirectional(dest, source, mapping, false);
                    }

                    UpdateMappingStatus(mapping, "Idle", 100, "Sync Complete");
                    // Only notify if not realtime (too spammy)
                    if (Mode == SyncMode.Interval)
                    {
                        SendNotification("Sync Complete", $"Synced {mapping.SourcePath}");
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error syncing {source} to {dest}: {ex.Message}");
                    UpdateMappingStatus(mapping, "Error", 0, ex.Message);
                    SendNotification("Sync Error", $"Error syncing {mapping.SourcePath}: {ex.Message}");
                }
            }
        }

        private void SyncDirectional(string sourceDir, string destDir, Mapping mapping, bool isPrimaryDirection)
        {
            // Analysis Phase
            var filesToCopy = new List<string>();
            long totalBytes = 0;
            
            // Recursive scan to respect exclusions
            ScanDirectory(sourceDir, sourceDir, mapping.Exclusions, filesToCopy, ref totalBytes, destDir);

            UpdateMappingStatus(mapping, "Syncing...", 0, $"0 / {FormatBytes(totalBytes)}");

            long copiedBytes = 0;
            long lastReportedBytes = 0;
            
            // Copy Files
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 4 };
            Parallel.ForEach(filesToCopy, parallelOptions, (file) =>
            {
                var relativePath = Path.GetRelativePath(sourceDir, file);
                var destFile = Path.Combine(destDir, relativePath);
                var targetDir = Path.GetDirectoryName(destFile);

                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
                
                try
                {
                    if (File.Exists(destFile) && UseRecycleBin)
                    {
                        RecycleItem(destFile);
                    }

                    File.Copy(file, destFile, true);
                    
                    var len = new FileInfo(file).Length;
                    long currentCopied = Interlocked.Add(ref copiedBytes, len);

                    double progress = totalBytes > 0 ? (double)currentCopied / totalBytes * 100 : 100;
                    
                    // Throttle UI updates
                    long last = Interlocked.Read(ref lastReportedBytes);
                    if (currentCopied - last >= 1024 * 1024 || currentCopied == totalBytes) 
                    {
                        if (Interlocked.CompareExchange(ref lastReportedBytes, currentCopied, last) == last)
                        {
                            UpdateMappingStatus(mapping, "Syncing...", progress, $"{FormatBytes(currentCopied)} / {FormatBytes(totalBytes)}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"Failed to copy {relativePath}: {ex.Message}");
                }
            });

            // Mirror Deletions - Only for Primary Direction (Source -> Dest) and if enabled
            // For TwoWay, we skip deletion mirroring to avoid data loss (simple union sync)
            if (MirrorDeletions && isPrimaryDirection && mapping.Direction == SyncDirection.OneWay)
            {
                UpdateMappingStatus(mapping, "Cleaning up...", 100, "Checking deletions...");
                
                // Recursive deletion scan
                ScanForDeletions(destDir, destDir, sourceDir, mapping.Exclusions);
            }
        }

        private void ScanDirectory(string rootDir, string currentDir, List<string> exclusions, List<string> filesToCopy, ref long totalBytes, string destRoot)
        {
            try
            {
                // Check files
                foreach (var file in Directory.EnumerateFiles(currentDir))
                {
                    if (IsExcluded(file, exclusions)) continue;

                    var relativePath = Path.GetRelativePath(rootDir, file);
                    var destFile = Path.Combine(destRoot, relativePath);

                    if (!File.Exists(destFile) || File.GetLastWriteTimeUtc(file) > File.GetLastWriteTimeUtc(destFile))
                    {
                        filesToCopy.Add(file);
                        totalBytes += new FileInfo(file).Length;
                    }
                }

                // Check directories
                foreach (var dir in Directory.EnumerateDirectories(currentDir))
                {
                    if (IsExcluded(dir, exclusions)) continue;
                    ScanDirectory(rootDir, dir, exclusions, filesToCopy, ref totalBytes, destRoot);
                }
            }
            catch (Exception ex)
            {
                Log($"Error scanning {currentDir}: {ex.Message}");
            }
        }

        private void ScanForDeletions(string rootDir, string currentDir, string sourceRoot, List<string> exclusions)
        {
            try
            {
                // Check files
                foreach (var file in Directory.EnumerateFiles(currentDir))
                {
                    if (IsExcluded(file, exclusions)) continue;

                    var relativePath = Path.GetRelativePath(rootDir, file);
                    var sourceFile = Path.Combine(sourceRoot, relativePath);

                    if (!File.Exists(sourceFile))
                    {
                        if (UseRecycleBin) RecycleItem(file);
                        else File.Delete(file);
                        Log($"Deleted: {relativePath}");
                    }
                }

                // Check directories
                foreach (var dir in Directory.EnumerateDirectories(currentDir))
                {
                    if (IsExcluded(dir, exclusions)) continue;
                    
                    ScanForDeletions(rootDir, dir, sourceRoot, exclusions);

                    // After processing children, check if directory itself should be deleted (if empty and not in source)
                    var relativePath = Path.GetRelativePath(rootDir, dir);
                    var sourceDir = Path.Combine(sourceRoot, relativePath);

                    if (!Directory.Exists(sourceDir))
                    {
                        if (!Directory.EnumerateFileSystemEntries(dir).Any())
                        {
                            if (UseRecycleBin) RecycleItem(dir);
                            else Directory.Delete(dir);
                            Log($"Deleted directory: {relativePath}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error cleaning {currentDir}: {ex.Message}");
            }
        }

        private bool IsExcluded(string path, List<string> exclusions)
        {
            if (exclusions == null || exclusions.Count == 0) return false;
            var name = Path.GetFileName(path);
            foreach (var exc in exclusions)
            {
                // Simple wildcard support
                if (exc.StartsWith("*") && name.EndsWith(exc.Substring(1), StringComparison.OrdinalIgnoreCase)) return true;
                if (exc.EndsWith("*") && name.StartsWith(exc.Substring(0, exc.Length - 1), StringComparison.OrdinalIgnoreCase)) return true;
                if (name.Equals(exc, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private void SendNotification(string title, string message)
        {
            try
            {
                var notification = new AppNotificationBuilder()
                    .AddText(title)
                    .AddText(message)
                    .BuildNotification();

                AppNotificationManager.Default.Show(notification);
            }
            catch (Exception ex)
            {
                Log($"Failed to send notification: {ex.Message}");
            }
        }

        private void UpdateMappingStatus(Mapping mapping, string status, double progress, string info)
        {
            if (DispatcherQueue != null)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    mapping.Status = status;
                    mapping.Progress = progress;
                    mapping.DataTransferInfo = info;
                });
            }
            else
            {
                mapping.Status = status;
                mapping.Progress = progress;
                mapping.DataTransferInfo = info;
            }
        }

        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = (decimal)bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }
            return string.Format("{0:n1}{1}", number, suffixes[counter]);
        }

        private class SettingsData
        {
            public SyncMode Mode { get; set; }
            public int IntervalMinutes { get; set; }
            public bool MirrorDeletions { get; set; }
            public bool MinimizeToTray { get; set; }
            public bool UseRecycleBin { get; set; }
            public bool RunAtStartup { get; set; }
            public bool IsFirstRun { get; set; } = true;
            public List<Mapping> Mappings { get; set; }
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    var data = JsonSerializer.Deserialize<SettingsData>(json);
                    if (data != null)
                    {
                        Mode = data.Mode;
                        IntervalMinutes = data.IntervalMinutes;
                        MirrorDeletions = data.MirrorDeletions;
                        MinimizeToTray = data.MinimizeToTray;
                        UseRecycleBin = data.UseRecycleBin;
                        RunAtStartup = data.RunAtStartup;
                        IsFirstRun = data.IsFirstRun;
                        _mappings.Clear();
                        if (data.Mappings != null)
                        {
                            foreach (var m in data.Mappings) _mappings.Add(m);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error loading settings: {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                var data = new SettingsData
                {
                    Mode = Mode,
                    IntervalMinutes = IntervalMinutes,
                    MirrorDeletions = MirrorDeletions,
                    MinimizeToTray = MinimizeToTray,
                    UseRecycleBin = UseRecycleBin,
                    RunAtStartup = RunAtStartup,
                    IsFirstRun = IsFirstRun,
                    Mappings = _mappings.ToList()
                };
                var json = JsonSerializer.Serialize(data);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                Log($"Error saving settings: {ex.Message}");
            }
        }

        private void Log(string message)
        {
            StatusChanged?.Invoke(this, $"{DateTime.Now}: {message}");
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            public uint wFunc;
            public string pFrom;
            public string pTo;
            public ushort fFlags;
            [MarshalAs(UnmanagedType.Bool)]
            public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            public string lpszProgressTitle;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);

        private const uint FO_DELETE = 0x0003;
        private const ushort FOF_ALLOWUNDO = 0x0040;
        private const ushort FOF_NOCONFIRMATION = 0x0010;
        private const ushort FOF_NOERRORUI = 0x0400;
        private const ushort FOF_SILENT = 0x0004;

        private void RecycleItem(string path)
        {
            try
            {
                var shf = new SHFILEOPSTRUCT
                {
                    wFunc = FO_DELETE,
                    pFrom = path + "\0\0",
                    fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_NOERRORUI | FOF_SILENT
                };
                SHFileOperation(ref shf);
            }
            catch (Exception ex)
            {
                Log($"Failed to recycle {path}: {ex.Message}");
            }
        }
    }
}
