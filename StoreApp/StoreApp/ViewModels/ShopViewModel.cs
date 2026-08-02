using System.Collections.ObjectModel;
using System.Windows.Input;
using StoreApp.Enums;
using StoreApp.Models.DTOs;
using StoreApp.Services;

namespace StoreApp.ViewModels
{
    public class ShopViewModel : BaseViewModel
    {
        private readonly IProductService _service;
        private readonly ICartService _cart;

        private string _searchTerm = string.Empty;
        private ProductCategory? _selectedCategory;
        private string _sortBy = "Featured";

        public ShopViewModel(IProductService service, ICartService cart)
        {
            _service = service;
            _cart = cart;

            Categories = new ObservableCollection<ProductCategory?> { null };
            foreach (var c in Enum.GetValues<ProductCategory>()) Categories.Add(c);

            SortOptions = new ObservableCollection<string> { "Featured", "Price: Low to High", "Price: High to Low", "Top Rated", "Newest" };

            SearchCommand = new RelayCommand(_ => Reload());
            AddToCartCommand = new RelayCommand(p =>
            {
                if (p is ProductDto product && product.InStock) _cart.Add(product);
            });

            Reload();
        }

        public ObservableCollection<ProductDto> Products { get; } = new();
        public ObservableCollection<ProductCategory?> Categories { get; }
        public ObservableCollection<string> SortOptions { get; }

        public string SearchTerm { get => _searchTerm; set => SetProperty(ref _searchTerm, value); }
        public ProductCategory? SelectedCategory { get => _selectedCategory; set { if (SetProperty(ref _selectedCategory, value)) Reload(); } }
        public string SortBy { get => _sortBy; set { if (SetProperty(ref _sortBy, value)) Reload(); } }

        public ICommand SearchCommand { get; }
        public ICommand AddToCartCommand { get; }

        private void Reload()
        {
            var data = _service.Search(SearchTerm, SelectedCategory, null, null);
            data = SortBy switch
            {
                "Price: Low to High" => data.OrderBy(p => p.EffectivePrice),
                "Price: High to Low" => data.OrderByDescending(p => p.EffectivePrice),
                "Top Rated" => data.OrderByDescending(p => p.Rating).ThenByDescending(p => p.ReviewsCount),
                "Newest" => data.OrderByDescending(p => p.Id),
                _ => data.OrderByDescending(p => p.HasDiscount).ThenByDescending(p => p.Rating)
            };
            Products.Clear();
            foreach (var p in data) Products.Add(p);
        }
    }
}
