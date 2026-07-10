using System.Windows;
using ETBTool.Themes;

namespace ETBTool
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ThemeManager.Instance.LoadSavedTheme();
        }
    }
}