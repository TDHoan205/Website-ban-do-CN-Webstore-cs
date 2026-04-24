using Webstore.Models;

namespace Webstore.Services
{
    public interface IAccountService
    {
        Task<Account?> GetAccountByIdAsync(int id);
        Task<Account?> GetAccountByUsernameAsync(string username);
        Task<Account?> GetAccountByEmailAsync(string email);
        Task UpdateProfileAsync(int accountId, string fullName, string? email, string phone, string? address);
        Task<bool> ValidateCredentialsAsync(string username, string password);
        Task RegisterAsync(Account account, string password);

        // Forgot password methods
        Task<bool> GenerateResetTokenAsync(string email);
        Task<bool> ValidateResetTokenAsync(string email, string token);
        Task<bool> ResetPasswordAsync(string email, string token, string newPassword);
    }
}
