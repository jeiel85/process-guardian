using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.Json;

namespace ProcessGuardian
{
    public partial class Form1 : Form
    {
        private static readonly Color ColorBackground = Color.FromArgb(18, 18, 20);
        private static readonly Color ColorCard = Color.FromArgb(30, 30, 35);
        private static readonly Color ColorAccent = Color.FromArgb(37, 99, 235);
        private static readonly Color ColorText = Color.FromArgb(248, 250, 252);
        private static readonly Color ColorStatusRunning = Color.FromArgb(34, 197, 94);
        private static readonly Color ColorStatusStopped = Color.FromArgb(239, 68, 68);
        private static readonly Color ColorStatusWarning = Color.FromArgb(245, 158, 11);

        private List<ProcessSlot> slots = new List<ProcessSlot>();
        private FlowLayoutPanel flowSlots;
        private Button btnAddSlot;
        private Label lblLang;
        private Label lblInterval;
        private ToolTip toolTip;
        private ToolStripMenuItem trayOpenItem;
        private ToolStripMenuItem trayExitItem;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private CancellationTokenSource? cts;
        private RichTextBox logBox;
        private bool isAdmin = false;
        private int currentLangIndex = 0;

        private int monitoringInterval = 3000;
        private int memoryThresholdMB = 2048;
        private string logFilePath = "";
        private string webhookUrl = "";
        private bool useWindowsEventLog = false;
        private bool useHangDetection = false;
        private int hangTimeoutSec = 30;
        private int startupDelaySec = 0;
        private Dictionary<string, DateTime> lastResponseTime = new Dictionary<string, DateTime>();

        public Form1()
        {
            InitializeComponent();
            CheckAdminStatus();
            DetectSystemLanguage();
            InitializeModernUI();
            LoadSettings();
            StartMonitoring();
            UpdateUITexts();
            Log(isAdmin ? "System started with Administrator privileges." : "System started with User privileges. (Some monitoring might be limited)",
                isAdmin ? ColorStatusRunning : ColorStatusWarning);
        }

        private void CheckAdminStatus()
        {
            try {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            } catch { isAdmin = false; }
        }

        private void DetectSystemLanguage()
        {
            string lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLower();
            if (lang == "ko") currentLangIndex = 1;
            else if (lang == "ja") currentLangIndex = 2;
            else if (lang == "zh") currentLangIndex = 3;
            else currentLangIndex = 0;
        }

        private void InitializeModernUI()
        {
            this.BackColor = ColorBackground;
            this.ForeColor = ColorText;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            toolTip = new ToolTip { AutoPopDelay = 6000, InitialDelay = 300 };

            // ── 헤더 ──────────────────────────────────────────────
            PictureBox logo = new PictureBox
            {
                Image = SystemIcons.Shield.ToBitmap(),
                Location = new Point(20, 16),
                Size = new Size(34, 34),
                SizeMode = PictureBoxSizeMode.StretchImage
            };
            this.Controls.Add(logo);

            Label headerTitle = new Label
            {
                Text = "Process Guardian Pro",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                Location = new Point(63, 8),
                AutoSize = true,
                ForeColor = ColorText
            };
            this.Controls.Add(headerTitle);

            Label lblAdminStatus = new Label
            {
                Text = isAdmin ? "✓ 관리자 모드" : "⚠  관리자 권한 없음 — 일부 기능 제한됨",
                ForeColor = isAdmin ? ColorStatusRunning : ColorStatusWarning,
                Font = new Font("Segoe UI", 8F),
                Location = new Point(65, 46),
                AutoSize = true
            };
            this.Controls.Add(lblAdminStatus);

            btnAddSlot = new Button
            {
                Text = "+ 신규 슬롯",
                Location = new Point(492, 16),
                Size = new Size(116, 38),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 50, 58),
                ForeColor = ColorAccent,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnAddSlot.FlatAppearance.BorderSize = 1;
            btnAddSlot.FlatAppearance.BorderColor = ColorAccent;
            btnAddSlot.Click += (s, e) => AddNewSlot();
            this.Controls.Add(btnAddSlot);

            // ── 슬롯 영역 ─────────────────────────────────────────
            flowSlots = new FlowLayoutPanel
            {
                Location = new Point(20, 72),
                Size = new Size(572, 520),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent
            };
            this.Controls.Add(flowSlots);

            // ── 설정 패널 ─────────────────────────────────────────
            Panel settingsPanel = new Panel
            {
                Location = new Point(20, 602),
                Size = new Size(572, 190),
                BackColor = ColorCard
            };
            settingsPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rp = GetRoundedRectanglePath(settingsPanel.ClientRectangle, 12);
                settingsPanel.Region = new Region(rp);
                using var pen = new Pen(Color.FromArgb(50, 50, 62), 1);
                e.Graphics.DrawPath(pen, rp);
            };
            this.Controls.Add(settingsPanel);

            Color secC = Color.FromArgb(72, 72, 92);
            Color lblC = Color.FromArgb(128, 128, 148);
            Color dimC = Color.FromArgb(88, 88, 108);
            Font secF = new Font("Segoe UI", 7.5F, FontStyle.Bold);
            Font lf = new Font("Segoe UI", 8F);
            Font uf = new Font("Segoe UI", 7.5F);

