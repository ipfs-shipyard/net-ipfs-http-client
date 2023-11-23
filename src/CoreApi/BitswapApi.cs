using Ipfs.CoreApi;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Http
{
    class BitswapApi : IBitswapApi
    {
        private readonly IpfsClient ipfs;

        internal BitswapApi(IpfsClient ipfs)
        {
            this.ipfs = ipfs;
        }

        public Task<IDataBlock> GetAsync(Cid id, CancellationToken cancel = default)
        {
            return ipfs.Block.GetAsync(id, cancel);
        }

        public async Task<IEnumerable<Cid>> WantsAsync(MultiHash? peer = null, CancellationToken cancel = default)
        {
            var json = await ipfs.DoCommandAsync("bitswap/wantlist", cancel, peer?.ToString());
            var keys = (JArray?)(JObject.Parse(json)["Keys"]);
            // https://github.com/ipfs/go-ipfs/issues/5077
            return keys
                .Select(k =>
                {
                    if (k.Type == JTokenType.String)
                        return Cid.Decode(k.ToString());
                    var obj = (JObject)k;
                    return Cid.Decode(obj["/"]!.ToString());
                });
        }

        public async Task UnwantAsync(Cid id, CancellationToken cancel = default)
        {
            await ipfs.DoCommandAsync("bitswap/unwant", cancel, id);
        }

        public async Task<BitswapLedger> LedgerAsync(Peer peer, CancellationToken cancel = default)
        {
            var json = await ipfs.DoCommandAsync("bitswap/ledger", cancel, peer.Id?.ToString());
            var o = JObject.Parse(json);
            return new BitswapLedger
            {
                Peer = (string)o["Peer"]!,
                DataReceived = (ulong?)o["Sent"] ?? 0,
                DataSent = (ulong?)o["Recv"] ?? 0,
                BlocksExchanged = (ulong?)o["Exchanged"] ?? 0,
            };
        }
    }
}
