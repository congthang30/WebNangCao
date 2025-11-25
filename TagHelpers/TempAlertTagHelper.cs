using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace NewWeb.TagHelpers;

[HtmlTargetElement("temp-alert")]
public class TempAlertTagHelper : TagHelper
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITempDataDictionaryFactory _tempDataFactory;

    private static readonly Dictionary<string, (string Css, string Icon)> AlertStyles =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["error"] = ("danger", "bi-exclamation-triangle"),
            ["success"] = ("success", "bi-check-circle"),
            ["warning"] = ("warning", "bi-exclamation-circle"),
            ["info"] = ("info", "bi-info-circle")
        };

    public TempAlertTagHelper(
        IHttpContextAccessor httpContextAccessor,
        ITempDataDictionaryFactory tempDataFactory)
    {
        _httpContextAccessor = httpContextAccessor;
        _tempDataFactory = tempDataFactory;
    }

    /// <summary>
    /// Danh sách key TempData, phân tách bằng dấu phẩy. Nếu bỏ trống sẽ duyệt tất cả key hiện có.
    /// </summary>
    public string? Keys { get; set; }

    /// <summary>
    /// Thêm class tùy ý cho phần tử alert.
    /// </summary>
    public string? CssClass { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            output.SuppressOutput();
            return;
        }

        var tempData = _tempDataFactory.GetTempData(httpContext);
        if (tempData == null || tempData.Count == 0)
        {
            output.SuppressOutput();
            return;
        }

        var keysToRender = GetKeysToRender(tempData.Keys);
        var contentBuilder = new StringBuilder();

        foreach (var key in keysToRender)
        {
            if (!tempData.TryGetValue(key, out var rawValue) || rawValue == null)
            {
                continue;
            }

            var message = rawValue.ToString();
            if (string.IsNullOrWhiteSpace(message))
            {
                continue;
            }

            var (alertCss, icon) = AlertStyles.TryGetValue(key, out var style)
                ? style
                : ("secondary", "bi-info-circle");

            contentBuilder.AppendLine($"""
<div class="alert alert-{alertCss} alert-dismissible fade show {CssClass}".Trim() role="alert">
    <i class="bi {icon} me-2"></i>{message}
    <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
</div>
""");
        }

        if (contentBuilder.Length == 0)
        {
            output.SuppressOutput();
            return;
        }

        output.TagName = null;
        output.Content.SetHtmlContent(contentBuilder.ToString());
    }

    private IEnumerable<string> GetKeysToRender(IEnumerable<string> availableKeys)
    {
        if (!string.IsNullOrWhiteSpace(Keys))
        {
            return Keys.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        return availableKeys;
    }
}

