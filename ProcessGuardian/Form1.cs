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
        // 현대적인 상용 디자인 컬러 테마
        private static readonly Color ColorBackground = Color.FromArgb(32, 32, 32);
        private static readonly Color ColorCard = Color.FromArgb(45, 45, 48);
        private static readonly Color ColorAccent = Color.FromArgb(0, 122, 204);
        private static readonly Color ColorText = Color.FromArgb(240, 240, 240);
        private static readonly Color ColorStatusRunning = Color.FromArgb(0, 255, 127);
        private static readonly Color ColorStatusStopped = Color.FromArgb(255, 69, 0);

        private Panel[] slotCards;
        private TextBox[] pathBoxes;
        private Label[] statusLeds;
        private Label[] statusTexts;

        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private System.Windows.Forms.Timer monitorTimer;

        public Form1()
        {
            InitializeComponent();
            InitializeModernUI(); // 현대적인 UI 초기화
            LoadSettings();       
            StartMonitoring();    
        }

        private void InitializeModernUI()
        {
            // 기본 폼 설정
            this.Text = "Process Guardian Professional";
            this.Size = new Size(550, 600);
            this.BackColor = ColorBackground;
            this.ForeColor = ColorText;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // 헤더 섹션
            Label header = new Label
            {
                Text = "Monitoring Dashboard",
                Font = new Font("Segoe UI Semibold", 18F),
                Location = new Point(20, 20),
                AutoSize = true,
                ForeColor = ColorAccent
            };
            this.Controls.Add(header);

            slotCards = new Panel[5];
            pathBoxes = new TextBox[5];
            statusLeds = new Label[5];
            statusTexts = new Label[5];

            // 5개의 모니터링 카드 생성
            for (int i = 0; i < 5; i++)
            {
                Panel card = new Panel
                {
                    Location = new Point(20, 70 + (i * 95)),
                    Size = new Size(495, 85),
                    BackColor = ColorCard,
                    Padding = new Padding(10)
                };
                
                // 상태 LED 아이콘
                statusLeds[i] = new Label
                {
                    Location = new Point(15, 15),
                    Size = new Size(12, 12),
                    BackColor = ColorStatusStopped,
                    Text = ""
                };
                
                Label lblSlot = new Label 
                { 
                    Text = $"PROCESS SLOT {i + 1}", 
                    Location = new Point(35, 12), 
                    Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                    ForeColor = Color.Gray,
                    AutoSize = true 
                };

                statusTexts[i] = new Label
                {
                    Text = "WAITING...",
                    Location = new Point(380, 12),
                    TextAlign = ContentAlignment.TopRight,
                    ForeColor = Color.DarkGray,
                    Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                    Width = 100
                };

                pathBoxes[i] = new TextBox 
                { 
                    Location = new Point(15, 40), 
                    Width = 400, 
                    BackColor = Color.FromArgb(30, 30, 30),
                    ForeColor = ColorText,
                    BorderStyle = BorderStyle.FixedSingle,
                    ReadOnly = true 
                };

                Button btnSelect = new Button 
                { 
                    Text = "Browse", 
                    Location = new Point(420, 38), 
                    Width = 60, 
                    Height = 25,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = ColorAccent,
                    ForeColor = Color.White,
                    Tag = i 
                };
                btnSelect.FlatAppearance.BorderSize = 0;
                btnSelect.Click += BtnSelect_Click;

                card.Controls.Add(statusLeds[i]);
                card.Controls.Add(lblSlot);
                card.Controls.Add(statusTexts[i]);
                card.Controls.Add(pathBoxes[i]);
                card.Controls.Add(btnSelect);

                this.Controls.Add(card);
                slotCards[i] = card;
            }

            // 언어 선택 UI (하단 배치)
            Label lblLang = new Label
            {
                Text = "Language:",
                Location = new Point(20, 535),
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8F)
            };
            ComboBox comboLang = new ComboBox
            {
                Location = new Point(85, 532),
                Width = 100,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = ColorCard,
                ForeColor = ColorText,
                FlatStyle = FlatStyle.Flat
            };
            comboLang.Items.AddRange(new string[] { "English", "한국어", "日本語", "简体中文" });
            comboLang.SelectedIndex = 0; // 기본값
            comboLang.SelectedIndexChanged += (s, e) => ChangeLanguage(comboLang.SelectedIndex);

            this.Controls.Add(lblLang);
            this.Controls.Add(comboLang);

            // 트레이 아이콘 및 메뉴
            trayMenu = new ContextMenuStrip();
            trayMenu.Renderer = new ToolStripProfessionalRenderer(new CustomColorTable()); // 다크 테마 메뉴
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
                    statusLeds[index].BackColor = Color.Orange;
                    SaveSettings();
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                trayIcon.ShowBalloonTip(2000, "Background Mode", "Guardian is still protecting your processes.", ToolTipIcon.Info);
            }
            base.OnFormClosing(e);
        }

        private void MonitorTimer_Tick(object sender, EventArgs e)
        {
            for (int i = 0; i < 5; i++)
            {
                string path = pathBoxes[i].Text;
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    statusLeds[i].BackColor = Color.FromArgb(60, 60, 60);
                    statusTexts[i].Text = "EMPTY";
                    continue;
                }

                string processName = Path.GetFileNameWithoutExtension(path);
                Process[] processes = Process.GetProcessesByName(processName);

                if (processes.Length > 0)
                {
                    statusLeds[i].BackColor = ColorStatusRunning;
                    statusTexts[i].Text = "RUNNING";
                    statusTexts[i].ForeColor = ColorStatusRunning;
                }
                else
                {
                    statusLeds[i].BackColor = ColorStatusStopped;
                    statusTexts[i].Text = "RESTARTING...";
                    statusTexts[i].ForeColor = ColorStatusStopped;
                    
                    try
                    {
                        Process.Start(path);
                        trayIcon.ShowBalloonTip(1000, "Guardian Alert", $"{processName} recovered successfully.", ToolTipIcon.Warning);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Recovery Error: {ex.Message}");
                        statusTexts[i].Text = "ERROR";
                    }
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
            } catch { /* Settings might not be initialized yet */ }
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

        // ---------------------------------------------------------
        // 5. Localization (다국어 지원)
        // ---------------------------------------------------------
        private int currentLangIndex = 0; // 0:EN, 1:KO, 2:JA, 3:ZH

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
                ["Open"] = new[] { "Open Dashboard", "대시보드 열기", "ダッシュボードを開く", "打开仪表板" },
                ["Exit"] = new[] { "Exit Guardian", "프로그램 종료", "ガー디안 종료", "退出" },
                ["Recovered"] = new[] { "recovered successfully.", "성공적으로 복구되었습니다.", "正常に復구되었습니다.", "成功恢复。" }
            };

            if (storage.ContainsKey(key)) return storage[key][currentLangIndex];
            return key;
        }

        private void ChangeLanguage(int index)
        {
            currentLangIndex = index;
            UpdateUITexts();
        }

        private void UpdateUITexts()
        {
            // 헤더 및 메뉴 갱신 로직 (간략화된 예시)
            this.Text = $"Process Guardian Professional ({GetStr("Title")})";
            // ... 각 컨트롤의 Text를 GetStr로 갱신하는 로직 추가 가능
        }
    }

    internal class CustomColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Color.FromArgb(45, 45, 48);
        public override Color MenuBorder => Color.FromArgb(60, 60, 60);
        public override Color MenuItemSelected => Color.FromArgb(62, 62, 64);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(62, 62, 64);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(62, 62, 64);
        public override Color MenuItemBorder => Color.FromArgb(0, 122, 204);
    }
}