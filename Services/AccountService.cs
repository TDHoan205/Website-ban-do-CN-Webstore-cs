using Webstore.Data.Repositories;
using Webstore.Models;
using System.Security.Cryptography;

namespace Webstore.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepository;

        public AccountService(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public async Task<Account?> GetAccountByIdAsync(int id)
        {
            return await _accountRepository.GetByIdAsync(id);
        }

        public async Task<Account?> GetAccountByUsernameAsync(string username)
        {
            var accounts = await _accountRepository.FindAsync(a => a.Username == username);
            return accounts.FirstOrDefault();
        }

        public async Task<Account?> GetAccountByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var accounts = await _accountRepository.FindAsync(a => a.Email != null && a.Email.Trim().ToLower() == normalizedEmail);
            return accounts.FirstOrDefault();
        }

        public async Task UpdateProfileAsync(int accountId, string fullName, string? email, string phone, string? address)
        {
            var account = await _accountRepository.GetByIdAsync(accountId);
            if (account != null)
            {
                account.FullName = fullName;
                account.Email = email;
                account.Phone = phone;
                account.Address = address;

                _accountRepository.Update(account);
                await _accountRepository.SaveChangesAsync();
            }
        }

        public async Task<bool> ValidateCredentialsAsync(string usernameOrEmail, string password)
        {
            if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrWhiteSpace(password))
                return false;

            // Try to find by username first, then by email
            var account = await GetAccountByUsernameAsync(usernameOrEmail.Trim());
            if (account == null)
            {
                account = await GetAccountByEmailAsync(usernameOrEmail);
            }

            if (account == null) return false;

            if (!string.IsNullOrEmpty(account.PasswordHash) && account.PasswordHash.Contains(':'))
            {
                var parts = account.PasswordHash.Split(':');
                if (parts.Length == 2)
                {
                    var salt = parts[0];
                    var storedHash = parts[1];
                    var inputHash = Webstore.Models.Security.PasswordHasher.HashPassword(password, salt);
                    return CryptographicOperations.FixedTimeEquals(Convert.FromHexString(inputHash), Convert.FromHexString(storedHash));
                }
            }

            // Fallback for plain text (should not happen in production)
            return account.PasswordHash == password;
        }

        public async Task RegisterAsync(Account account, string password)
        {
            // Normalize email to prevent duplicates
            if (!string.IsNullOrWhiteSpace(account.Email))
            {
                account.Email = account.Email.Trim().ToLowerInvariant();
            }
            
            var salt = Webstore.Models.Security.PasswordHasher.GenerateSalt();
            account.PasswordHash = salt + ":" + Webstore.Models.Security.PasswordHasher.HashPassword(password, salt);
            if (string.IsNullOrEmpty(account.Role)) account.Role = "Customer";

            await _accountRepository.AddAsync(account);
            await _accountRepository.SaveChangesAsync();
        }

        public async Task<bool> GenerateResetTokenAsync(string email)
        {
            var account = await GetAccountByEmailAsync(email);
            if (account == null) return false;

            // Generate a secure reset token
            var resetToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            account.ResetToken = resetToken;
            account.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(30);

            _accountRepository.Update(account);
            await _accountRepository.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ValidateResetTokenAsync(string email, string token)
        {
            var account = await GetAccountByEmailAsync(email);
            if (account == null) return false;

            if (account.ResetToken != token) return false;
            if (account.ResetTokenExpiry == null) return false;
            if (account.ResetTokenExpiry < DateTime.UtcNow) return false;

            return true;
        }

        public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
        {
            var account = await GetAccountByEmailAsync(email);
            if (account == null) return false;

            if (account.ResetToken != token) return false;
            if (account.ResetTokenExpiry == null) return false;
            if (account.ResetTokenExpiry < DateTime.UtcNow) return false;

            // Hash new password
            var salt = Webstore.Models.Security.PasswordHasher.GenerateSalt();
            account.PasswordHash = salt + ":" + Webstore.Models.Security.PasswordHasher.HashPassword(newPassword, salt);

            // Clear reset token
            account.ResetToken = null;
            account.ResetTokenExpiry = null;

            _accountRepository.Update(account);
            await _accountRepository.SaveChangesAsync();

            return true;
        }
    }
}
