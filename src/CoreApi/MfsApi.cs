using Ipfs.CoreApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Http
{
    /// <inheritdoc />
    public class MfsApi : IMfsApi
    {
        private IpfsClient ipfs;

        internal MfsApi(IpfsClient ipfs)
        {
            this.ipfs = ipfs;
        }

        /// <inheritdoc />
        public async Task CopyAsync(string sourceMfsPathOrCid, string destMfsPath, bool? parents = null, CancellationToken cancel = default)
        {
            List<string> args = new List<string>() { $"arg={destMfsPath}" };
            if (parents.HasValue)
                args.Add($"parents={parents.Value.ToString().ToLower()}");

            await ipfs.DoCommandAsync("files/cp", cancel, sourceMfsPathOrCid, args.ToArray());
        }

        /// <inheritdoc />
        public async Task<Cid> FlushAsync(string path = null, CancellationToken cancel = default)
        {
            var json = await ipfs.DoCommandAsync("files/flush", cancel, path);
            var r = JObject.Parse(json);
            string cid = (string)r["Cid"];
            return (Cid)cid;
        }

        /// <inheritdoc />
        /// <remarks>
        ///   <para>
        ///   The <c>files/ls</c> API iterates the <c>PBNode.Links</c> array of the directory
        ///   without decoding each child node's UnixFS <c>Data</c> blob. As a result, the
        ///   <see cref="IFileSystemNode.Mode"/>, <see cref="IFileSystemNode.Mtime"/>, and
        ///   <see cref="IFileSystemNode.MtimeNsecs"/> properties will always be <c>null</c>
        ///   for nodes returned by this method.
        ///   </para>
        ///   <para>
        ///   To retrieve UnixFS 1.5 metadata (mode/mtime), use <see cref="StatAsync(string, CancellationToken)"/> on
        ///   individual paths, which decodes the target node's UnixFS <c>Data</c> blob.
        ///   </para>
        /// </remarks>
        public async Task<IEnumerable<IFileSystemNode>> ListAsync(string path, bool? U = null, CancellationToken cancel = default)
        {
            List<string> args = new List<string>() { "long=true" };
            if (U.HasValue)
                args.Add($"U={U.Value.ToString().ToLower()}");

            IEnumerable<IFileSystemNode> nodes = null;

            var json = await ipfs.DoCommandAsync("files/ls", cancel, path, args.ToArray());
            var r = JObject.Parse(json);
            var links = r["Entries"] as JArray;
            if (links != null)
            {
                // Note: Mode, Mtime, and MtimeNsecs are intentionally not populated here.
                // The files/ls API does not decode each child's UnixFS Data blob, so these
                // fields are not available. Use StatAsync to retrieve UnixFS 1.5 metadata.
                nodes = links
                    .Select(l => new FileSystemNode()
                    {
                        Name = (string)l["Name"],
                        Id = (string)l["Hash"],
                        Size = (ulong)l["Size"],
                        IsDirectory = (int)l["Type"] == 1,
                    })
                    .ToArray();
            }

            return nodes;
        }

        /// <inheritdoc />
        public async Task MakeDirectoryAsync(string path, bool? parents = null, int? cidVersion = null, string multiHash = null, CancellationToken cancel = default)
        {
            List<string> args = new List<string>();
            if (parents.HasValue)
                args.Add($"parents={parents.Value.ToString().ToLower()}");
            if (cidVersion.HasValue)
                args.Add($"cid-version={cidVersion.Value}");
            if (!string.IsNullOrWhiteSpace(multiHash))
                args.Add($"hash={multiHash}");

            await ipfs.DoCommandAsync("files/mkdir", cancel, path, args.ToArray());
        }

        /// <inheritdoc />
        public async Task MoveAsync(string sourceMfsPath, string destMfsPath, CancellationToken cancel = default)
        {
            var args = new string[] { $"arg={destMfsPath}" };
            await ipfs.DoCommandAsync("files/mv", cancel, sourceMfsPath, args);
        }

        /// <inheritdoc />
        public async Task<string> ReadFileAsync(string path, long? offset = null, long? count = null, CancellationToken cancel = default)
        {
            if (count == 0)
                count = int.MaxValue; // go-ipfs only accepts int lengths

            List<string> args = new List<string>();
            if (offset != null)
                args.Add($"offset={offset.Value}");
            if (offset != null)
                args.Add($"count={count.Value}");

            using (var data = await ipfs.PostDownloadAsync("files/read", cancel, path, args?.ToArray()))
            using (var text = new StreamReader(data))
            {
                return await text.ReadToEndAsync();
            }
        }

        /// <inheritdoc />
        public async Task<Stream> ReadFileStreamAsync(string path, long? offset = null, long? count = null, CancellationToken cancel = default)
        {
            if (count == 0)
                count = int.MaxValue; // go-ipfs only accepts int lengths

            List<string> args = new List<string>();
            if (offset != null)
                args.Add($"offset={offset.Value}");
            if (offset != null)
                args.Add($"count={count.Value}");

            return await ipfs.PostDownloadAsync("files/read", cancel, path, args?.ToArray());
        }

        /// <inheritdoc />
        public async Task RemoveAsync(string path, bool? recursive = null, bool? force = null, CancellationToken cancel = default)
        {
            List<string> args = new List<string>();
            if (recursive.HasValue)
                args.Add($"recursive={recursive.Value.ToString().ToLower()}");
            if (force.HasValue)
                args.Add($"force={force.Value.ToString().ToLower()}");

            await ipfs.DoCommandAsync("files/rm", cancel, path, args.ToArray());
        }

        /// <inheritdoc />
        public async Task<FileStatResult> StatAsync(string path, CancellationToken cancel = default)
        {
            var json = await ipfs.DoCommandAsync("files/stat", cancel, path);
            return (FileStatResult)FileStatResultFromJson(json);
        }

        /// <inheritdoc />
        public async Task<FileStatWithLocalityResult> StatAsync(string path, bool withLocal, CancellationToken cancel = default)
        {
            var args = new string[] { $"with-local={withLocal.ToString().ToLower()}" };
            var json = await ipfs.DoCommandAsync("files/stat", cancel, path, args.ToArray());
            return FileStatResultFromJson(json);
        }

        private FileStatWithLocalityResult FileStatResultFromJson(string json)
        {
            var r = JObject.Parse(json);
            return new FileStatWithLocalityResult()
            {
                Blocks = (int)r["Blocks"],
                CumulativeSize = (Int64)r["CumulativeSize"],
                Hash = (Cid)(string)r["Hash"],
                Local = ((bool?)r["Local"]) ?? false,
                Size = (Int64)r["Size"],
                SizeLocal = (Int64?)r["SizeLocal"] ?? 0,
                IsDirectory = (string)r["Type"] == "directory",
                Mode = (string)r["Mode"],
                Mtime = (long?)r["Mtime"],
                MtimeNsecs = (int?)r["MtimeNsecs"],
                WithLocality = ((bool?)r["WithLocality"]) ?? false,
            };
        }

        /// <inheritdoc />
        public async Task WriteAsync(string path, string text, MfsWriteOptions options, CancellationToken cancel = default)
        {
            using (MemoryStream textData = new MemoryStream())
            using (StreamWriter writer = new StreamWriter(textData))
            {
                writer.Write(text);
                writer.Flush();
                textData.Position = 0;
                await WriteAsync(path, textData, options, cancel);
            }
        }

        /// <inheritdoc />
        public async Task WriteAsync(string path, byte[] data, MfsWriteOptions options, CancellationToken cancel = default)
        {
            using (MemoryStream byteStream = new MemoryStream(data))
            {
                await WriteAsync(path, byteStream, options, cancel);
            }
        }

/// <inheritdoc />
        public async Task TouchAsync(string path, DateTimeOffset? mtime = null, CancellationToken cancel = default)
        {
            var args = new List<string>();
            if (mtime.HasValue)
            {
                args.Add($"mtime={mtime.Value.ToUnixTimeSeconds()}");
                long nsecs = (mtime.Value.Ticks % TimeSpan.TicksPerSecond) * 100;
                if (nsecs > 0)
                    args.Add($"mtime-nsecs={nsecs}");
            }

            await ipfs.DoCommandAsync("files/touch", cancel, path, args.ToArray());
        }

        /// <inheritdoc />
        public async Task ChmodAsync(string path, string mode, bool? recursive = null, CancellationToken cancel = default)
        {
            var args = new List<string> { $"arg={path}" };
            if (recursive.HasValue)
                args.Add($"recursive={recursive.Value.ToString().ToLower()}");

            await ipfs.DoCommandAsync("files/chmod", cancel, mode, args.ToArray());
        }

        /// <inheritdoc />
        public async Task WriteAsync(string path, Stream data, MfsWriteOptions options, CancellationToken cancel = default)
        {
            if (options == null)
                options = new MfsWriteOptions();
            var opts = new List<string>();
            opts.Add($"arg={path}");
            if (options.Create.HasValue)
                opts.Add($"create={options.Create.ToString().ToLower()}");
            if (options.Parents.HasValue)
                opts.Add($"parents={options.Parents.ToString().ToLower()}");
            if (options.Offset.HasValue)
                opts.Add($"offset={options.Offset.Value}");
            if (options.Count.HasValue)
                opts.Add($"count={options.Count.Value}");
            if (options.CidVersion.HasValue)
                opts.Add($"cid-version={options.CidVersion.Value}");
            if (options.Truncate.HasValue)
                opts.Add($"truncate={options.Truncate.ToString().ToLower()}");
            if (options.RawLeaves.HasValue)
                opts.Add($"raw-leaves={options.RawLeaves.ToString().ToLower()}");
            if (options.Hash != null && options.Hash != MultiHash.DefaultAlgorithmName)
                opts.Add($"hash=${options.Hash}");
            if (options.Flush.HasValue)
                opts.Add($"flush={options.Flush.ToString().ToLower()}");

            if (string.IsNullOrEmpty(path) || !path.StartsWith("/"))
                throw new ArgumentException("Argument path is required and must start with '/'.");
            
            var name=path.Split(new char[] { '/' }).Last();
            if (string.IsNullOrEmpty(path) || !path.StartsWith("/"))
                throw new ArgumentException("Argument path must specify a filename.");

            await ipfs.Upload2Async("files/write", cancel, data, name, opts.ToArray());
        }
    }
}










