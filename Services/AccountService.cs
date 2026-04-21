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

        public async Task<bool> ValidateCredentialsAsync(string username, string password)
        {
            var account = await GetAccountByUsernameAsync(username);
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
            
            return account.PasswordHash == password;
        }

        public async Task RegisterAsync(Account account, string password)
        {
            var salt = Webstore.Models.Security.PasswordHasher.GenerateSalt();
            account.PasswordHash = salt + ":" + Webstore.Models.Security.PasswordHasher.HashPassword(password, salt);
            if (string.IsNullOrEmpty(account.Role)) account.Role = "Customer";

            await _accountRepository.AddAsync(account);
            await _accountRepository.SaveChangesAsync();
        }
    }
}
