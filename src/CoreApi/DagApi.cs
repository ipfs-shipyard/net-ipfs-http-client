using Ipfs.CoreApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Ipfs.CoreApi.CarImportOutput;

#nullable enable

namespace Ipfs.Http
{
    class DagApi : IDagApi
    {
        private IpfsClient ipfs;

        internal DagApi(IpfsClient ipfs)
        {
            this.ipfs = ipfs;
        }

        public async Task<Cid> PutAsync(
            JObject data,
            string storeCodec = "dag-cbor",
            string inputCodec = "dag-json",
            bool? pin = null,
            MultiHash? hash = null,
            bool? allowBigBlock = null,
            CancellationToken cancel = default)
        {
            using (var ms = new MemoryStream())
            {
                using (var sw = new StreamWriter(ms, new UTF8Encoding(false), 4096, true) { AutoFlush = true })
                using (var jw = new JsonTextWriter(sw))
                {
                    var serializer = new JsonSerializer
                    {
                        Culture = CultureInfo.InvariantCulture
                    };
                    serializer.Serialize(jw, data);
                }
                ms.Position = 0;
                return await PutAsync(ms, storeCodec, inputCodec, pin, hash, allowBigBlock, cancel);
            }
        }

        public async Task<Cid> PutAsync(
            object data,
            string storeCodec = "dag-cbor",
            string inputCodec = "dag-json",
            bool? pin = null,
            MultiHash? hash = null,
            bool? allowBigBlock = null,
            CancellationToken cancel = default)
        {
            using (var ms = new MemoryStream(
                Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)),
                false))
            {
                return await PutAsync(ms, storeCodec, inputCodec, pin, hash, allowBigBlock, cancel);
            }
        }

        public async Task<Cid> PutAsync(
            Stream data,
            string storeCodec = "dag-cbor",
            string inputCodec = "dag-json",
            bool? pin = null,
            MultiHash? hash = null,
            bool? allowBigBlock = null,
            CancellationToken cancel = default)
        {
            string[] options = [
                $"store-codec={storeCodec}",
                $"input-codec={inputCodec}"
            ];

            if (hash != null)
                options = [.. options, $"hash={hash}"];

            if (pin != null)
                options = [.. options, $"pin={pin.ToString().ToLowerInvariant()}"];

            if (allowBigBlock != null)
                options = [.. options, $"allow-big-block={allowBigBlock.ToString().ToLowerInvariant()}"];

            var json = await ipfs.UploadAsync("dag/put", cancel, data, null, options);

            var parsed = JObject.Parse(json);
            var cid = parsed["Cid"]?.ToObject<DagCid>();
            if (cid is null)
                throw new InvalidDataException("The response did not contain a CID.");

            return (Cid)cid;
        }

        public async Task<JObject> GetAsync(
            Cid id,
            string outputCodec = "dag-json",
            CancellationToken cancel = default)
        {
            var json = await ipfs.DoCommandAsync("dag/get", cancel, id, $"output-codec={outputCodec}");
            return JObject.Parse(json);
        }


        public async Task<JToken> GetAsync(
            string path,
            string outputCodec = "dag-json",
            CancellationToken cancel = default)
        {
            var json = await ipfs.DoCommandAsync("dag/get", cancel, path, $"output-codec={outputCodec}");
            return JToken.Parse(json);
        }

        public async Task<T> GetAsync<T>(Cid id, string outputCodec = "dag-json", CancellationToken cancel = default)
        {
            var json = await ipfs.DoCommandAsync("dag/get", cancel, id, $"output-codec={outputCodec}");
            var res = JsonConvert.DeserializeObject<T>(json);
            if (res is null)
                throw new InvalidDataException($"The response did not deserialize to the provided type.");

            return res;
        }

        public Task<DagResolveOutput> ResolveAsync(string path, CancellationToken cancel = default)
        {
            return ipfs.DoCommandAsync<DagResolveOutput>("dag/resolve", cancel, path);
        }

        public async Task<DagStatSummary> StatAsync(string cid, IProgress<DagStatSummary>? progress = null, CancellationToken cancel = default)
        {
            using var stream = await ipfs.PostDownloadAsync("dag/stat", cancel, cid, $"progress={(progress is not null).ToString().ToLowerInvariant()}");
            DagStatSummary? current = null;

            // Read line-by-line
            using var reader = new StreamReader(stream);
            while (!reader.EndOfStream)
            {
                cancel.ThrowIfCancellationRequested();
                var json = await reader.ReadLineAsync();

                current = JsonConvert.DeserializeObject<DagStatSummary>(json);

                if (current is not null)
                    progress?.Report(current);
            }

            return current ?? throw new InvalidDataException("The response did not contain a DAG stat summary.");
        }

        public Task<Stream> ExportAsync(string path, CancellationToken cancellationToken = default)
        {
            // Kubo expects POST for dag/export
            return ipfs.PostDownloadAsync("dag/export", cancellationToken, path);
        }

        public async Task<CarImportOutput> ImportAsync(Stream stream, bool? pinRoots = null, bool stats = false, CancellationToken cancellationToken = default)
        {
            // Respect Kubo default (pin roots = true) by omitting the flag when null.
            var optionsList = new System.Collections.Generic.List<string>();
            if (pinRoots.HasValue)
                optionsList.Add($"pin-roots={pinRoots.Value.ToString().ToLowerInvariant()}");

            optionsList.Add($"stats={stats.ToString().ToLowerInvariant()}");
            var options = optionsList.ToArray();

            using var resultStream = await ipfs.Upload2Async("dag/import", cancellationToken, stream, null, options);

            // Read line-by-line
            using var reader = new StreamReader(resultStream);

            // First output line may be absent on older Kubo when pin-roots=false
            var json = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(json))
            {
                return new CarImportOutput();
            }

            var res = JsonConvert.DeserializeObject<CarImportOutput>(json);
            if (res is null)
                throw new InvalidDataException($"The response did not deserialize to {nameof(CarImportOutput)}.");

            // Second output is always of type DagStatSummary
            if (stats)
            {
                json = await reader.ReadLineAsync();
                if (!string.IsNullOrEmpty(json))
                {
                    var importStats = JsonConvert.DeserializeObject<CarImportStats>(json);
                    if (importStats is null)
                        throw new InvalidDataException($"The response did not deserialize a {nameof(CarImportStats)}.");

                    res.Stats = importStats;
                }
            }

            return res;
        }
    }
}
