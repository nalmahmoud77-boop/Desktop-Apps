using System.Windows;
using PersonalTaskManagement.Licensing;
using PersonalTaskManagement.Views.Dialogs;

namespace PersonalTaskManagement.Views
{
    public partial class LicenseWindow : Window
    {
        private readonly LicenseService _service;

        /// <summary>Set to the successful evaluation once the user activates a valid key.</summary>
        public LicenseEvaluation? Result { get; private set; }

        public LicenseWindow(LicenseService service, LicenseEvaluation current)
        {
            InitializeComponent();
            _service = service;
            MachineIdText.Text = MachineIdentity.Id;
            ShowStatus(current);
        }

        private void ShowStatus(LicenseEvaluation eval)
        {
            StatusText.Text = eval.State == LicenseState.NotActivated
                ? "No license is active yet. Paste a key below to activate AgileFlow."
                : eval.Message;

            ClockWarn.Visibility = eval.ClockTampered ? Visibility.Visible : Visibility.Collapsed;

            // Tint the banner red for problem states.
            bool problem = eval.State is LicenseState.Expired
                or LicenseState.InvalidSignature
                or LicenseState.WrongMachine
                or LicenseState.Malformed;

            if (problem)
            {
                StatusBanner.Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FEF2F2"));
                StatusBanner.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FECACA"));
                StatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#991B1B"));
            }
        }

        private void Activate_Click(object sender, RoutedEventArgs e)
        {
            string key = KeyBox.Text;
            if (string.IsNullOrWhiteSpace(key))
            {
                MessageDialog.Warn("Please paste your license key first.", "No key entered", this);
                return;
            }

            LicenseEvaluation eval = _service.Activate(key);
            if (eval.State == LicenseState.Valid)
            {
                Result = eval;
                MessageDialog.Show(eval.Message, "License activated", MessageKind.Success, this);
                DialogResult = true;
            }
            else
            {
                MessageDialog.Error(eval.Message, "Activation failed", this);
                ShowStatus(eval);
            }
        }

        private void CopyMachineId_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(MachineIdentity.Id);
                MessageDialog.Info("Machine ID copied to the clipboard.", "Copied", this);
            }
            catch
            {
                // clipboard can transiently fail; ignore
            }
        }

        private void Quit_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
