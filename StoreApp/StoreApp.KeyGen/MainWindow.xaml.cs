using System.IO;
using System.Windows;
using Microsoft.Win32;
using StoreApp.Licensing;

namespace StoreApp.KeyGen
{
    public partial class MainWindow : Window
    {
        private LicenseTier? _lastTier;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void GenerateMonthly_Click(object sender, RoutedEventArgs e) => Generate(LicenseTier.Monthly);
        private void GenerateYearly_Click(object sender, RoutedEventArgs e) => Generate(LicenseTier.Yearly);
        private void GenerateLifetime_Click(object sender, RoutedEventArgs e) => Generate(LicenseTier.Lifetime);

        private void Generate(LicenseTier tier)
        {
            _lastTier = tier;
            KeyTextBox.Text = LicenseKey.Generate(tier);
            CopyButton.IsEnabled = true;
            SaveButton.IsEnabled = true;
            StatusText.Text = $"Generated {tier} key.";
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(KeyTextBox.Text)) return;
            try
            {
                Clipboard.SetText(KeyTextBox.Text);
                StatusText.Text = "Key copied to clipboard.";
            }
            catch
            {
                StatusText.Text = "Could not access clipboard.";
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(KeyTextBox.Text)) return;
            var dlg = new SaveFileDialog
            {
                FileName = $"StoreApp-{_lastTier?.ToString() ?? "License"}-{DateTime.UtcNow:yyyyMMddHHmmss}.txt",
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
            };
            if (dlg.ShowDialog(this) != true) return;

            try
            {
                File.WriteAllText(dlg.FileName, KeyTextBox.Text);
                StatusText.Text = $"Saved to {dlg.FileName}";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Save failed: {ex.Message}";
            }
        }
    }
}
