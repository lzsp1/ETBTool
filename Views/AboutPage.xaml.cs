using System.Windows;
using System.Windows.Controls;
using ETBTool.Models;
using ETBTool.Utils;

namespace ETBTool.Views
{
    public partial class AboutPage : Page
    {
        public AboutPage()
        {
            InitializeComponent();
            LanguageManager.LanguageChanged += RefreshText;
            Unloaded += (_, _) => LanguageManager.LanguageChanged -= RefreshText;
            VerText.Text = $"v{GamePaths.DisplayVersion}";
            RefreshText();
        }

        private void RefreshText()
        {
            LblDesc.Text = LanguageManager.AboutDesc;
            AboutDesc.Text = LanguageManager.AboutDesc;
            LblFeatures.Text = LanguageManager.AboutFeatures;
            FeatureList.Text = LanguageManager.AboutFeatureList;
            LblLicense.Text = LanguageManager.AboutLicense;
            LicenseText.Text = LanguageManager.AboutLicenseText;
            BtnConfig.Content = LanguageManager.BtnConfig;
            BtnConfig.ToolTip = GamePaths.AppConfigPath;
            BtnWebsite.Content = LanguageManager.BtnWebsite;
            BtnSource.Content = LanguageManager.BtnSource;
        BtnGuide.Content = LanguageManager.IsChinese ? "工具使用说明" : "User Guide";
        }

        private void Config_Click(object s, RoutedEventArgs e)
        {
            ExternalLauncher.OpenAppConfigFolder();
        }

        private void Web_Click(object s, RoutedEventArgs e)
        {
            ExternalLauncher.OpenWebsite(GamePaths.OfficialWebsite);
        }

        private void Guide_Click(object s, RoutedEventArgs e)
        {
            ExternalLauncher.OpenWebsite("https://acnq9mbub8ek.feishu.cn/wiki/BkWNwcleiiGIuwkFqLYcLcoXnXf");
        }

        private void Source_Click(object s, RoutedEventArgs e)
        {
            ExternalLauncher.OpenWebsite(GamePaths.SourceCodeUrl);
        }
    }
}