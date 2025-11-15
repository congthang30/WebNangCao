using NewWeb.Models;

namespace NewWeb.ViewModels
{
    public class HomeViewModel
    {
        public List<Product> Products { get; set; } = new List<Product>();
        public List<Category> Categories { get; set; } = new List<Category>();
        public List<Brand> Brands { get; set; } = new List<Brand>();
        
        // Filter parameters
        public string? SearchQuery { get; set; }
        public int? SelectedCategoryId { get; set; }
        public int? SelectedBrandId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        
        // Statistics
        public int TotalProducts { get; set; }
        public int FeaturedProductsCount { get; set; }
        public bool HasFilters => !string.IsNullOrEmpty(SearchQuery) 
            || SelectedCategoryId.HasValue 
            || SelectedBrandId.HasValue 
            || MinPrice.HasValue 
            || MaxPrice.HasValue;
    }
}

