using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MyShopClient.Services.SecureStorage
{
   

    public class SecureStorageService : ISecureStorageService
    {
        private readonly string _folder;

        public SecureStorageService()
        {
            _folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MyShopClient");

            Directory.CreateDirectory(_folder);
        }

        private string GetPath(string key) => Path.Combine(_folder, key + ".bin");

        public async Task SaveEncryptedAsync(string key, string plainText)
        {
            var bytes = Encoding.UTF8.GetBytes(plainText);
            var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);

            await File.WriteAllBytesAsync(GetPath(key), protectedBytes);
        }

        public async Task<string?> LoadDecryptedAsync(string key)
        {
            var path = GetPath(key);
            if (!File.Exists(path)) return null;

            var protectedBytes = await File.ReadAllBytesAsync(path);
            var bytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }

        public Task DeleteAsync(string key)
        {
            var path = GetPath(key);
            if (File.Exists(path))
                File.Delete(path);

            return Task.CompletedTask;
        }
    }
}
