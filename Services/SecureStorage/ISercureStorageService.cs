using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyShopClient.Services.SecureStorage
{
    public interface ISecureStorageService
    {
        Task SaveEncryptedAsync(string key, string plainText);
        Task<string?> LoadDecryptedAsync(string key);
        Task DeleteAsync(string key);
    }
}
