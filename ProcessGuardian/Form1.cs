using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
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

        private List<ProcessSlot> slots = new();
        private FlowLayoutPanel? flowSlots;
        private Button? btnAddSlot;

        private Label? lblLang;
        private Label? lblInterval;
        private ToolStripMenuItem? trayOpenItem;
        private ToolStripMenuItem? trayExitItem;

        private NotifyIcon? trayIcon;
        private ContextMenuStrip? trayMenu;
        
        private CancellationTokenSource? cts;
        private RichTextBox? logBox;
        private Label? lblAdminWarn;
        private bool isAdmin = false;

        private int monitoringInterval = 3000;

        public Form1()
        {
            InitializeComponent();
            CheckAdminStatus();
            InitializeModernUI(); 
            SetupNotificationSettings();
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
            this.Size = new Size(580, 850); 
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
            NumericUpDown numInterval = new NumericUpDown { Location = new Point(310, 602), Width = 50, Minimum = 1, Maximum = 60, Value = 3, BackColor = ColorCard, ForeColor = ColorText, BorderStyle = BorderStyle.FixedSingle };
            numInterval.ValueChanged += (s, e) => { monitoringInterval = (int)numInterval.Value * 1000; };

            this.Controls.Add(lblLang);
            this.Controls.Add(comboLang);
            this.Controls.Add(lblInterval);
            this.Controls.Add(numInterval);

            CheckBox chkAutoStart = new CheckBox { Text = "Auto Start", Location = new Point(410, 602), AutoSize = true, ForeColor = Color.FromArgb(120, 120, 130), Font = new Font("Segoe UI", 8F) };
            chkAutoStart.Checked = IsAutoStartEnabled();
            chkAutoStart.CheckedChanged += (s, e) => SetAutoStart(chkAutoStart.Checked);
            this.Controls.Add(chkAutoStart);
            
            CheckBox chkSystemAutoStart = new CheckBox { Text = "System-wide (Admin)", Location = new Point(250, 602), AutoSize = true, ForeColor = Color.FromArgb(120, 120, 130), Font = new Font("Segoe UI", 8F) };
            chkSystemAutoStart.Checked = IsSystemAutoStartEnabled();
            chkSystemAutoStart.CheckedChanged += (s, e) => SetSystemAutoStart(chkSystemAutoStart.Checked);
            this.Controls.Add(chkSystemAutoStart);

            trayMenu = new ContextMenuStrip();
            trayMenu.Renderer = new DarkModeRenderer(); 
            trayOpenItem = new ToolStripMenuItem("Open Dashboard", null, (s, e) => ShowForm());
            trayExitItem = new ToolStripMenuItem("Exit Guardian", null, (s, e) => ExitApp());
            trayMenu.Items.Add(trayOpenItem);
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
                Size = new Size(480, 140),
                BackColor = Color.FromArgb(25, 25, 30),
                ForeColor = Color.FromArgb(200, 200, 210),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                Font = new Font("Consolas", 8.5F)
            };
            this.Controls.Add(logBox);
            
            Button btnExportLog = new Button
            {
                Text = "Export",
                Location = new Point(510, 645),
                Size = new Size(45, 25),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 50, 55),
                ForeColor = ColorText,
                Font = new Font("Segoe UI", 8F)
            };
            btnExportLog.FlatAppearance.BorderSize = 0;
            btnExportLog.Click += (s, e) => ExportLog();
            btnExportLog.MouseEnter += (s, e) => { if (s is Button b) b.BackColor = Color.FromArgb(70, 70, 75); };
            btnExportLog.MouseLeave += (s, e) => { if (s is Button b) b.BackColor = Color.FromArgb(50, 50, 55); };
            this.Controls.Add(btnExportLog);
            
            Button btnClearLog = new Button
            {
                Text = "Clear",
                Location = new Point(510, 675),
                Size = new Size(45, 25),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 50, 55),
                ForeColor = ColorText,
                Font = new Font("Segoe UI", 8F)
            };
            btnClearLog.FlatAppearance.BorderSize = 0;
            btnClearLog.Click += (s, e) => { if (logBox != null) logBox.Clear(); };
            btnClearLog.MouseEnter += (s, e) => { if (s is Button b) b.BackColor = Color.FromArgb(70, 70, 75); };
            btnClearLog.MouseLeave += (s, e) => { if (s is Button b) b.BackColor = Color.FromArgb(50, 50, 55); };
            this.Controls.Add(btnClearLog);
        }

        private void AddNewSlot(string path = "", string arguments = "", int memThreshold = 2048, int cpuThreshold = 80, int maxRestart = 3, string profileName = "")
        {
            int i = slots.Count;
            ProcessSlot slot = new ProcessSlot { 
                Index = i, 
                Path = path,
                Arguments = arguments,
                MemoryThresholdMB = memThreshold,
                CpuThresholdPercent = cpuThreshold,
                MaxRestartCount = maxRestart,
                ProfileName = profileName
            };
            
            Panel card = new Panel { Size = new Size(500, 145), BackColor = ColorCard, Padding = new Padding(15), Margin = new Padding(0, 0, 0, 10) };
            card.Paint += Card_Paint;
            
            slot.Led = new Label { Location = new Point(18, 18), Size = new Size(14, 14), BackColor = Color.Transparent, Tag = "stopped" };
            slot.Led.Paint += StatusLed_Paint;
            
            slot.SlotLabel = new Label { Location = new Point(42, 16), Font = new Font("Segoe UI Semibold", 9F), ForeColor = Color.FromArgb(150, 150, 160), AutoSize = true, Text = $"{GetStr("Slot")} {i + 1}" };
            slot.StatusText = new Label { Text = "IDLE", Location = new Point(350, 16), TextAlign = ContentAlignment.TopRight, ForeColor = Color.FromArgb(100, 100, 110), Font = new Font("Segoe UI", 9F, FontStyle.Bold), Width = 140 };
            slot.PathBox = new TextBox { Text = path, Location = new Point(18, 48), Width = 320, BackColor = Color.FromArgb(20, 20, 25), ForeColor = ColorText, BorderStyle = BorderStyle.None, ReadOnly = true, Font = new Font("Segoe UI", 9F) };

            // Arguments input field
            slot.ArgumentsBox = new TextBox { 
                Text = arguments, 
                Location = new Point(18, 75), 
                Width = 320, 
                BackColor = Color.FromArgb(20, 20, 25), 
                ForeColor = Color.FromArgb(150, 150, 160), 
                BorderStyle = BorderStyle.None, 
                Font = new Font("Segoe UI", 8F),
                Tag = i
            };
            slot.ArgumentsBox.Enter += (s, e) => { if (slot.ArgumentsBox.Text == GetStr("ArgsHint")) { slot.ArgumentsBox.Text = ""; slot.ArgumentsBox.ForeColor = ColorText; } };
            slot.ArgumentsBox.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(slot.ArgumentsBox.Text)) { slot.ArgumentsBox.Text = GetStr("ArgsHint"); slot.ArgumentsBox.ForeColor = Color.FromArgb(150, 150, 160); } };
            slot.ArgumentsBox.TextChanged += (s, e) => { if (slot.ArgumentsBox != null) slot.Arguments = slot.ArgumentsBox.Text != GetStr("ArgsHint") ? slot.ArgumentsBox.Text : ""; SaveSettingsDebounced(); };
            if (string.IsNullOrEmpty(arguments)) slot.ArgumentsBox.Text = GetStr("ArgsHint");

            // Resource usage labels (CPU/Memory)
            slot.CpuLabel = new Label { Text = "CPU: --", Location = new Point(350, 48), AutoSize = true, ForeColor = Color.FromArgb(100, 100, 110), Font = new Font("Consolas", 8F) };
            slot.MemoryLabel = new Label { Text = "MEM: --", Location = new Point(350, 68), AutoSize = true, ForeColor = Color.FromArgb(100, 100, 110), Font = new Font("Consolas", 8F) };
            
            // Pause toggle button
            Button btnPause = new Button { 
                Text = "⏸", 
                Location = new Point(350, 92), 
                Width = 28, 
                Height = 24, 
                FlatStyle = FlatStyle.Flat, 
                BackColor = Color.FromArgb(50, 50, 55), 
                ForeColor = ColorText, 
                Font = new Font("Segoe UI", 10F), 
                Tag = i
            };
            btnPause.FlatAppearance.BorderSize = 0;
            btnPause.Click += (s, e) => {
                slot.IsPaused = !slot.IsPaused;
                btnPause.Text = slot.IsPaused ? "▶" : "⏸";
                if (slot.Led != null) { 
                    slot.Led.Tag = slot.IsPaused ? "paused" : (IsProcessRunning(slot.Path) ? "running" : "stopped"); 
                    slot.Led.Invalidate(); 
                }
                if (slot.StatusText != null) {
                    slot.StatusText.Text = slot.IsPaused ? "PAUSED" : GetStr("Running");
                    slot.StatusText.ForeColor = slot.IsPaused ? Color.FromArgb(100, 100, 110) : ColorStatusRunning;
                }
                Log($"Slot {i + 1} {(slot.IsPaused ? "paused" : "resumed")}.");
            };
            btnPause.MouseEnter += (s, e) => { if (s is Button b) b.BackColor = Color.FromArgb(70, 70, 75); };
            btnPause.MouseLeave += (s, e) => { if (s is Button b) b.BackColor = Color.FromArgb(50, 50, 55); };

            slot.BrowseBtn = new Button { Text = GetStr("Browse"), Location = new Point(345, 45), Width = 70, Height = 24, FlatStyle = FlatStyle.Flat, BackColor = ColorAccent, ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 8F), Tag = i };
            slot.BrowseBtn.FlatAppearance.BorderSize = 0;
            slot.BrowseBtn.Click += BtnSelect_Click;
            slot.BrowseBtn.MouseEnter += (s, e) => { if (s is Button b) b.BackColor = Color.FromArgb(50, 110, 250); };
            slot.BrowseBtn.MouseLeave += (s, e) => { if (s is Button b) b.BackColor = ColorAccent; };

            // Settings button for thresholds
            Button btnSettings = new Button { Text = "⚙", Location = new Point(420, 45), Width = 28, Height = 24, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(50, 50, 55), ForeColor = Color.FromArgb(150, 150, 160), Font = new Font("Segoe UI", 10F), Tag = i };
            btnSettings.FlatAppearance.BorderSize = 0;
            btnSettings.Click += BtnSettings_Click;
            btnSettings.MouseEnter += (s, e) => { if (s is Button b) b.BackColor = Color.FromArgb(70, 70, 75); };
            btnSettings.MouseLeave += (s, e) => { if (s is Button b) b.BackColor = Color.FromArgb(50, 50, 55); };

            slot.DeleteBtn = new Button { Text = "✕", Location = new Point(452, 45), Width = 28, Height = 24, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(80, 30, 30), ForeColor = Color.FromArgb(239, 68, 68), Font = new Font("Segoe UI", 10F, FontStyle.Bold), Tag = i };
            slot.DeleteBtn.FlatAppearance.BorderSize = 0;
            slot.DeleteBtn.Click += BtnDelete_Click;
            slot.DeleteBtn.MouseEnter += (s, e) => { if (s is Button b) b.BackColor = Color.FromArgb(120, 40, 40); };
            slot.DeleteBtn.MouseLeave += (s, e) => { if (s is Button b) b.BackColor = Color.FromArgb(80, 30, 30); };

            card.Controls.Add(slot.Led);
            card.Controls.Add(slot.SlotLabel);
            card.Controls.Add(slot.StatusText);
            card.Controls.Add(slot.PathBox);
            card.Controls.Add(slot.ArgumentsBox);
            card.Controls.Add(slot.CpuLabel);
            card.Controls.Add(slot.MemoryLabel);
            card.Controls.Add(btnPause);
            card.Controls.Add(slot.BrowseBtn);
            card.Controls.Add(btnSettings);
            card.Controls.Add(slot.DeleteBtn);
            
            slot.Card = card;
            slots.Add(slot);
            flowSlots?.Controls.Add(card);
            
            if ((flowSlots?.Controls.Count ?? 0) > 1) Log($"New monitor slot added (Total: {slots.Count})");
        }

        private void BtnSettings_Click(object? sender, EventArgs e)
        {
            if (sender is not Button btn || btn.Tag is null) return;
            int index = (int)btn.Tag;
            if (index >= slots.Count) return;
            
            var slot = slots[index];
            using (var form = new Form { Text = $"Slot {index + 1} Settings", Width = 350, Height = 250, StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, BackColor = ColorBackground, ForeColor = ColorText })
            {
                var lblMem = new Label { Text = "Memory Threshold (MB):", Location = new Point(20, 20), AutoSize = true, ForeColor = ColorText };
                var numMem = new NumericUpDown { Location = new Point(20, 45), Width = 100, Minimum = 100, Maximum = 10000, Value = slot.MemoryThresholdMB, BackColor = ColorCard, ForeColor = ColorText };
                var lblCpu = new Label { Text = "CPU Threshold (%):", Location = new Point(20, 80), AutoSize = true, ForeColor = ColorText };
                var numCpu = new NumericUpDown { Location = new Point(20, 105), Width = 100, Minimum = 10, Maximum = 100, Value = slot.CpuThresholdPercent, BackColor = ColorCard, ForeColor = ColorText };
                var lblRestart = new Label { Text = "Max Restart Count:", Location = new Point(20, 140), AutoSize = true, ForeColor = ColorText };
                var numRestart = new NumericUpDown { Location = new Point(20, 165), Width = 100, Minimum = 1, Maximum = 10, Value = slot.MaxRestartCount, BackColor = ColorCard, ForeColor = ColorText };
                var btnSave = new Button { Text = "Save", Location = new Point(130, 200), Width = 80, FlatStyle = FlatStyle.Flat, BackColor = ColorAccent, ForeColor = Color.White };
                var btnCancel = new Button { Text = "Cancel", Location = new Point(220, 200), Width = 80, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(50, 50, 55), ForeColor = ColorText };
                
                btnSave.Click += (s, ev) => {
                    slot.MemoryThresholdMB = (int)numMem.Value;
                    slot.CpuThresholdPercent = (int)numCpu.Value;
                    slot.MaxRestartCount = (int)numRestart.Value;
                    SaveSettings();
                    form.Close();
                };
                btnCancel.Click += (s, ev) => form.Close();
                
                form.Controls.AddRange(new Control[] { lblMem, numMem, lblCpu, numCpu, lblRestart, numRestart, btnSave, btnCancel });
                form.ShowDialog();
            }
        }

        private void BtnSelect_Click(object? sender, EventArgs e)
        {
            if (sender is not Button btn || btn.Tag is null) return;
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

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (sender is not Button btn || btn.Tag is null) return;
            int index = (int)btn.Tag;
            if (index < slots.Count)
            {
                var slot = slots[index];
                string removedName = !string.IsNullOrEmpty(slot.Path) ? Path.GetFileName(slot.Path) : "empty slot";
                
                // Remove from UI
                if (slot.Card != null) flowSlots?.Controls.Remove(slot.Card);
                if (slot.Led != null) slot.Led.Dispose();
                if (slot.StatusText != null) slot.StatusText.Dispose();
                if (slot.PathBox != null) slot.PathBox.Dispose();
                if (slot.BrowseBtn != null) slot.BrowseBtn.Dispose();
                if (slot.DeleteBtn != null) slot.DeleteBtn.Dispose();
                if (slot.SlotLabel != null) slot.SlotLabel.Dispose();
                if (slot.ArgumentsBox != null) slot.ArgumentsBox.Dispose();
                if (slot.CpuLabel != null) slot.CpuLabel.Dispose();
                if (slot.MemoryLabel != null) slot.MemoryLabel.Dispose();
                
                slots.RemoveAt(index);
                
                // Re-index remaining slots
                for (int i = index; i < slots.Count; i++)
                {
                    slots[i].Index = i;
                    if (slots[i].BrowseBtn != null) slots[i].BrowseBtn!.Tag = i;
                    if (slots[i].DeleteBtn != null) slots[i].DeleteBtn!.Tag = i;
                    if (slots[i].ArgumentsBox != null) slots[i].ArgumentsBox!.Tag = i;
                }
                
                SaveSettings();
                Log($"Slot removed ({removedName}). Remaining: {slots.Count}");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; this.Hide(); trayIcon?.ShowBalloonTip(2000, "Background Mode", "Guardian is still protecting your processes.", ToolTipIcon.Info); }
            base.OnFormClosing(e);
        }

        private string GetSettingsPath() => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "slots.json");

        private void LoadSettings()
        {
            try {
                string path = GetSettingsPath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var data = JsonSerializer.Deserialize<SlotData[]>(json);
                    if (data != null && data.Length > 0)
                    {
                        // Load notification settings from first slot (global setting)
                        var firstItem = data[0];
                        if (!string.IsNullOrEmpty(firstItem.WebhookUrl)) webhookUrl = firstItem.WebhookUrl;
                        notificationsEnabled = firstItem.NotificationsEnabled;
                        
                        // Load slots
                        foreach (var item in data)
                        {
                            AddNewSlot(item.Path ?? "", item.Arguments ?? "", item.MemoryThresholdMB > 0 ? item.MemoryThresholdMB : 2048, item.CpuThresholdPercent > 0 ? item.CpuThresholdPercent : 80, item.MaxRestartCount > 0 ? item.MaxRestartCount : 3, item.ProfileName ?? "");
                        }
                    }
                }
            } catch { }
            // Ensure at least 1 slot exists
            if (slots.Count == 0) AddNewSlot("");
        }

        private async void SaveSettings()
        {
            try {
                var data = slots.Select(s => new SlotData { 
                    Path = s.Path, 
                    Arguments = s.Arguments,
                    MemoryThresholdMB = s.MemoryThresholdMB,
                    CpuThresholdPercent = s.CpuThresholdPercent,
                    MaxRestartCount = s.MaxRestartCount,
                    WebhookUrl = webhookUrl,
                    NotificationsEnabled = notificationsEnabled,
                    ProfileName = s.ProfileName
                }).ToArray();
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(GetSettingsPath(), json);
            } catch { }
        }
        
        private System.Timers.Timer? debounceTimer;
        private void SaveSettingsDebounced()
        {
            debounceTimer?.Stop();
            debounceTimer?.Dispose();
            debounceTimer = new System.Timers.Timer(1000);
            debounceTimer.Elapsed += (s, e) => { debounceTimer?.Stop(); SaveSettings(); };
            debounceTimer.Start();
        }

        private class SlotData
        {
            public string? Path { get; set; }
            public string? Arguments { get; set; }
            public int MemoryThresholdMB { get; set; } = 2048;
            public int CpuThresholdPercent { get; set; } = 80;
            public int MaxRestartCount { get; set; } = 3;
            public string? WebhookUrl { get; set; }
            public bool NotificationsEnabled { get; set; } = false;
            public string? ProfileName { get; set; }
        }
        
        // Notification Settings
        private string? webhookUrl = null;
        private bool notificationsEnabled = false;
        
        private async Task SendWebhookNotification(string title, string message)
        {
            if (string.IsNullOrEmpty(webhookUrl)) return;
            try {
                using var client = new HttpClient();
                var payload = new { content = $"**{title}**\n{message}" };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                await client.PostAsync(webhookUrl, content);
            } catch { }
        }
        
        private void WriteToEventLog(string message, EventLogEntryType type = EventLogEntryType.Information)
        {
            try {
                string logName = "ProcessGuardian";
                if (!EventLog.SourceExists(logName))
                {
                    EventLog.CreateEventSource(logName, logName);
                }
                EventLog.WriteEntry(logName, message, type);
            } catch { }
        }
        
        private void SetupNotificationSettings()
        {
            MenuStrip mainMenu = new MenuStrip();
            mainMenu.BackColor = ColorBackground;
            mainMenu.ForeColor = ColorText;
            
            ToolStripMenuItem settingsItem = new ToolStripMenuItem("Settings");
            settingsItem.DropDownItems.Add("Notification Settings...", null, (s, e) => ShowNotificationSettings());
            settingsItem.DropDownItems.Add("Check for Updates...", null, (s, e) => CheckForUpdates());
            settingsItem.DropDownItems.Add(new ToolStripSeparator());
            settingsItem.DropDownItems.Add("Export Settings...", null, (s, e) => ExportSettings());
            settingsItem.DropDownItems.Add("Import Settings...", null, (s, e) => ImportSettings());
            settingsItem.DropDownItems.Add(new ToolStripSeparator());
            settingsItem.DropDownItems.Add("About...", null, (s, e) => ShowAbout());
            
            mainMenu.Items.Add(settingsItem);
            this.MainMenuStrip = mainMenu;
            this.Controls.Add(mainMenu);
        }
        
        private void ShowAbout()
        {
            MessageBox.Show("Process Guardian Professional\nv1.3.2\n\nA powerful process monitoring and auto-restart tool for Windows.\n\n© 2026", "About Process Guardian", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        private async void CheckForUpdates()
        {
            try {
                Log("Checking for updates...");
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                var response = await client.GetStringAsync("https://api.github.com/repos/jeiel85/process-guardian/releases/latest");
                var release = JsonSerializer.Deserialize<GitHubRelease>(response);
                if (release != null) {
                    var currentVersion = new Version("1.3.0");
                    var latestVersion = new Version(release.TagName?.TrimStart('v') ?? "1.3.0");
                    if (latestVersion > currentVersion) {
                        Log($"New version available: {release.TagName}");
                        MessageBox.Show($"A new version ({release.TagName}) is available!\n\n{release.Body}", "Update Available", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    } else {
                        Log("You are using the latest version.");
                        MessageBox.Show("You are using the latest version.", "No Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            } catch (Exception ex) {
                Log($"Failed to check for updates: {ex.Message}", ColorStatusWarning);
                MessageBox.Show("Failed to check for updates. Please try again later.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        
        private class GitHubRelease
        {
            public string? TagName { get; set; }
            public string? Body { get; set; }
        }
        
        private void ExportSettings()
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "JSON files (*.json)|*.json";
                sfd.FileName = $"ProcessGuardian_config_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try {
                        var data = slots.Select(s => new SlotData { 
                            Path = s.Path, 
                            Arguments = s.Arguments,
                            MemoryThresholdMB = s.MemoryThresholdMB,
                            CpuThresholdPercent = s.CpuThresholdPercent,
                            MaxRestartCount = s.MaxRestartCount,
                            WebhookUrl = webhookUrl,
                            NotificationsEnabled = notificationsEnabled
                        }).ToArray();
                        string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(sfd.FileName, json);
                        Log("Settings exported successfully.");
                    } catch (Exception ex) {
                        Log($"Failed to export settings: {ex.Message}", ColorStatusStopped);
                    }
                }
            }
        }
        
        private void ImportSettings()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "JSON files (*.json)|*.json";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try {
                        string json = File.ReadAllText(ofd.FileName);
                        var data = JsonSerializer.Deserialize<SlotData[]>(json);
                        if (data != null && data.Length > 0)
                        {
                            // Clear existing slots
                            while (slots.Count > 0)
                            {
                                var slot = slots[0];
                                if (slot.Card != null) flowSlots?.Controls.Remove(slot.Card);
                                slots.RemoveAt(0);
                            }
                            
                            // Import notification settings
                            var firstItem = data[0];
                            if (!string.IsNullOrEmpty(firstItem.WebhookUrl)) webhookUrl = firstItem.WebhookUrl;
                            notificationsEnabled = firstItem.NotificationsEnabled;
                            
                            // Import slots
                            foreach (var item in data)
                            {
                                AddNewSlot(item.Path ?? "", item.Arguments ?? "", item.MemoryThresholdMB > 0 ? item.MemoryThresholdMB : 2048, item.CpuThresholdPercent > 0 ? item.CpuThresholdPercent : 80, item.MaxRestartCount > 0 ? item.MaxRestartCount : 3);
                            }
                            
                            SaveSettings();
                            Log($"Settings imported: {data.Length} slot(s) loaded.");
                        }
                    } catch (Exception ex) {
                        Log($"Failed to import settings: {ex.Message}", ColorStatusStopped);
                    }
                }
            }
        }
        
        private void ShowNotificationSettings()
        {
            using (var form = new Form { Text = "Notification Settings", Width = 450, Height = 200, StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, BackColor = ColorBackground, ForeColor = ColorText })
            {
                var lblWebhook = new Label { Text = "Webhook URL (Discord/Slack):", Location = new Point(20, 20), AutoSize = true, ForeColor = ColorText };
                var txtWebhook = new TextBox { Location = new Point(20, 45), Width = 400, BackColor = ColorCard, ForeColor = ColorText, Text = webhookUrl ?? "" };
                var chkEnabled = new CheckBox { Text = "Enable webhook notifications", Location = new Point(20, 80), AutoSize = true, ForeColor = ColorText, Checked = notificationsEnabled };
                var btnSave = new Button { Text = "Save", Location = new Point(170, 120), Width = 80, FlatStyle = FlatStyle.Flat, BackColor = ColorAccent, ForeColor = Color.White };
                var btnCancel = new Button { Text = "Cancel", Location = new Point(260, 120), Width = 80, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(50, 50, 55), ForeColor = ColorText };
                
                btnSave.Click += (s, ev) => {
                    webhookUrl = string.IsNullOrWhiteSpace(txtWebhook.Text) ? null : txtWebhook.Text;
                    notificationsEnabled = chkEnabled.Checked;
                    SaveSettings();
                    form.Close();
                };
                btnCancel.Click += (s, ev) => form.Close();
                
                form.Controls.AddRange(new Control[] { lblWebhook, txtWebhook, chkEnabled, btnSave, btnCancel });
                form.ShowDialog();
            }
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
                        if (slot.IsPaused) continue;

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
                var psi = new ProcessStartInfo();
                psi.FileName = slot.Path;
                if (!string.IsNullOrWhiteSpace(slot.Arguments) && slot.Arguments != GetStr("ArgsHint"))
                {
                    psi.Arguments = slot.Arguments;
                }
                Process.Start(psi);
                Log($"Recovered: {Path.GetFileName(slot.Path)}", ColorStatusRunning);
                WriteToEventLog($"Process recovered: {Path.GetFileName(slot.Path)}", EventLogEntryType.SuccessAudit);
                trayIcon?.ShowBalloonTip(1000, "Guardian Alert", $"{Path.GetFileName(slot.Path)} " + GetStr("Recovered"), ToolTipIcon.Warning);
                if (notificationsEnabled && !string.IsNullOrEmpty(webhookUrl))
                {
                    _ = SendWebhookNotification("Process Recovered", $"{Path.GetFileName(slot.Path)} has been restarted successfully.");
                }
                slot.FailureCount = 0;
            }
            catch (Exception ex)
            {
                slot.FailureCount++;
                Log($"Failed to restart {Path.GetFileName(slot.Path)}: {ex.Message}", ColorStatusStopped);
                
                if (slot.FailureCount >= slot.MaxRestartCount)
                {
                    slot.IsBackingOff = true;
                    slot.NextCheckTime = DateTime.Now.AddSeconds(monitoringInterval * 10 / 1000); 
                    Log($"Restart limit ({slot.MaxRestartCount}) reached for {Path.GetFileName(slot.Path)}. Entering backoff mode.", ColorStatusWarning);
                    if (notificationsEnabled && !string.IsNullOrEmpty(webhookUrl))
                    {
                        _ = SendWebhookNotification("Process Error", $"{Path.GetFileName(slot.Path)} has failed to restart {slot.MaxRestartCount} times. Entering backoff mode.");
                    }
                    
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
                        double cpuPercent = 0;
                        try { cpuPercent = proc.TotalProcessorTime.TotalMilliseconds / (Environment.ProcessorCount * (DateTime.Now - proc.StartTime).TotalMilliseconds) * 100; }
                        catch { }
                        
                        // Update labels
                        if (slot.CpuLabel != null) slot.CpuLabel.Text = $"CPU: {cpuPercent:F1}%";
                        if (slot.MemoryLabel != null) slot.MemoryLabel.Text = $"MEM: {memMB}MB";
                        
                        // Check thresholds
                        if (memMB > slot.MemoryThresholdMB) {
                            Log($"[Watchdog] Memory Alert: {Path.GetFileName(slot.Path)} using {memMB}MB (limit: {slot.MemoryThresholdMB}MB)", ColorStatusWarning);
                        }
                        if (cpuPercent > slot.CpuThresholdPercent) {
                            Log($"[Watchdog] CPU Alert: {Path.GetFileName(slot.Path)} at {cpuPercent:F1}% (limit: {slot.CpuThresholdPercent}%)", ColorStatusWarning);
                        }
                        
                        // Check if process is hanging (not responding)
                        if (proc.MainWindowHandle != IntPtr.Zero) {
                            bool isResponding = true;
                            try { isResponding = proc.Responding; }
                            catch { isResponding = true; }
                            
                            if (!isResponding) {
                                Log($"[Watchdog] HANG DETECTED: {Path.GetFileName(slot.Path)} is not responding!", ColorStatusStopped);
                                // Optionally could restart the hung process
                            }
                        }
                    }
                }
            } catch { }
        }

        private string GetLogFilePath()
        {
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ProcessGuardian", "logs");
            if (!Directory.Exists(appDataPath)) Directory.CreateDirectory(appDataPath);
            return Path.Combine(appDataPath, DateTime.Now.ToString("yyyy-MM-dd") + ".log");
        }

        private async Task WriteLogToFileAsync(string message)
        {
            try {
                string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                await File.AppendAllTextAsync(GetLogFilePath(), logLine + Environment.NewLine);
            } catch { }
        }
        
        private void ExportLog()
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Log files (*.log)|*.log|Text files (*.txt)|*.txt|All files (*.*)|*.*";
                sfd.FileName = $"ProcessGuardian_log_{DateTime.Now:yyyyMMdd_HHmmss}.log";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try {
                        File.WriteAllText(sfd.FileName, logBox?.Text ?? "");
                        Log("Log exported successfully.");
                    } catch (Exception ex) {
                        Log($"Failed to export log: {ex.Message}", ColorStatusStopped);
                    }
                }
            }
        }

        private void Log(string message, Color? color = null)
        {
            if (logBox?.InvokeRequired == true)
            {
                logBox.Invoke(new Action(() => Log(message, color)));
                return;
            }
            if (logBox == null) return;
            logBox.SelectionStart = logBox.TextLength;
            logBox.SelectionLength = 0;
            logBox.SelectionColor = color ?? ColorText;
            logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            logBox.ScrollToCaret();
            // Also write to file
            _ = WriteLogToFileAsync(message);
        }

        private bool IsAutoStartEnabled()
        {
            try {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false)) { return key?.GetValue("ProcessGuardian") != null; }
            } catch { return false; }
        }

        private void SetAutoStart(bool enable)
        {
            try {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true)) {
                    if (enable) key?.SetValue("ProcessGuardian", $"\"{Application.ExecutablePath}\"");
                    else key?.DeleteValue("ProcessGuardian", false);
                }
                Log(enable ? "Auto-start enabled (User)." : "Auto-start disabled (User).");
            } catch (Exception ex) { Log($"Failed to set auto-start: {ex.Message}", ColorStatusStopped); }
        }
        
        private bool IsSystemAutoStartEnabled()
        {
            try {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false)) { return key?.GetValue("ProcessGuardian") != null; }
            } catch { return false; }
        }
        
        private void SetSystemAutoStart(bool enable)
        {
            try {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true)) {
                    if (enable) key?.SetValue("ProcessGuardian", $"\"{Application.ExecutablePath}\"");
                    else key?.DeleteValue("ProcessGuardian", false);
                }
                Log(enable ? "Auto-start enabled (System-wide)." : "Auto-start disabled (System-wide).");
            } catch (Exception ex) { Log($"Failed to set system auto-start: {ex.Message}", ColorStatusStopped); }
        }

        private void ShowForm() { this.Show(); this.WindowState = FormWindowState.Normal; this.Activate(); }
        private void ExitApp() { cts?.Cancel(); if (trayIcon != null) { trayIcon.Visible = false; } Application.Exit(); }

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
                ["ArgsHint"] = new[] { "Command line arguments (optional)", "시작 인수 (선택)", "起動引数 (任意)", "启动参数 (可选)" }
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
            if (btnAddSlot != null) btnAddSlot.Text = "+ " + GetStr("Slot");

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].SlotLabel != null) slots[i].SlotLabel!.Text = $"{GetStr("Slot")} {i + 1}";
                if (slots[i].BrowseBtn != null) slots[i].BrowseBtn!.Text = GetStr("Browse");
            }
        }

        private void Card_Paint(object? sender, PaintEventArgs e)
        {
            if (sender is not Panel card) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            int radius = 15;
            GraphicsPath path = GetRoundedRectanglePath(card.ClientRectangle, radius);
            card.Region = new Region(path);
            using (Pen pen = new Pen(Color.FromArgb(50, 50, 60), 1)) { e.Graphics.DrawPath(pen, path); }
        }

        private void StatusLed_Paint(object? sender, PaintEventArgs e)
        {
            if (sender is not Label led) return;
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
        public string Arguments { get; set; } = "";
        public int MemoryThresholdMB { get; set; } = 2048;
        public int CpuThresholdPercent { get; set; } = 80;
        public int MaxRestartCount { get; set; } = 3;
        public int FailureCount { get; set; } = 0;
        public DateTime NextCheckTime { get; set; } = DateTime.MinValue;
        public bool IsBackingOff { get; set; } = false;
        public bool IsPaused { get; set; } = false;
        public string ProfileName { get; set; } = "";  // For group/profile management

        public Panel? Card { get; set; }
        public Label? Led { get; set; }
        public Label? StatusText { get; set; }
        public TextBox? PathBox { get; set; }
        public Button? BrowseBtn { get; set; }
        public Button? DeleteBtn { get; set; }
        public Label? SlotLabel { get; set; }
        public TextBox? ArgumentsBox { get; set; }
        public Label? CpuLabel { get; set; }
        public Label? MemoryLabel { get; set; }
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
}