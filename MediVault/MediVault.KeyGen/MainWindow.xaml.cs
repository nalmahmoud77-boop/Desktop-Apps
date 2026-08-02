using System.IO;
using System.Windows;
using MediVault.Licensing;
using Microsoft.Win32;

namespace MediVault.KeyGen;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void GenerateMonthly_Click(object sender, RoutedEventArgs e) => Generate(LicenseTier.Monthly);
    private void GenerateYearly_Click(object sender, RoutedEventArgs e) => Generate(LicenseTier.Yearly);
    private void GenerateLifetime_Click(object sender, RoutedEventArgs e) => Generate(LicenseTier.Lifetime);

    private void Generate(LicenseTier tier)
    {
        var name = (IssuedToBox.Text ?? string.Empty).Trim();
        KeyBox.Text = LicenseKey.Generate(tier, name);
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(KeyBox.Text)) return;
        Clipboard.SetText(KeyBox.Text);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(KeyBox.Text))
        {
            MessageBox.Show(this, "Generate a key first.", "MediVault KeyGen", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dlg = new SaveFileDialog
        {
            Filter = "License key (*.lic)|*.lic|Text file (*.txt)|*.txt",
            FileName = $"medivault-license-{DateTime.UtcNow:yyyyMMddHHmmss}.lic"
        };
        if (dlg.ShowDialog(this) != true) return;

        File.WriteAllText(dlg.FileName, KeyBox.Text);
    }
}
