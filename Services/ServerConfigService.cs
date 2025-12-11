using System;
using System.IO;
using System.Text.Json;
using MyShopClient.Models;

namespace MyShopClient.Services
{
    public interface IServerConfigService
    {
        ServerConfig Current { get; }
        ServerConfig Load();
        void Save(ServerConfig config);
        string GraphQlEndpoint { get; }
    }

    public class ServerConfigService : IServerConfigService
    {
        private readonly string _filePath;

        public ServerConfig Current { get; private set; }

        public string GraphQlEndpoint => "/graphql";
        public ServerConfigService()
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MyShopClient");

            Directory.CreateDirectory(folder);
            _filePath = Path.Combine(folder, "serverconfig.json");

            Current = Load();
        }

        public ServerConfig Load()
        {
            if (!File.Exists(_filePath))
            {
                Current = new ServerConfig();
                return Current;
            }

            try
            {
                var json = File.ReadAllText(_filePath);
                var cfg = JsonSerializer.Deserialize<ServerConfig>(json);
                Current = cfg ?? new ServerConfig();
            }
            catch
            {
                Current = new ServerConfig();
            }

            return Current;
        }

        public void Save(ServerConfig config)
        {
            Current = config;
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_filePath, json);
        }
    }
}
