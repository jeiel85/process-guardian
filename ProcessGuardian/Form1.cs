using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;

namespace ProcessGuardian
{
    public partial class Form1 : Form
    {
        // 현대적인 상용 디자인 컬러 테마 (Mockup 기반 업그레이드)
        private static readonly Color ColorBackground = Color.FromArgb(18, 18, 20); // 더 깊은 다크 톤
        private static readonly Color ColorCard = Color.FromArgb(30, 30, 35);       // 세련된 그래파이트
        private static readonly Color ColorAccent = Color.FromArgb(37, 99, 235);    // 프리미엄 블루
        private static readonly Color ColorText = Color.FromArgb(248, 250, 252);
        private static readonly Color ColorStatusRunning = Color.FromArgb(34, 197, 94); // 선명한 그린
        private static readonly Color ColorStatusStopped = Color.FromArgb(239, 68, 68);  // 선명한 레드
        private static readonly Color ColorStatusWarning = Color.FromArgb(245, 158, 11); // 오렌지

        private Panel[] slotCards;
        private TextBox[] pathBoxes;
        private Label[] statusLeds;
        private Label[] statusTexts;
        private Button[] browseButtons;

        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private System.Windows.Forms.Timer monitorTimer;

        public Form1()
        {
            InitializeComponent();
            InitializeModernUI(); 
            LoadSettings();       
            StartMonitoring();    
        }

        private void InitializeModernUI()
        {
            this.Text = "Process Guardian Professional";
            this.Size = new Size(580, 680); 
            this.BackColor = ColorBackground;
            this.ForeColor = ColorText;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            PictureBox logo = new PictureBox { Image = SystemIcons.Shield.ToBitmap(), Location = new Point(25, 22), Size = new Size(32, 32), SizeMode = PictureBoxSizeMode.StretchImage };
            this.Controls.Add(logo);

            Label header = new Label { Text = "Process Guardian Pro", Font = new Font("Segoe UI", 20F, FontStyle.Bold), Location = new Point(65, 18), AutoSize = true, ForeColor = ColorText };
            this.Controls.Add(header);

            slotCards = new Panel[5];
            pathBoxes = new TextBox[5];
            statusLeds = new Label[5];
            statusTexts = new Label[5];
            browseButtons = new Button[5];

            for (int i = 0; i < 5; i++)
            {
                Panel card = new Panel { Location = new Point(25, 80 + (i * 105)), Size = new Size(515, 90), BackColor = ColorCard, Padding = new Padding(15) };
                card.Paint += Card_Paint;
                
                statusLeds[i] = new Label { Location = new Point(18, 18), Size = new Size(14, 14), BackColor = Color.Transparent, Tag = "stopped" };
                statusLeds[i].Paint += StatusLed_Paint;
                
                Label lblSlot = new Label { Text = $"MONITOR SLOT {i + 1}", Location = new Point(42, 16), Font = new Font("Segoe UI Semibold", 9F), ForeColor = Color.FromArgb(150, 150, 160), AutoSize = true };
                statusTexts[i] = new Label { Text = "IDLE", Location = new Point(350, 16), TextAlign = ContentAlignment.TopRight, ForeColor = Color.FromArgb(100, 100, 110), Font = new Font("Segoe UI", 9F, FontStyle.Bold), Width = 140 };
                pathBoxes[i] = new TextBox { Location = new Point(18, 48), Width = 400, BackColor = Color.FromArgb(20, 20, 25), ForeColor = ColorText, BorderStyle = BorderStyle.None, ReadOnly = true, Font = new Font("Segoe UI", 9F) };

                Button btnSelect = new Button { Text = "Browse", Location = new Point(428, 45), Width = 70, Height = 28, FlatStyle = FlatStyle.Flat, BackColor = ColorAccent, ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 9F), Tag = i };
                btnSelect.FlatAppearance.BorderSize = 0;
                btnSelect.Click += BtnSelect_Click;
                btnSelect.MouseEnter += (s, e) => { ((Button)s).BackColor = Color.FromArgb(50, 110, 250); };
                btnSelect.MouseLeave += (s, e) => { ((Button)s).BackColor = ColorAccent; };

                card.Controls.Add(statusLeds[i]);
                card.Controls.Add(lblSlot);
                card.Controls.Add(statusTexts[i]);
                card.Controls.Add(pathBoxes[i]);
                card.Controls.Add(btnSelect);
                this.Controls.Add(card);
                slotCards[i] = card;
                browseButtons[i] = btnSelect;
            }

            Label lblLang = new Label { Text = "Language:", Location = new Point(25, 605), AutoSize = true, ForeColor = Color.FromArgb(120, 120, 130), Font = new Font("Segoe UI", 8F) };
            ComboBox comboLang = new ComboBox { Location = new Point(90, 602), Width = 100, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = ColorCard, ForeColor = ColorText, FlatStyle = FlatStyle.Flat };
            comboLang.Items.AddRange(new string[] { "English", "한국어", "日本語", "简体中文" });
            comboLang.SelectedIndex = 0;
            comboLang.SelectedIndexChanged += (s, e) => ChangeLanguage(comboLang.SelectedIndex);

