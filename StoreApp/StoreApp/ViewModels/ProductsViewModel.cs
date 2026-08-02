using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using StoreApp.Enums;
using StoreApp.Models.DTOs;
using StoreApp.Services;

namespace StoreApp.ViewModels
{
    public class ProductsViewModel : BaseViewModel
    {
        private readonly IProductService _service;
        private readonly ICartService _cart;

        private string _searchTerm = string.Empty;
        private ProductCategory? _selectedCategory;
        private ProductDto? _selected;
        private ProductDto _editing = new();
        private bool _isEditing;

        public ProductsViewModel(IProductService service, ICartService cart)
        {
            _service = service;
            _cart = cart;

            Categories = new ObservableCollection<ProductCategory?> { null };
            foreach (var c in Enum.GetValues<ProductCategory>()) Categories.Add(c);

            Statuses = new ObservableCollection<ProductStatus>(Enum.GetValues<ProductStatus>());
            CategoryOptions = new ObservableCollection<ProductCategory>(Enum.GetValues<ProductCategory>());

            SearchCommand = new RelayCommand(_ => Reload());
            ClearFiltersCommand = new RelayCommand(_ =>
            {
                SearchTerm = string.Empty;
                SelectedCategory = null;
                Reload();
            });
            AddNewCommand = new RelayCommand(_ => StartNew());
            EditCommand = new RelayCommand(_ => StartEdit(), _ => Selected != null);
            SaveCommand = new RelayCommand(_ => Save(), _ => IsEditing);
            CancelEditCommand = new RelayCommand(_ => CancelEdit());
            DeleteCommand = new RelayCommand(_ => Delete(), _ => Selected != null);
            UploadImageCommand = new RelayCommand(_ => UploadImage(), _ => IsEditing);
            AddToCartCommand = new RelayCommand(p =>
            {
                if (p is ProductDto product && product.InStock) _cart.Add(product);
            });

            Reload();
        }

        public ObservableCollection<ProductDto> Products { get; } = new();
        public ObservableCollection<ProductCategory?> Categories { get; }
        public ObservableCollection<ProductCategory> CategoryOptions { get; }
        public ObservableCollection<ProductStatus> Statuses { get; }

        public string SearchTerm { get => _searchTerm; set => SetProperty(ref _searchTerm, value); }
        public ProductCategory? SelectedCategory { get => _selectedCategory; set { if (SetProperty(ref _selectedCategory, value)) Reload(); } }
        public ProductDto? Selected { get => _selected; set => SetProperty(ref _selected, value); }
        public ProductDto Editing { get => _editing; set => SetProperty(ref _editing, value); }
        public bool IsEditing { get => _isEditing; set => SetProperty(ref _isEditing, value); }

        public ICommand SearchCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand AddNewCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand UploadImageCommand { get; }
        public ICommand AddToCartCommand { get; }

        public void Reload()
        {
            Products.Clear();
            foreach (var p in _service.Search(SearchTerm, SelectedCategory, null, null))
                Products.Add(p);
        }

        private void StartNew()
        {
            Editing = new ProductDto { Status = ProductStatus.Active, Category = ProductCategory.Other, Stock = 0 };
            IsEditing = true;
        }

        private void StartEdit()
        {
            if (Selected == null) return;
            Editing = new ProductDto
            {
                Id = Selected.Id,
                Sku = Selected.Sku,
                Name = Selected.Name,
                Description = Selected.Description,
                Brand = Selected.Brand,
                Category = Selected.Category,
                Status = Selected.Status,
                Price = Selected.Price,
                DiscountPrice = Selected.DiscountPrice,
                Stock = Selected.Stock,
                Rating = Selected.Rating,
                ReviewsCount = Selected.ReviewsCount,
                ImageUrl = Selected.ImageUrl
            };
            IsEditing = true;
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(Editing.Name) || string.IsNullOrWhiteSpace(Editing.Sku))
            {
                MessageBox.Show("SKU and Name are required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (Editing.Id == 0) _service.Create(Editing);
            else _service.Update(Editing);
            IsEditing = false;
            Reload();
        }

        private void CancelEdit()
        {
            IsEditing = false;
        }

        private void UploadImage()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select product image",
                Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp|All files|*.*"
            };
            if (dlg.ShowDialog() != true) return;
            Editing.ImageUrl = dlg.FileName;
            OnPropertyChanged(nameof(Editing));
        }

        private void Delete()
        {
            if (Selected == null) return;
            var result = MessageBox.Show($"Delete '{Selected.Name}'?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;
            _service.Delete(Selected.Id);
            Reload();
        }
    }
}
