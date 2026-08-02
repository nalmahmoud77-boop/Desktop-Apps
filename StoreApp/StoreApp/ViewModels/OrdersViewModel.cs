using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using StoreApp.Enums;
using StoreApp.Models.DTOs;
using StoreApp.Services;

namespace StoreApp.ViewModels
{
    public class OrdersViewModel : BaseViewModel
    {
        private readonly IOrderService _orders;
        private OrderDto? _selected;
        private OrderStatus? _filterStatus;

        public OrdersViewModel(IOrderService orders)
        {
            _orders = orders;
            StatusFilters = new ObservableCollection<OrderStatus?> { null };
            foreach (var s in Enum.GetValues<OrderStatus>()) StatusFilters.Add(s);
            StatusOptions = new ObservableCollection<OrderStatus>(Enum.GetValues<OrderStatus>());

            RefreshCommand = new RelayCommand(_ => Reload());
            UpdateStatusCommand = new RelayCommand(p =>
            {
                if (Selected == null || p is not OrderStatus s) return;
                _orders.UpdateStatus(Selected.Id, s);
                Reload();
            }, _ => Selected != null);
            CancelOrderCommand = new RelayCommand(_ =>
            {
                if (Selected == null) return;
                if (MessageBox.Show($"Cancel order {Selected.OrderNumber}?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                _orders.Cancel(Selected.Id);
                Reload();
            }, _ => Selected != null && Selected.Status != OrderStatus.Cancelled && Selected.Status != OrderStatus.Delivered);

            Reload();
        }

        public ObservableCollection<OrderDto> Orders { get; } = new();
        public ObservableCollection<OrderStatus?> StatusFilters { get; }
        public ObservableCollection<OrderStatus> StatusOptions { get; }

        public OrderDto? Selected { get => _selected; set => SetProperty(ref _selected, value); }
        public OrderStatus? FilterStatus { get => _filterStatus; set { if (SetProperty(ref _filterStatus, value)) Reload(); } }

        public ICommand RefreshCommand { get; }
        public ICommand UpdateStatusCommand { get; }
        public ICommand CancelOrderCommand { get; }

        public void Reload()
        {
            Orders.Clear();
            var data = FilterStatus.HasValue ? _orders.GetByStatus(FilterStatus.Value) : _orders.GetAll();
            foreach (var o in data) Orders.Add(o);
        }
    }
}
