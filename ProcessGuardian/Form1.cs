using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ProcessGuardian
{
    public partial class Form1 : Form
    {
        // 컨트롤 배열 관리 (코딩 편의성)
        private TextBox[] pathBoxes;
        private Button[] selectButtons;

        // 트레이 아이콘 컴포넌트
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private System.Windows.Forms.Timer monitorTimer;

        public Form1()
        {
            InitializeComponent();
            InitializeCustomUI(); // UI 동적 생성 및 초기화
            LoadSettings();       // 저장된 경로 불러오기
            StartMonitoring();    // 모니터링 시작
        }

        // ---------------------------------------------------------
        // 1. UI 및 초기화 영역
        // ---------------------------------------------------------
        private void InitializeCustomUI()
        {
            this.Text = "Process Guardian Settings";
            this.Size = new Size(500, 350);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            pathBoxes = new TextBox[5];
            selectButtons = new Button[5];

            // 5개의 슬롯 생성
            for (int i = 0; i < 5; i++)
            {
                Label lbl = new Label { Text = $"Slot {i + 1}:", Location = new Point(20, 20 + (i * 40)), AutoSize = true };

                pathBoxes[i] = new TextBox { Location = new Point(80, 20 + (i * 40)), Width = 300, ReadOnly = true };

                selectButtons[i] = new Button { Text = "...", Location = new Point(390, 18 + (i * 40)), Width = 40, Tag = i };
                selectButtons[i].Click += BtnSelect_Click;

                this.Controls.Add(lbl);
                this.Controls.Add(pathBoxes[i]);
                this.Controls.Add(selectButtons[i]);
            }

            // 트레이 아이콘 설정
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("설정 열기", null, (s, e) => ShowForm());
            trayMenu.Items.Add("-"); // 구분선
            trayMenu.Items.Add("종료", null, (s, e) => ExitApp());

            trayIcon = new NotifyIcon();
            trayIcon.Text = "Process Guardian (모니터링 중)";
            // 주의: 실제 아이콘 파일이 없으면 에러가 날 수 있으므로 시스템 아이콘 사용
            trayIcon.Icon = SystemIcons.Shield;
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += (s, e) => ShowForm();

            // 타이머 설정 (3초마다 체크)
            monitorTimer = new System.Windows.Forms.Timer();
            monitorTimer.Interval = 3000;
            monitorTimer.Tick += MonitorTimer_Tick;
        }

        // ---------------------------------------------------------
        // 2. 이벤트 핸들러 (버튼 클릭 등)
        // ---------------------------------------------------------
        private void BtnSelect_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            int index = (int)btn.Tag;

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Executable files (*.exe)|*.exe";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    pathBoxes[index].Text = ofd.FileName;
                    SaveSettings(); // 경로 변경 즉시 저장
                }
            }
        }

        // 창 닫기(X)를 누르면 숨기기 처리
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true; // 진짜 종료 방지
                this.Hide();     // 숨기기
                trayIcon.ShowBalloonTip(1000, "숨김 모드", "프로그램이 트레이로 최소화되었습니다.", ToolTipIcon.Info);
            }
            base.OnFormClosing(e);
        }

        // ---------------------------------------------------------
        // 3. 핵심 로직: 프로세스 감시 및 재실행
        // ---------------------------------------------------------
        private void MonitorTimer_Tick(object sender, EventArgs e)
        {
            for (int i = 0; i < 5; i++)
            {
                string path = pathBoxes[i].Text;
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) continue;

                string processName = Path.GetFileNameWithoutExtension(path);

                // 해당 이름의 프로세스가 실행 중인지 확인
                Process[] processes = Process.GetProcessesByName(processName);

                if (processes.Length == 0)
                {
                    // 프로세스가 없으면 재실행
                    try
                    {
                        Process.Start(path);
                        // 로그를 남기거나 알림을 줄 수 있음 (너무 자주 뜨면 귀찮으므로 생략 가능)
                        trayIcon.ShowBalloonTip(1000, "재실행", $"{processName}이(가) 다시 시작되었습니다.", ToolTipIcon.Warning);
                    }
                    catch (Exception ex)
                    {
                        // 실행 실패 시 처리 (여기서는 조용히 넘어감)
                        Debug.WriteLine($"실행 실패: {ex.Message}");
                    }
                }
            }
        }

        // ---------------------------------------------------------
        // 4. 유틸리티 (설정 저장/로드, 종료)
        // ---------------------------------------------------------
        private void LoadSettings()
        {
            pathBoxes[0].Text = Properties.Settings.Default.Path1;
            pathBoxes[1].Text = Properties.Settings.Default.Path2;
            pathBoxes[2].Text = Properties.Settings.Default.Path3;
            pathBoxes[3].Text = Properties.Settings.Default.Path4;
            pathBoxes[4].Text = Properties.Settings.Default.Path5;
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

        private void StartMonitoring()
        {
            monitorTimer.Start();
        }

        private void ShowForm()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
        }

        private void ExitApp()
        {
            monitorTimer.Stop();
            trayIcon.Visible = false;
            Application.Exit(); // 진짜 종료
        }
    }
}