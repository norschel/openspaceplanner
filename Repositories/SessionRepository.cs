using System.Collections.Generic;
using System.Threading.Tasks;
using openspace.Models;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using System;
using Newtonsoft.Json;
using JsonDotNet.CustomContractResolvers;

namespace openspace.Repositories
{
    public class SessionRepository : SessionRepositoryBase
    {
        private bool _sessionsInitialized = false;
        private CloudBlobContainer _container;
        private readonly IConfiguration _configuration;

        public SessionRepository(IConfiguration configuration)
        {
            _configuration = configuration;
            _sessions = new List<Session>();
        }

        public async Task InitializeAsync()
        {
            var storageAccount = new CloudStorageAccount(
                new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(
                _configuration["TableStorageAccount"],
                _configuration["TableStorageKey"]), true);

            var blobClient = storageAccount.CreateCloudBlobClient();
            _container = blobClient.GetContainerReference(_configuration["TableStorageContainer"]);
            await _container.CreateIfNotExistsAsync();

            if (!_sessionsInitialized)
            {
                _sessionsInitialized = true;

                var blockBlob = _container.GetBlockBlobReference("sessions");
                if (await blockBlob.ExistsAsync())
                {
                    _sessions = new List<Session>(JsonConvert.DeserializeObject<Session[]>(blockBlob.DownloadTextAsync().Result));

                    string schemaVersion = "1.0";
                    if (blockBlob.Metadata.ContainsKey("SchemaVersion"))
                    {
                        schemaVersion = blockBlob.Metadata["SchemaVersion"];
                    }

                    if (schemaVersion.Equals("1.0"))
                    {
                        InMemoryMigrateToV2(_sessions);
                    }
                }
            }
        }

        protected override void Save()
        {
            var propertiesContractResolver = new PropertiesContractResolver();
            propertiesContractResolver.ExcludeProperties.Add("Session.Name");
            var serializerSettings = new JsonSerializerSettings();
            serializerSettings.ContractResolver = propertiesContractResolver;
            JsonConvert.SerializeObject(a, serializerSettings);

            var blockBlob = _container.GetBlockBlobReference("sessions");
            if (_configuration["SchemaVersion"].Equals("2.0"))
            {
                blockBlob.Metadata["SchemaVersion"] = "2.0";
                InMemoryMigrateToV2(_sessions);
                JsonConvert.SerializeObject(_sessions.ToArray(), settings);
            }


            JsonConvert.SerializeObject(_sessions.ToArray())


            blockBlob.UploadTextAsync();
        }

        protected void InMemoryMigrateToV2(ICollection<Session> sessions)
        {
            foreach (var session in sessions)
            {
                session.DisplayName = session.Name;
            }
        }
    }
}
