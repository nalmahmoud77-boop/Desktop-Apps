using System;
using System.Collections.ObjectModel;
using System.Linq;
using PersonalTaskManagement.Models;

namespace PersonalTaskManagement.ViewModels
{
    public class TaskViewModel : BaseViewModel
    {
        private readonly TaskItem _model;

        public TaskViewModel(TaskItem model)
        {
            _model = model;
            Tags = new ObservableCollection<Tag>(model.Tags);
        }

        public TaskItem Model => _model;

        public int Id => _model.Id;

        public string Title
        {
            get => _model.Title;
            set { if (_model.Title != value) { _model.Title = value; OnPropertyChanged(); } }
        }

        public string DescriptionPlain
        {
            get => _model.DescriptionPlain;
            set { if (_model.DescriptionPlain != value) { _model.DescriptionPlain = value; OnPropertyChanged(); OnPropertyChanged(nameof(DescriptionPreview)); } }
        }

        public string DescriptionXaml
        {
            get => _model.DescriptionXaml;
            set { if (_model.DescriptionXaml != value) { _model.DescriptionXaml = value; OnPropertyChanged(); } }
        }

        public string DescriptionPreview
        {
            get
            {
                var text = (_model.DescriptionPlain ?? string.Empty).Trim();
                if (text.Length <= 120) return text;
                return text.Substring(0, 117) + "...";
            }
        }

        public Priority Priority
        {
            get => _model.Priority;
            set { if (_model.Priority != value) { _model.Priority = value; OnPropertyChanged(); } }
        }

        public DateTime? DueDate
        {
            get => _model.DueDate;
            set { if (_model.DueDate != value) { _model.DueDate = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsOverdue)); } }
        }

        public bool IsOverdue => DueDate.HasValue && DueDate.Value.Date < DateTime.Today;

        public DateTime LastModified
        {
            get => _model.LastModified;
            set { if (_model.LastModified != value) { _model.LastModified = value; OnPropertyChanged(); } }
        }

        public int BoardColumnId
        {
            get => _model.BoardColumnId;
            set { if (_model.BoardColumnId != value) { _model.BoardColumnId = value; OnPropertyChanged(); } }
        }

        public int SortOrder
        {
            get => _model.SortOrder;
            set { if (_model.SortOrder != value) { _model.SortOrder = value; OnPropertyChanged(); } }
        }

        public ObservableCollection<Tag> Tags { get; }

        public string TagsSummary => Tags.Count == 0 ? string.Empty : string.Join(", ", Tags.Select(t => t.Name));

        public void RefreshTags()
        {
            OnPropertyChanged(nameof(TagsSummary));
        }

        public bool MatchesSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return true;
            var q = query.Trim();
            if (Title.Contains(q, StringComparison.OrdinalIgnoreCase)) return true;
            if (!string.IsNullOrEmpty(DescriptionPlain) && DescriptionPlain.Contains(q, StringComparison.OrdinalIgnoreCase)) return true;
            return Tags.Any(t => t.Name.Contains(q, StringComparison.OrdinalIgnoreCase));
        }
    }
}
