using NewWeb.Models;

namespace NewWeb.ViewModels
{
    public class ProductDetailViewModel
    {
        public Product Product { get; set; } = default!;
        public List<Product> RelatedProducts { get; set; } = new List<Product>();
        public List<Rating> ApprovedRatings { get; set; } = new List<Rating>();
        
        // User permissions
        public bool HasPurchased { get; set; }
        public bool HasRated { get; set; }
        public bool IsLoggedIn { get; set; }
        
        // Rating statistics
        public double AverageRating => ApprovedRatings.Any() 
            ? ApprovedRatings.Average(r => r.Star ?? 0) 
            : 0;
        
        public int TotalRatings => ApprovedRatings.Count;
        
        public int FiveStarCount => ApprovedRatings.Count(r => r.Star == 5);
        public int FourStarCount => ApprovedRatings.Count(r => r.Star == 4);
        public int ThreeStarCount => ApprovedRatings.Count(r => r.Star == 3);
        public int TwoStarCount => ApprovedRatings.Count(r => r.Star == 2);
        public int OneStarCount => ApprovedRatings.Count(r => r.Star == 1);
        
        // Calculated properties
        public decimal? OriginalPrice
        {
            get
            {
                if (Product.Discount > 0 && Product.SellingPrice.HasValue)
                {
                    return Product.SellingPrice.Value / (1 - (Product.Discount.Value / 100));
                }
                return null;
            }
        }
        
        public decimal? SavedAmount
        {
            get
            {
                if (OriginalPrice.HasValue && Product.SellingPrice.HasValue)
                {
                    return OriginalPrice.Value - Product.SellingPrice.Value;
                }
                return null;
            }
        }
        
        public bool IsInStock => Product.Quantity > 0;
        public bool IsLowStock => Product.Quantity > 0 && Product.Quantity <= 10;
    }
}

