using System;
using System.IO;
using System.Threading.Tasks;

namespace MyShopClient.Infrastructure.Storage
{
 public interface IFileCacheService
 {
 Task SaveAsync(string key, byte[] data);
 Task<byte[]?> LoadAsync(string key);
 Task DeleteAsync(string key);
 }

 public class FileCacheService : IFileCacheService
 {
 private readonly string _folder;
 public FileCacheService(string folderName = "cache")
 {
 _folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), folderName);
 Directory.CreateDirectory(_folder);
 }

 private string PathFor(string key) => System.IO.Path.Combine(_folder, key + ".bin");

 public async Task SaveAsync(string key, byte[] data)
 {
 await File.WriteAllBytesAsync(PathFor(key), data);
 }

 public async Task<byte[]?> LoadAsync(string key)
 {
 var p = PathFor(key);
 if (!File.Exists(p)) return null;
 return await File.ReadAllBytesAsync(p);
 }

 public Task DeleteAsync(string key)
 {
 var p = PathFor(key);
 if (File.Exists(p)) File.Delete(p);
 return Task.CompletedTask;
 }
 }
}