            // ── 기본 설정 ──────────────────────────────────────────
            AddSectionHeader(settingsPanel, "기본 설정", 12, 10, secC, secF);

            lblLang = new Label { Location = new Point(12, 31), AutoSize = true, ForeColor = lblC, Font = lf };
            ComboBox comboLang = new ComboBox
            {
                Location = new Point(55, 28),
                Width = 90,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(22, 22, 28),
                ForeColor = ColorText,
                FlatStyle = FlatStyle.Flat,
                Font = lf
            };
            comboLang.Items.AddRange(new string[] { "English", "한국어", "日本語", "简体中文" });
            comboLang.SelectedIndex = currentLangIndex;
            comboLang.SelectedIndexChanged += (s, e) => ChangeLanguage(comboLang.SelectedIndex);
            settingsPanel.Controls.Add(lblLang);
            settingsPanel.Controls.Add(comboLang);

            lblInterval = new Label { Location = new Point(158, 31), AutoSize = true, ForeColor = lblC, Font = lf };
            NumericUpDown numInterval = new NumericUpDown
            {
                Location = new Point(228, 28),
                Width = 46,
                Minimum = 1, Maximum = 60,
                Value = Math.Max(1, monitoringInterval / 1000),
                BackColor = Color.FromArgb(22, 22, 28),
                ForeColor = ColorText,
                BorderStyle = BorderStyle.FixedSingle,
                Font = lf
            };
            numInterval.ValueChanged += (s, e) => { monitoringInterval = (int)numInterval.Value * 1000; };
            Label lblIntervalUnit = new Label { Text = "초", Location = new Point(276, 31), AutoSize = true, ForeColor = lblC, Font = uf };
            settingsPanel.Controls.AddRange(new Control[] { lblInterval, numInterval, lblIntervalUnit });

            Label lblMem = new Label { Text = "메모리 경고 임계값:", Location = new Point(296, 31), AutoSize = true, ForeColor = lblC, Font = lf };
            NumericUpDown numMem = new NumericUpDown
            {
                Location = new Point(416, 28),
                Width = 56,
                Minimum = 512, Maximum = 16384,
                Value = memoryThresholdMB,
                BackColor = Color.FromArgb(22, 22, 28),
                ForeColor = ColorText,
                BorderStyle = BorderStyle.FixedSingle,
                Font = lf
            };
            numMem.ValueChanged += (s, e) => { memoryThresholdMB = (int)numMem.Value; };
            Label lblMemUnit = new Label { Text = "MB", Location = new Point(475, 31), AutoSize = true, ForeColor = lblC, Font = uf };
            settingsPanel.Controls.AddRange(new Control[] { lblMem, numMem, lblMemUnit });

            // ── 실행 옵션 ──────────────────────────────────────────
            AddSectionHeader(settingsPanel, "실행 옵션", 12, 55, secC, secF);

            CheckBox chkAutoStart = new CheckBox
            {
                Text = "Windows 시작 시 자동 실행",
                Location = new Point(12, 73),
                AutoSize = true,
                ForeColor = lblC,
                Font = lf
            };
            chkAutoStart.Checked = IsAutoStartEnabled();
            chkAutoStart.CheckedChanged += (s, e) => SetAutoStart(chkAutoStart.Checked);
            settingsPanel.Controls.Add(chkAutoStart);

            Label lblDelay = new Label { Text = "시작 지연:", Location = new Point(215, 74), AutoSize = true, ForeColor = lblC, Font = lf };
            NumericUpDown numDelay = new NumericUpDown
            {
                Location = new Point(272, 71),
                Width = 46,
                Minimum = 0, Maximum = 300,
                Value = startupDelaySec,
                BackColor = Color.FromArgb(22, 22, 28),
                ForeColor = ColorText,
                BorderStyle = BorderStyle.FixedSingle,
                Font = lf
            };
            numDelay.ValueChanged += (s, e) => { startupDelaySec = (int)numDelay.Value; };
            Label lblDelayDesc = new Label { Text = "초  (부팅 후 감시 시작까지 대기)", Location = new Point(321, 74), AutoSize = true, ForeColor = dimC, Font = uf };
            settingsPanel.Controls.AddRange(new Control[] { lblDelay, numDelay, lblDelayDesc });

            // ── 모니터링 ───────────────────────────────────────────
            AddSectionHeader(settingsPanel, "모니터링", 12, 99, secC, secF);

            CheckBox chkEventLog = new CheckBox
            {
                Text = "이벤트 로그 기록 (Windows 이벤트 뷰어)",
                Location = new Point(12, 117),
                AutoSize = true,
                ForeColor = lblC,
                Font = lf
            };
            chkEventLog.Checked = useWindowsEventLog;
            chkEventLog.CheckedChanged += (s, e) => { useWindowsEventLog = chkEventLog.Checked; Properties.Settings.Default.UseWindowsEventLog = useWindowsEventLog; };
            settingsPanel.Controls.Add(chkEventLog);

