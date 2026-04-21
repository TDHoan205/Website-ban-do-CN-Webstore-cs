using Webstore.Models;

namespace Webstore.Services
{
    public interface IAccountService
    {
        Task<Account?> GetAccountByIdAsync(int id);
        Task<Account?> GetAccountByUsernameAsync(string username);
        Task UpdateProfileAsync(int accountId, string fullName, string? email, string phone, string? address);
        Task<bool> ValidateCredentialsAsync(string username, string password);
        Task RegisterAsync(Account account, string password);
    }
}
