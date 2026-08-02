using System.Collections.ObjectModel;
using StoreApp.Models.DTOs;
using StoreApp.Services;

namespace StoreApp.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly IDashboardService _dashboard;

        public DashboardViewModel(IDashboardService dashboard)
        {
            _dashboard = dashboard;
            Refresh();
        }

        public ObservableCollection<ProductDto> TopProducts { get; } = new();
        public ObservableCollection<OrderDto> RecentOrders { get; } = new();

        private DashboardDto _data = new();
        public DashboardDto Data { get => _data; set => SetProperty(ref _data, value); }

        public void Refresh()
        {
            Data = _dashboard.GetDashboard();
            TopProducts.Clear();
            foreach (var p in Data.TopProducts) TopProducts.Add(p);
            RecentOrders.Clear();
            foreach (var o in Data.RecentOrders) RecentOrders.Add(o);
        }
    }
}
