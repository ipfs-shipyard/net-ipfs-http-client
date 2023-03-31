using Ipfs.CoreApi;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Http
{

    class StatApi : IStatsApi
    {
        private readonly IpfsClient ipfs;

        internal StatApi(IpfsClient ipfs)
        {
            this.ipfs = ipfs;
        }

        public Task<BandwidthData> BandwidthAsync(CancellationToken cancel = default)
        {
            return ipfs.DoCommandAsync<BandwidthData>("stats/bw", cancel);
        }

        public async Task<BitswapData> BitswapAsync(CancellationToken cancel = default)
        {
            var json = await ipfs.DoCommandAsync("stats/bitswap", cancel);
            var stat = JObject.Parse(json);
            return new BitswapData
            {
                BlocksReceived = (ulong?)stat["BlocksReceived"] ?? 0,
                DataReceived = (ulong?)stat["DataReceived"] ?? 0,
                BlocksSent = (ulong?)stat["BlocksSent"] ?? 0,
                DataSent = (ulong?)stat["DataSent"] ?? 0,
                DupBlksReceived = (ulong?)stat["DupBlksReceived"] ?? 0,
                DupDataReceived = (ulong?)stat["DupDataReceived"] ?? 0,
                ProvideBufLen = (int?)stat["ProvideBufLen"] ?? 0,
                Peers = ((JArray?)stat["Peers"]).Select(s => new MultiHash((string?)s)),
                Wantlist = ((JArray?)stat["Wantlist"]).Select(o => Cid.Decode(o["/"]?.ToString() ?? string.Empty))
            };
        }

        public Task<RepositoryData> RepositoryAsync(CancellationToken cancel = default)
        {
            return ipfs.DoCommandAsync<RepositoryData>("stats/repo", cancel);
        }
    }
}
