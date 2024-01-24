using Ipfs.CoreApi;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Http
{
    class KeyApi : IKeyApi
    {
        /// <summary>
        ///   Information about a local key.
        /// </summary>
        public class KeyInfo : IKey
        {
            /// <inheritdoc />
            public Cid Id { get; set; }

            /// <inheritdoc />
            public string Name { get; set; }

            /// <inheritdoc />
            public override string ToString()
            {
                return Name;
            }

        }
        IpfsClient ipfs;

        internal KeyApi(IpfsClient ipfs)
        {
            this.ipfs = ipfs;
        }

        public async Task<IKey> CreateAsync(string name, string keyType, int size, CancellationToken cancel = default(CancellationToken))
        {
            var json = await ipfs.DoCommandAsync("key/gen", cancel, name, $"type={keyType}", $"size={size}", "ipns-base=base36");
            var jobject = JObject.Parse(json);

            string id = (string)jobject["Id"];
            string apiName = (string)jobject["Name"];

            return new KeyInfo
            {
                Id = id,
                Name = apiName
            };
        }

        public async Task<IEnumerable<IKey>> ListAsync(CancellationToken cancel = default(CancellationToken))
        {
            var json = await ipfs.DoCommandAsync("key/list", cancel, null, "l=true", "ipns-base=base36");
            var keys = (JArray)(JObject.Parse(json)["Keys"]);

            return keys
                .Select(k =>
                {
                    string id = (string)k["Id"];
                    string name = (string)k["Name"];

                    return new KeyInfo
                    {
                        Id = id,
                        Name = name
                    };
                });
        }

        public async Task<IKey> RemoveAsync(string name, CancellationToken cancel = default(CancellationToken))
        {
            var json = await ipfs.DoCommandAsync("key/rm", cancel, name, "ipns-base=base36");
            var keys = JObject.Parse(json)["Keys"] as JArray;

            return keys?
                    .Select(k =>
                    {
                        string id = (string)k["Id"];
                        string keyName = (string)k["Name"];

                        return new KeyInfo
                        {
                            Id = id,
                            Name = keyName
                        };
                    })
                .First();
        }

        public async Task<IKey> RenameAsync(string oldName, string newName, CancellationToken cancel = default(CancellationToken))
        {
            var json = await ipfs.DoCommandAsync("key/rename", cancel, oldName, $"arg={newName}", "ipns-base=base36");
            var jobject = JObject.Parse(json);

            string id = (string)jobject["Id"];
            string currentName = (string)jobject["Now"];

            return new KeyInfo
            {
                Id = id,
                Name = currentName
            };
        }

        public Task<string> ExportAsync(string name, char[] password, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<IKey> ImportAsync(string name, string pem, char[] password = null, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }
}