            Label lblInterval = new Label { Text = "Interval (sec):", Location = new Point(220, 605), AutoSize = true, ForeColor = Color.FromArgb(120, 120, 130), Font = new Font("Segoe UI", 8F) };
            NumericUpDown numInterval = new NumericUpDown { Location = new Point(310, 602), Width = 50, Minimum = 1, Maximum = 60, Value = 3, BackColor = ColorCard, ForeColor = ColorText, BorderStyle = BorderStyle.FixedSingle };
            numInterval.ValueChanged += (s, e) => { if (monitorTimer != null) monitorTimer.Interval = (int)numInterval.Value * 1000; };

            this.Controls.Add(lblLang);
            this.Controls.Add(comboLang);
            this.Controls.Add(lblInterval);
            this.Controls.Add(numInterval);

            trayMenu = new ContextMenuStrip();
            trayMenu.Renderer = new ToolStripProfessionalRenderer(new CustomColorTable()); 
            trayMenu.Items.Add("Open Dashboard", null, (s, e) => ShowForm());
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("Exit Guardian", null, (s, e) => ExitApp());

            trayIcon = new NotifyIcon();
            trayIcon.Text = "Process Guardian Pro";
            trayIcon.Icon = SystemIcons.Shield;
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += (s, e) => ShowForm();

            monitorTimer = new System.Windows.Forms.Timer();
            monitorTimer.Interval = 3000;
            monitorTimer.Tick += MonitorTimer_Tick;
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
                    pathBoxes[index].Text = ofd.FileName;
                    statusTexts[index].Text = "LOADED";
                    statusLeds[index].Tag = "warning";
                    statusLeds[index].Invalidate();
                    SaveSettings();
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; this.Hide(); trayIcon.ShowBalloonTip(2000, "Background Mode", "Guardian is still protecting your processes.", ToolTipIcon.Info); }
            base.OnFormClosing(e);
        }

        private void MonitorTimer_Tick(object sender, EventArgs e)
        {
            for (int i = 0; i < 5; i++)
            {
                string path = pathBoxes[i].Text;
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) { statusLeds[i].Tag = "idle"; statusLeds[i].Invalidate(); statusTexts[i].Text = "EMPTY"; statusTexts[i].ForeColor = Color.FromArgb(80, 80, 90); continue; }
                string processName = Path.GetFileNameWithoutExtension(path);
                Process[] processes = Process.GetProcessesByName(processName);
                if (processes.Length > 0) { statusLeds[i].Tag = "running"; statusLeds[i].Invalidate(); statusTexts[i].Text = "RUNNING"; statusTexts[i].ForeColor = ColorStatusRunning; }
                else {
                    statusLeds[i].Tag = "stopped"; statusLeds[i].Invalidate(); statusTexts[i].Text = "RESTARTING..."; statusTexts[i].ForeColor = ColorStatusStopped;
                    try { Process.Start(path); trayIcon.ShowBalloonTip(1000, "Guardian Alert", $" recovered successfully.", ToolTipIcon.Warning); }
                    catch (Exception ex) { Debug.WriteLine($"Recovery Error: {ex.Message}"); statusTexts[i].Text = "ERROR"; }
                }
            }
        }

        private void LoadSettings()
        {
            try {
                pathBoxes[0].Text = Properties.Settings.Default.Path1;
                pathBoxes[1].Text = Properties.Settings.Default.Path2;
                pathBoxes[2].Text = Properties.Settings.Default.Path3;
                pathBoxes[3].Text = Properties.Settings.Default.Path4;
                pathBoxes[4].Text = Properties.Settings.Default.Path5;
            } catch { }
        }

        private void SaveSettings()
        {
            Properties.Settings.Default.Path1 = pathBoxes[0].Text;
            Properties.Settings.Default.Path2 = pathBoxes[1].Text;
            Properties.Settings.Default.Path3 = pathBoxes[2].Text;
            Properties.Settings.Default.Path4 = pathBoxes[3].Text;
            Properties.Settings.Default.Path5 = pathBoxes[4].Text;
            Properties.Settings.Default.Save();
        }

        private void StartMonitoring() => monitorTimer.Start();
        private void ShowForm() { this.Show(); this.WindowState = FormWindowState.Normal; this.Activate(); }
        private void ExitApp() { monitorTimer.Stop(); trayIcon.Visible = false; Application.Exit(); }

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
                ["Recovered"] = new[] { "recovered successfully.", "성공적으로 복구되었습니다.", "正常에 復구되었습니다.", "成功恢复。" }
            };
            if (storage.ContainsKey(key)) return storage[key][currentLangIndex];
            return key;
        }

        private void ChangeLanguage(int index) { currentLangIndex = index; UpdateUITexts(); }
        private void UpdateUITexts() { this.Text = "Process Guardian Professional"; }

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
