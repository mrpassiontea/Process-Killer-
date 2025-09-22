using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ProcessKiller
{
    public partial class ProcessKillerForm : Form
    {
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private Form highlightForm;
        private System.Windows.Forms.Timer debugTimer;

        // Win32 API imports
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("kernel32.dll")]
        private static extern uint GetLastError();

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private const int HOTKEY_ID = 9000;
        private const int MOD_CONTROL = 0x0002;
        private const int MOD_ALT = 0x0001;
        private const int VK_F12 = 0x7B;  // F12 key
        private const int WM_HOTKEY = 0x0312;

        public ProcessKillerForm()
        {
            InitializeComponent();
            CreateTrayIcon();

            // Make the form visible but minimized to ensure it can receive messages
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;

            // Start a timer to attempt hotkey registration after form is fully loaded
            debugTimer = new System.Windows.Forms.Timer();
            debugTimer.Interval = 1000; // 1 second delay
            debugTimer.Tick += (s, e) => {
                debugTimer.Stop();
                AttemptHotkeyRegistration();
            };
            debugTimer.Start();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(300, 200);
            this.Text = "Process Killer";
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void CreateTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Kill Active Window (Manual)", null, OnKillActive);
            trayMenu.Items.Add("Test Window Detection", null, OnTestWindowDetection);
            trayMenu.Items.Add("Force Register Hotkey", null, OnForceRegisterHotkey);
            trayMenu.Items.Add("Open Task Manager", null, OnOpenTaskManager);
            trayMenu.Items.Add("-");
            trayMenu.Items.Add("Exit", null, OnExit);

            trayIcon = new NotifyIcon()
            {
                Text = "Process Killer - Attempting to register F12 hotkey...",
                Icon = SystemIcons.Application,
                ContextMenuStrip = trayMenu,
                Visible = true
            };

            trayIcon.DoubleClick += (sender, e) => OnKillActive(sender, e);
        }

        private void AttemptHotkeyRegistration()
        {
            try
            {
                // Ensure we have a valid handle
                if (!this.IsHandleCreated)
                {
                    this.CreateHandle();
                }

                MessageBox.Show($"About to register hotkey...\nHandle: {this.Handle}\nHandle valid: {this.Handle != IntPtr.Zero}",
                    "Pre-Registration Debug");

                // Try F12 first (simplest)
                bool success = RegisterHotKey(this.Handle, HOTKEY_ID, 0, VK_F12);

                if (success)
                {
                    MessageBox.Show("SUCCESS! F12 hotkey registered!", "Success");
                    trayIcon.Text = "Process Killer - Press F12 to kill active window";
                    trayIcon.ShowBalloonTip(5000, "Success!", "F12 hotkey registered! Press F12 to kill active window.", ToolTipIcon.Info);
                }
                else
                {
                    uint error = GetLastError();
                    MessageBox.Show($"F12 registration FAILED!\nError: {error}\nTrying Ctrl+F12...", "Failed");

                    // Try Ctrl+F12
                    success = RegisterHotKey(this.Handle, HOTKEY_ID, MOD_CONTROL, VK_F12);
                    if (success)
                    {
                        MessageBox.Show("Ctrl+F12 registered successfully!", "Alternative Success");
                        trayIcon.Text = "Process Killer - Press Ctrl+F12 to kill active window";
                        trayIcon.ShowBalloonTip(5000, "Alternative Success!", "Ctrl+F12 registered!", ToolTipIcon.Info);
                    }
                    else
                    {
                        error = GetLastError();
                        MessageBox.Show($"Both F12 and Ctrl+F12 failed!\nError: {error}", "Total Failure");
                        trayIcon.Text = "Process Killer - Hotkey registration failed, use menu";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Exception during registration: {ex.Message}", "Exception");
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                MessageBox.Show($"HOTKEY DETECTED!\nMessage: {m.Msg}\nwParam: {m.WParam}\nOur ID: {HOTKEY_ID}", "Hotkey Received!");

                if (m.WParam.ToInt32() == HOTKEY_ID)
                {
                    KillActiveWindowProcess();
                }
            }

            base.WndProc(ref m);
        }

        private void OnTestWindowDetection(object sender, EventArgs e)
        {
            TestActiveWindowDetection();
        }

        private void TestActiveWindowDetection()
        {
            try
            {
                IntPtr activeWindow = GetForegroundWindow();

                if (activeWindow == IntPtr.Zero)
                {
                    MessageBox.Show("GetForegroundWindow returned NULL!", "Error");
                    return;
                }

                int processId;
                int result = GetWindowThreadProcessId(activeWindow, out processId);

                if (result == 0 || processId == 0)
                {
                    MessageBox.Show($"GetWindowThreadProcessId failed!\nResult: {result}\nProcessId: {processId}", "Error");
                    return;
                }

                Process process = Process.GetProcessById(processId);

                MessageBox.Show($"SUCCESS!\nWindow Handle: {activeWindow}\nProcess ID: {processId}\nProcess Name: {process.ProcessName}\nWindow Title: {process.MainWindowTitle}",
                    "Window Detection Success");

                // Test the highlight border
                CreateHighlightBorder(activeWindow);

                DialogResult result2 = MessageBox.Show("Can you see the yellow border around the detected window?", "Border Test", MessageBoxButtons.YesNo);

                RemoveHighlightBorder();

                if (result2 == DialogResult.No)
                {
                    MessageBox.Show("Border creation might have failed. Check if the window is visible and not minimized.", "Border Issue");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Exception in window detection: {ex.Message}\n\nStack trace:\n{ex.StackTrace}", "Exception");
            }
        }

        private void OnForceRegisterHotkey(object sender, EventArgs e)
        {
            // Unregister first
            UnregisterHotKey(this.Handle, HOTKEY_ID);
            System.Threading.Thread.Sleep(100);

            // Try again
            AttemptHotkeyRegistration();
        }

        private void CreateHighlightBorder(IntPtr windowHandle)
        {
            try
            {
                RECT rect;
                if (!GetWindowRect(windowHandle, out rect))
                {
                    MessageBox.Show("GetWindowRect failed!", "Border Error");
                    return;
                }

                // Remove any existing highlight
                RemoveHighlightBorder();

                // Create a thick yellow border
                highlightForm = new Form()
                {
                    FormBorderStyle = FormBorderStyle.None,
                    TopMost = true,
                    ShowInTaskbar = false,
                    StartPosition = FormStartPosition.Manual,
                    BackColor = Color.Yellow,
                    Size = new Size(rect.Right - rect.Left + 20, rect.Bottom - rect.Top + 20),
                    Location = new Point(rect.Left - 10, rect.Top - 10),
                    Opacity = 0.7
                };

                // Create inner transparent area to create border effect
                var innerPanel = new Panel()
                {
                    BackColor = Color.Magenta,
                    Size = new Size(rect.Right - rect.Left, rect.Bottom - rect.Top),
                    Location = new Point(10, 10)
                };
                innerPanel.BackColor = Color.FromArgb(1, 255, 255, 255); // Almost transparent

                highlightForm.Controls.Add(innerPanel);
                highlightForm.Show();

                MessageBox.Show($"Border created at: {rect.Left}, {rect.Top}, size: {rect.Right - rect.Left}x{rect.Bottom - rect.Top}", "Border Info");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not create border: {ex.Message}", "Border Error");
            }
        }

        private void RemoveHighlightBorder()
        {
            try
            {
                if (highlightForm != null)
                {
                    highlightForm.Close();
                    highlightForm.Dispose();
                    highlightForm = null;
                }
            }
            catch { }
        }

        private void OnKillActive(object sender, EventArgs e)
        {
            KillActiveWindowProcess();
        }

        private void OnOpenTaskManager(object sender, EventArgs e)
        {
            try
            {
                Process.Start("taskmgr.exe");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open Task Manager: {ex.Message}", "Error");
            }
        }

        private void KillActiveWindowProcess()
        {
            try
            {
                IntPtr activeWindow = GetForegroundWindow();

                if (activeWindow == IntPtr.Zero)
                {
                    MessageBox.Show("No active window found.", "Error");
                    return;
                }

                int processId;
                GetWindowThreadProcessId(activeWindow, out processId);

                if (processId == 0)
                {
                    MessageBox.Show("Could not get process ID.", "Error");
                    return;
                }

                Process process = Process.GetProcessById(processId);

                // Don't kill our own process or critical system processes
                if (process.Id == Process.GetCurrentProcess().Id)
                {
                    MessageBox.Show("Cannot kill the Process Killer application itself.", "Invalid Target");
                    return;
                }

                string[] protectedProcesses = { "explorer", "winlogon", "csrss", "smss", "services", "dwm", "lsass" };
                foreach (string protectedName in protectedProcesses)
                {
                    if (process.ProcessName.ToLower().Contains(protectedName))
                    {
                        MessageBox.Show($"Cannot kill protected system process: {process.ProcessName}", "Protected Process");
                        return;
                    }
                }

                // Create the yellow highlight border around target window
                CreateHighlightBorder(activeWindow);

                DialogResult result = MessageBox.Show(
                    $"Kill process '{process.ProcessName}' (PID: {processId})?\n\nThe target window should be highlighted with a yellow border.",
                    "Confirm Process Kill",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                // Remove the highlight border
                RemoveHighlightBorder();

                if (result == DialogResult.Yes)
                {
                    process.Kill();
                    MessageBox.Show($"Successfully killed {process.ProcessName}", "Process Killed");
                }
            }
            catch (Exception ex)
            {
                RemoveHighlightBorder();
                MessageBox.Show($"Error killing process: {ex.Message}", "Error");
            }
        }

        private void OnExit(object sender, EventArgs e)
        {
            RemoveHighlightBorder();
            UnregisterHotKey(this.Handle, HOTKEY_ID);
            trayIcon.Visible = false;
            debugTimer?.Stop();
            debugTimer?.Dispose();
            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                RemoveHighlightBorder();
                UnregisterHotKey(this.Handle, HOTKEY_ID);
                trayIcon?.Dispose();
                trayMenu?.Dispose();
                debugTimer?.Stop();
                debugTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // Program entry point
    class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ProcessKillerForm());
        }
    }
}