            CheckBox chkHang = new CheckBox
            {
                Text = "응답 없음 감지",
                Location = new Point(12, 137),
                AutoSize = true,
                ForeColor = lblC,
                Font = lf
            };
            chkHang.Checked = useHangDetection;
            chkHang.CheckedChanged += (s, e) => { useHangDetection = chkHang.Checked; Properties.Settings.Default.UseHangDetection = useHangDetection; };
            settingsPanel.Controls.Add(chkHang);

            Label lblHangDesc = new Label { Text = "— 실행 중이어도 프로세스가 응답 없으면 감지", Location = new Point(122, 138), AutoSize = true, ForeColor = dimC, Font = uf };
            settingsPanel.Controls.Add(lblHangDesc);

            Label lblHang = new Label { Text = "응답 대기:", Location = new Point(356, 138), AutoSize = true, ForeColor = lblC, Font = lf };
            NumericUpDown numHang = new NumericUpDown
            {
                Location = new Point(416, 135),
                Width = 46,
                Minimum = 5, Maximum = 300,
                Value = hangTimeoutSec,
                BackColor = Color.FromArgb(22, 22, 28),
                ForeColor = ColorText,
                BorderStyle = BorderStyle.FixedSingle,
                Font = lf
            };
            numHang.ValueChanged += (s, e) => { hangTimeoutSec = (int)numHang.Value; };
            Label lblHangUnit = new Label { Text = "초", Location = new Point(465, 138), AutoSize = true, ForeColor = lblC, Font = uf };
            settingsPanel.Controls.AddRange(new Control[] { lblHang, numHang, lblHangUnit, lblHangDesc });

            // ── 알림 Webhook ───────────────────────────────────────
            AddSectionHeader(settingsPanel, "알림  Webhook", 12, 162, secC, secF);

            // ── 트레이 아이콘 ──────────────────────────────────────
            trayMenu = new ContextMenuStrip();
            trayMenu.Renderer = new DarkModeRenderer();
            trayOpenItem = new ToolStripMenuItem("Open Dashboard", null, (s, e) => ShowForm());
            trayExitItem = new ToolStripMenuItem("Exit Guardian", null, (s, e) => ExitApp());
            ToolStripMenuItem traySettingsItem = new ToolStripMenuItem(GetStr("Settings"));
            ToolStripMenuItem menuExport = new ToolStripMenuItem(GetStr("Export"), null, (s, e) => ExportSettings());
            ToolStripMenuItem menuImport = new ToolStripMenuItem(GetStr("Import"), null, (s, e) => ImportSettings());
            ToolStripMenuItem menuProfile = new ToolStripMenuItem(GetStr("Profile"));
            traySettingsItem.DropDownItems.Add(menuExport);
            traySettingsItem.DropDownItems.Add(menuImport);
            traySettingsItem.DropDownItems.Add(new ToolStripSeparator());
            traySettingsItem.DropDownItems.Add(menuProfile);
            trayMenu.Items.Add(trayOpenItem);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(traySettingsItem);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(trayExitItem);

            trayIcon = new NotifyIcon { Text = "Process Guardian Pro", Icon = SystemIcons.Shield, ContextMenuStrip = trayMenu, Visible = true };
            trayIcon.DoubleClick += (s, e) => ShowForm();

            // ── 로그 ──────────────────────────────────────────────
            logBox = new RichTextBox
            {
                Location = new Point(20, 802),
                Size = new Size(572, 148),
                BackColor = Color.FromArgb(22, 22, 28),
                ForeColor = Color.FromArgb(200, 200, 210),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                Font = new Font("Consolas", 8.5F)
            };
            this.Controls.Add(logBox);

            // Webhook 텍스트박스는 settingsPanel 아래 별도 패널에 배치
            Panel webhookPanel = new Panel
            {
                Location = new Point(20, 800 - 58),
                Size = new Size(572, 28),
                BackColor = Color.Transparent
            };
            TextBox txtWebhook = new TextBox
            {
                Text = webhookUrl,
                Location = new Point(0, 0),
                Width = 572,
                Height = 26,
                BackColor = Color.FromArgb(22, 22, 28),
                ForeColor = Color.FromArgb(180, 180, 200),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 8F),
                PlaceholderText = "https://hooks.slack.com/...   (비워두면 알림 없음)"
            };
            txtWebhook.Leave += (s, e) => { webhookUrl = txtWebhook.Text; Properties.Settings.Default.WebhookUrl = webhookUrl; };
            webhookPanel.Controls.Add(txtWebhook);
            this.Controls.Add(webhookPanel);

