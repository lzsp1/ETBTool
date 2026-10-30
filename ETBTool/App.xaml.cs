using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ETBTool.Utils;
using ETBTool.Services;
using ETBTool.Models;

namespace ETBTool
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // ★ 全局异常捕获
            DispatcherUnhandledException += OnDispatcherUnhandled;
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandled;
            TaskScheduler.UnobservedTaskException += OnTaskUnhandled;
            try
            {
                if (!Directory.Exists(GamePaths.AppConfigPath)) Directory.CreateDirectory(GamePaths.AppConfigPath);
                var environment = RuntimeEnvironment.Inspect();
                if (environment.Broken)
                {
                    Logger.Log($"运行环境受限：低完整性={environment.LowIntegrity}，配置可写={environment.Writable}，日志可写={environment.LogWritable}，路径={environment.ProgramPath}");
                    MessageBox.Show(RuntimeEnvironment.Advice(environment), "运行环境受限", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"创建配置目录失败：{ex.Message}");
                MessageBox.Show($"无法创建配置目录：\n{GamePaths.AppConfigPath}\n\n{ex.Message}",
                    "配置目录不可用", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            try { BackgroundMusicService.Instance.Initialize(); }
            catch (Exception ex) { LogCrash("Background music startup", ex); }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try { BackgroundMusicService.Instance.Dispose(); }
            catch (Exception ex) { LogCrash("Background music shutdown", ex); }
            base.OnExit(e);
        }

        private void OnDispatcherUnhandled(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogCrash("UI Thread", e.Exception);
            try
            {
                MessageBox.Show(
                    $"UI 崩溃，已记录日志：\n\n{e.Exception.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch { }
            e.Handled = true;
        }

        private void OnDomainUnhandled(object sender, UnhandledExceptionEventArgs e)
        {
            LogCrash("Domain", e.ExceptionObject as Exception);
        }

        private void OnTaskUnhandled(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            LogCrash("Task", e.Exception?.InnerException);
            e.SetObserved();
        }

        private static void LogCrash(string source, Exception? ex)
        {
            try
            {
                var logDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ETBTool");
                Directory.CreateDirectory(logDir);
                var path = Path.Combine(logDir, "crash.log");
                var text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}]\n{ex}\n\n";
                File.AppendAllText(path, text);
            }
            catch { }
        }
    }
}