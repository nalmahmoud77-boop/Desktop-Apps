using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace PersonalTaskManagement.Views.Dialogs
{
    public enum MessageKind
    {
        Info,
        Success,
        Warning,
        Error,
        Question
    }

    /// <summary>
    /// A styled replacement for the native <see cref="MessageBox"/>.
    /// </summary>
    public partial class MessageDialog : Window
    {
        private bool _confirmed;

        private MessageDialog()
        {
            InitializeComponent();
            MouseLeftButtonDown += (_, e) =>
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                    DragMove();
            };
        }

        private void ApplyKind(MessageKind kind)
        {
            // (glyph, foreground, badge background)
            (string glyph, string fg, string bg) = kind switch
            {
                MessageKind.Success  => ("✓", "#16A34A", "#DCFCE7"),
                MessageKind.Warning  => ("!", "#D97706", "#FEF3C7"),
                MessageKind.Error    => ("✕", "#DC2626", "#FEE2E2"),
                MessageKind.Question => ("?", "#2563EB", "#DBEAFE"),
                _                    => ("i", "#2563EB", "#DBEAFE"),
            };

            IconGlyph.Text = glyph;
            IconGlyph.Foreground = Brush(fg);
            IconBadge.Background = Brush(bg);
        }

        private static SolidColorBrush Brush(string hex) =>
            new((Color)ColorConverter.ConvertFromString(hex));

        private void Primary_Click(object sender, RoutedEventArgs e)
        {
            _confirmed = true;
            DialogResult = true;
        }

        private void Secondary_Click(object sender, RoutedEventArgs e)
        {
            _confirmed = false;
            DialogResult = false;
        }

        // ----------------- Public API -----------------

        /// <summary>Shows an informational / success / warning / error message with a single OK button.</summary>
        public static void Show(string message, string title,
            MessageKind kind = MessageKind.Info, Window? owner = null)
        {
            var dlg = Create(message, title, kind, owner);
            dlg.PrimaryActionButton.Content = "OK";
            dlg.ShowDialog();
        }

        public static void Info(string message, string title = "Information", Window? owner = null)
            => Show(message, title, MessageKind.Info, owner);

        public static void Warn(string message, string title = "Warning", Window? owner = null)
            => Show(message, title, MessageKind.Warning, owner);

        public static void Error(string message, string title = "Error", Window? owner = null)
            => Show(message, title, MessageKind.Error, owner);

        /// <summary>
        /// Shows a confirmation dialog. Returns <c>true</c> when the primary (confirm) button is chosen.
        /// When <paramref name="destructive"/> is true the confirm button uses the danger style.
        /// </summary>
        public static bool Confirm(string message, string title,
            string confirmText = "OK", string cancelText = "Cancel",
            bool destructive = false, MessageKind kind = MessageKind.Question, Window? owner = null)
        {
            var dlg = Create(message, title, kind, owner);
            dlg.PrimaryActionButton.Content = confirmText;
            dlg.SecondaryActionButton.Content = cancelText;
            dlg.SecondaryActionButton.Visibility = Visibility.Visible;
            if (destructive)
                dlg.PrimaryActionButton.Style = (Style)dlg.FindResource("DangerButton");
            dlg.ShowDialog();
            return dlg._confirmed;
        }

        /// <summary>Convenience confirm for delete/destructive actions (warning icon + red confirm button).</summary>
        public static bool ConfirmDelete(string message, string title = "Confirm Delete",
            string confirmText = "Delete", Window? owner = null)
            => Confirm(message, title, confirmText, "Cancel", destructive: true,
                       kind: MessageKind.Warning, owner: owner);

        private static MessageDialog Create(string message, string title, MessageKind kind, Window? owner)
        {
            var dlg = new MessageDialog
            {
                Owner = owner ?? Application.Current?.MainWindow
            };
            dlg.Title = title;
            dlg.TitleText.Text = title;
            dlg.MessageText.Text = message;
            dlg.ApplyKind(kind);
            return dlg;
        }
    }
}
