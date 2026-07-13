using System;
using System.Diagnostics;
using System.IO;
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
            BtnWebsite.Content = LanguageManager.BtnWebsite;
            BtnSource.Content = LanguageManager.BtnSource;
        }

        private void Config_Click(object s, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(GamePaths.AppConfigPath))
                    Directory.CreateDirectory(GamePaths.AppConfigPath);
                Process.Start("explorer.exe", GamePaths.AppConfigPath);
            }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }

        private void Web_Click(object s, RoutedEventArgs e)
        {
            try { Process.Start(new ProcessStartInfo(GamePaths.OfficialWebsite) { UseShellExecute = true }); }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }

        private void Source_Click(object s, RoutedEventArgs e)
        {
            try { Process.Start(new ProcessStartInfo(GamePaths.SourceCodeUrl) { UseShellExecute = true }); }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }
    }
}