using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using StoreApp.Models.DTOs;
using StoreApp.Services;

namespace StoreApp.ViewModels
{
    public class CustomersViewModel : BaseViewModel
    {
        private readonly ICustomerService _service;
        private string _searchTerm = string.Empty;
        private CustomerDto? _selected;
        private CustomerDto _editing = new();
        private bool _isEditing;

        public CustomersViewModel(ICustomerService service)
        {
            _service = service;

            SearchCommand = new RelayCommand(_ => Reload());
            AddNewCommand = new RelayCommand(_ => { Editing = new CustomerDto(); IsEditing = true; });
            EditCommand = new RelayCommand(_ =>
            {
                if (Selected == null) return;
                Editing = new CustomerDto
                {
                    Id = Selected.Id,
                    FullName = Selected.FullName,
                    Email = Selected.Email,
                    Phone = Selected.Phone,
                    Address = Selected.Address,
                    City = Selected.City,
                    Country = Selected.Country
                };
                IsEditing = true;
            }, _ => Selected != null);
            SaveCommand = new RelayCommand(_ => Save(), _ => IsEditing);
            CancelEditCommand = new RelayCommand(_ => IsEditing = false);
            DeleteCommand = new RelayCommand(_ => Delete(), _ => Selected != null);

            Reload();
        }

        public ObservableCollection<CustomerDto> Customers { get; } = new();

        public string SearchTerm { get => _searchTerm; set => SetProperty(ref _searchTerm, value); }
        public CustomerDto? Selected { get => _selected; set => SetProperty(ref _selected, value); }
        public CustomerDto Editing { get => _editing; set => SetProperty(ref _editing, value); }
        public bool IsEditing { get => _isEditing; set => SetProperty(ref _isEditing, value); }

        public ICommand SearchCommand { get; }
        public ICommand AddNewCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand DeleteCommand { get; }

        public void Reload()
        {
            Customers.Clear();
            foreach (var c in _service.Search(SearchTerm)) Customers.Add(c);
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(Editing.FullName))
            {
                MessageBox.Show("Full name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (Editing.Id == 0) _service.Create(Editing);
            else _service.Update(Editing);
            IsEditing = false;
            Reload();
        }

        private void Delete()
        {
            if (Selected == null) return;
            if (MessageBox.Show($"Delete customer '{Selected.FullName}'?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            _service.Delete(Selected.Id);
            Reload();
        }
    }
}
