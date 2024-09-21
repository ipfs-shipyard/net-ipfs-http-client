using Ipfs.CoreApi;
using Ipfs.Http.CoreApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Http
{
    /// <inheritdoc/>
    public class FilestoreApi : IFilestoreApi
    {
        private IpfsClient ipfs;

        /// <inheritdoc/>
        internal FilestoreApi(IpfsClient ipfs)
        {
            this.ipfs = ipfs;
        }

        /// <inheritdoc/>
        public async Task<IFilestoreApiObjectResponse> ListAsync(string cid, bool fileOrder, CancellationToken token)
        {
            var json = await ipfs.DoCommandAsync("filestore/ls", token, cid, fileOrder.ToString());

            return JsonConvert.DeserializeObject<FilestoreObjectResponse>(json);
        }

        /// <inheritdoc/>
        public async Task<IFilestoreApiObjectResponse> VerifyObjectsAsync(string cid, bool fileOrder, CancellationToken token)
        {
            var json = await ipfs.DoCommandAsync("filestore/verify", token, cid, fileOrder.ToString());

            return JsonConvert.DeserializeObject<FilestoreObjectResponse>(json);
        }

        /// <inheritdoc/>
        public async Task<IDupsResponse> DupsAsync(CancellationToken token)
        {
            var json = await ipfs.DoCommandAsync("filestore/dups", token);

            return JsonConvert.DeserializeObject<DupsResponse>(json);
        }
    }

}