            this.Size = new Size(632, 988);
        }

        private void AddSectionHeader(Panel parent, string text, int x, int y, Color color, Font font)
        {
            Label lbl = new Label { Text = text, Location = new Point(x, y), AutoSize = true, ForeColor = color, Font = font };
            parent.Controls.Add(lbl);
            int divX = x + TextRenderer.MeasureText(text, font).Width + 6;
            Panel div = new Panel { Location = new Point(divX, y + 6), Size = new Size(parent.Width - divX - 12, 1), BackColor = Color.FromArgb(45, 45, 60) };
            parent.Controls.Add(div);
        }

        private void AddNewSlot(string path = "", string args = "")
        {
            int i = slots.Count;
            ProcessSlot slot = new ProcessSlot { Index = i, Path = path, Args = args };

            Panel card = new Panel { Size = new Size(550, 88), BackColor = ColorCard, Padding = new Padding(12), Margin = new Padding(0, 0, 0, 8) };
            card.Paint += Card_Paint;

            slot.Led = new Label { Location = new Point(15, 16), Size = new Size(14, 14), BackColor = Color.Transparent, Tag = "stopped" };
            slot.Led.Paint += StatusLed_Paint;

            slot.SlotLabel = new Label { Location = new Point(38, 14), Font = new Font("Segoe UI Semibold", 9F), ForeColor = Color.FromArgb(150, 150, 165), AutoSize = true, Text = $"{GetStr("Slot")} {i + 1}" };
            slot.StatusText = new Label { Text = "IDLE", Location = new Point(424, 14), TextAlign = ContentAlignment.TopRight, ForeColor = Color.FromArgb(100, 100, 115), Font = new Font("Segoe UI", 9F, FontStyle.Bold), Width = 112 };

            slot.PathBox = new TextBox { Text = path, Location = new Point(15, 50), Width = 328, BackColor = Color.FromArgb(20, 20, 25), ForeColor = ColorText, BorderStyle = BorderStyle.None, ReadOnly = true, Font = new Font("Segoe UI", 9F) };

            slot.BrowseBtn = new Button
            {
                Text = GetStr("Browse"),
                Location = new Point(350, 44),
                Width = 92,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorAccent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 8.5F),
                Tag = i,
                Cursor = Cursors.Hand
            };
            slot.BrowseBtn.FlatAppearance.BorderSize = 0;
            slot.BrowseBtn.Click += BtnSelect_Click;
            slot.BrowseBtn.MouseEnter += (s, e) => { ((Button)s).BackColor = Color.FromArgb(50, 110, 250); };
            slot.BrowseBtn.MouseLeave += (s, e) => { ((Button)s).BackColor = ColorAccent; };
            toolTip.SetToolTip(slot.BrowseBtn, "감시할 실행 파일(.exe)을 선택합니다.");

            slot.DeleteBtn = new Button
            {
                Text = "×",
                Location = new Point(448, 44),
                Width = 32,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorStatusStopped,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                Tag = i,
                Cursor = Cursors.Hand
            };
            slot.DeleteBtn.FlatAppearance.BorderSize = 0;
            slot.DeleteBtn.Click += BtnDelete_Click;
            slot.DeleteBtn.MouseEnter += (s, e) => { ((Button)s).BackColor = Color.FromArgb(200, 50, 50); };
            slot.DeleteBtn.MouseLeave += (s, e) => { ((Button)s).BackColor = ColorStatusStopped; };
            toolTip.SetToolTip(slot.DeleteBtn, "이 슬롯을 삭제합니다.");

            card.Controls.Add(slot.Led);
            card.Controls.Add(slot.SlotLabel);
            card.Controls.Add(slot.StatusText);
            card.Controls.Add(slot.PathBox);
            card.Controls.Add(slot.BrowseBtn);
            card.Controls.Add(slot.DeleteBtn);

            slot.Card = card;
            slots.Add(slot);
            flowSlots.Controls.Add(card);

            if (flowSlots.Controls.Count > 1) Log($"New monitor slot added (Total: {slots.Count})");
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            int index = (int)btn.Tag;
            if (index >= 0 && index < slots.Count)
            {
                var slot = slots[index];
                if (slot.Card != null) flowSlots.Controls.Remove(slot.Card);
                slots.RemoveAt(index);
                SaveSettings();
                Log($"Slot {index + 1} removed (Remaining: {slots.Count})");

                for (int i = 0; i < slots.Count; i++)
                {
                    slots[i].Index = i;
                    if (slots[i].SlotLabel != null) slots[i].SlotLabel.Text = $"{GetStr("Slot")} {i + 1}";
                    if (slots[i].BrowseBtn != null) slots[i].BrowseBtn.Tag = i;
                    if (slots[i].DeleteBtn != null) slots[i].DeleteBtn.Tag = i;
                }
            }
        }

        private void BtnSelect_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            int index = (int)btn.Tag;
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Executable files (*.exe)|*.exe";
                ofd.Title = "Select Application to Monitor";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    if (index < slots.Count) {
                        var slot = slots[index];
                        slot.Path = ofd.FileName;
                        slot.FailureCount = 0;
                        slot.IsBackingOff = false;
                        if (slot.PathBox != null) slot.PathBox.Text = ofd.FileName;
                        if (slot.StatusText != null) slot.StatusText.Text = GetStr("Loaded");
                        if (slot.Led != null) { slot.Led.Tag = "warning"; slot.Led.Invalidate(); }
                    }
                    SaveSettings();
                    Log($"New process registered to Slot {index + 1}: {Path.GetFileName(ofd.FileName)}");
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; this.Hide(); trayIcon.ShowBalloonTip(2000, "Background Mode", "Guardian is still protecting your processes.", ToolTipIcon.Info); }
            base.OnFormClosing(e);
        }

        private void StartMonitoring()
        {
            cts = new CancellationTokenSource();
            Task.Run(() => MonitorLoopAsync(cts.Token));
            Log("Monitoring loop started.", ColorStatusRunning);
        }

        private async Task MonitorLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    for (int i = 0; i < slots.Count; i++)
                    {
                        var slot = slots[i];
                        if (string.IsNullOrWhiteSpace(slot.Path)) continue;
                        if (slot.IsBackingOff && DateTime.Now < slot.NextCheckTime) continue;

                        bool isRunning = IsProcessRunning(slot.Path);

                        if (this.IsHandleCreated)
                        {
                            this.Invoke(new Action(() => {
                                if (isRunning)
                                {
                                    if (slot.Led != null) { slot.Led.Tag = "running"; slot.Led.Invalidate(); }
                                    if (slot.StatusText != null) { slot.StatusText.Text = GetStr("Running"); slot.StatusText.ForeColor = ColorStatusRunning; }
                                    slot.FailureCount = 0;
                                    slot.IsBackingOff = false;
                                    CheckProcessResources(slot);
                                }
                                else
                                {
                                    if (slot.Led != null) { slot.Led.Tag = "stopped"; slot.Led.Invalidate(); }
                                    if (slot.StatusText != null) { slot.StatusText.Text = GetStr("Restarting"); slot.StatusText.ForeColor = ColorStatusStopped; }
                                    AttemptRecovery(slot);
                                }
                            }));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Monitor Loop Error: {ex.Message}");
                }
                await Task.Delay(monitoringInterval, token);
            }
        }

        private bool IsProcessRunning(string targetPath)
        {
            try
            {
                string targetName = Path.GetFileNameWithoutExtension(targetPath);
                Process[] processes = Process.GetProcessesByName(targetName);
                foreach (var p in processes)
                {
                    try
                    {
                        if (string.Equals(p.MainModule?.FileName, targetPath, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                    catch { }
                }
            }
            catch { }
            return false;
        }

        private void AttemptRecovery(ProcessSlot slot)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(slot.PreScript)) RunScript(slot.PreScript);

                if (startupDelaySec > 0 && slot.FailureCount == 0)
                {
                    Log($"Waiting {startupDelaySec}s before starting {Path.GetFileName(slot.Path)}...");
                    Thread.Sleep(startupDelaySec * 1000);
                }

                ProcessStartInfo psi = new ProcessStartInfo(slot.Path);
                if (!string.IsNullOrWhiteSpace(slot.Args)) psi.Arguments = slot.Args;
                Process.Start(psi);

                if (!string.IsNullOrWhiteSpace(slot.PostScript)) RunScript(slot.PostScript);

                Log($"Recovered: {Path.GetFileName(slot.Path)}", ColorStatusRunning);
                LogToFile($"Recovered: {Path.GetFileName(slot.Path)}");
                trayIcon.ShowBalloonTip(1000, "Guardian Alert", $"{Path.GetFileName(slot.Path)} " + GetStr("Recovered"), ToolTipIcon.Warning);
                slot.FailureCount = 0;
            }
            catch (Exception ex)
            {
                slot.FailureCount++;
                Log($"Failed to restart {Path.GetFileName(slot.Path)}: {ex.Message}", ColorStatusStopped);
                LogToFile($"Failed to restart {Path.GetFileName(slot.Path)}: {ex.Message}");

                if (slot.FailureCount >= 3)
                {
                    slot.IsBackingOff = true;
                    slot.NextCheckTime = DateTime.Now.AddSeconds(monitoringInterval * 10 / 1000);
                    Log($"Threshold reached for {Path.GetFileName(slot.Path)}. Entering backoff mode.", ColorStatusWarning);
                    LogToFile($"Backoff: {Path.GetFileName(slot.Path)}");
                    if (slot.Led != null) { slot.Led.Tag = "warning"; slot.Led.Invalidate(); }
                    if (slot.StatusText != null) slot.StatusText.Text = "ERROR (LIMIT)";
                }
            }
        }

        private void CheckProcessResources(ProcessSlot slot)
        {
            try {
                string targetName = Path.GetFileNameWithoutExtension(slot.Path);
                Process[] p = Process.GetProcessesByName(targetName);
                foreach (var proc in p) {
                    if (string.Equals(proc.MainModule?.FileName, slot.Path, StringComparison.OrdinalIgnoreCase)) {
                        long memMB = proc.WorkingSet64 / 1024 / 1024;
                        slot.LastMemoryMB = memMB;
                        try { proc.Refresh(); slot.LastCpuPercent = proc.TotalProcessorTime.TotalMilliseconds; } catch { }

                        if (memMB > memoryThresholdMB) {
                            Log($"[Watchdog] Resource Alert: {Path.GetFileName(slot.Path)} memory usage is high ({memMB}MB).", ColorStatusWarning);
                            WriteToWindowsEventLog($"Memory warning: {Path.GetFileName(slot.Path)} using {memMB}MB");
                        }

                        if (slot.IsHangDetectionEnabled && useHangDetection) {
                            bool responding = CheckProcessResponding(proc);
                            slot.IsResponding = responding;
                            if (!responding) {
                                Log($"[Hang Detection] {Path.GetFileName(slot.Path)} is not responding!", ColorStatusStopped);
                                WriteToWindowsEventLog($"Process hang detected: {Path.GetFileName(slot.Path)}");
                                SendWebhookAlert($"⚠ Hang Detected: {Path.GetFileName(slot.Path)} is not responding");
                            }
                        }
                    }
                }
            } catch { }
        }

        private bool CheckProcessResponding(Process process)
        {
            try {
                if (process.HasExited) return false;
                if (process.MainWindowHandle != IntPtr.Zero)
                    return NativeMethods.SendMessageTimeout(process.MainWindowHandle, NativeMethods.WM_NULL, IntPtr.Zero, IntPtr.Zero, NativeMethods.SMTO_ABORTIFHUNG, (uint)hangTimeoutSec * 1000, out _);
                return true;
            } catch { return false; }
        }

        private void WriteToWindowsEventLog(string message)
        {
            if (!useWindowsEventLog) return;
            try {
                string source = "Process Guardian";
                if (!EventLog.SourceExists(source)) EventLog.CreateEventSource(source, "Application");
                EventLog.WriteEntry(source, message, EventLogEntryType.Warning);
            } catch { }
        }

        private async void SendWebhookAlert(string message)
        {
            if (string.IsNullOrEmpty(webhookUrl)) return;
            try {
                using var client = new HttpClient();
                var payload = new { text = message };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                await client.PostAsync(webhookUrl, content);
                Log($"[Webhook] Alert sent: {message}");
            } catch (Exception ex) {
                Log($"[Webhook] Failed: {ex.Message}", ColorStatusStopped);
            }
        }

        private void RunScript(string scriptPath)
        {
            try {
                if (string.IsNullOrWhiteSpace(scriptPath)) return;
                ProcessStartInfo psi = new ProcessStartInfo { FileName = scriptPath, UseShellExecute = false, RedirectStandardOutput = true };
                using var proc = Process.Start(psi);
                if (proc != null) { proc.StandardOutput.ReadToEnd(); proc.WaitForExit(10000); Log($"Script executed: {Path.GetFileName(scriptPath)}"); }
            } catch (Exception ex) {
                Log($"Script failed: {ex.Message}", ColorStatusStopped);
            }
        }

        private void LogToFile(string message)
        {
            if (string.IsNullOrEmpty(logFilePath)) return;
            try { File.AppendAllText(logFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}"); } catch { }
        }

        private void LoadSettings()
        {
            try {
                monitoringInterval = Properties.Settings.Default.MonitoringInterval * 1000;
                memoryThresholdMB = Properties.Settings.Default.MemoryThresholdMB;
                logFilePath = Properties.Settings.Default.LogFilePath;
                hangTimeoutSec = Properties.Settings.Default.HangTimeoutSec;
                startupDelaySec = Properties.Settings.Default.StartupDelaySec;

                string pathsJson = Properties.Settings.Default.Paths;

                if (!string.IsNullOrEmpty(pathsJson) && pathsJson != "[]")
                {
                    var slotsData = JsonSerializer.Deserialize<List<SlotData>>(pathsJson);
                    if (slotsData != null)
                        foreach (var sd in slotsData) AddNewSlot(sd.Path, sd.Args);
                }
                else
                {
                    string[] savedPaths = {
                        Properties.Settings.Default.Path1,
                        Properties.Settings.Default.Path2,
                        Properties.Settings.Default.Path3,
                        Properties.Settings.Default.Path4,
                        Properties.Settings.Default.Path5
                    };
                    for (int i = 0; i < 5; i++)
                        if (!string.IsNullOrEmpty(savedPaths[i])) AddNewSlot(savedPaths[i]);
                }
            } catch { }
        }

        private void SaveSettings()
        {
            try {
                var slotsData = new List<SlotData>();
                for (int i = 0; i < slots.Count; i++)
                    slotsData.Add(new SlotData { Path = slots[i].Path, Args = slots[i].Args });
                Properties.Settings.Default.Paths = JsonSerializer.Serialize(slotsData);
                Properties.Settings.Default.SlotArgs = "[]";
                Properties.Settings.Default.MonitoringInterval = monitoringInterval / 1000;
                Properties.Settings.Default.MemoryThresholdMB = memoryThresholdMB;
                Properties.Settings.Default.LogFilePath = logFilePath;
                Properties.Settings.Default.WebhookUrl = webhookUrl;
                Properties.Settings.Default.UseWindowsEventLog = useWindowsEventLog;
                Properties.Settings.Default.UseHangDetection = useHangDetection;
                Properties.Settings.Default.HangTimeoutSec = hangTimeoutSec;
                Properties.Settings.Default.StartupDelaySec = startupDelaySec;
                Properties.Settings.Default.Save();
            } catch { }
        }

        private void ExportSettings()
        {
            try {
                SaveFileDialog sfd = new SaveFileDialog { Filter = "JSON files (*.json)|*.json", FileName = "ProcessGuardian_Config.json" };
                if (sfd.ShowDialog() == DialogResult.OK) {
                    var config = new {
                        version = "1.6.1",
                        slots = slots.Select(s => new { path = s.Path, args = s.Args }).ToList(),
                        monitoringInterval = monitoringInterval / 1000,
                        memoryThresholdMB,
                        webhookUrl,
                        useWindowsEventLog,
                        useHangDetection
                    };
                    File.WriteAllText(sfd.FileName, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
                    Log($"Settings exported to {Path.GetFileName(sfd.FileName)}");
                }
            } catch (Exception ex) { Log($"Export failed: {ex.Message}", ColorStatusStopped); }
        }

        private void ImportSettings()
        {
            try {
                OpenFileDialog ofd = new OpenFileDialog { Filter = "JSON files (*.json)|*.json" };
                if (ofd.ShowDialog() == DialogResult.OK) {
                    string json = File.ReadAllText(ofd.FileName);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("slots", out var slotsElem)) {
                        foreach (var slot in slots) if (slot.Card != null) flowSlots.Controls.Remove(slot.Card);
                        slots.Clear();
                        foreach (var slotElem in slotsElem.EnumerateArray()) {
                            string p = slotElem.GetProperty("path").GetString() ?? "";
                            string a = slotElem.TryGetProperty("args", out var ap) ? ap.GetString() ?? "" : "";
                            AddNewSlot(p, a);
                        }
                    }
                    if (root.TryGetProperty("monitoringInterval", out var iv)) monitoringInterval = iv.GetInt32() * 1000;
                    if (root.TryGetProperty("memoryThresholdMB", out var mt)) memoryThresholdMB = mt.GetInt32();
                    if (root.TryGetProperty("webhookUrl", out var wh)) webhookUrl = wh.GetString() ?? "";
                    if (root.TryGetProperty("useWindowsEventLog", out var wl)) useWindowsEventLog = wl.GetBoolean();
                    if (root.TryGetProperty("useHangDetection", out var hd)) useHangDetection = hd.GetBoolean();

                    SaveSettings();
                    Log($"Settings imported from {Path.GetFileName(ofd.FileName)}");
                }
            } catch (Exception ex) { Log($"Import failed: {ex.Message}", ColorStatusStopped); }
        }

        private void Log(string message, Color? color = null)
        {
            if (logBox.InvokeRequired) { logBox.Invoke(new Action(() => Log(message, color))); return; }
            logBox.SelectionStart = logBox.TextLength;
            logBox.SelectionLength = 0;
            logBox.SelectionColor = color ?? ColorText;
            logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            logBox.ScrollToCaret();
        }

        private bool IsAutoStartEnabled()
        {
            try {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false))
                    if (key?.GetValue("ProcessGuardian") != null) return true;
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false))
                    if (key?.GetValue("ProcessGuardian") != null) return true;
            } catch { }
            return false;
        }

        private void SetAutoStart(bool enable)
        {
            try {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true)) {
                    if (enable) key?.SetValue("ProcessGuardian", $"\"{Application.ExecutablePath}\"");
                    else key?.DeleteValue("ProcessGuardian", false);
                }
                Log(enable ? "Auto-start enabled (Current User)." : "Auto-start disabled.");
            } catch (Exception ex) { Log($"Failed to set auto-start: {ex.Message}", ColorStatusStopped); }
        }

        private void SetSystemAutoStart(bool enable)
        {
            if (!isAdmin) { Log("Administrator privileges required for system-wide auto-start.", ColorStatusWarning); return; }
            try {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true)) {
                    if (enable) key?.SetValue("ProcessGuardian", $"\"{Application.ExecutablePath}\"");
                    else key?.DeleteValue("ProcessGuardian", false);
                }
                Log(enable ? "System-wide auto-start enabled." : "System-wide auto-start disabled.");
            } catch (Exception ex) { Log($"Failed to set system auto-start: {ex.Message}", ColorStatusStopped); }
        }

        private void ShowForm() { this.Show(); this.WindowState = FormWindowState.Normal; this.Activate(); }

        private void ExitApp()
        {
            Log("Graceful shutdown initiated...");
            foreach (var slot in slots)
            {
                if (!string.IsNullOrWhiteSpace(slot.Path))
                {
                    try {
                        string targetName = Path.GetFileNameWithoutExtension(slot.Path);
                        foreach (var p in Process.GetProcessesByName(targetName))
                        {
                            if (string.Equals(p.MainModule?.FileName, slot.Path, StringComparison.OrdinalIgnoreCase))
                            {
                                p.CloseMainWindow();
                                if (!p.WaitForExit(5000)) p.Kill();
                                Log($"Terminated: {targetName}");
                            }
                        }
                    } catch { }
                }
            }
            cts?.Cancel();
            trayIcon.Visible = false;
            Application.Exit();
        }

        private string GetStr(string key)
        {
            var storage = new Dictionary<string, string[]>()
            {
                ["Title"] = new[] { "Monitoring Dashboard", "모니터링 대시보드", "モニタリングダッシュボード", "监控仪表板" },
                ["Slot"] = new[] { "PROCESS SLOT", "프로세스 슬롯", "プロセススロット", "进程槽" },
                ["Browse"] = new[] { "Browse", "찾아보기", "参照", "浏览" },
                ["Running"] = new[] { "RUNNING", "실행 중", "実行中", "运行中" },
                ["Stopped"] = new[] { "STOPPED", "중지됨", "停止中", "已停止" },
                ["Restarting"] = new[] { "RESTARTING...", "재시작 중...", "再起動中...", "正在重启..." },
                ["Empty"] = new[] { "EMPTY", "비어 있음", "空", "空" },
                ["Recovered"] = new[] { "recovered successfully.", "성공적으로 복구되었습니다.", "正常に復旧されました.", "成功恢复。" },
                ["Open"] = new[] { "Open Dashboard", "대시보드 열기", "ダッシュボードを開く", "打开仪表板" },
                ["Exit"] = new[] { "Exit Guardian", "프로그램 종료", "終了", "退出" },
                ["Loaded"] = new[] { "LOADED", "로드됨", "ロード済み", "已加载" },
                ["Lang"] = new[] { "Language:", "언어:", "言語:", "语言:" },
                ["Interval"] = new[] { "Interval:", "감시 주기:", "間隔:", "间隔:" },
                ["Export"] = new[] { "Export Settings", "설정 내보내기", "設定エクスポート", "Export" },
                ["Import"] = new[] { "Import Settings", "설정 가져오기", "設定インポート", "Import" },
                ["Settings"] = new[] { "Settings", "설정", "設定", "Settings" },
                ["Profile"] = new[] { "Profile", "프로필", "プロファイル", "Profile" },
            };
            if (storage.ContainsKey(key)) return storage[key][currentLangIndex];
            return key;
        }

        private void ChangeLanguage(int index) { currentLangIndex = index; UpdateUITexts(); }

        private void UpdateUITexts()
        {
            this.Text = GetStr("Title");
            if (lblLang != null) lblLang.Text = GetStr("Lang");
            if (lblInterval != null) lblInterval.Text = GetStr("Interval");
            if (trayOpenItem != null) trayOpenItem.Text = GetStr("Open");
            if (trayExitItem != null) trayExitItem.Text = GetStr("Exit");
            if (btnAddSlot != null) btnAddSlot.Text = "+ 신규 슬롯";

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].SlotLabel != null) slots[i].SlotLabel.Text = $"{GetStr("Slot")} {i + 1}";
                if (slots[i].BrowseBtn != null) slots[i].BrowseBtn.Text = GetStr("Browse");
            }
        }

        private void Card_Paint(object sender, PaintEventArgs e)
        {
            Panel card = (Panel)sender;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            GraphicsPath path = GetRoundedRectanglePath(card.ClientRectangle, 14);
            card.Region = new Region(path);
            using (Pen pen = new Pen(Color.FromArgb(50, 50, 62), 1)) { e.Graphics.DrawPath(pen, path); }
        }

        private void StatusLed_Paint(object sender, PaintEventArgs e)
        {
            Label led = (Label)sender;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            string status = led.Tag?.ToString() ?? "idle";
            Color baseColor = status == "running" ? ColorStatusRunning
                            : status == "stopped" ? ColorStatusStopped
                            : status == "warning" ? ColorStatusWarning
                            : Color.FromArgb(60, 60, 70);

            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(led.ClientRectangle);
                using (PathGradientBrush pgb = new PathGradientBrush(path))
                {
                    pgb.CenterColor = Color.White;
                    pgb.SurroundColors = new Color[] { baseColor };
                    e.Graphics.FillEllipse(pgb, led.ClientRectangle);
                }
            }
            using (Pen pen = new Pen(Color.FromArgb(100, baseColor), 2))
            {
                Rectangle rect = led.ClientRectangle;
                rect.Inflate(1, 1);
                e.Graphics.DrawEllipse(pen, rect);
            }
        }

        private GraphicsPath GetRoundedRectanglePath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public class ProcessSlot
    {
        public int Index { get; set; }
        public string Path { get; set; } = "";
        public string Args { get; set; } = "";
        public string PreScript { get; set; } = "";
        public string PostScript { get; set; } = "";
        public int FailureCount { get; set; } = 0;
        public DateTime NextCheckTime { get; set; } = DateTime.MinValue;
        public bool IsBackingOff { get; set; } = false;
        public long LastMemoryMB { get; set; } = 0;
        public double LastCpuPercent { get; set; } = 0;
        public bool IsHangDetectionEnabled { get; set; } = false;
        public DateTime LastResponsivenessCheck { get; set; } = DateTime.MinValue;
        public bool IsResponding { get; set; } = true;
        public DateTime StartTime { get; set; } = DateTime.MinValue;

        public Panel? Card { get; set; }
        public Label? Led { get; set; }
        public Label? StatusText { get; set; }
        public TextBox? PathBox { get; set; }
        public Button? BrowseBtn { get; set; }
        public Button? DeleteBtn { get; set; }
        public Label? SlotLabel { get; set; }
    }

    public class SlotData
    {
        public string Path { get; set; } = "";
        public string Args { get; set; } = "";
    }

    internal class DarkModeRenderer : ToolStripProfessionalRenderer
    {
        public DarkModeRenderer() : base(new CustomColorTable()) { }
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e) { e.TextColor = Color.White; base.OnRenderItemText(e); }
    }

    internal class CustomColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Color.FromArgb(30, 30, 35);
        public override Color MenuBorder => Color.FromArgb(50, 50, 60);
        public override Color MenuItemSelected => Color.FromArgb(37, 99, 235);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(37, 99, 235);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(37, 99, 235);
        public override Color MenuItemBorder => Color.FromArgb(37, 99, 235);
    }

    internal static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);
        public const uint WM_NULL = 0x0000;
        public const uint SMTO_ABORTIFHUNG = 0x0002;
    }
}
