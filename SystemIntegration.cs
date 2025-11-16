using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace TaskbarSystemMonitor
{
    public static class StartupManager
    {
        private const string APP_NAME = "TaskbarSystemMonitor";
        private const string REGISTRY_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        public static bool IsStartupEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY, false))
                {
                    return key?.GetValue(APP_NAME) != null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking startup status: {ex.Message}");
                return false;
            }
        }

        public static bool EnableStartup()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY, true))
                {
                    if (key != null)
                    {
                        // Use AppContext.BaseDirectory for single-file apps compatibility
                        string exePath = Assembly.GetExecutingAssembly().Location;
                        if (string.IsNullOrEmpty(exePath))
                        {
                            // Single-file app - use the current executable path
                            exePath = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "TaskbarSystemMonitor.exe");
                        }
                        else if (exePath.EndsWith(".dll"))
                        {
                            // For .NET Core/5+ apps, we need to use the executable
                            exePath = Path.ChangeExtension(exePath, ".exe");
                        }

                        key.SetValue(APP_NAME, $"\"{exePath}\"");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to enable startup: {ex.Message}", "Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return false;
        }

        public static bool DisableStartup()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY, true))
                {
                    key?.DeleteValue(APP_NAME, false);
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to disable startup: {ex.Message}", "Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return false;
        }

        public static void ToggleStartup()
        {
            if (IsStartupEnabled())
            {
                DisableStartup();
            }
            else
            {
                EnableStartup();
            }
        }
    }

    public static class SystemIntegration
    {
        public static void ShowAboutDialog()
        {
            string message = $"Taskbar System Monitor v1.0\n\n" +
                           $"Real-time system monitoring for Windows taskbar.\n\n" +
                           $"Features:\n" +
                           $"• CPU Usage\n" +
                           $"• RAM Usage\n" +
                           $"• Disk Activity\n" +
                           $"• Network Activity\n" +
                           $"• GPU Usage\n\n" +
                           $"Right-click the tray icon for options.";

            MessageBox.Show(message, "About Taskbar System Monitor",
                          MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void ShowSettingsDialog()
        {
            using (var settingsForm = new SettingsForm())
            {
                settingsForm.ShowDialog();
            }
        }

        public static void OpenTaskManager()
        {
            try
            {
                Process.Start("taskmgr.exe");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open Task Manager: {ex.Message}", "Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void OpenResourceMonitor()
        {
            try
            {
                Process.Start("resmon.exe");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open Resource Monitor: {ex.Message}", "Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}