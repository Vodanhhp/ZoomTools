using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Automation;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZoomMini
{
    public sealed class MainForm : Form
    {
        private const int KeyUp = 0x0002;
        private const byte VkMenu = 0x12;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte virtualKey, byte scanCode, int flags, UIntPtr extraInfo);

        private readonly Button _micButton;
        private readonly Button _muteAllButton;
        private readonly Button _cameraButton;
        private readonly Label _statusLabel;
        private readonly Label _minimizeLabel;
        private readonly Label _closeLabel;
        private readonly System.Windows.Forms.Timer _zoomCheckTimer;
        private readonly System.Windows.Forms.Timer _uiStateTimer;
        private bool? _detectedMicOn;
        private bool? _detectedCameraOn;
        private int _readingUiState;

        private bool _micOn;
        private bool _cameraOn;
        private bool _muteAllPressed;
        private bool _zoomRunning;
        private bool _zoomStateInitialized;

        private static readonly Color BackgroundColor = Color.FromArgb(8, 12, 20);
        private static readonly Color PanelColor = Color.FromArgb(13, 25, 42);
        private static readonly Color ZoomBlue = Color.FromArgb(14, 113, 235);
        private static readonly Color ButtonOff = Color.FromArgb(19, 31, 48);
        private static readonly Color TextColor = Color.White;
        private static readonly Color MutedText = Color.FromArgb(170, 190, 215);
        private static readonly Color DisabledColor = Color.FromArgb(45, 52, 62);

        public MainForm()
        {
            Text = "Công cụ Zoom";
            ClientSize = new Size(228, 132);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true;
            ShowInTaskbar = true;
            BackColor = BackgroundColor;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            // Small custom top strip: no Windows title bar.
            Panel topPanel = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(228, 25),
                BackColor = PanelColor
            };
            Label appName = new Label
            {
                Text = "ZOOM CONTROL",
                Location = new Point(9, 3),
                Size = new Size(130, 19),
                ForeColor = MutedText,
                BackColor = PanelColor,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            Label helpLabel = CreateWindowLabel("Help", 143, 3);
            helpLabel.Size = new Size(34, 18);
            helpLabel.Font = new Font("Segoe UI", 7.5F, FontStyle.Bold);
            helpLabel.Click += HelpLabel_Click;
            _minimizeLabel = CreateWindowLabel("—", 181, 2);
            _closeLabel = CreateWindowLabel("×", 204, 1);
            _minimizeLabel.Click += delegate { WindowState = FormWindowState.Minimized; };
            _closeLabel.Click += delegate { Close(); };
            topPanel.Controls.Add(appName);
            topPanel.Controls.Add(helpLabel);
            topPanel.Controls.Add(_minimizeLabel);
            topPanel.Controls.Add(_closeLabel);
            topPanel.MouseDown += DragWindow;
            appName.MouseDown += DragWindow;

            _micButton = CreateActionButton("MIC", 8, 32, 103, 34);
            _muteAllButton = CreateActionButton("MUTE ALL", 117, 32, 103, 34);
            _cameraButton = CreateActionButton("CAMERA", 8, 72, 212, 30);

            _micButton.Click += MicButton_Click;
            _cameraButton.Click += CameraButton_Click;
            _muteAllButton.MouseDown += MuteAllButton_MouseDown;
            _muteAllButton.MouseUp += MuteAllButton_MouseUp;
            _muteAllButton.MouseCaptureChanged += MuteAllButton_MouseCaptureChanged;

            _statusLabel = new Label
            {
                AutoSize = false,
                Location = new Point(7, 106),
                Size = new Size(214, 19),
                ForeColor = MutedText,
                BackColor = BackgroundColor,
                Text = "Đang kiểm tra Zoom...",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Regular)
            };

            Controls.Add(topPanel);
            Controls.Add(_micButton);
            Controls.Add(_muteAllButton);
            Controls.Add(_cameraButton);
            Controls.Add(_statusLabel);

            _zoomCheckTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _zoomCheckTimer.Tick += delegate { CheckZoomProcess(); };
            CheckZoomProcess();
            _zoomCheckTimer.Start();

            _uiStateTimer = new System.Windows.Forms.Timer { Interval = 2500 };
            _uiStateTimer.Tick += delegate { ReadZoomButtonStates(); };
            _uiStateTimer.Start();

            UpdateToggleButtons();
            UpdateMuteAllButton();
        }

        private static Label CreateWindowLabel(string text, int x, int y)
        {
            Label label = new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(20, 20),
                ForeColor = TextColor,
                BackColor = PanelColor,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            label.MouseEnter += delegate { label.ForeColor = ZoomBlue; };
            label.MouseLeave += delegate { label.ForeColor = TextColor; };
            return label;
        }

        private static Button CreateActionButton(string text, int x, int y, int width, int height)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, height),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                BackColor = ButtonOff,
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                UseVisualStyleBackColor = false,
                TabStop = false,
                Cursor = Cursors.Hand
            };
        }

        private void HelpLabel_Click(object sender, EventArgs e)
        {
            string helpText =
                "CÁCH BẬT PHÍM TẮT ZOOM\n\n" +
                "1. Mở Zoom Workplace và đăng nhập.\n" +
                "2. Nhấn ảnh đại diện hoặc biểu tượng bánh răng Settings.\n" +
                "3. Chọn Settings → Keyboard Shortcuts.\n" +
                "4. Tìm các phím tắt Mute/Unmute My Audio (Alt + A), Mute All (Alt + M) và Start/Stop Video (Alt + V).\n" +
                "5. Đánh dấu tùy chọn Global Shortcut cho từng phím tắt nếu Zoom hiển thị tùy chọn này.\n\n" +
                "Lưu ý: Mute All yêu cầu quyền Host/Co-host. Tên mục có thể khác đôi chút tùy phiên bản Zoom.\n\n" +
                "Liên hệ: tuanluongpham@gmail.com";

            MessageBox.Show(this, helpText, "Help - Công cụ Zoom",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DragWindow(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            ReleaseCapture();
            SendMessage(Handle, 0xA1, new IntPtr(2), IntPtr.Zero);
        }

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        private void CheckZoomProcess()
        {
            bool running;
            try
            {
                Process[] processes = Process.GetProcessesByName("Zoom");
                running = processes.Length > 0;
                foreach (Process process in processes) process.Dispose();
            }
            catch
            {
                running = false;
            }

            if (_zoomStateInitialized && running == _zoomRunning) return;

            _zoomStateInitialized = true;
            _zoomRunning = running;
            _micButton.Enabled = running;
            _muteAllButton.Enabled = running;
            _cameraButton.Enabled = running;

            if (running)
            {
                _statusLabel.Text = "Đã phát hiện Zoom • Sẵn sàng";
                _statusLabel.ForeColor = Color.FromArgb(90, 190, 255);
            }
            else
            {
                _statusLabel.Text = "Chưa phát hiện Zoom đang chạy";
                _statusLabel.ForeColor = MutedText;
                _micOn = false;
                _cameraOn = false;
                _muteAllPressed = false;
            }

            UpdateToggleButtons();
            UpdateMuteAllButton();
        }


        // Inspect only top-level windows owned by Zoom. Meeting controls identify an active meeting.
        // UI Automation runs in the background so it cannot freeze this app's UI.
        private void ReadZoomButtonStates()
        {
            if (!_zoomRunning || Interlocked.Exchange(ref _readingUiState, 1) == 1)
                return;

            Task.Run(delegate
            {
                bool? micOn = null;
                bool? cameraOn = null;
                bool meetingFound = false;

                try
                {
                    HashSet<int> zoomProcessIds = new HashSet<int>();
                    Process[] processes = Process.GetProcessesByName("Zoom");
                    foreach (Process process in processes)
                    {
                        try { zoomProcessIds.Add(process.Id); }
                        catch { }
                        finally { process.Dispose(); }
                    }

                    List<IntPtr> zoomWindows = new List<IntPtr>();
                    EnumWindows(delegate (IntPtr hWnd, IntPtr lParam)
                    {
                        if (!IsWindowVisible(hWnd)) return true;

                        uint processId;
                        GetWindowThreadProcessId(hWnd, out processId);
                        if (zoomProcessIds.Contains((int)processId))
                            zoomWindows.Add(hWnd);

                        return true;
                    }, IntPtr.Zero);

                    foreach (IntPtr window in zoomWindows)
                    {
                        try
                        {
                            AutomationElement zoomWindow = AutomationElement.FromHandle(window);
                            if (zoomWindow == null) continue;

                            AutomationElementCollection buttons = zoomWindow.FindAll(
                                TreeScope.Descendants,
                                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));

                            bool? windowMicOn = null;
                            bool? windowCameraOn = null;

                            foreach (AutomationElement button in buttons)
                            {
                                string name = button.Current.Name;
                                if (string.IsNullOrWhiteSpace(name)) continue;

                                string value = name.Trim().ToLowerInvariant();

                                // Check "unmute" before "mute" because the former contains the latter.
                                if (windowMicOn == null)
                                {
                                    if (value.Contains("unmute") || value.Contains("enable audio"))
                                        windowMicOn = false;
                                    else if (value.Contains("mute") || value.Contains("disable audio"))
                                        windowMicOn = true;
                                }

                                if (windowCameraOn == null)
                                {
                                    if (value.Contains("stop video") || value.Contains("turn off video") ||
                                        value.Contains("stop my video") || value.Contains("stop camera"))
                                        windowCameraOn = true;
                                    else if (value.Contains("start video") || value.Contains("turn on video") ||
                                             value.Contains("start my video") || value.Contains("start camera"))
                                        windowCameraOn = false;
                                }
                            }

                            // A visible microphone control is the strongest indication that this is a meeting window.
                            if (windowMicOn.HasValue || windowCameraOn.HasValue)
                            {
                                meetingFound = true;
                                if (windowMicOn.HasValue) micOn = windowMicOn;
                                if (windowCameraOn.HasValue) cameraOn = windowCameraOn;
                                if (micOn.HasValue && cameraOn.HasValue) break;
                            }
                        }
                        catch
                        {
                            // Ignore inaccessible or closing Zoom windows.
                        }
                    }
                }
                catch
                {
                    // UI Automation can be unavailable while Zoom is starting or closing.
                }

                try
                {
                    if (!IsDisposed && IsHandleCreated)
                    {
                        BeginInvoke((MethodInvoker)delegate
                        {
                            if (meetingFound)
                            {
                                if (micOn.HasValue) _micOn = micOn.Value;
                                if (cameraOn.HasValue) _cameraOn = cameraOn.Value;
                                UpdateToggleButtons();
                                _statusLabel.Text = "Đã đọc trạng thái cuộc họp";
                                _statusLabel.ForeColor = Color.FromArgb(90, 190, 255);
                            }
                        });
                    }
                }
                catch
                {
                    // The form may be closing.
                }
                finally
                {
                    Interlocked.Exchange(ref _readingUiState, 0);
                }
            });
        }

        private bool CanSendShortcut()
        {
            CheckZoomProcess();
            if (_zoomRunning) return true;
            _statusLabel.Text = "Hãy mở Zoom trước";
            return false;
        }

        private void MicButton_Click(object sender, EventArgs e)
        {
            if (!CanSendShortcut()) return;
            SendZoomShortcut('A');
            _micOn = !_micOn;
            UpdateToggleButtons();
            _statusLabel.Text = "Đã gửi phím tắt MIC (Alt + A)";
        }

        private void CameraButton_Click(object sender, EventArgs e)
        {
            if (!CanSendShortcut()) return;
            SendZoomShortcut('V');
            _cameraOn = !_cameraOn;
            UpdateToggleButtons();
            _statusLabel.Text = "Đã gửi phím tắt CAMERA (Alt + V)";
        }

        private void MuteAllButton_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || _muteAllPressed || !CanSendShortcut()) return;
            _muteAllPressed = true;
            UpdateMuteAllButton();
            SendZoomShortcut('M');
            _statusLabel.Text = "Đã gửi phím tắt MUTE ALL (Alt + M)";
        }

        private void MuteAllButton_MouseUp(object sender, MouseEventArgs e)
        {
            _muteAllPressed = false;
            UpdateMuteAllButton();
            if (_zoomRunning)
            {
                _statusLabel.Text = "Đã phát hiện Zoom • Sẵn sàng";
                _statusLabel.ForeColor = Color.FromArgb(90, 190, 255);
            }
        }

        private void MuteAllButton_MouseCaptureChanged(object sender, EventArgs e)
        {
            if (!_muteAllButton.Capture)
            {
                _muteAllPressed = false;
                UpdateMuteAllButton();
            }
        }

        private void UpdateToggleButtons()
        {
            _micButton.BackColor = !_zoomRunning ? DisabledColor : (_micOn ? ZoomBlue : ButtonOff);
            _cameraButton.BackColor = !_zoomRunning ? DisabledColor : (_cameraOn ? ZoomBlue : ButtonOff);
        }

        private void UpdateMuteAllButton()
        {
            _muteAllButton.BackColor = !_zoomRunning ? DisabledColor : (_muteAllPressed ? ZoomBlue : ButtonOff);
        }

        private static void SendZoomShortcut(char key)
        {
            byte virtualKey = (byte)char.ToUpperInvariant(key);
            keybd_event(VkMenu, 0, 0, UIntPtr.Zero);
            keybd_event(virtualKey, 0, 0, UIntPtr.Zero);
            keybd_event(virtualKey, 0, KeyUp, UIntPtr.Zero);
            keybd_event(VkMenu, 0, KeyUp, UIntPtr.Zero);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _zoomCheckTimer.Stop();
            _zoomCheckTimer.Dispose();
            _uiStateTimer.Stop();
            _uiStateTimer.Dispose();
            base.OnFormClosed(e);
        }
    }
}