using System;
using System.IO;
using System.Windows.Forms;

namespace ProcessGuardian
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => LogFatalError(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => LogFatalError(e.ExceptionObject as Exception);

            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }

        private static void LogFatalError(Exception? ex)
        {
            if (ex == null) return;
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log");
            string errorInfo = $"\n[{DateTime.Now}] FATAL ERROR:\n{ex}\n{new string('-', 30)}\n";
            File.AppendAllText(logPath, errorInfo);
            MessageBox.Show($"심각한 오류가 발생했습니다. 로그 파일을 확인해 주세요: {logPath}\n\n오류 내용: {ex.Message}", 
                "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}