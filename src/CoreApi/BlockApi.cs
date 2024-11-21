using Ipfs.CoreApi;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Ipfs.Http
{
    class BlockApi : IBlockApi
    {
        IpfsClient ipfs;

        internal BlockApi(IpfsClient ipfs)
        {
            this.ipfs = ipfs;
        }

        public async Task<byte[]> GetAsync(Cid id, CancellationToken cancel = default)
        {
            return await ipfs.DownloadBytesAsync("block/get", cancel, id);
        }

        public async Task<IBlockStat> PutAsync(
            byte[] data,
            string cidCodec = "raw",
            MultiHash? hash = null,
            bool? pin = null,
            bool? allowBigBlock = null,
            CancellationToken cancel = default)
        {
            using var stream = new MemoryStream(data);
            return await PutAsync(stream, cidCodec, hash, pin, allowBigBlock, cancel);
        }

        public async Task<IBlockStat> PutAsync(
            Stream data,
            string cidCodec = "raw",
            MultiHash? hash = null,
            bool? pin = null,
            bool? allowBigBlock = null,
            CancellationToken cancel = default)
        {
            string[] options = [
                $"cid-codec={cidCodec}"
            ];

            if (hash != null)
                options = [.. options, $"mhtype={hash}", $"mhlen={hash.Algorithm.DigestSize}"];

            if (pin != null)
                options = [.. options, $"pin={pin.ToString().ToLowerInvariant()}"];

            if (allowBigBlock != null)
                options = [.. options, $"allow-big-block={allowBigBlock.ToString().ToLowerInvariant()}"];

            var json = await ipfs.UploadAsync("block/put", cancel, data, null, options);
            var res = JObject.Parse(json).ToObject<Block>();
            if (res is null)
                throw new InvalidDataException("The response did not contain a block.");

            return res;
        }

        public async Task<IBlockStat> StatAsync(Cid id, CancellationToken cancel = default)
        {
            var json = await ipfs.DoCommandAsync("block/stat", cancel, id);

            var parsed = JObject.Parse(json);
            if (parsed is null)
                throw new InvalidDataException("The response could not be parsed.");

            var error = (string?)parsed["Error"];
            if (error != null)
                throw new HttpRequestException(error);

            var res = parsed.ToObject<Block>();
            if (res is null)
                throw new InvalidDataException("The response could not be deserialized.");

            return res;
        }

        public async Task<Cid> RemoveAsync(Cid id, bool ignoreNonexistent = false, CancellationToken cancel = default)
        {
            var json = await ipfs.DoCommandAsync("block/rm", cancel, id, "force=" + ignoreNonexistent.ToString().ToLowerInvariant());

            var parsed = JObject.Parse(json);
            if (parsed is null)
                throw new InvalidDataException("The response could not be parsed.");

            var error = (string?)parsed["Error"];
            if (error != null)
                throw new HttpRequestException(error);

            var cid = parsed["Hash"]?.ToObject<Cid>();
            if (cid is null)
                throw new InvalidDataException("The response could not be deserialized.");

            return cid;
        }
    }

}
