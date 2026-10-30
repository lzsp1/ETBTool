using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using ETBTool.Utils;
using ETBTool.ViewModels;

namespace ETBTool.Views
{
    public partial class DownloadModPage : Page
    {
        private readonly ModBrowserViewModel _vm;
        private readonly DispatcherTimer _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };

        public DownloadModPage()
        {
            InitializeComponent();
            _vm = new ModBrowserViewModel();
            DataContext = _vm;

            LoginPwdBox.PasswordChanged += (_, _) => _vm.LoginPassword = LoginPwdBox.Password;
            RegPwdBox.PasswordChanged += (_, _) => _vm.RegPassword = RegPwdBox.Password;

            _vm.PropertyChanged += OnVmChanged;
            LanguageManager.LanguageChanged += RefreshText;
            Unloaded += (_, _) =>
            {
                LanguageManager.LanguageChanged -= RefreshText;
                _vm.PropertyChanged -= OnVmChanged;
                _searchTimer.Stop();
            };

            _searchTimer.Tick += (_, _) => { _searchTimer.Stop(); try { _vm.SearchCmd.Execute(null); } catch { } };

            Loaded += async (_, _) => { await _vm.InitAsync(); SearchBox?.Focus(); };
            RefreshText();
        }

        private void OnVmChanged(object? s, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_vm.ShowDetail))
                Dispatcher.BeginInvoke(() => { if (_vm.ShowDetail) FadeIn(DetailOverlay); else FadeOut(DetailOverlay); });
            if (e.PropertyName == nameof(_vm.ShowAbout))
                Dispatcher.BeginInvoke(() => { if (_vm.ShowAbout) FadeIn(AboutOverlay); else FadeOut(AboutOverlay); });
            if (e.PropertyName == nameof(_vm.ShowBrowse) && _vm.ShowBrowse)
                Dispatcher.BeginInvoke(() => SearchBox?.Focus(), DispatcherPriority.Background);
        }

        private void RefreshText()
        {
            LoginTitle.Text = LanguageManager.ModLibTitle;
            LoginSub.Text = LanguageManager.ModLoginSub;
            LblLoginAcct.Text = LanguageManager.ModLoginAccount;
            LblLoginPwd.Text = LanguageManager.ModLoginPassword;
            BtnLogin.Content = LanguageManager.ModLoginBtn;
            // ★ 忘记密码
            ForgotPwdHint.Text = LanguageManager.ModDlForgotPwd;
            // ★ 登录页关于按钮
            BtnLoginAbout.Content = LanguageManager.ModDlAboutTitle;

            RunNoAccount.Text = LanguageManager.ModLoginNoAccount + " ";
            RunGoReg.Text = LanguageManager.ModLoginGoRegister;
            RegTitle.Text = LanguageManager.ModRegTitle;
            LblRegEmail.Text = LanguageManager.ModRegEmail;
            LblRegCode.Text = LanguageManager.ModRegCode;
            LblRegUser.Text = LanguageManager.ModRegUsername;
            LblRegPwd.Text = LanguageManager.ModRegPassword;
            BtnRegister.Content = LanguageManager.ModRegBtn;
            RunHasAcct.Text = LanguageManager.ModRegHasAccount + " ";
            RunGoLogin.Text = LanguageManager.ModRegGoLogin;
            BtnAboutMod.Content = LanguageManager.ModDlAboutTitle;
            BtnLogout.Content = LanguageManager.ModBrowseLogout;
            CatAll.Content = LanguageManager.ModBrowseAll;
            CatMod.Content = LanguageManager.ModBrowseMod;
            CatTool.Content = LanguageManager.ModBrowseTool;
            CatOther.Content = LanguageManager.ModBrowseOther;
            BtnSearch.Content = LanguageManager.ModBrowseSearch;
            LoadingText.Text = LanguageManager.ModBrowseLoading;
            BtnPrev.Content = LanguageManager.ModBrowsePrev;
            BtnNext.Content = LanguageManager.ModBrowseNext;
            LblActiveDl.Text = LanguageManager.ModDlActiveTasks;
            BtnClearDl.Content = LanguageManager.ModDlClearAll;
            DtlGame.Text = LanguageManager.ModDetailGame;
            DtlLang.Text = LanguageManager.ModDetailLang;
            DtlAuthor.Text = LanguageManager.ModDetailAuthor;
            DtlUploader.Text = LanguageManager.ModDetailUploader;
            DtlDate.Text = LanguageManager.ModDetailDate;
            DtlDl.Text = LanguageManager.ModDetailDownloads;
            DtlDescLabel.Text = LanguageManager.ModDetailDesc;
            DtlInstLabel.Text = LanguageManager.ModDetailInstructions;
            DtlDepLabel.Text = LanguageManager.ModDetailDeps;
            BtnDlMod.Content = LanguageManager.ModDetailDownload;
            BtnDlDirect.Content = LanguageManager.IsChinese ? "直链下载" : "Direct download";
            AboutTitle.Text = LanguageManager.ModDlAboutTitle;
            AboutText.Text = LanguageManager.ModDlAboutText;
            BtnAboutOK.Content = LanguageManager.ModDlAboutUnderstood;
            BtnAboutOfficial.Content = LanguageManager.ModDlAboutOfficial;
            _vm.RefreshCodeBtn();
        }

        private void FadeIn(UIElement el)
        {
            el.Opacity = 0; el.Visibility = Visibility.Visible;
            el.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        }

        private void FadeOut(UIElement el)
        {
            var a = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
            a.Completed += (_, _) => el.Visibility = Visibility.Collapsed;
            el.BeginAnimation(UIElement.OpacityProperty, a);
        }

        private void Login_Click(object s, RoutedEventArgs e) => _vm.LoginCmd.Execute(null);
        private void Login_KeyDown(object s, KeyEventArgs e) { if (e.Key == Key.Enter) { _vm.LoginCmd.Execute(null); e.Handled = true; } }
        private void GoRegister_Click(object s, RoutedEventArgs e) => _vm.GoRegisterCmd.Execute(null);
        private void Reg_Click(object s, RoutedEventArgs e) => _vm.RegisterCmd.Execute(null);
        private void Reg_KeyDown(object s, KeyEventArgs e) { if (e.Key == Key.Enter) { _vm.RegisterCmd.Execute(null); e.Handled = true; } }
        private void GoLogin_Click(object s, RoutedEventArgs e) => _vm.GoLoginCmd.Execute(null);
        private void Search_KeyDown(object s, KeyEventArgs e) { if (e.Key == Key.Enter) { _searchTimer.Stop(); _vm.SearchCmd.Execute(null); e.Handled = true; } }
        private void CloseDetail_Click(object s, RoutedEventArgs e) => _vm.ShowDetail = false;
        private void DetailOverlay_MouseDown(object s, MouseButtonEventArgs e) => _vm.ShowDetail = false;
        private void DetailCard_MouseDown(object s, MouseButtonEventArgs e) => e.Handled = true;
        private void AboutMod_Click(object s, RoutedEventArgs e) => _vm.ShowAbout = true;
        private void AboutOK_Click(object s, RoutedEventArgs e) => _vm.ShowAbout = false;
        private void AboutOfficial_Click(object s, RoutedEventArgs e) { _vm.ShowAbout = false; _vm.OpenOfficialSite(); }
        private void AboutOverlay_MouseDown(object s, MouseButtonEventArgs e) => _vm.ShowAbout = false;
        private void AboutCard_MouseDown(object s, MouseButtonEventArgs e) => e.Handled = true;
    }
}