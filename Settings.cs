using Microsoft.Win32;
using System;

namespace TaskbarSystemMonitor
{
    public class Settings
    {
        private const string REGISTRY_KEY = @"SOFTWARE\TaskbarSystemMonitor";
        private static Settings? _instance;
        private static readonly object _lock = new object();

        // Events for settings changes
        public event Action? SettingsChanged;

        // Settings properties
        public bool ShowCpu { get; set; } = true;
        public bool ShowRam { get; set; } = true;
        public bool ShowDisk { get; set; } = true;
        public bool ShowNetwork { get; set; } = true;
        public bool ShowGpu { get; set; } = true;
        public int UpdateInterval { get; set; } = 1000;
        public int Opacity { get; set; } = 85;

        private Settings()
        {
            LoadSettings();
        }

        public static Settings Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new Settings();
                        }
                    }
                }
                return _instance;
            }
        }

        public void LoadSettings()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY))
                {
                    if (key != null)
                    {
                        ShowCpu = (int)(key.GetValue("ShowCpu", 1) ?? 1) != 0;
                        ShowRam = (int)(key.GetValue("ShowRam", 1) ?? 1) != 0;
                        ShowDisk = (int)(key.GetValue("ShowDisk", 1) ?? 1) != 0;
                        ShowNetwork = (int)(key.GetValue("ShowNetwork", 1) ?? 1) != 0;
                        ShowGpu = (int)(key.GetValue("ShowGpu", 1) ?? 1) != 0;
                        UpdateInterval = (int)(key.GetValue("UpdateInterval", 1000) ?? 1000);
                        Opacity = (int)(key.GetValue("Opacity", 85) ?? 85);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
                // Use default values
            }
        }

        public void SaveSettings()
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(REGISTRY_KEY))
                {
                    if (key != null)
                    {
                        key.SetValue("ShowCpu", ShowCpu ? 1 : 0);
                        key.SetValue("ShowRam", ShowRam ? 1 : 0);
                        key.SetValue("ShowDisk", ShowDisk ? 1 : 0);
                        key.SetValue("ShowNetwork", ShowNetwork ? 1 : 0);
                        key.SetValue("ShowGpu", ShowGpu ? 1 : 0);
                        key.SetValue("UpdateInterval", UpdateInterval);
                        key.SetValue("Opacity", Opacity);
                    }
                }

                // Notify listeners that settings have changed
                SettingsChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }
    }
}
