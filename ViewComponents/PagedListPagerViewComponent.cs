using Microsoft.AspNetCore.Mvc;
using Webstore.Models;

namespace Webstore.ViewComponents;

public class PagedListPagerViewComponent : ViewComponent
{
    public Task<IViewComponentResult> InvokeAsync(
        int currentPage,
        int totalPages,
        int totalCount,
        int pageSize,
        bool hasPrevious,
        bool hasNext,
        string? search = null,
        string? sortOrder = null,
        string? controllerName = null,
        string? actionName = null,
        string containerClass = "")
    {
        var vm = new PagerViewModel
        {
            CurrentPage = currentPage,
            TotalPages = totalPages,
            TotalCount = totalCount,
            PageSize = pageSize,
            HasPrevious = hasPrevious,
            HasNext = hasNext,
            Search = search,
            SortOrder = sortOrder,
            ContainerClass = containerClass,
            ControllerName = controllerName ?? Request.RouteValues["controller"]?.ToString() ?? "Home",
            ActionName = actionName ?? Request.RouteValues["action"]?.ToString() ?? "Index"
        };

        return Task.FromResult<IViewComponentResult>(View(vm));
    }

    public Task<IViewComponentResult> InvokeWithModel(PagedList<object> model, string? search = null, string? sortOrder = null, string containerClass = "")
    {
        return InvokeAsync(
            model.CurrentPage,
            model.TotalPages,
            model.TotalCount,
            model.PageSize,
            model.HasPrevious,
            model.HasNext,
            search,
            sortOrder,
            null,
            null,
            containerClass);
    }
}

public class PagerViewModel
{
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
    public string? Search { get; set; }
    public string? SortOrder { get; set; }
    public string ContainerClass { get; set; } = "";
    public string ControllerName { get; set; } = "Home";
    public string ActionName { get; set; } = "Index";

    public string GetPageUrl(int pageNumber)
    {
        var qp = new Dictionary<string, string?>
        {
            ["pageNumber"] = pageNumber.ToString(),
            ["search"] = Search,
            ["sortOrder"] = SortOrder,
            ["pageSize"] = PageSize.ToString()
        };
        var qs = string.Join("&", qp.Where(kv => !string.IsNullOrEmpty(kv.Value))
                                     .Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value ?? "")}"));
        return $"/{ControllerName}/{ActionName}" + (string.IsNullOrEmpty(qs) ? "" : $"?{qs}");
    }

    public string GetPageSizeUrl(int size)
    {
        var qp = new Dictionary<string, string?>
        {
            ["pageNumber"] = "1",
            ["search"] = Search,
            ["sortOrder"] = SortOrder,
            ["pageSize"] = size.ToString()
        };
        var qs = string.Join("&", qp.Where(kv => kv.Value != null)
                                     .Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value ?? "")}"));
        return $"/{ControllerName}/{ActionName}" + (string.IsNullOrEmpty(qs) ? "" : $"?{qs}");
    }
}
