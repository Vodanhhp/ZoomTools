using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

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
        private readonly Label _noticeLabel;
        private readonly Timer _noticeTimer;

        // These are local assumptions based on clicks in this app, not live Zoom states.
        private bool _micOn;
        private bool _cameraOn;
        private bool _muteAllPressed;

        private static readonly Color WindowColor = Color.FromArgb(48, 48, 48);
        private static readonly Color OnColor = Color.FromArgb(22, 134, 91);
        private static readonly Color OffColor = Color.FromArgb(217, 83, 79);
        private static readonly Color LinkColor = Color.FromArgb(100, 181, 246);

        public MainForm()
        {
            Text = "Công cụ Zoom";
            ClientSize = new Size(236, 158);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = true;
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true;
            ShowIcon = false;
            BackColor = WindowColor;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            _micButton = CreateActionButton("MIC", 8, 8, 104, 38);
            _muteAllButton = CreateActionButton("MUTE ALL", 120, 8, 108, 38);
            _cameraButton = CreateActionButton("CAMERA", 8, 51, 220, 38);

            _micButton.Click += MicButton_Click;
            _cameraButton.Click += CameraButton_Click;
            _muteAllButton.MouseDown += MuteAllButton_MouseDown;
            _muteAllButton.MouseUp += MuteAllButton_MouseUp;
            _muteAllButton.MouseCaptureChanged += MuteAllButton_MouseCaptureChanged;

            LinkLabel settingsLink = CreateLink("Cài đặt", 10, 98, 58);
            LinkLabel helpLink = CreateLink("Help", 174, 98, 48);
            settingsLink.LinkClicked += delegate { ShowNotice("Mục Cài đặt sẽ được bổ sung sau."); };
            helpLink.LinkClicked += delegate
            {
                ShowNotice("Bật Global Shortcut trong Zoom để dùng phím tắt.");
            };

            _noticeLabel = new Label
            {
                AutoSize = false,
                Location = new Point(9, 124),
                Size = new Size(218, 22),
                ForeColor = Color.Gainsboro,
                BackColor = WindowColor,
                Text = "Sẵn sàng",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8F, FontStyle.Regular)
            };

            Controls.Add(_micButton);
            Controls.Add(_muteAllButton);
            Controls.Add(_cameraButton);
            Controls.Add(settingsLink);
            Controls.Add(helpLink);
            Controls.Add(_noticeLabel);

            _noticeTimer = new Timer { Interval = 2800 };
            _noticeTimer.Tick += delegate
            {
                _noticeTimer.Stop();
                _noticeLabel.Text = "Sẵn sàng";
            };

            UpdateToggleButtons();
            UpdateMuteAllButton();
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
                BackColor = OffColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                UseVisualStyleBackColor = false,
                TabStop = false,
                Cursor = Cursors.Hand
            };
        }

        private static LinkLabel CreateLink(string text, int x, int y, int width)
        {
            return new LinkLabel
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 20),
                LinkColor = LinkColor,
                ActiveLinkColor = Color.White,
                VisitedLinkColor = LinkColor,
                BackColor = WindowColor,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                LinkBehavior = LinkBehavior.HoverUnderline
            };
        }

        private void MicButton_Click(object sender, EventArgs e)
        {
            SendZoomShortcut('A');
            _micOn = !_micOn;
            UpdateToggleButtons();
            ShowNotice("Đã gửi Alt + A đến Zoom.");
        }

        private void CameraButton_Click(object sender, EventArgs e)
        {
            SendZoomShortcut('V');
            _cameraOn = !_cameraOn;
            UpdateToggleButtons();
            ShowNotice("Đã gửi Alt + V đến Zoom.");
        }

        private void MuteAllButton_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || _muteAllPressed)
                return;

            _muteAllPressed = true;
            UpdateMuteAllButton();
            SendZoomShortcut('M');
            ShowNotice("Đã gửi Alt + M. Cần quyền Host/Co-host.");
        }

        private void MuteAllButton_MouseUp(object sender, MouseEventArgs e)
        {
            _muteAllPressed = false;
            UpdateMuteAllButton();
        }

        private void MuteAllButton_MouseCaptureChanged(object sender, EventArgs e)
        {
            if (! _muteAllButton.Capture)
            {
                _muteAllPressed = false;
                UpdateMuteAllButton();
            }
        }

        private void UpdateToggleButtons()
        {
            _micButton.BackColor = _micOn ? OnColor : OffColor;
            _cameraButton.BackColor = _cameraOn ? OnColor : OffColor;
        }

        private void UpdateMuteAllButton()
        {
            _muteAllButton.BackColor = _muteAllPressed ? OnColor : OffColor;
        }

        private void ShowNotice(string message)
        {
            _noticeTimer.Stop();
            _noticeLabel.Text = message;
            _noticeTimer.Start();
        }

        private static void SendZoomShortcut(char key)
        {
            byte virtualKey = (byte)char.ToUpperInvariant(key);

            // Sends Alt + key once. Zoom must have Global Shortcut enabled.
            keybd_event(VkMenu, 0, 0, UIntPtr.Zero);
            keybd_event(virtualKey, 0, 0, UIntPtr.Zero);
            keybd_event(virtualKey, 0, KeyUp, UIntPtr.Zero);
            keybd_event(VkMenu, 0, KeyUp, UIntPtr.Zero);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _noticeTimer.Dispose();
            base.OnFormClosed(e);
        }
    }
}
