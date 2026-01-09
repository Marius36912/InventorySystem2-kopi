using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace InventorySystem2.Auth;

public class AccountService(AuthDbContext db, PasswordHasher hasher)
{
    public async Task NewAccountAsync(string username, string password, bool isAdmin = false)
    {
        var (salt, saltedPasswordHash) = hasher.Hash(password);
        db.Add(new Account
        {
            Username = username,
            Salt = salt,
            SaltedPasswordHash = saltedPasswordHash,
            isAdmin = isAdmin
        });
        await db.SaveChangesAsync();
    }

    public Task<bool> UsernameExistsAsync(string username)
        => db.Accounts.AnyAsync(a => a.Username == username);

    public async Task<bool> CredentialsCorrectAsync(string username, string password)
    {
        var account = await db.Accounts.FirstAsync(a => a.Username == username);
        return hasher.PasswordCorrect(password, account.Salt, account.SaltedPasswordHash);
    }

    public Task<Account> GetAccountAsync(string username)
        => db.Accounts.FirstAsync(a => a.Username == username);
}

public class PasswordHasher(
    int saltLength = 128 / 8,
    int hashIterations = 600_000
)
{
    public bool PasswordCorrect(string password, byte[] salt, byte[] saltedPasswordHash)
        => CryptographicOperations.FixedTimeEquals(Hash(salt, password), saltedPasswordHash);

    private byte[] Hash(byte[] salt, string password)
        => Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            hashIterations,
            HashAlgorithmName.SHA256,
            256 / 8
        );

    public (byte[] Salt, byte[] Hash) Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(saltLength);
        return (salt, Hash(salt, password));
    }
}