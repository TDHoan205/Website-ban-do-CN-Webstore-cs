namespace Webstore.Data.Repositories
{
    public interface IProductRepository : IRepository<Models.Product>
    {
        Task<IEnumerable<Models.Product>> GetProductsWithDetailsAsync();
        Task<Models.Product?> GetProductWithDetailsAsync(int id);
    }

    public interface IOrderRepository : IRepository<Models.Order>
    {
        Task<IEnumerable<Models.Order>> GetOrdersByAccountIdAsync(int accountId);
        Task<Models.Order?> GetOrderWithDetailsAsync(int id);
    }
    
    public interface ICategoryRepository : IRepository<Models.Category> { }
    public interface IInventoryRepository : IRepository<Models.Inventory> { }
    public interface ISupplierRepository : IRepository<Models.Supplier> { }
    public interface IAccountRepository : IRepository<Models.Account> { }
    public interface IEmployeeRepository : IRepository<Models.Employee> { }
}
