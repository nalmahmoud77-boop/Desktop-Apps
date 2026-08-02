using System.Windows;
using System.Windows.Input;

namespace PersonalTaskManagement.Views.Dialogs
{
    public partial class InputDialog : Window
    {
        public string ResultText { get; private set; } = string.Empty;

        public InputDialog(string title, string prompt, string initialValue)
        {
            InitializeComponent();
            Title = title;
            PromptText.Text = prompt;
            ValueBox.Text = initialValue ?? string.Empty;
            Loaded += (_, _) => { ValueBox.Focus(); ValueBox.SelectAll(); };
            MouseLeftButtonDown += (_, e) =>
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                    DragMove();
            };
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            ResultText = ValueBox.Text ?? string.Empty;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        public static string? Prompt(string title, string prompt, string initialValue = "")
        {
            var dlg = new InputDialog(title, prompt, initialValue)
            {
                Owner = Application.Current?.MainWindow
            };
            return dlg.ShowDialog() == true ? dlg.ResultText : null;
        }
    }
}
