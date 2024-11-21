using Ipfs.CoreApi;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

#nullable enable

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
        public async IAsyncEnumerable<FilestoreItem> ListAsync(string? cid = null, bool? fileOrder = null, [EnumeratorCancellation] CancellationToken token = default)
        {
            string[] options = [];

            if (fileOrder is not null)
                options = [..options, $"file-order={fileOrder.ToString().ToLowerInvariant()}"];
            
            using var stream = await ipfs.PostDownloadAsync("filestore/ls", token, cid, options);

            // Read line-by-line
            using var reader = new StreamReader(stream);
            while (!reader.EndOfStream)
            {
                token.ThrowIfCancellationRequested();
                var json = await reader.ReadLineAsync();

                var res = JsonConvert.DeserializeObject<FilestoreItem>(json);
                if (res is not null)
                    yield return res;
            }
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<FilestoreItem> VerifyObjectsAsync(string? cid = null, bool? fileOrder = null, [EnumeratorCancellation] CancellationToken token = default)
        {
            using var stream = await ipfs.PostDownloadAsync("filestore/verify", token, cid, $"{fileOrder}");

            // Read line-by-line
            using var reader = new StreamReader(stream);
            while (!reader.EndOfStream)
            {
                token.ThrowIfCancellationRequested();
                var json = await reader.ReadLineAsync();

                var res = JsonConvert.DeserializeObject<FilestoreItem>(json);
                if (res is not null)
                    yield return res;
            }
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<FilestoreDuplicate> DupsAsync([EnumeratorCancellation] CancellationToken token = default)
        {
            using var stream = await ipfs.PostDownloadAsync("filestore/dups", token);

            // Read line-by-line
            using var reader = new StreamReader(stream);
            while (!reader.EndOfStream)
            {
                token.ThrowIfCancellationRequested();
                var json = await reader.ReadLineAsync();

                var res = JsonConvert.DeserializeObject<FilestoreDuplicate>(json);
                if (res is not null)
                    yield return res;
            }
        }
    }

}
