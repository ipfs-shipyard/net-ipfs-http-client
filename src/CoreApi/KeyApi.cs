using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ipfs.CoreApi;
using Newtonsoft.Json.Linq;

namespace Ipfs.Http
{
    internal class KeyApi : IKeyApi
    {
        /// <summary>
        ///   Information about a local key.
        /// </summary>
        public sealed class KeyInfo : IKey
        {
            public KeyInfo(MultiHash id, string name)
            {
                Id = id;
                Name = name;
            }

            /// <inheritdoc />
            public MultiHash Id { get; }

            /// <inheritdoc />
            public string Name { get; }

            /// <inheritdoc />
            public override string ToString()
            {
                return Name ?? string.Empty;
            }
        }

        private readonly IpfsClient ipfs;

        internal KeyApi(IpfsClient ipfs)
        {
            this.ipfs = ipfs;
        }

        public async Task<IKey> CreateAsync(string name, string keyType, int size, CancellationToken cancel = default)
        {
            return await ipfs.DoCommandAsync<KeyInfo>("key/gen", cancel,
                name,
                $"type={keyType}",
                $"size={size}");
        }

        public async Task<IEnumerable<IKey>> ListAsync(CancellationToken cancel = default)
        {
            var json = await ipfs.DoCommandAsync("key/list", cancel, null, "l=true");
            var keys = (JArray?)(JObject.Parse(json)["Keys"]);
            return keys
                .Select(k => new KeyInfo((string)k["Id"]!, (string)k["Name"]!));
        }

        public async Task<IKey?> RemoveAsync(string name, CancellationToken cancel = default)
        {
            var json = await ipfs.DoCommandAsync("key/rm", cancel, name);
            var keys = JObject.Parse(json)["Keys"] as JArray;

            return keys?
                .Select(k => new KeyInfo((string)k["Id"]!, (string)k["Name"]!))
                .First();
        }

        public async Task<IKey> RenameAsync(string oldName, string newName, CancellationToken cancel = default)
        {
            var json = await ipfs.DoCommandAsync("key/rename", cancel, oldName, $"arg={newName}");
            var key = JObject.Parse(json);
            return new KeyInfo((string)key["Id"]!, (string)key["Now"]!);
        }

        public Task<string> ExportAsync(string name, char[] password, CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public Task<IKey> ImportAsync(string name, string pem, char[]? password = null, CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }
    }
}
