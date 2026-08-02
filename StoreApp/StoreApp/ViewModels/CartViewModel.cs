using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using StoreApp.Enums;
using StoreApp.Models.DTOs;
using StoreApp.Services;

namespace StoreApp.ViewModels
{
    public class CartViewModel : BaseViewModel
    {
        private readonly ICartService _cart;
        private readonly IOrderService _orders;
        private readonly ICustomerService _customers;

        private CustomerDto? _selectedCustomer;
        private PaymentMethod _paymentMethod = PaymentMethod.CreditCard;
        private decimal _discount;
        private string _notes = string.Empty;

        public CartViewModel(ICartService cart, IOrderService orders, ICustomerService customers)
        {
            _cart = cart;
            _orders = orders;
            _customers = customers;

            PaymentMethods = new ObservableCollection<PaymentMethod>(Enum.GetValues<PaymentMethod>());
            Customers = new ObservableCollection<CustomerDto>(_customers.GetAll());

            _cart.Changed += SyncFromService;
            SyncFromService();

            IncreaseCommand = new RelayCommand(p => { if (p is CartItemDto i) _cart.UpdateQuantity(i.ProductId, i.Quantity + 1); });
            DecreaseCommand = new RelayCommand(p => { if (p is CartItemDto i) _cart.UpdateQuantity(i.ProductId, i.Quantity - 1); });
            RemoveCommand = new RelayCommand(p => { if (p is CartItemDto i) _cart.Remove(i.ProductId); });
            ClearCommand = new RelayCommand(_ => _cart.Clear());
            CheckoutCommand = new RelayCommand(_ => Checkout(), _ => Items.Count > 0 && SelectedCustomer != null);
        }

        public ObservableCollection<CartItemDto> Items { get; } = new();
        public ObservableCollection<PaymentMethod> PaymentMethods { get; }
        public ObservableCollection<CustomerDto> Customers { get; }

        public CustomerDto? SelectedCustomer { get => _selectedCustomer; set => SetProperty(ref _selectedCustomer, value); }
        public PaymentMethod PaymentMethod { get => _paymentMethod; set => SetProperty(ref _paymentMethod, value); }
        public decimal Discount { get => _discount; set { if (SetProperty(ref _discount, value)) RecalcTotals(); } }
        public string Notes { get => _notes; set => SetProperty(ref _notes, value); }

        public decimal Subtotal => _cart.Subtotal;
        public decimal Tax => Math.Round(Subtotal * 0.10m, 2);
        public decimal Shipping => Subtotal == 0 ? 0 : (Subtotal >= 100 ? 0 : 9.99m);
        public decimal Total => Math.Max(0, Subtotal + Tax + Shipping - Discount);
        public int TotalItems => _cart.TotalItems;

        public ICommand IncreaseCommand { get; }
        public ICommand DecreaseCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand CheckoutCommand { get; }

        public event Action? CheckoutCompleted;

        private void SyncFromService()
        {
            Items.Clear();
            foreach (var i in _cart.Items) Items.Add(i);
            RecalcTotals();
        }

        private void RecalcTotals()
        {
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(Tax));
            OnPropertyChanged(nameof(Shipping));
            OnPropertyChanged(nameof(Total));
            OnPropertyChanged(nameof(TotalItems));
        }

        private void Checkout()
        {
            try
            {
                if (SelectedCustomer == null) return;
                var order = _orders.Checkout(SelectedCustomer.Id, _cart.Items, PaymentMethod, Discount, Notes);
                _cart.Clear();
                Discount = 0;
                Notes = string.Empty;
                MessageBox.Show($"Order {order.OrderNumber} placed.\nTotal: {order.Total:C}", "Checkout complete", MessageBoxButton.OK, MessageBoxImage.Information);
                CheckoutCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Checkout failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void RefreshCustomers()
        {
            Customers.Clear();
            foreach (var c in _customers.GetAll()) Customers.Add(c);
        }
    }
}
