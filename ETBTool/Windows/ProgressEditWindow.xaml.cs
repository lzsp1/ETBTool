using System;
using System.Windows;
using System.Windows.Input;
using ETBTool.Utils;

namespace ETBTool.Windows
{
    public partial class ProgressEditWindow : Window
    {
        public int ProgressLevel { get; private set; }

        public ProgressEditWindow(string saveName, int currentLevel)
        {
            InitializeComponent();
            TitleText.Text = LanguageManager.IsChinese ? "编辑进度" : "Edit Progress";
            SaveNameText.Text = saveName;
            ProgressInput.Text = currentLevel.ToString();
            ProgressLevel = currentLevel;
            ProgressInput.SelectAll();
            ProgressInput.Focus();
        }

        private void Save_Click(object s, RoutedEventArgs e)
        {
            if (int.TryParse(ProgressInput.Text.Trim(), out int level) && level >= 0)
            {
                ProgressLevel = level;
                DialogResult = true;
            }
            else
            {
                Toast.Show(
                    LanguageManager.IsChinese ? "请输入有效的非负整数" : "Please enter a valid non-negative integer",
                    ToastType.Warning);
            }
        }

        private void Cancel_Click(object s, RoutedEventArgs e) => DialogResult = false;
        private void TitleBar_Drag(object s, MouseButtonEventArgs e) { if (e.ClickCount != 2) DragMove(); }
        private void Close_Click(object s, RoutedEventArgs e) => DialogResult = false;
    }
}