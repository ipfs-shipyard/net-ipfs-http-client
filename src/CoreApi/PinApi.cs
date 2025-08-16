using Google.Protobuf;
using Ipfs.CoreApi;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace Ipfs.Http
{
    class PinApi : IPinApi
    {
        private IpfsClient ipfs;

        internal PinApi(IpfsClient ipfs)
        {
            this.ipfs = ipfs;
        }

        public async Task<IEnumerable<Cid>> AddAsync(string path, PinAddOptions options, CancellationToken cancel = default)
        {
            options ??= new PinAddOptions();
            var optList = new List<string>
            {
                "recursive=" + options.Recursive.ToString().ToLowerInvariant()
            };
            if (!string.IsNullOrEmpty(options.Name))
            {
                optList.Add("name=" + options.Name);
            }
            var json = await ipfs.DoCommandAsync("pin/add", cancel, path, optList.ToArray());
            return ((JArray)JObject.Parse(json)["Pins"]) .Select(p => (Cid)(string)p);
        }

        public async Task<IEnumerable<Cid>> ListAsync(CancellationToken cancel = default)
        {
            var json = await ipfs.DoCommandAsync("pin/ls", cancel);
            var keys = (JObject)(JObject.Parse(json)["Keys"]);
            return keys
                .Properties()
                .Select(p => (Cid)p.Name);
        }

        public async Task<IEnumerable<Cid>> ListAsync(PinType type, CancellationToken cancel = default(CancellationToken))
        {
            var typeOpt = type.ToString().ToLowerInvariant();
            var json = await ipfs.DoCommandAsync("pin/ls", cancel,
                null,
                $"type={typeOpt}");
            var keys = (JObject)(JObject.Parse(json)["Keys"]);
            return keys
                .Properties()
                .Select(p => (Cid)p.Name);
        }

        public async Task<IEnumerable<Cid>> RemoveAsync(Cid id, bool recursive = true, CancellationToken cancel = default(CancellationToken))
        {
            var opts = "recursive=" + recursive.ToString().ToLowerInvariant();
            var json = await ipfs.DoCommandAsync("pin/rm", cancel, id, opts);
            return ((JArray)JObject.Parse(json)["Pins"])
                .Select(p => (Cid)(string)p);
        }

    }

}
