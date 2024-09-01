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
using static Ipfs.Http.CoreApi.FilestoreApi;

namespace Ipfs.Http.CoreApi
{
    /// <summary>
    /// Concrete implementation of <see cref="IFilestoreApi"/>.
    /// </summary>
    public class FilestoreApi : IFilestoreApi
    {
        private IpfsClient ipfs;

        internal FilestoreApi(IpfsClient ipfs)
        {
            this.ipfs = ipfs;
        }

        /// <summary>
        /// List async api for <see cref="IFilestoreApi"/>.
        /// </summary>
        /// <param name="cid"></param>
        /// <param name="token"></param>
        /// <param name="fileOrder"></param>
        /// <returns></returns>
        public async Task<IFilesStoreApiObjectResponse> ListAsync(string cid, CancellationToken token, bool fileOrder)
        {
            var json = await ipfs.DoCommandAsync("filestore/ls", token, cid, fileOrder.ToString());

            return JsonConvert.DeserializeObject<FilestoreObjectResponse>(json);
        }

        /// <summary>
        /// Object verification api for <see cref="IFilestoreApi"/>.
        /// </summary>
        /// <param name="cid"></param>
        /// <param name="fileOrder"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<IFilesStoreApiObjectResponse> VerifyObjectsAsync(string cid, bool fileOrder, CancellationToken token)
        {
            var json = await ipfs.DoCommandAsync("filestore/verify", token, cid, fileOrder.ToString());

            return JsonConvert.DeserializeObject<FilestoreObjectResponse>(json);
        }

        /// <summary>
        /// Executes Dups command in <see cref="IFilestoreApi"/>.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<IDupsResponse> DupsAsync(CancellationToken token)
        {
            var json = await ipfs.DoCommandAsync("filestore/dups", token);

            return JsonConvert.DeserializeObject<DupsResponse>(json);
        }
    }

}
