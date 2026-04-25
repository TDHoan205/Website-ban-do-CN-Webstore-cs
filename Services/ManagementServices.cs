using Webstore.Data.Repositories;
using Webstore.Models;

namespace Webstore.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly IInventoryRepository _repository;

        public InventoryService(IInventoryRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<Inventory>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<Inventory?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<Inventory?> GetByProductIdAsync(int productId)
        {
            var results = await _repository.FindAsync(i => i.ProductId == productId);
            return results.FirstOrDefault();
        }

        public async Task UpdateStockAsync(int productId, int quantityChange)
        {
            var inventory = await GetByProductIdAsync(productId);
            if (inventory != null)
            {
                inventory.StockQuantity += quantityChange;
                inventory.LastUpdated = DateTime.Now;
                _repository.Update(inventory);
                await _repository.SaveChangesAsync();
            }
        }

        public async Task SetStockAsync(int inventoryId, int quantity)
        {
            var inventory = await _repository.GetByIdAsync(inventoryId);
            if (inventory != null)
            {
                inventory.StockQuantity = quantity;
                inventory.LastUpdated = DateTime.Now;
                _repository.Update(inventory);
                await _repository.SaveChangesAsync();
            }
        }

        public async Task<bool> IsInStockAsync(int productId, int requestedQuantity)
        {
            var inventory = await GetByProductIdAsync(productId);
            return inventory != null && inventory.StockQuantity >= requestedQuantity;
        }

        public async Task CreateAsync(Inventory inventory)
        {
            await _repository.AddAsync(inventory);
            await _repository.SaveChangesAsync();
        }

        public async Task UpdateAsync(Inventory inventory)
        {
            inventory.LastUpdated = DateTime.Now;
            _repository.Update(inventory);
            await _repository.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var inventory = await _repository.GetByIdAsync(id);
            if (inventory != null)
            {
                _repository.Remove(inventory);
                await _repository.SaveChangesAsync();
            }
        }
    }

    public class SupplierService : ISupplierService
    {
        private readonly ISupplierRepository _repository;

        public SupplierService(ISupplierRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<Supplier>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<Supplier?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<Supplier> CreateAsync(Supplier supplier)
        {
            await _repository.AddAsync(supplier);
            await _repository.SaveChangesAsync();
            return supplier;
        }

        public async Task UpdateAsync(Supplier supplier)
        {
            _repository.Update(supplier);
            await _repository.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var supplier = await _repository.GetByIdAsync(id);
            if (supplier != null)
            {
                _repository.Remove(supplier);
                await _repository.SaveChangesAsync();
            }
        }
    }

    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repository;

        public CategoryService(ICategoryRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<Category>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<Category> CreateAsync(Category category)
        {
            await _repository.AddAsync(category);
            await _repository.SaveChangesAsync();
            return category;
        }

        public async Task UpdateAsync(Category category)
        {
            _repository.Update(category);
            await _repository.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var category = await _repository.GetByIdAsync(id);
            if (category != null)
            {
                _repository.Remove(category);
                await _repository.SaveChangesAsync();
            }
        }
    }
}
