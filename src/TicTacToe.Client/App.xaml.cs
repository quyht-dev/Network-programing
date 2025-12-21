using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace TicTacToe.Client;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // 1) Lỗi trên UI thread (hay gặp nhất)
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(args.Exception.ToString(), "UI Unhandled Exception");
            args.Handled = true; // quan trọng: không cho app tự tắt
        };

        // 2) Lỗi ở thread khác
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            MessageBox.Show(args.ExceptionObject?.ToString() ?? "Unknown", "Domain Unhandled Exception");
        };

        // 3) Lỗi task
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            MessageBox.Show(args.Exception.ToString(), "Task Unobserved Exception");
            args.SetObserved();
        };

        base.OnStartup(e);
    }
}

