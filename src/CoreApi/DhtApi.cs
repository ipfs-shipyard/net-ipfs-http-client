using Ipfs.CoreApi;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Http
{
    class DhtApi : IDhtApi
    {
        private readonly IpfsClient ipfs;

        internal DhtApi(IpfsClient ipfs)
        {
            this.ipfs = ipfs;
        }

        public Task<Peer> FindPeerAsync(MultiHash id, CancellationToken cancel = default)
        {
            return ipfs.IdAsync(id, cancel);
        }

        public async Task<IEnumerable<Peer>> FindProvidersAsync(Cid id, int limit = 20, Action<Peer>? providerFound = null, CancellationToken cancel = default)
        {
            // TODO: providerFound action
            var stream = await ipfs.PostDownloadAsync("dht/findprovs", cancel, id, $"num-providers={limit}");
            return ProviderFromStream(stream, limit);
        }

        public Task<byte[]> GetAsync(byte[] key, CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public Task ProvideAsync(Cid cid, bool advertise = true, CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public Task PutAsync(byte[] key, out byte[] value, CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> TryGetAsync(byte[] key, out byte[] value, CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        IEnumerable<Peer> ProviderFromStream(Stream stream, int limit = int.MaxValue)
        {
            using (var sr = new StreamReader(stream))
            {
                var n = 0;
                while (!sr.EndOfStream && n < limit)
                {
                    var json = sr.ReadLine();

                    var r = JObject.Parse(json);
                    var id = (string?)r["ID"];
                    if (!string.IsNullOrEmpty(id))
                    {
                        ++n;
                        yield return new Peer { Id = new MultiHash(id!) };
                    }
                    else
                    {
                        var responses = (JArray?)r["Responses"];
                        if (responses is not null)
                        {
                            foreach (var response in responses)
                            {
                                var rid = (string?)response["ID"];
                                if (!string.IsNullOrEmpty(rid))
                                {
                                    ++n;
                                    yield return new Peer { Id = new MultiHash(rid!) };
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
