using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using PersonalTaskManagement.Data;
using PersonalTaskManagement.Messaging;
using PersonalTaskManagement.Models;
using PersonalTaskManagement.Views.Dialogs;

namespace PersonalTaskManagement.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly IMessenger _messenger;
        private BoardViewModel? _currentBoard;
        private string _searchQuery = string.Empty;
        private string _statusText = "Ready.";

        public MainViewModel(IMessenger messenger)
        {
            _messenger = messenger;
            Boards = new ObservableCollection<BoardViewModel>();
            AvailableTags = new ObservableCollection<Tag>();
            LoadFromDatabase();

            AddBoardCommand = new RelayCommand(AddBoard);
            RenameBoardCommand = new RelayCommand(RenameBoard, () => CurrentBoard != null);
            DeleteBoardCommand = new RelayCommand(DeleteBoard, () => CurrentBoard != null && Boards.Count > 1);
            AddColumnCommand = new RelayCommand(AddColumn, () => CurrentBoard != null);
            RenameColumnCommand = new RelayCommand(p => RenameColumn(p as ColumnViewModel), p => p is ColumnViewModel);
            DeleteColumnCommand = new RelayCommand(p => DeleteColumn(p as ColumnViewModel), p => p is ColumnViewModel cv && CurrentBoard != null && CurrentBoard.Columns.Count > 1);
            AddTaskCommand = new RelayCommand(p => AddTask(p as ColumnViewModel), p => p is ColumnViewModel);
            EditTaskCommand = new RelayCommand(p => EditTask(p as TaskViewModel), p => p is TaskViewModel);
            DeleteTaskCommand = new RelayCommand(p => DeleteTask(p as TaskViewModel), p => p is TaskViewModel);
            ClearSearchCommand = new RelayCommand(() => SearchQuery = string.Empty);
        }

        public ObservableCollection<BoardViewModel> Boards { get; }

        public ObservableCollection<Tag> AvailableTags { get; }

        public BoardViewModel? CurrentBoard
        {
            get => _currentBoard;
            set
            {
                if (SetField(ref _currentBoard, value))
                {
                    OnPropertyChanged(nameof(HasBoard));
                }
            }
        }

        public bool HasBoard => CurrentBoard != null;

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetField(ref _searchQuery, value ?? string.Empty))
                {
                    _messenger.Send(new SearchChangedMessage(_searchQuery));
                }
            }
        }

        public string StatusText
        {
            get => _statusText;
            set => SetField(ref _statusText, value);
        }

        private string _licenseSummary = "AgileFlow";
        public string LicenseSummary
        {
            get => _licenseSummary;
            set => SetField(ref _licenseSummary, value);
        }

        public ICommand AddBoardCommand { get; }
        public ICommand RenameBoardCommand { get; }
        public ICommand DeleteBoardCommand { get; }
        public ICommand AddColumnCommand { get; }
        public ICommand RenameColumnCommand { get; }
        public ICommand DeleteColumnCommand { get; }
        public ICommand AddTaskCommand { get; }
        public ICommand EditTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }
        public ICommand ClearSearchCommand { get; }

        private void LoadFromDatabase()
        {
            using var db = new AppDbContext();
            var boards = db.Boards
                .Include(b => b.Columns)
                    .ThenInclude(c => c.Tasks)
                        .ThenInclude(t => t.Tags)
                .OrderBy(b => b.SortOrder)
                .AsNoTracking()
                .ToList();

            Boards.Clear();
            foreach (var b in boards) Boards.Add(new BoardViewModel(b, _messenger));
            CurrentBoard = Boards.FirstOrDefault();

            AvailableTags.Clear();
            foreach (var tag in db.Tags.AsNoTracking().OrderBy(t => t.Name).ToList())
                AvailableTags.Add(tag);
        }

        public void AddBoard()
        {
            var name = Views.Dialogs.InputDialog.Prompt("New Board", "Board name:", "New Board");
            if (string.IsNullOrWhiteSpace(name)) return;

            using var db = new AppDbContext();
            var nextSort = db.Boards.Any() ? db.Boards.Max(b => b.SortOrder) + 1 : 0;
            var board = new Board
            {
                Name = name.Trim(),
                SortOrder = nextSort,
                Columns =
                {
                    new BoardColumn { Name = "To Do", SortOrder = 0 },
                    new BoardColumn { Name = "In Progress", SortOrder = 1 },
                    new BoardColumn { Name = "Done", SortOrder = 2 }
                }
            };
            db.Boards.Add(board);
            db.SaveChanges();

            var detached = db.Boards
                .Include(b => b.Columns)
                    .ThenInclude(c => c.Tasks)
                        .ThenInclude(t => t.Tags)
                .AsNoTracking()
                .First(b => b.Id == board.Id);

            var vm = new BoardViewModel(detached, _messenger);
            Boards.Add(vm);
            CurrentBoard = vm;
            Status($"Board '{vm.Name}' created.");
        }

        public void RenameBoard()
        {
            if (CurrentBoard == null) return;
            var name = Views.Dialogs.InputDialog.Prompt("Rename Board", "Board name:", CurrentBoard.Name);
            if (string.IsNullOrWhiteSpace(name)) return;

            using var db = new AppDbContext();
            var entity = db.Boards.Find(CurrentBoard.Id);
            if (entity == null) return;
            entity.Name = name.Trim();
            db.SaveChanges();
            CurrentBoard.Name = entity.Name;
            Status($"Board renamed to '{entity.Name}'.");
        }

        public void DeleteBoard()
        {
            if (CurrentBoard == null) return;
            if (!MessageDialog.ConfirmDelete(
                    $"Delete the board “{CurrentBoard.Name}” and all of its tasks? This cannot be undone.",
                    "Delete Board"))
                return;

            using var db = new AppDbContext();
            var entity = db.Boards.Find(CurrentBoard.Id);
            if (entity != null)
            {
                db.Boards.Remove(entity);
                db.SaveChanges();
            }
            var name = CurrentBoard.Name;
            CurrentBoard.Detach();
            Boards.Remove(CurrentBoard);
            CurrentBoard = Boards.FirstOrDefault();
            Status($"Board '{name}' deleted.");
        }

        public void AddColumn()
        {
            if (CurrentBoard == null) return;
            var name = Views.Dialogs.InputDialog.Prompt("New Column", "Column name:", "New Column");
            if (string.IsNullOrWhiteSpace(name)) return;

            using var db = new AppDbContext();
            var nextSort = db.Columns.Where(c => c.BoardId == CurrentBoard.Id).Any()
                ? db.Columns.Where(c => c.BoardId == CurrentBoard.Id).Max(c => c.SortOrder) + 1
                : 0;
            var col = new BoardColumn { Name = name.Trim(), SortOrder = nextSort, BoardId = CurrentBoard.Id };
            db.Columns.Add(col);
            db.SaveChanges();

            var detached = db.Columns.Include(c => c.Tasks).ThenInclude(t => t.Tags).AsNoTracking().First(c => c.Id == col.Id);
            var vm = new ColumnViewModel(detached, _messenger);
            CurrentBoard.Columns.Add(vm);
            _messenger.Send(new ColumnAddedMessage(col.Id, CurrentBoard.Id));
            Status($"Column '{vm.Name}' added.");
        }

        public void RenameColumn(ColumnViewModel? cv)
        {
            if (cv == null) return;
            var name = Views.Dialogs.InputDialog.Prompt("Rename Column", "Column name:", cv.Name);
            if (string.IsNullOrWhiteSpace(name)) return;

            using var db = new AppDbContext();
            var entity = db.Columns.Find(cv.Id);
            if (entity == null) return;
            entity.Name = name.Trim();
            db.SaveChanges();
            cv.Name = entity.Name;
            Status($"Column renamed to '{entity.Name}'.");
        }

        public void DeleteColumn(ColumnViewModel? cv)
        {
            if (cv == null || CurrentBoard == null) return;
            if (CurrentBoard.Columns.Count <= 1)
            {
                MessageDialog.Info("A board must have at least one column.", "Cannot delete column");
                return;
            }
            if (!MessageDialog.ConfirmDelete(
                    $"Delete the column “{cv.Name}” and all of its tasks? This cannot be undone.",
                    "Delete Column"))
                return;

            using var db = new AppDbContext();
            var entity = db.Columns.Find(cv.Id);
            if (entity != null)
            {
                db.Columns.Remove(entity);
                db.SaveChanges();
            }
            cv.Detach();
            CurrentBoard.Columns.Remove(cv);
            _messenger.Send(new ColumnRemovedMessage(cv.Id, CurrentBoard.Id));
            Status($"Column '{cv.Name}' deleted.");
        }

        public void AddTask(ColumnViewModel? cv)
        {
            if (cv == null) return;

            var draft = new TaskItem
            {
                Title = "New Task",
                Priority = Priority.Medium,
                BoardColumnId = cv.Id,
                SortOrder = cv.Tasks.Any() ? cv.Tasks.Max(t => t.SortOrder) + 1 : 0
            };

            var dlg = new Views.Dialogs.TaskEditDialog(draft, AvailableTags.ToList())
            {
                Owner = Application.Current?.MainWindow
            };

            if (dlg.ShowDialog() != true) return;

            var selectedTagIds = draft.Tags.Select(t => t.Id).ToList();

            using var db = new AppDbContext();
            var entity = new TaskItem
            {
                Title = draft.Title,
                DescriptionXaml = draft.DescriptionXaml,
                DescriptionPlain = draft.DescriptionPlain,
                Priority = draft.Priority,
                DueDate = draft.DueDate,
                BoardColumnId = cv.Id,
                SortOrder = draft.SortOrder
            };

            foreach (var tag in db.Tags.Where(t => selectedTagIds.Contains(t.Id)).ToList())
                entity.Tags.Add(tag);

            db.Tasks.Add(entity);
            db.SaveChanges();

            var refreshed = db.Tasks.Include(t => t.Tags).AsNoTracking().First(t => t.Id == entity.Id);
            var taskVm = new TaskViewModel(refreshed);
            cv.Tasks.Add(taskVm);
            cv.TasksView.Refresh();
            _messenger.Send(new TaskCreatedMessage(refreshed.Id, cv.Id));
            Status($"Task '{taskVm.Title}' created.");
        }

        public void EditTask(TaskViewModel? tvm)
        {
            if (tvm == null) return;

            TaskItem snapshot;
            using (var read = new AppDbContext())
            {
                snapshot = read.Tasks.AsNoTracking().Include(t => t.Tags).First(t => t.Id == tvm.Id);
            }

            var dlg = new Views.Dialogs.TaskEditDialog(snapshot, AvailableTags.ToList())
            {
                Owner = Application.Current?.MainWindow
            };

            if (dlg.ShowDialog() != true) return;

            var selectedTagIds = snapshot.Tags.Select(t => t.Id).ToHashSet();

            using var db = new AppDbContext();
            var entity = db.Tasks.Include(t => t.Tags).First(t => t.Id == tvm.Id);

            entity.Title = snapshot.Title;
            entity.DescriptionXaml = snapshot.DescriptionXaml;
            entity.DescriptionPlain = snapshot.DescriptionPlain;
            entity.Priority = snapshot.Priority;
            entity.DueDate = snapshot.DueDate;

            entity.Tags.Clear();
            foreach (var tag in db.Tags.Where(t => selectedTagIds.Contains(t.Id)).ToList())
                entity.Tags.Add(tag);

            db.SaveChanges();

            tvm.Title = entity.Title;
            tvm.DescriptionPlain = entity.DescriptionPlain;
            tvm.DescriptionXaml = entity.DescriptionXaml;
            tvm.Priority = entity.Priority;
            tvm.DueDate = entity.DueDate;
            tvm.LastModified = entity.LastModified;

            tvm.Tags.Clear();
            foreach (var t in entity.Tags) tvm.Tags.Add(t);
            tvm.RefreshTags();

            _messenger.Send(new TaskUpdatedMessage(entity.Id));
            Status($"Task '{entity.Title}' updated.");
        }

        public void DeleteTask(TaskViewModel? tvm)
        {
            if (tvm == null || CurrentBoard == null) return;
            if (!MessageDialog.ConfirmDelete(
                    $"Delete the task “{tvm.Title}”? This cannot be undone.",
                    "Delete Task"))
                return;

            using var db = new AppDbContext();
            var entity = db.Tasks.Find(tvm.Id);
            if (entity != null)
            {
                db.Tasks.Remove(entity);
                db.SaveChanges();
            }

            var owner = CurrentBoard.Columns.FirstOrDefault(c => c.Tasks.Contains(tvm));
            owner?.Tasks.Remove(tvm);

            if (owner != null) _messenger.Send(new TaskDeletedMessage(tvm.Id, owner.Id));
            Status($"Task '{tvm.Title}' deleted.");
        }

        public void MoveTask(TaskViewModel taskVm, ColumnViewModel fromColumn, ColumnViewModel toColumn, int newIndex)
        {
            if (taskVm == null || fromColumn == null || toColumn == null) return;
            if (fromColumn == toColumn)
            {
                int currentIndex = fromColumn.Tasks.IndexOf(taskVm);
                if (currentIndex < 0) return;
                if (newIndex < 0) newIndex = 0;
                if (newIndex > fromColumn.Tasks.Count) newIndex = fromColumn.Tasks.Count;
                if (newIndex > currentIndex) newIndex--;
                if (newIndex < 0) newIndex = 0;
                if (newIndex >= fromColumn.Tasks.Count) newIndex = fromColumn.Tasks.Count - 1;
                if (currentIndex == newIndex) return;
                fromColumn.Tasks.Move(currentIndex, newIndex);
            }
            else
            {
                fromColumn.Tasks.Remove(taskVm);
                if (newIndex < 0) newIndex = 0;
                if (newIndex > toColumn.Tasks.Count) newIndex = toColumn.Tasks.Count;
                toColumn.Tasks.Insert(newIndex, taskVm);
                taskVm.BoardColumnId = toColumn.Id;
            }

            ResequenceAndPersist(fromColumn);
            if (fromColumn != toColumn) ResequenceAndPersist(toColumn);

            _messenger.Send(new TaskMovedMessage(taskVm.Id, fromColumn.Id, toColumn.Id, newIndex));
            Status($"Moved '{taskVm.Title}' to '{toColumn.Name}'.");
        }

        private void ResequenceAndPersist(ColumnViewModel column)
        {
            for (int i = 0; i < column.Tasks.Count; i++)
                column.Tasks[i].SortOrder = i;

            using var db = new AppDbContext();
            var ids = column.Tasks.Select(t => t.Id).ToList();
            var entities = db.Tasks.Where(t => ids.Contains(t.Id)).ToList();
            var byId = entities.ToDictionary(t => t.Id);
            for (int i = 0; i < column.Tasks.Count; i++)
            {
                if (byId.TryGetValue(column.Tasks[i].Id, out var e))
                {
                    e.SortOrder = i;
                    e.BoardColumnId = column.Id;
                }
            }
            db.SaveChanges();

            foreach (var t in column.Tasks)
            {
                if (byId.TryGetValue(t.Id, out var e))
                    t.LastModified = e.LastModified;
            }

            column.TasksView.Refresh();
        }

        private void Status(string text) => StatusText = text;
    }
}
