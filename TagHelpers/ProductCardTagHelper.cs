using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Razor.TagHelpers;
using NewWeb.Models;

/*Tag Helper dùng để thêm logic server-side vào thẻ HTML trong Razor View, giúp:

Tạo URL đúng từ controller/action

Bind input với Model

Hiển thị validation

Tạo form/link động

Code Razor gọn, dễ đọc hơn*/

namespace NewWeb.TagHelpers;

[HtmlTargetElement("product-card", Attributes = "product")]
public class ProductCardTagHelper : TagHelper
{
    public Product? Product { get; set; }

    /// <summary>
    /// Hiển thị nút hành động (ví dụ wishlist).
    /// </summary>
    public bool ShowActions { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (Product == null)
        {
            output.SuppressOutput();
            return;
        }

        var detailUrl = $"/Products/Detail?id={Product.ProductId}";
        var description = BuildDescription(Product.Description);
        var price = Product.SellingPrice?.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));
        var quantity = Product.Quantity ?? 0;

        var builder = new StringBuilder();
        builder.AppendLine("""<div class="product-card-modern">""");
        builder.AppendLine($"""    <a href="{detailUrl}" class="product-link">""");
        builder.AppendLine("""        <div class="product-image-container">""");
        builder.AppendLine(BuildImageMarkup(Product));
        builder.AppendLine(BuildBadges(Product));
        builder.AppendLine("        </div>");
        builder.AppendLine("        <div class=\"product-info\">");
        builder.AppendLine($"            <h3 class=\"product-title\">{Product.ProductName}</h3>");
        if (!string.IsNullOrWhiteSpace(description))
        {
            builder.AppendLine("            <p class=\"product-description\">");
            builder.AppendLine($"                {description}");
            builder.AppendLine("            </p>");
        }
        builder.AppendLine("            <div class=\"d-flex justify-content-between align-items-center\">");
        builder.AppendLine("                <div class=\"product-price\">");
        if (!string.IsNullOrEmpty(price))
        {
            builder.AppendLine($"                    <span>{price} ₫</span>");
        }
        builder.AppendLine("                </div>");
        builder.AppendLine(BuildStockMarkup(quantity));
        builder.AppendLine("            </div>");
        builder.AppendLine("        </div>");
        builder.AppendLine("    </a>");

        if (ShowActions)
        {
            builder.AppendLine(
                $"""    <button type="button" class="wishlist-btn" data-product-id="{Product.ProductId}" title="Thêm vào yêu thích">""");
            builder.AppendLine("        <i class=\"bi bi-heart\"></i>");
            builder.AppendLine("    </button>");
        }

        builder.AppendLine("</div>");

        output.TagName = null;
        output.Content.SetHtmlContent(builder.ToString());
    }

    private static string BuildImageMarkup(Product product)
    {
        if (!string.IsNullOrEmpty(product.ImagePr))
        {
            return
                $"""            <img src="{product.ImagePr}" alt="{product.ProductName}" loading="lazy" onerror="this.src='/images/no-image.svg';" />""";
        }

        return """            <img src="/images/no-image.svg" alt="Không có ảnh" />""";
    }

    private static string BuildBadges(Product product)
    {
        var builder = new StringBuilder();
        if (product.IsFeatured == true)
        {
            builder.AppendLine("""
            <span class="badge bg-warning text-dark position-absolute top-0 start-0 m-2">
                <i class="bi bi-star-fill"></i> Nổi bật
            </span>
""");
        }

        if (product.Discount.GetValueOrDefault() > 0)
        {
            builder.AppendLine($"""
            <span class="badge bg-danger position-absolute top-0 end-0 m-2">
                -{(int)product.Discount.GetValueOrDefault()}%
            </span>
""");
        }

        return builder.ToString();
    }

    private static string BuildStockMarkup(int quantity)
    {
        if (quantity > 0 && quantity <= 10)
        {
            return $"""
                <small class="text-warning">
                    <i class="bi bi-exclamation-triangle"></i> Còn {quantity}
                </small>
""";
        }

        if (quantity > 10)
        {
            return """
                <small class="text-success">
                    <i class="bi bi-check-circle"></i> Còn hàng
                </small>
""";
        }

        return """
                <small class="text-danger">
                    <i class="bi bi-x-circle"></i> Hết hàng
                </small>
""";
    }

    private static string? BuildDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var trimmed = description.Trim();
        return trimmed.Length > 80 ? $"{trimmed[..80]}..." : trimmed;
    }
}

