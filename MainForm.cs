using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MultiBloxManager
{
    public partial class MainForm : Form
    {
        private const string GitHubUrl = "https://github.com/Jirkosaur/MultiBloxManager";
        private const string TargetProcessName = "RobloxPlayerBeta";
        private const int PollingIntervalMs = 1800;
        private const int MutexRegistrationDelayMs = 2500;

        private readonly HashSet<int> _processedPids = new();
        private readonly object _lockObj = new();
        private CancellationTokenSource? _monitoringCts;
        private bool _isMonitoring = true;
        private bool _isExiting = false;
        private Icon? _generatedAppIcon;

        public MainForm()
        {
            InitializeComponent();
            SetupAppIcon();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            NativeMethods.EnableImmersiveDarkMode(this.Handle);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            AppendLog("MultiBloxManager initialized. Pure NT P/Invoke engine ready.", Color.FromArgb(78, 201, 176));
            AppendLog("Monitoring for Roblox client instances...", Color.FromArgb(170, 170, 170));
            StartMonitoring();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                ShowInTaskbar = false;
                trayIcon.ShowBalloonTip(
                    1200,
                    "MultiBloxManager",
                    "Minimized to system tray. Multi-instance monitoring is active.",
                    ToolTipIcon.None);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_isExiting && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                WindowState = FormWindowState.Minimized;
                return;
            }

            StopMonitoring();
            trayIcon.Visible = false;
            trayIcon.Dispose();
            _generatedAppIcon?.Dispose();
            base.OnFormClosing(e);
        }

        #region Monitoring Engine

        private void StartMonitoring()
        {
            lock (_lockObj)
            {
                if (_monitoringCts != null)
                {
                    return;
                }

                _monitoringCts = new CancellationTokenSource();
                var token = _monitoringCts.Token;

                Task.Run(async () =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        try
                        {
                            if (_isMonitoring)
                            {
                                CheckForRobloxInstances(token);
                            }

                            await Task.Delay(PollingIntervalMs, token);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            AppendLog($"[!] Monitoring loop error: {ex.Message}", Color.FromArgb(244, 71, 71));
                        }
                    }
                }, token);
            }
        }

        private void StopMonitoring()
        {
            lock (_lockObj)
            {
                _monitoringCts?.Cancel();
                _monitoringCts?.Dispose();
                _monitoringCts = null;
            }
        }

        private void CheckForRobloxInstances(CancellationToken token)
        {
            Process[] processes;
            try
            {
                processes = Process.GetProcessesByName(TargetProcessName);
            }
            catch
            {
                return;
            }

            var currentPids = new HashSet<int>();

            foreach (var proc in processes)
            {
                int pid = proc.Id;
                currentPids.Add(pid);

                bool isNewInstance;
                lock (_lockObj)
                {
                    isNewInstance = _processedPids.Add(pid);
                }

                if (isNewInstance)
                {
                    // Asynchronously unlock the singleton mutex for newly launched process
                    _ = ProcessRobloxInstanceAsync(pid, token);
                }

                proc.Dispose();
            }

            // Purge dead processes from processed cache to keep memory clean
            lock (_lockObj)
            {
                _processedPids.RemoveWhere(pid => !currentPids.Contains(pid));
            }
        }

        private async Task ProcessRobloxInstanceAsync(int pid, CancellationToken token)
        {
            AppendLog($"Detected {TargetProcessName} (PID: {pid}). Waiting 2.5s for engine initialization...", Color.FromArgb(220, 220, 170));

            try
            {
                await Task.Delay(MutexRegistrationDelayMs, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            // Attempt to close the singleton event
            bool success = NativeMethods.UnlockRobloxSingleton(pid, out string message);

            if (success)
            {
                AppendLog($"[+] PID {pid}: {message}", Color.FromArgb(78, 201, 176));

                // Elevate process priority to AboveNormal to mitigate Windows background throttling
                try
                {
                    using var proc = Process.GetProcessById(pid);
                    if (!proc.HasExited)
                    {
                        proc.PriorityClass = ProcessPriorityClass.AboveNormal;
                        AppendLog($"[+] PID {pid}: Priority boosted to AboveNormal.", Color.FromArgb(156, 220, 254));
                    }
                }
                catch (Exception ex)
                {
                    AppendLog($"[!] PID {pid}: Priority boost skipped ({ex.Message}).", Color.FromArgb(170, 170, 170));
                }
            }
            else
            {
                AppendLog($"[-] PID {pid}: {message}", Color.FromArgb(206, 145, 120));
            }
        }

        #endregion

        #region UI Interactions

        private void ToggleMonitoring()
        {
            _isMonitoring = !_isMonitoring;

            if (_isMonitoring)
            {
                lblStatus.Text = "● Monitoring Active";
                lblStatus.ForeColor = Color.FromArgb(78, 201, 176);
                btnToggle.Text = "Pause";
                menuToggle.Text = "Pause Monitoring";
                AppendLog("Monitoring resumed.", Color.FromArgb(78, 201, 176));
            }
            else
            {
                lblStatus.Text = "○ Paused";
                lblStatus.ForeColor = Color.FromArgb(133, 133, 133);
                btnToggle.Text = "Resume";
                menuToggle.Text = "Resume Monitoring";
                AppendLog("Monitoring paused by user.", Color.FromArgb(133, 133, 133));
            }
        }

        private void RestoreWindow()
        {
            Show();
            WindowState = FormWindowState.Normal;
            ShowInTaskbar = true;
            BringToFront();
            Activate();
        }

        private void AppendLog(string message, Color? color = null)
        {
            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(() => AppendLog(message, color)));
                }
                catch (ObjectDisposedException)
                {
                    // Form is shutting down
                }
                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string line = $"[{timestamp}] {message}\r\n";

            rtbConsole.SelectionStart = rtbConsole.TextLength;
            rtbConsole.SelectionLength = 0;
            rtbConsole.SelectionColor = color ?? Color.FromArgb(212, 212, 212);
            rtbConsole.AppendText(line);
            rtbConsole.ScrollToCaret();
        }

        private void BtnToggle_Click(object? sender, EventArgs e)
        {
            ToggleMonitoring();
        }

        private void BtnClear_Click(object? sender, EventArgs e)
        {
            rtbConsole.Clear();
            AppendLog("Console cleared.", Color.FromArgb(133, 133, 133));
        }

        private void MenuOpen_Click(object? sender, EventArgs e)
        {
            RestoreWindow();
        }

        private void MenuToggle_Click(object? sender, EventArgs e)
        {
            ToggleMonitoring();
        }

        private void MenuGitHub_Click(object? sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(GitHubUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open browser: {ex.Message}", "MultiBloxManager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void MenuExit_Click(object? sender, EventArgs e)
        {
            _isExiting = true;
            Application.Exit();
        }

        private void TrayIcon_DoubleClick(object? sender, EventArgs e)
        {
            RestoreWindow();
        }

        #endregion

        #region Visual Assets

        private void SetupAppIcon()
        {
            try
            {
                using var bmp = new Bitmap(32, 32);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;

                    // Dark circle background
                    using var brush = new SolidBrush(Color.FromArgb(37, 37, 38));
                    g.FillEllipse(brush, 1, 1, 29, 29);

                    // Emerald border
                    using var pen = new Pen(Color.FromArgb(78, 201, 176), 2);
                    g.DrawEllipse(pen, 1, 1, 29, 29);

                    // Monogram "M"
                    using var font = new Font("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Pixel);
                    using var textBrush = new SolidBrush(Color.FromArgb(78, 201, 176));
                    var sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString("M", font, textBrush, new RectangleF(0, 0, 32, 32), sf);
                }

                IntPtr hIcon = bmp.GetHicon();
                _generatedAppIcon = Icon.FromHandle(hIcon);
                this.Icon = _generatedAppIcon;
                trayIcon.Icon = _generatedAppIcon;
            }
            catch
            {
                // Fallback to standard system icon if rendering fails
                trayIcon.Icon = SystemIcons.Application;
            }
        }

        #endregion
    }
}
