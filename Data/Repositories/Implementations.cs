using Microsoft.EntityFrameworkCore;
using Webstore.Models;
using Webstore.Models.AI;

namespace Webstore.Data.Repositories
{
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        public ProductRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Product>> GetProductsWithDetailsAsync()
        {
            return await _dbSet.Include(p => p.Category).Include(p => p.Supplier).ToListAsync();
        }

        public async Task<Product?> GetProductWithDetailsAsync(int id)
        {
            return await _dbSet.Include(p => p.Category).Include(p => p.Supplier)
                               .FirstOrDefaultAsync(p => p.ProductId == id);
        }
    }

    public class OrderRepository : GenericRepository<Order>, IOrderRepository
    {
        public OrderRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Order>> GetOrdersByAccountIdAsync(int accountId)
        {
            return await _dbSet.Where(o => o.AccountId == accountId)
                               .OrderByDescending(o => o.OrderDate)
                               .ToListAsync();
        }

        public async Task<Order?> GetOrderWithDetailsAsync(int id)
        {
            return await _dbSet.Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                               .Include(o => o.Account)
                               .FirstOrDefaultAsync(o => o.OrderId == id);
        }
    }

    public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
    {
        public CategoryRepository(ApplicationDbContext context) : base(context) { }
    }

    public class InventoryRepository : GenericRepository<Inventory>, IInventoryRepository
    {
        public InventoryRepository(ApplicationDbContext context) : base(context) { }
    }

    public class SupplierRepository : GenericRepository<Supplier>, ISupplierRepository
    {
        public SupplierRepository(ApplicationDbContext context) : base(context) { }
    }

    public class AccountRepository : GenericRepository<Account>, IAccountRepository
    {
        public AccountRepository(ApplicationDbContext context) : base(context) { }
    }

    public class EmployeeRepository : GenericRepository<Employee>, IEmployeeRepository
    {
        public EmployeeRepository(ApplicationDbContext context) : base(context) { }
    }
}
