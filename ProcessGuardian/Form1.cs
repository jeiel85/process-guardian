using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
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
        private ToolStripMenuItem trayOpenItem;
        private ToolStripMenuItem trayExitItem;

        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        
        private CancellationTokenSource? cts;
        private RichTextBox logBox;
        private Label lblAdminWarn;
        private bool isAdmin = false;

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

        private void InitializeModernUI()
        {
            this.Text = "Process Guardian Professional";
            this.Size = new Size(620, 920); 
            this.BackColor = ColorBackground;
            this.ForeColor = ColorText;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            PictureBox logo = new PictureBox { Image = SystemIcons.Shield.ToBitmap(), Location = new Point(25, 22), Size = new Size(32, 32), SizeMode = PictureBoxSizeMode.StretchImage };
            this.Controls.Add(logo);

            Label header = new Label { Text = "Process Guardian Pro", Font = new Font("Segoe UI", 20F, FontStyle.Bold), Location = new Point(65, 18), AutoSize = true, ForeColor = ColorText };
            this.Controls.Add(header);

            if (!isAdmin)
            {
                lblAdminWarn = new Label { Text = "⚠ USER MODE", ForeColor = ColorStatusStopped, Font = new Font("Segoe UI", 8F, FontStyle.Bold), Location = new Point(420, 32), AutoSize = true };
                this.Controls.Add(lblAdminWarn);
            }

            btnAddSlot = new Button
            {
                Text = "+ Add Slot",
                Location = new Point(440, 20),
                Size = new Size(100, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 50, 55),
                ForeColor = ColorAccent,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnAddSlot.FlatAppearance.BorderSize = 1;
            btnAddSlot.FlatAppearance.BorderColor = ColorAccent;
            btnAddSlot.Click += (s, e) => AddNewSlot(); 
            this.Controls.Add(btnAddSlot);

            flowSlots = new FlowLayoutPanel
            {
                Location = new Point(25, 80),
                Size = new Size(530, 520),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent
            };
            this.Controls.Add(flowSlots);

            lblLang = new Label { Location = new Point(25, 605), AutoSize = true, ForeColor = Color.FromArgb(120, 120, 130), Font = new Font("Segoe UI", 8F) };
            ComboBox comboLang = new ComboBox { Location = new Point(100, 602), Width = 100, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = ColorCard, ForeColor = ColorText, FlatStyle = FlatStyle.Flat };
            comboLang.Items.AddRange(new string[] { "English", "한국어", "日本語", "简体中文" });
            comboLang.SelectedIndex = 0;
            comboLang.SelectedIndexChanged += (s, e) => ChangeLanguage(comboLang.SelectedIndex);

            lblInterval = new Label { Location = new Point(220, 605), AutoSize = true, ForeColor = Color.FromArgb(120, 120, 130), Font = new Font("Segoe UI", 8F) };
            NumericUpDown numInterval = new NumericUpDown { Location = new Point(310, 602), Width = 40, Minimum = 1, Maximum = 60, Value = 3, BackColor = ColorCard, ForeColor = ColorText, BorderStyle = BorderStyle.FixedSingle };
            numInterval.ValueChanged += (s, e) => { monitoringInterval = (int)numInterval.Value * 1000; };

            Label lblMemThreshold = new Label { Text = GetStr("MemThreshold"), Location = new Point(360, 605), AutoSize = true, ForeColor = Color.FromArgb(120, 120, 130), Font = new Font("Segoe UI", 8F) };
            NumericUpDown numMemThreshold = new NumericUpDown { Location = new Point(425, 602), Width = 40, Minimum = 512, Maximum = 16384, Value = memoryThresholdMB, BackColor = ColorCard, ForeColor = ColorText, BorderStyle = BorderStyle.FixedSingle };
            numMemThreshold.ValueChanged += (s, e) => { memoryThresholdMB = (int)numMemThreshold.Value; };

            this.Controls.Add(lblLang);
            this.Controls.Add(comboLang);
            this.Controls.Add(lblInterval);
            this.Controls.Add(numInterval);
            this.Controls.Add(lblMemThreshold);
            this.Controls.Add(numMemThreshold);

            // 추가 설정 컨트롤들
            CheckBox chkAutoStart = new CheckBox { Text = GetStr("AutoStart"), Location = new Point(25, 628), AutoSize = true, ForeColor = Color.FromArgb(120, 120, 130), Font = new Font("Segoe UI", 8F) };
            chkAutoStart.Checked = IsAutoStartEnabled();
            chkAutoStart.CheckedChanged += (s, e) => SetAutoStart(chkAutoStart.Checked);
            this.Controls.Add(chkAutoStart);

            CheckBox chkWinEventLog = new CheckBox { Text = GetStr("WinEventLog"), Location = new Point(120, 628), AutoSize = true, ForeColor = Color.FromArgb(120, 120, 130), Font = new Font("Segoe UI", 8F) };
            chkWinEventLog.Checked = useWindowsEventLog;
            chkWinEventLog.CheckedChanged += (s, e) => { useWindowsEventLog = chkWinEventLog.Checked; Properties.Settings.Default.UseWindowsEventLog = useWindowsEventLog; };
            this.Controls.Add(chkWinEventLog);

            CheckBox chkHangDetect = new CheckBox { Text = GetStr("HangDetect"), Location = new Point(230, 628), AutoSize = true, ForeColor = Color.FromArgb(120, 120, 130), Font = new Font("Segoe UI", 8F) };
            chkHangDetect.Checked = useHangDetection;
            chkHangDetect.CheckedChanged += (s, e) => { useHangDetection = chkHangDetect.Checked; Properties.Settings.Default.UseHangDetection = useHangDetection; };
            this.Controls.Add(chkHangDetect);

            // Hang Timeout 설정
            Label lblHangTimeout = new Label { Text = GetStr("HangTimeout"), Location = new Point(230, 652), AutoSize = true, ForeColor = Color.FromArgb(120, 120, 130), Font = new Font("Segoe UI", 8F) };
            NumericUpDown numHangTimeout = new NumericUpDown { Location = new Point(330, 650), Width = 40, Minimum = 5, Maximum = 300, Value = hangTimeoutSec, BackColor = ColorCard, ForeColor = ColorText, BorderStyle = BorderStyle.FixedSingle };
            numHangTimeout.ValueChanged += (s, e) => { hangTimeoutSec = (int)numHangTimeout.Value; };
            this.Controls.Add(lblHangTimeout);
            this.Controls.Add(numHangTimeout);

            // 시작 지연 설정
            Label lblStartupDelay = new Label { Text = GetStr("StartupDelay"), Location = new Point(380, 652), AutoSize = true, ForeColor = Color.FromArgb(120, 120, 130), Font = new Font("Segoe UI", 8F) };
            NumericUpDown numStartupDelay = new NumericUpDown { Location = new Point(470, 650), Width = 40, Minimum = 0, Maximum = 300, Value = startupDelaySec, BackColor = ColorCard, ForeColor = ColorText, BorderStyle = BorderStyle.FixedSingle };
            numStartupDelay.ValueChanged += (s, e) => { startupDelaySec = (int)numStartupDelay.Value; };
            this.Controls.Add(lblStartupDelay);
            this.Controls.Add(numStartupDelay);

            this.Size = new Size(620, 970);

            // Webhook URL 입력
            Label lblWebhook = new Label { Text = "Webhook:", Location = new Point(330, 628), AutoSize = true, ForeColor = Color.FromArgb(120, 120, 130), Font = new Font("Segoe UI", 8F) };
            TextBox txtWebhook = new TextBox { Text = webhookUrl, Location = new Point(390, 626), Width = 160, BackColor = ColorCard, ForeColor = ColorText, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 8F) };
            txtWebhook.Leave += (s, e) => { webhookUrl = txtWebhook.Text; Properties.Settings.Default.WebhookUrl = webhookUrl; };
            this.Controls.Add(lblWebhook);
            this.Controls.Add(txtWebhook);

            trayMenu = new ContextMenuStrip();
            trayMenu.Renderer = new DarkModeRenderer(); 
            trayOpenItem = new ToolStripMenuItem("Open Dashboard", null, (s, e) => ShowForm());
            trayExitItem = new ToolStripMenuItem("Exit Guardian", null, (s, e) => ExitApp());
            // 설정 메뉴
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

            trayIcon = new NotifyIcon();
            trayIcon.Text = "Process Guardian Pro";
            trayIcon.Icon = SystemIcons.Shield;
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += (s, e) => ShowForm();

            logBox = new RichTextBox
            {
                Location = new Point(25, 645),
                Size = new Size(515, 140),
                BackColor = Color.FromArgb(25, 25, 30),
                ForeColor = Color.FromArgb(200, 200, 210),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                Font = new Font("Consolas", 8.5F)
            };
            this.Controls.Add(logBox);
        }

        private void AddNewSlot(string path = "", string args = "")
        {
            int i = slots.Count;
            ProcessSlot slot = new ProcessSlot { Index = i, Path = path, Args = args };
            
            Panel card = new Panel { Size = new Size(500, 90), BackColor = ColorCard, Padding = new Padding(15), Margin = new Padding(0, 0, 0, 10) };
            card.Paint += Card_Paint;
            
            slot.Led = new Label { Location = new Point(18, 18), Size = new Size(14, 14), BackColor = Color.Transparent, Tag = "stopped" };
            slot.Led.Paint += StatusLed_Paint;
            
            slot.SlotLabel = new Label { Location = new Point(42, 16), Font = new Font("Segoe UI Semibold", 9F), ForeColor = Color.FromArgb(150, 150, 160), AutoSize = true, Text = $"{GetStr("Slot")} {i + 1}" };
            slot.StatusText = new Label { Text = "IDLE", Location = new Point(350, 16), TextAlign = ContentAlignment.TopRight, ForeColor = Color.FromArgb(100, 100, 110), Font = new Font("Segoe UI", 9F, FontStyle.Bold), Width = 100 };
            slot.PathBox = new TextBox { Text = path, Location = new Point(18, 48), Width = 340, BackColor = Color.FromArgb(20, 20, 25), ForeColor = ColorText, BorderStyle = BorderStyle.None, ReadOnly = true, Font = new Font("Segoe UI", 9F) };

            slot.BrowseBtn = new Button { Text = GetStr("Browse"), Location = new Point(365, 45), Width = 50, Height = 28, FlatStyle = FlatStyle.Flat, BackColor = ColorAccent, ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 8F), Tag = i };
            slot.BrowseBtn.FlatAppearance.BorderSize = 0;
            slot.BrowseBtn.Click += BtnSelect_Click;
            slot.BrowseBtn.MouseEnter += (s, e) => { ((Button)s).BackColor = Color.FromArgb(50, 110, 250); };
            slot.BrowseBtn.MouseLeave += (s, e) => { ((Button)s).BackColor = ColorAccent; };

            slot.DeleteBtn = new Button { Text = "×", Location = new Point(420, 45), Width = 30, Height = 28, FlatStyle = FlatStyle.Flat, BackColor = ColorStatusStopped, ForeColor = Color.White, Font = new Font("Segoe UI", 14F, FontStyle.Bold), Tag = i };
            slot.DeleteBtn.FlatAppearance.BorderSize = 0;
            slot.DeleteBtn.Click += BtnDelete_Click;
            slot.DeleteBtn.MouseEnter += (s, e) => { ((Button)s).BackColor = Color.FromArgb(200, 50, 50); };
            slot.DeleteBtn.MouseLeave += (s, e) => { ((Button)s).BackColor = ColorStatusStopped; };

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
                
                // Re-index remaining slots
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
                                    if (slot.StatusText != null) 
                                    {
                                        slot.StatusText.Text = GetStr("Running");
                                        slot.StatusText.ForeColor = ColorStatusRunning;
                                    }
                                    slot.FailureCount = 0;
                                    slot.IsBackingOff = false;
                                    CheckProcessResources(slot);
                                }
                                else
                                {
                                    if (slot.Led != null) { slot.Led.Tag = "stopped"; slot.Led.Invalidate(); }
                                    if (slot.StatusText != null) 
                                    {
                                        slot.StatusText.Text = GetStr("Restarting");
                                        slot.StatusText.ForeColor = ColorStatusStopped;
                                    }
                                    
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
                        {
                            return true;
                        }
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
                // 시작 전 스크립트 실행
                if (!string.IsNullOrWhiteSpace(slot.PreScript))
                {
                    RunScript(slot.PreScript);
                }

                // 시작 지연 적용
                if (startupDelaySec > 0 && slot.FailureCount == 0)
                {
                    Log($"Waiting {startupDelaySec}s before starting {Path.GetFileName(slot.Path)}...");
                    Thread.Sleep(startupDelaySec * 1000);
                }

                ProcessStartInfo psi = new ProcessStartInfo(slot.Path);
                if (!string.IsNullOrWhiteSpace(slot.Args))
                {
                    psi.Arguments = slot.Args;
                }
                Process.Start(psi);
                
                // 시작 후 스크립트 실행
                if (!string.IsNullOrWhiteSpace(slot.PostScript))
                {
                    RunScript(slot.PostScript);
                }

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
                    
                    if (slot.Led != null) {
                        slot.Led.Tag = "warning";
                        slot.Led.Invalidate();
                    }
                    if (slot.StatusText != null) {
                        slot.StatusText.Text = "ERROR (LIMIT)";
                    }
                }
            }
        }

        private void CheckProcessResources(ProcessSlot slot)
        {
            try {
                string targetName = Path.GetFileNameWithoutExtension(slot.Path);
                Process[] p = Process.GetProcessesByName(targetName);
                foreach(var proc in p) {
                    if (string.Equals(proc.MainModule?.FileName, slot.Path, StringComparison.OrdinalIgnoreCase)) {
                        long memMB = proc.WorkingSet64 / 1024 / 1024;
                        slot.LastMemoryMB = memMB;
                        
                        try {
                            proc.Refresh();
                            slot.LastCpuPercent = proc.TotalProcessorTime.TotalMilliseconds;
                        } catch { }

                        if (memMB > memoryThresholdMB) {
                             Log($"[Watchdog] Resource Alert: {Path.GetFileName(slot.Path)} memory usage is high ({memMB}MB).", ColorStatusWarning);
                             WriteToWindowsEventLog($"Memory warning: {Path.GetFileName(slot.Path)} using {memMB}MB");
                        }

                        // Hang Detection: UI 스레드가 응답하는지 확인
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
                // 메인 창이 응답하는지 확인 (WM_NULL 메시지 응답 확인)
                if (process.MainWindowHandle != IntPtr.Zero) {
                    return NativeMethods.SendMessageTimeout(process.MainWindowHandle, NativeMethods.WM_NULL, IntPtr.Zero, IntPtr.Zero, NativeMethods.SMTO_ABORTIFHUNG, (uint)hangTimeoutSec * 1000, out _);
                }
                return true; // 창이 없으면 응답으로 간주
            } catch { return false; }
        }

        private void WriteToWindowsEventLog(string message)
        {
            if (!useWindowsEventLog) return;
            try {
                string source = "Process Guardian";
                if (!EventLog.SourceExists(source)) {
                    EventLog.CreateEventSource(source, "Application");
                }
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
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = scriptPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(10000);
                    Log($"Script executed: {Path.GetFileName(scriptPath)}");
                }
            } catch (Exception ex) {
                Log($"Script failed: {ex.Message}", ColorStatusStopped);
            }
        }

        private void LogToFile(string message)
        {
            if (string.IsNullOrEmpty(logFilePath)) return;
            try {
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
            } catch { }
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
                string argsJson = Properties.Settings.Default.SlotArgs;

                if (!string.IsNullOrEmpty(pathsJson) && pathsJson != "[]")
                {
                    var slotsData = JsonSerializer.Deserialize<List<SlotData>>(pathsJson);
                    if (slotsData != null)
                    {
                        foreach (var sd in slotsData)
                        {
                            AddNewSlot(sd.Path, sd.Args);
                        }
                    }
                }
                else
                {
                    // Legacy: load from old Path1-5
                    string[] savedPaths = {
                        Properties.Settings.Default.Path1,
                        Properties.Settings.Default.Path2,
                        Properties.Settings.Default.Path3,
                        Properties.Settings.Default.Path4,
                        Properties.Settings.Default.Path5
                    };
                    for (int i = 0; i < 5; i++)
                    {
                        if (!string.IsNullOrEmpty(savedPaths[i]))
                            AddNewSlot(savedPaths[i]);
                    }
                }
            } catch { }
        }

        private void SaveSettings()
        {
            try {
                var slotsData = new List<SlotData>();
                for (int i = 0; i < slots.Count; i++)
                {
                    slotsData.Add(new SlotData { Path = slots[i].Path, Args = slots[i].Args });
                }
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
                        version = "1.4.0",
                        slots = slots.Select(s => new { path = s.Path, args = s.Args }).ToList(),
                        monitoringInterval = monitoringInterval / 1000,
                        memoryThresholdMB = memoryThresholdMB,
                        webhookUrl = webhookUrl,
                        useWindowsEventLog = useWindowsEventLog,
                        useHangDetection = useHangDetection
                    };
                    string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(sfd.FileName, json);
                    Log($"Settings exported to {Path.GetFileName(sfd.FileName)}");
                }
            } catch (Exception ex) {
                Log($"Export failed: {ex.Message}", ColorStatusStopped);
            }
        }

        private void ImportSettings()
        {
            try {
                OpenFileDialog ofd = new OpenFileDialog { Filter = "JSON files (*.json)|*.json" };
                if (ofd.ShowDialog() == DialogResult.OK) {
                    string json = File.ReadAllText(ofd.FileName);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    
                    // 슬롯 로드
                    if (root.TryGetProperty("slots", out var slotsElem)) {
                        foreach (var slot in slots) {
                            if (slot.Card != null) flowSlots.Controls.Remove(slot.Card);
                        }
                        slots.Clear();
                        
                        foreach (var slotElem in slotsElem.EnumerateArray()) {
                            string path = slotElem.GetProperty("path").GetString() ?? "";
                            string args = slotElem.TryGetProperty("args", out var argsProp) ? argsProp.GetString() ?? "" : "";
                            AddNewSlot(path, args);
                        }
                    }
                    
                    // 설정 로드
                    if (root.TryGetProperty("monitoringInterval", out var interval))
                        monitoringInterval = interval.GetInt32() * 1000;
                    if (root.TryGetProperty("memoryThresholdMB", out var memThreshold))
                        memoryThresholdMB = memThreshold.GetInt32();
                    if (root.TryGetProperty("webhookUrl", out var webhook))
                        webhookUrl = webhook.GetString() ?? "";
                    if (root.TryGetProperty("useWindowsEventLog", out var winLog))
                        useWindowsEventLog = winLog.GetBoolean();
                    if (root.TryGetProperty("useHangDetection", out var hangDetect))
                        useHangDetection = hangDetect.GetBoolean();
                    
                    SaveSettings();
                    Log($"Settings imported from {Path.GetFileName(ofd.FileName)}");
                }
            } catch (Exception ex) {
                Log($"Import failed: {ex.Message}", ColorStatusStopped);
            }
        }

        private void Log(string message, Color? color = null)
        {
            if (logBox.InvokeRequired)
            {
                logBox.Invoke(new Action(() => Log(message, color)));
                return;
            }
            logBox.SelectionStart = logBox.TextLength;
            logBox.SelectionLength = 0;
            logBox.SelectionColor = color ?? ColorText;
            logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            logBox.ScrollToCaret();
        }

        private bool IsAutoStartEnabled()
        {
            try {
                // HKCU (현재 사용자)
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false)) { 
                    if (key?.GetValue("ProcessGuardian") != null) return true; 
                }
                // HKLM (시스템 전체 - 관리자용)
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false)) {
                    if (key?.GetValue("ProcessGuardian") != null) return true;
                }
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

        /// <summary>
        /// 관리자 권한으로 시스템 전체 자동 시작 설정 (HKLM)
        /// </summary>
        private void SetSystemAutoStart(bool enable)
        {
            if (!isAdmin) {
                Log("Administrator privileges required for system-wide auto-start.", ColorStatusWarning);
                return;
            }
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
            // Graceful Shutdown: 감시 중인 모든 프로세스 정리
            Log("Graceful shutdown initiated...");
            foreach (var slot in slots)
            {
                if (!string.IsNullOrWhiteSpace(slot.Path))
                {
                    try
                    {
                        string targetName = Path.GetFileNameWithoutExtension(slot.Path);
                        Process[] procs = Process.GetProcessesByName(targetName);
                        foreach (var p in procs)
                        {
                            if (string.Equals(p.MainModule?.FileName, slot.Path, StringComparison.OrdinalIgnoreCase))
                            {
                                p.CloseMainWindow();
                                if (!p.WaitForExit(5000))
                                {
                                    p.Kill();
                                }
                                Log($"Terminated: {targetName}");
                            }
                        }
                    }
                    catch { }
                }
            }
            cts?.Cancel(); 
            trayIcon.Visible = false; 
            Application.Exit(); 
        }

        private int currentLangIndex = 0; 

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
                ["Recovered"] = new[] { "recovered successfully.", "성공적으로 복구되었습니다.", "正常에 復구되었습니다.", "成功恢复。" },
                ["Open"] = new[] { "Open Dashboard", "대시보드 열기", "ダッシュボードを開く", "打开仪表板" },
                ["Exit"] = new[] { "Exit Guardian", "프로그램 종료", "ガー디안 종료", "退出" },
                ["Loaded"] = new[] { "LOADED", "로드됨", "ロード済み", "已加载" },
                ["Lang"] = new[] { "Language:", "언어 설정:", "言語設定:", "语言设置:" },
                ["Interval"] = new[] { "Interval (sec):", "간격 (초):", "間隔 (秒):", "间隔 (秒):" },
                ["MemThreshold"] = new[] { "Memory (MB):", "메모리 (MB):", "メモリ (MB):", "Memory (MB):" },
                ["AutoStart"] = new[] { "Auto Start:", "자동 시작:", "自動開始:", "Auto Start:" },
                ["WinEventLog"] = new[] { "Event Log:", "이벤트 로그:", "イベントログ:", "Event Log:" },
                ["HangDetect"] = new[] { "Hang Detect:", "응답 감지:", "応答検出:", "Hang Detect:" },
                ["Export"] = new[] { "Export Settings", "설정 내보내기", "設定エクスポート", "Export" },
                ["Import"] = new[] { "Import Settings", "설정 가져오기", "設定インポート", "Import" },
                ["Settings"] = new[] { "Settings", "설정", "設定", "Settings" },
                ["Profile"] = new[] { "Profile", "프로필", "プロファイル", "Profile" },
                ["StartupDelay"] = new[] { "Startup Delay:", "시작 지연:", "起動遅延:", "Startup Delay:" },
                ["HangTimeout"] = new[] { "Hang Timeout:", "응답 시간:", "応答タイムアウト:", "Hang Timeout:" }
            };
            if (storage.ContainsKey(key)) return storage[key][currentLangIndex];
            return key;
        }

        private void ChangeLanguage(int index) { currentLangIndex = index; UpdateUITexts(); }
        
        private void UpdateUITexts() 
        { 
            this.Text = GetStr("Title"); 
            lblLang.Text = GetStr("Lang");
            lblInterval.Text = GetStr("Interval");
            trayOpenItem.Text = GetStr("Open");
            trayExitItem.Text = GetStr("Exit");
            if (btnAddSlot != null) btnAddSlot.Text = "+ " + GetStr("Slot");

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
            int radius = 15;
            GraphicsPath path = GetRoundedRectanglePath(card.ClientRectangle, radius);
            card.Region = new Region(path);
            using (Pen pen = new Pen(Color.FromArgb(50, 50, 60), 1)) { e.Graphics.DrawPath(pen, path); }
        }

        private void StatusLed_Paint(object sender, PaintEventArgs e)
        {
            Label led = (Label)sender;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color baseColor;
            string status = led.Tag?.ToString() ?? "idle";
            if (status == "running") baseColor = ColorStatusRunning;
            else if (status == "stopped") baseColor = ColorStatusStopped;
            else if (status == "warning") baseColor = ColorStatusWarning;
            else baseColor = Color.FromArgb(60, 60, 70);

            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(led.ClientRectangle);
                using (PathGradientBrush pgb = new PathGradientBrush(path)) { pgb.CenterColor = Color.White; pgb.SurroundColors = new Color[] { baseColor }; e.Graphics.FillEllipse(pgb, led.ClientRectangle); }
            }
            using (Pen pen = new Pen(Color.FromArgb(100, baseColor), 2)) { Rectangle rect = led.ClientRectangle; rect.Inflate(1, 1); e.Graphics.DrawEllipse(pen, rect); }
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
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);
        public const uint WM_NULL = 0x0000;
        public const uint SMTO_ABORTIFHUNG = 0x0002;
    }
}