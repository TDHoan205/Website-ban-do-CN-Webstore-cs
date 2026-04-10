using Microsoft.EntityFrameworkCore;

namespace Webstore.Models
{
    public class PagedList<T> : List<T>
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;

        public PagedList(List<T> items, int count, int pageNumber, int pageSize)
        {
            TotalCount = count;
            PageSize = pageSize;
            CurrentPage = pageNumber;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            AddRange(items);
        }

        // ✅ Đồng bộ
        public static PagedList<T> Create(IQueryable<T> source, int pageNumber, int pageSize)
        {
            var count = source.Count();
            var items = source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            return new PagedList<T>(items, count, pageNumber, pageSize);
        }

        // ✅ Bất đồng bộ (cho EF Core)
        public static async Task<PagedList<T>> CreateAsync(IQueryable<T> source, int pageNumber, int pageSize)
        {
            // If the incoming IQueryable is backed by EF Core's provider this will run asynchronously.
            // If not (e.g. in-memory queries during tests), fall back to synchronous execution.
            int count;
            List<T> items;
            try
            {
                count = await source.CountAsync();
                items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is NotSupportedException || ex is MissingMethodException)
            {
                // Provider doesn't support async extensions (e.g. in-memory IQueryable) — execute synchronously.
                count = source.Count();
                items = source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            }
            return new PagedList<T>(items, count, pageNumber, pageSize);
        }
    }
}
