using Ipfs.CoreApi;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Http
{
    class BootstrapApi : IBootstrapApi
    {
        private readonly IpfsClient ipfs;

        internal BootstrapApi(IpfsClient ipfs)
        {
            this.ipfs = ipfs;
        }

        public async Task<MultiAddress?> AddAsync(MultiAddress address, CancellationToken cancel = default)
        {
            var json = await ipfs.DoCommandAsync("bootstrap/add", cancel, address.ToString());
            var addrs = (JArray?)(JObject.Parse(json)["Peers"]);
            var a = addrs.FirstOrDefault();
            if (a is null)
                return null;
            return new MultiAddress((string)a!);
        }

        public async Task<IEnumerable<MultiAddress>> AddDefaultsAsync(CancellationToken cancel = default)
        {
            var json = await ipfs.DoCommandAsync("bootstrap/add/default", cancel);
            var addrs = (JArray?)(JObject.Parse(json)["Peers"]);
            if (addrs is null)
            {
                return Enumerable.Empty<MultiAddress>();
            }

            return addrs
                .Select(a => MultiAddress.TryCreate((string)a!))
                .Where(ma => ma is not null)
                .Cast<MultiAddress>();
        }

        public async Task<IEnumerable<MultiAddress>> ListAsync(CancellationToken cancel = default)
        {
            var json = await ipfs.DoCommandAsync("bootstrap/list", cancel);
            var addrs = (JArray?)(JObject.Parse(json)["Peers"]);
            if (addrs is null)
            {
                return Enumerable.Empty<MultiAddress>();
            }

            return addrs
                .Select(a => MultiAddress.TryCreate((string)a!))
                .Where(ma => ma is not null)
                .Cast<MultiAddress>();
        }

        public Task RemoveAllAsync(CancellationToken cancel = default)
        {
            return ipfs.DoCommandAsync("bootstrap/rm/all", cancel);
        }

        public async Task<MultiAddress?> RemoveAsync(MultiAddress address, CancellationToken cancel = default)
        {
            var json = await ipfs.DoCommandAsync("bootstrap/rm", cancel, address.ToString());
            var addrs = (JArray?)(JObject.Parse(json)["Peers"]);
            var a = addrs.FirstOrDefault();
            if (a is null)
                return null;
            return new MultiAddress((string)a!);
        }
    }
}
