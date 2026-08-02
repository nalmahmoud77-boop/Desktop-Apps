using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using PersonalTaskManagement.Messaging;
using PersonalTaskManagement.Models;

namespace PersonalTaskManagement.ViewModels
{
    public class ColumnViewModel : BaseViewModel
    {
        private readonly BoardColumn _model;
        private readonly IMessenger _messenger;
        private string _searchQuery = string.Empty;

        public ColumnViewModel(BoardColumn model, IMessenger messenger)
        {
            _model = model;
            _messenger = messenger;
            Tasks = new ObservableCollection<TaskViewModel>(
                model.Tasks.OrderBy(t => t.SortOrder).Select(t => new TaskViewModel(t)));

            TasksView = (ListCollectionView)CollectionViewSource.GetDefaultView(Tasks);
            TasksView.Filter = TaskFilter;
            TasksView.CustomSort = new TaskSortComparer();

            _messenger.Subscribe<SearchChangedMessage>(this, OnSearchChanged);
            _messenger.Subscribe<TaskUpdatedMessage>(this, OnTaskUpdated);
        }

        public BoardColumn Model => _model;

        public int Id => _model.Id;

        public string Name
        {
            get => _model.Name;
            set { if (_model.Name != value) { _model.Name = value; OnPropertyChanged(); } }
        }

        public int SortOrder
        {
            get => _model.SortOrder;
            set { if (_model.SortOrder != value) { _model.SortOrder = value; OnPropertyChanged(); } }
        }

        public int BoardId => _model.BoardId;

        public ObservableCollection<TaskViewModel> Tasks { get; }

        public ListCollectionView TasksView { get; }

        public int VisibleCount => TasksView.Cast<object>().Count();

        private bool TaskFilter(object obj) => obj is TaskViewModel t && t.MatchesSearch(_searchQuery);

        private void OnSearchChanged(SearchChangedMessage msg)
        {
            _searchQuery = msg.Query ?? string.Empty;
            TasksView.Refresh();
            OnPropertyChanged(nameof(VisibleCount));
        }

        private void OnTaskUpdated(TaskUpdatedMessage msg)
        {
            var t = Tasks.FirstOrDefault(x => x.Id == msg.TaskId);
            if (t != null) TasksView.Refresh();
        }

        public void Detach()
        {
            _messenger.UnsubscribeAll(this);
        }

        private sealed class TaskSortComparer : System.Collections.IComparer
        {
            public int Compare(object? x, object? y)
            {
                if (x is TaskViewModel a && y is TaskViewModel b)
                    return a.SortOrder.CompareTo(b.SortOrder);
                return 0;
            }
        }
    }
}
