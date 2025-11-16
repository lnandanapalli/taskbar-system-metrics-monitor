using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TaskbarSystemMonitor
{
    public partial class TaskbarOverlayForm : Form
    {
        // Windows API constants
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int WS_EX_TOOLWINDOW = 0x80;
        private const int GWL_EXSTYLE = -20;
        private const int HWND_TOPMOST = -1;
        private const int SWP_NOSIZE = 0x1;
        private const int SWP_NOMOVE = 0x2;
        private const int SWP_NOACTIVATE = 0x10;

        // Windows API imports
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int X, int Y, int cx, int cy, int uFlags);

        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("shell32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private SystemMetrics _systemMetrics = null!;
        private SystemMetricsRenderer _renderer = null!;
        private NotifyIcon _trayIcon = null!;
        private System.Windows.Forms.Timer _positionTimer = null!;

        // Drag functionality
        private bool _isDragging = false;
        private Point _dragStartPoint;
        private Point _dragStartLocation;

        // Position persistence
        private const string REGISTRY_KEY = @"SOFTWARE\TaskbarSystemMonitor";
        private const string POS_X_VALUE = "PositionX";
        private const string POS_Y_VALUE = "PositionY";

        public TaskbarOverlayForm()
        {
            // Enable double buffering to prevent flickering
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint |
                         ControlStyles.DoubleBuffer |
                         ControlStyles.ResizeRedraw, true);
            this.UpdateStyles();

            InitializeComponent();
            InitializeOverlay();
            SetupTrayIcon();
            StartMonitoring();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form properties
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(300, 40);
            this.FormBorderStyle = FormBorderStyle.None;
            this.Name = "TaskbarOverlayForm";
            this.Text = "Taskbar System Monitor";
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.BackColor = Color.Black;
            this.TransparencyKey = Color.Magenta;

            this.ResumeLayout(false);
        }

        private void InitializeOverlay()
        {
            // Make window layered and allow interaction
            int exStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
            SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle | WS_EX_LAYERED | WS_EX_TOOLWINDOW);

            // Set transparency from settings
            ApplyOpacitySetting();

            // Load saved position or use default
            LoadSavedPosition();

            // Keep on top
            SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);

            // Add mouse event handlers for dragging
            this.MouseDown += OnMouseDown;
            this.MouseMove += OnMouseMove;
            this.MouseUp += OnMouseUp;

            // Set up timer to maintain always-on-top behavior
            _positionTimer = new System.Windows.Forms.Timer();
            _positionTimer.Interval = 500; // Check every 500ms for better responsiveness
            _positionTimer.Tick += (s, e) =>
            {
                // Keep window on top
                SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            };
            _positionTimer.Start();

            // Subscribe to settings changes
            Settings.Instance.SettingsChanged += OnSettingsChanged;
        }

        private void ApplyOpacitySetting()
        {
            var settings = Settings.Instance;
            // Convert opacity percentage (0-100) to byte value (0-255)
            byte opacity = (byte)(settings.Opacity * 255 / 100);
            SetLayeredWindowAttributes(this.Handle, 0, opacity, 0x2);
        }

        private void OnSettingsChanged()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(OnSettingsChanged));
                return;
            }

            // Apply new opacity
            ApplyOpacitySetting();

            // Trigger redraw to update visible metrics
            this.Invalidate();
        }
        private void PositionOnTaskbar()
        {
            try
            {
                // Find the taskbar
                IntPtr taskbarHandle = FindWindow("Shell_TrayWnd", null!);
                if (taskbarHandle != IntPtr.Zero)
                {
                    GetWindowRect(taskbarHandle, out RECT taskbarRect);

                    // Position to the left of the system tray
                    int x = taskbarRect.Right - 350; // Adjust this value to position correctly
                    int y = taskbarRect.Top + 2;
                    int height = taskbarRect.Bottom - taskbarRect.Top - 4;

                    // Update form size and position
                    this.SetBounds(x, y, 300, height);

                    // Keep on top
                    SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error positioning on taskbar: {ex.Message}");
            }
        }

        private void SetupTrayIcon()
        {
            _trayIcon = new NotifyIcon()
            {
                Icon = SystemIcons.Application,
                Visible = true,
                Text = "Taskbar System Monitor - Click for options"
            };

            var contextMenu = new ContextMenuStrip();

            // Add menu items
            contextMenu.Items.Add("Settings...", null, (s, e) => SystemIntegration.ShowSettingsDialog());
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Task Manager", null, (s, e) => SystemIntegration.OpenTaskManager());
            contextMenu.Items.Add("Resource Monitor", null, (s, e) => SystemIntegration.OpenResourceMonitor());
            contextMenu.Items.Add(new ToolStripSeparator());

            var startupItem = new ToolStripMenuItem("Start with Windows");
            startupItem.Checked = StartupManager.IsStartupEnabled();
            startupItem.Click += (s, e) =>
            {
                StartupManager.ToggleStartup();
                startupItem.Checked = StartupManager.IsStartupEnabled();
            };
            contextMenu.Items.Add(startupItem);

            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Reset Position", null, (s, e) => ResetPosition());
            contextMenu.Items.Add("About...", null, (s, e) => SystemIntegration.ShowAboutDialog());
            contextMenu.Items.Add("Exit", null, (s, e) => Application.Exit());

            _trayIcon.ContextMenuStrip = contextMenu;

            _trayIcon.DoubleClick += (s, e) => SystemIntegration.ShowSettingsDialog();
        }

        private void StartMonitoring()
        {
            _systemMetrics = new SystemMetrics();
            _renderer = new SystemMetricsRenderer(this);

            _systemMetrics.MetricsUpdated += OnMetricsUpdated;
        }

        private void OnMetricsUpdated(SystemMetricsData metrics)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<SystemMetricsData>(OnMetricsUpdated), metrics);
                return;
            }

            _renderer.UpdateMetrics(metrics);

            // Only invalidate if the window is visible and not being dragged
            if (this.Visible && !_isDragging)
            {
                this.Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Use high quality rendering settings for smooth graphics
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            base.OnPaint(e);
            _renderer?.Render(e.Graphics);
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(value);
            // Remove automatic positioning - let user drag it where they want
        }

        private void OnMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                _dragStartPoint = e.Location;
                _dragStartLocation = this.Location;
                this.Cursor = Cursors.SizeAll;

                // Suspend layout during drag for better performance
                this.SuspendLayout();
            }
        }

        private void OnMouseMove(object? sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                Point newLocation = new Point(
                    _dragStartLocation.X + (e.X - _dragStartPoint.X),
                    _dragStartLocation.Y + (e.Y - _dragStartPoint.Y)
                );
                this.Location = newLocation;
            }
        }

        private void OnMouseUp(object? sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                this.Cursor = Cursors.Default;

                // Resume layout and refresh display
                this.ResumeLayout(true);
                this.Invalidate();

                SavePosition();
            }
        }
        private void LoadSavedPosition()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(REGISTRY_KEY))
                {
                    if (key != null)
                    {
                        int x = (int)(key.GetValue(POS_X_VALUE, -1) ?? -1);
                        int y = (int)(key.GetValue(POS_Y_VALUE, -1) ?? -1);

                        if (x >= 0 && y >= 0)
                        {
                            // Validate position is on screen
                            var screen = Screen.FromPoint(new Point(x, y));
                            if (x >= screen.Bounds.Left && x <= screen.Bounds.Right - this.Width &&
                                y >= screen.Bounds.Top && y <= screen.Bounds.Bottom - this.Height)
                            {
                                this.SetBounds(x, y, this.Width, this.Height);
                                return;
                            }
                        }
                    }
                }
            }
            catch { /* Ignore errors */ }

            // Default position if no saved position or invalid
            var primaryScreen = Screen.PrimaryScreen;
            if (primaryScreen != null)
            {
                int x = primaryScreen.WorkingArea.Right - this.Width - 100;
                int y = primaryScreen.WorkingArea.Bottom - this.Height - 50;
                this.SetBounds(x, y, this.Width, this.Height);
            }
        }

        private void SavePosition()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(REGISTRY_KEY))
                {
                    key?.SetValue(POS_X_VALUE, this.Location.X);
                    key?.SetValue(POS_Y_VALUE, this.Location.Y);
                }
            }
            catch { /* Ignore errors */ }
        }

        private void ResetPosition()
        {
            // Reset to default position
            var primaryScreen = Screen.PrimaryScreen;
            if (primaryScreen != null)
            {
                int x = primaryScreen.WorkingArea.Right - this.Width - 100;
                int y = primaryScreen.WorkingArea.Bottom - this.Height - 50;
                this.SetBounds(x, y, this.Width, this.Height);

                // Ensure window stays on top after reset
                SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);

                // Re-enable dragging (in case it was lost)
                this.MouseDown -= OnMouseDown;
                this.MouseMove -= OnMouseMove;
                this.MouseUp -= OnMouseUp;
                this.MouseDown += OnMouseDown;
                this.MouseMove += OnMouseMove;
                this.MouseUp += OnMouseUp;

                SavePosition();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
                this.Hide();
                return;
            }

            _positionTimer?.Stop();
            _systemMetrics?.Dispose();
            _trayIcon?.Dispose();
            base.OnFormClosing(e);
        }

        // Prevent activation but allow mouse interaction
        protected override bool ShowWithoutActivation => true;

        private const int WM_MOUSEACTIVATE = 0x21;
        private const int MA_ACTIVATE = 1;
        private const int MA_NOACTIVATE = 3;

        protected override void WndProc(ref Message m)
        {
            // Allow mouse interaction for dragging
            if (m.Msg == WM_MOUSEACTIVATE)
            {
                m.Result = new IntPtr(MA_ACTIVATE);
                return;
            }

            base.WndProc(ref m);
        }
    }
}