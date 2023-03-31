using Ipfs.CoreApi;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Http
{
    class SwarmApi : ISwarmApi
    {
        private readonly IpfsClient ipfs;

        internal SwarmApi(IpfsClient ipfs)
        {
            this.ipfs = ipfs;
        }

        public async Task<IEnumerable<Peer>> AddressesAsync(CancellationToken cancel = default)
        {
            var json = await ipfs.DoCommandAsync("swarm/addrs", cancel);
            var obj = (JObject?)JObject.Parse(json);
            if (obj is null)
            {
                return Enumerable.Empty<Peer>();
            }

            var addrs = (JObject?)obj["Addrs"];
            if (addrs is null)
            {
                return Enumerable.Empty<Peer>();
            }

            return addrs
                .Properties()
                .Select(p => new Peer
                {
                    Id = p.Name,
                    Addresses = ((JArray)p.Value)
                        .Select(a => MultiAddress.TryCreate((string)a!))
                        .Where(ma => ma is not null)
                        .Cast<MultiAddress>()
                });
        }

        public async Task<IEnumerable<Peer>> PeersAsync(CancellationToken cancel = default)
        {
            var json = await ipfs.DoCommandAsync("swarm/peers", cancel, null, "verbose=true");
            var result = JObject.Parse(json);

            // Older servers return an array of strings
            var strings = (JArray?)result["Strings"];
            if (strings is not null)
            {
                return strings
                   .Select(s =>
                   {
                       var parts = ((string)s)!.Split(' ');
                       var address = new MultiAddress(parts[0]);
                       return new Peer
                       {
                           Id = address.PeerId,
                           ConnectedAddress = parts[0],
                           Latency = Duration.Parse(parts[1])
                       };
                   });
            }

            // Current servers return JSON
            var peers = (JArray?)result["Peers"];
            if (peers is not null)
            {
                return peers.Select(p => new Peer
                {
                    Id = (string)p["Peer"]!,
                    ConnectedAddress = new MultiAddress((string?)p["Addr"] + "/ipfs/" + (string?)p["Peer"]),
                    Latency = Duration.Parse((string?)p["Latency"] ?? "0")
                });
            }

            // Hmmm. Another change we can handle
            throw new FormatException("Unknown response from 'swarm/peers");
        }

        public async Task ConnectAsync(MultiAddress address, CancellationToken cancel = default)
        {
            await ipfs.DoCommandAsync("swarm/connect", cancel, address.ToString());
        }

        public async Task DisconnectAsync(MultiAddress address, CancellationToken cancel = default)
        {
            await ipfs.DoCommandAsync("swarm/disconnect", cancel, address.ToString());
        }

        public async Task<MultiAddress?> AddAddressFilterAsync(MultiAddress address, bool persist = false, CancellationToken cancel = default)
        {
            // go-ipfs always does persist, https://github.com/ipfs/go-ipfs/issues/4605
            var json = await ipfs.DoCommandAsync("swarm/filters/add", cancel, address.ToString());
            var addrs = (JArray?)(JObject.Parse(json)["Strings"]);
            var a = addrs.FirstOrDefault();
            if (a is null)
                return null;
            return new MultiAddress((string)a!);
        }

        public async Task<IEnumerable<MultiAddress>> ListAddressFiltersAsync(bool persist = false, CancellationToken cancel = default)
        {
            JArray? addrs;
            if (persist)
            {
                addrs = await ipfs.Config.GetAsync("Swarm.AddrFilters", cancel) as JArray;
            }
            else
            {
                var json = await ipfs.DoCommandAsync("swarm/filters", cancel);
                addrs = (JObject.Parse(json)["Strings"]) as JArray;
            }

            if (addrs is null)
                return Enumerable.Empty<MultiAddress>();
            return addrs
                .Select(a => MultiAddress.TryCreate((string)a!))
                .Where(ma => ma is not null)
                .Cast<MultiAddress>();
        }

        public async Task<MultiAddress?> RemoveAddressFilterAsync(MultiAddress address, bool persist = false, CancellationToken cancel = default)
        {
            // go-ipfs always does persist, https://github.com/ipfs/go-ipfs/issues/4605
            var json = await ipfs.DoCommandAsync("swarm/filters/rm", cancel, address.ToString());
            var addrs = (JArray?)(JObject.Parse(json)["Strings"]);
            var a = addrs.FirstOrDefault();
            if (a is null)
                return null;
            return new MultiAddress((string)a!);
        }
    }
}
