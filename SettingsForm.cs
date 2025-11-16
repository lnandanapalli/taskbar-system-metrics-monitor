using System;
using System.Drawing;
using System.Windows.Forms;

namespace TaskbarSystemMonitor
{
    public partial class SettingsForm : Form
    {
        private CheckBox chkStartWithWindows = null!;
        private CheckBox chkShowCpu = null!;
        private CheckBox chkShowRam = null!;
        private CheckBox chkShowDisk = null!;
        private CheckBox chkShowNetwork = null!;
        private CheckBox chkShowGpu = null!;
        private NumericUpDown numUpdateInterval = null!;
        private NumericUpDown numOpacity = null!;

        public SettingsForm()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form properties
            this.Text = "Taskbar Monitor Settings";
            this.Size = new Size(350, 300);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;

            // Startup checkbox
            chkStartWithWindows = new CheckBox
            {
                Text = "Start with Windows",
                Location = new Point(20, 20),
                Size = new Size(200, 20),
                ForeColor = Color.White
            };

            // Visible metrics group
            var lblMetrics = new Label
            {
                Text = "Visible Metrics:",
                Location = new Point(20, 60),
                Size = new Size(100, 20),
                ForeColor = Color.White
            };

            chkShowCpu = new CheckBox
            {
                Text = "CPU Usage",
                Location = new Point(40, 85),
                Size = new Size(100, 20),
                ForeColor = Color.White,
                Checked = true
            };

            chkShowRam = new CheckBox
            {
                Text = "RAM Usage",
                Location = new Point(40, 110),
                Size = new Size(100, 20),
                ForeColor = Color.White,
                Checked = true
            };

            chkShowDisk = new CheckBox
            {
                Text = "Disk Activity",
                Location = new Point(40, 135),
                Size = new Size(100, 20),
                ForeColor = Color.White,
                Checked = true
            };

            chkShowNetwork = new CheckBox
            {
                Text = "Network Activity",
                Location = new Point(150, 85),
                Size = new Size(120, 20),
                ForeColor = Color.White,
                Checked = true
            };

            chkShowGpu = new CheckBox
            {
                Text = "GPU Usage",
                Location = new Point(150, 110),
                Size = new Size(100, 20),
                ForeColor = Color.White,
                Checked = true
            };

            // Update interval
            var lblUpdateInterval = new Label
            {
                Text = "Update Interval (ms):",
                Location = new Point(20, 170),
                Size = new Size(120, 20),
                ForeColor = Color.White
            };

            numUpdateInterval = new NumericUpDown
            {
                Location = new Point(150, 168),
                Size = new Size(80, 20),
                Minimum = 500,
                Maximum = 5000,
                Value = 1000,
                Increment = 250,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White
            };

            // Opacity
            var lblOpacity = new Label
            {
                Text = "Opacity (%):",
                Location = new Point(20, 200),
                Size = new Size(80, 20),
                ForeColor = Color.White
            };

            numOpacity = new NumericUpDown
            {
                Location = new Point(150, 198),
                Size = new Size(80, 20),
                Minimum = 50,
                Maximum = 100,
                Value = 85,
                Increment = 5,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White
            };

            // Buttons
            var btnOk = new Button
            {
                Text = "OK",
                Location = new Point(180, 230),
                Size = new Size(75, 25),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };

            var btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(260, 230),
                Size = new Size(75, 25),
                BackColor = Color.FromArgb(100, 100, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };

            btnOk.Click += BtnOk_Click;

            // Add controls
            this.Controls.AddRange(new Control[]
            {
                chkStartWithWindows,
                lblMetrics,
                chkShowCpu,
                chkShowRam,
                chkShowDisk,
                chkShowNetwork,
                chkShowGpu,
                lblUpdateInterval,
                numUpdateInterval,
                lblOpacity,
                numOpacity,
                btnOk,
                btnCancel
            });

            this.ResumeLayout(false);
        }

        private void LoadSettings()
        {
            chkStartWithWindows.Checked = StartupManager.IsStartupEnabled();

            var settings = Settings.Instance;
            chkShowCpu.Checked = settings.ShowCpu;
            chkShowRam.Checked = settings.ShowRam;
            chkShowDisk.Checked = settings.ShowDisk;
            chkShowNetwork.Checked = settings.ShowNetwork;
            chkShowGpu.Checked = settings.ShowGpu;
            numUpdateInterval.Value = settings.UpdateInterval;
            numOpacity.Value = settings.Opacity;
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            SaveSettings();
        }

        private void SaveSettings()
        {
            // Update startup setting
            if (chkStartWithWindows.Checked)
            {
                StartupManager.EnableStartup();
            }
            else
            {
                StartupManager.DisableStartup();
            }

            // Save all settings
            var settings = Settings.Instance;
            settings.ShowCpu = chkShowCpu.Checked;
            settings.ShowRam = chkShowRam.Checked;
            settings.ShowDisk = chkShowDisk.Checked;
            settings.ShowNetwork = chkShowNetwork.Checked;
            settings.ShowGpu = chkShowGpu.Checked;
            settings.UpdateInterval = (int)numUpdateInterval.Value;
            settings.Opacity = (int)numOpacity.Value;
            settings.SaveSettings();

            this.DialogResult = DialogResult.OK;
        }
    }
}