using System.Collections.ObjectModel;
using System.Linq;
using PersonalTaskManagement.Messaging;
using PersonalTaskManagement.Models;

namespace PersonalTaskManagement.ViewModels
{
    public class BoardViewModel : BaseViewModel
    {
        private readonly Board _model;
        private readonly IMessenger _messenger;

        public BoardViewModel(Board model, IMessenger messenger)
        {
            _model = model;
            _messenger = messenger;
            Columns = new ObservableCollection<ColumnViewModel>(
                model.Columns.OrderBy(c => c.SortOrder).Select(c => new ColumnViewModel(c, messenger)));
        }

        public Board Model => _model;

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

        public ObservableCollection<ColumnViewModel> Columns { get; }

        public void Detach()
        {
            foreach (var c in Columns) c.Detach();
        }
    }
}
