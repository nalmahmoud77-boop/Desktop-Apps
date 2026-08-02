using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Markup;
using PersonalTaskManagement.Models;

namespace PersonalTaskManagement.Views.Dialogs
{
    public partial class TaskEditDialog : Window
    {
        private readonly TaskItem _task;
        private readonly ObservableCollection<TagPickerItem> _tagItems;

        public TaskEditDialog(TaskItem task, IEnumerable<Tag> availableTags)
        {
            InitializeComponent();
            _task = task;

            TitleBox.Text = task.Title;
            LoadDescription(task.DescriptionXaml, task.DescriptionPlain);
            DueDatePicker.SelectedDate = task.DueDate;

            switch (task.Priority)
            {
                case Priority.Low: PrioLow.IsChecked = true; break;
                case Priority.High: PrioHigh.IsChecked = true; break;
                default: PrioMedium.IsChecked = true; break;
            }

            var selectedIds = task.Tags.Select(t => t.Id).ToHashSet();
            _tagItems = new ObservableCollection<TagPickerItem>(
                availableTags.OrderBy(t => t.Name).Select(t => new TagPickerItem(t, selectedIds.Contains(t.Id))));
            TagsList.ItemsSource = _tagItems;

            Title = task.Id == 0 ? "New Task" : "Edit Task";
            HeaderText.Text = Title;
            Loaded += (_, _) => TitleBox.Focus();
        }

        private void LoadDescription(string xaml, string plain)
        {
            var doc = new FlowDocument();
            if (!string.IsNullOrWhiteSpace(xaml))
            {
                try
                {
                    using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml));
                    var loaded = (FlowDocument)XamlReader.Load(stream);
                    DescriptionBox.Document = loaded;
                    return;
                }
                catch
                {
                }
            }

            if (!string.IsNullOrEmpty(plain))
            {
                doc.Blocks.Add(new Paragraph(new Run(plain)));
            }
            DescriptionBox.Document = doc;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleBox.Text))
            {
                MessageDialog.Warn("Please enter a title for this task.", "Title required", this);
                TitleBox.Focus();
                return;
            }

            _task.Title = TitleBox.Text.Trim();

            string xaml;
            using (var stream = new MemoryStream())
            {
                XamlWriter.Save(DescriptionBox.Document, stream);
                xaml = Encoding.UTF8.GetString(stream.ToArray());
            }
            _task.DescriptionXaml = xaml;

            var range = new TextRange(DescriptionBox.Document.ContentStart, DescriptionBox.Document.ContentEnd);
            _task.DescriptionPlain = (range.Text ?? string.Empty).Trim();

            _task.DueDate = DueDatePicker.SelectedDate;

            _task.Priority = PrioHigh.IsChecked == true ? Priority.High
                           : PrioLow.IsChecked == true ? Priority.Low
                           : Priority.Medium;

            _task.Tags.Clear();
            foreach (var item in _tagItems.Where(i => i.IsSelected))
                _task.Tags.Add(item.Tag);

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ClearDate_Click(object sender, RoutedEventArgs e)
        {
            DueDatePicker.SelectedDate = null;
        }

        private sealed class TagPickerItem : INotifyPropertyChanged
        {
            private bool _isSelected;
            public Tag Tag { get; }
            public bool IsSelected
            {
                get => _isSelected;
                set { if (_isSelected != value) { _isSelected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected))); } }
            }

            public TagPickerItem(Tag tag, bool isSelected) { Tag = tag; _isSelected = isSelected; }

            public event PropertyChangedEventHandler? PropertyChanged;
        }
    }
}
