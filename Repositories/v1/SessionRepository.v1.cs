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

namespace openspace.Repositories
{
    public class SessionRepositoryV1 : ISessionRepositoryV1
    {
        private bool _sessionsInitialized = false;
        private CloudBlobContainer _container;
        private readonly IConfiguration _configuration;
        private static ICollection<Session> _sessions;

        public SessionRepositoryV1(IConfiguration configuration)
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
                    var sessions = new List<Session>(JsonConvert.DeserializeObject<Session[]>(blockBlob.DownloadTextAsync().Result));
                    _sessions = MigrateDownstream(GetSchemaVersion(blockBlob), sessions).ToList();
                }
            }
        }

        public Task<IEnumerable<Session>> Get() => Task.FromResult(_sessions.AsEnumerable());

        public Task<Session> Get(int id) => Task.FromResult(_sessions.FirstOrDefault(s => s.Id == id));

        public async Task<Session> Create()
        {
            Session session;

            lock (_sessions)
            {
                var lastId = _sessions.Any() ? _sessions.Max(s => s.Id) : 1;
                session = new Session() { Id = lastId + 1 };
                session.Name = "Session #" + session.Id;
                _sessions.Add(session);
            }

            await SaveAsync();

            return session;
        }

        public async Task Update(int sessionId, Action<Session> func)
        {
            lock (_sessions)
            {
                var session = _sessions.FirstOrDefault(s => sessionId == s.Id);
                func(session);
            }

            await SaveAsync();
        }

        public async Task Update(Session session)
        {
            lock (_sessions)
            {
                var oldSession = _sessions.FirstOrDefault(s => session.Id == s.Id);
                if (oldSession != null)
                {
                    _sessions.Remove(oldSession);
                }

                _sessions.Add(session);
            }

            await SaveAsync();
        }

        public async Task<bool> Delete(int id)
        {
            lock (_sessions)
            {
                var session = _sessions.FirstOrDefault(s => s.Id == id);
                if (session == null) return false;

                _sessions.Remove(session);
            }

            await SaveAsync();

            return true;
        }


        private async Task SaveAsync()
        {
            var blockBlob = _container.GetBlockBlobReference("sessions");

            var sessions = MigrateUpstream(GetSchemaVersion(blockBlob), _sessions);

            await blockBlob.UploadTextAsync(JsonConvert.SerializeObject(sessions));
        }

        private string GetSchemaVersion(CloudBlockBlob blockBlob)
        {
            return blockBlob.Metadata.ContainsKey("SchemaVersion")
                ? blockBlob.Metadata["SchemaVersion"]
                : "1.0";
        }

        // database 2.0 -> client 1.0
        private IEnumerable<Session> MigrateDownstream(string schemaVersion, IEnumerable<Session> sessions)
        {
            foreach (var session in sessions)
            {
                if (schemaVersion == "2.0")
                {
                    session.Name = session.DisplayName;
                }

                yield return session;
            }
        }

        // database 2.0 <- client 1.0
        private IEnumerable<Session> MigrateUpstream(string schemaVersion, IEnumerable<Session> sessions)
        {
            foreach (var session in sessions)
            {
                if (schemaVersion == "2.0")
                {
                    session.DisplayName = session.Name;
                }

                yield return session;
            }
        }
    }
}
