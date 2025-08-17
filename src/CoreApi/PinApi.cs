using Ipfs.CoreApi;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.IO;
using Newtonsoft.Json;

#nullable enable
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
            var dto = JsonConvert.DeserializeObject<PinChangeResponseDto>(json);
            var pins = dto?.Pins ?? new List<string>();
            return pins.Select(p => (Cid)p);
        }

        public async Task<IEnumerable<Cid>> AddAsync(string path, PinAddOptions options, IProgress<BlocksPinnedProgress> progress, CancellationToken cancel = default)
        {
            options ??= new PinAddOptions();
            var optList = new List<string>
            {
                "recursive=" + options.Recursive.ToString().ToLowerInvariant(),
                "progress=true"
            };
            if (!string.IsNullOrEmpty(options.Name))
            {
                optList.Add("name=" + options.Name);
            }
            var pinned = new List<Cid>();
            var stream = await ipfs.PostDownloadAsync("pin/add", cancel, path, optList.ToArray());
            using var sr = new StreamReader(stream);
            while (!sr.EndOfStream && !cancel.IsCancellationRequested)
            {
                var line = await sr.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                var dto = JsonConvert.DeserializeObject<PinChangeResponseDto>(line);
                if (dto is null)
                    continue;
                if (dto.Progress.HasValue)
                {
                    progress?.Report(new BlocksPinnedProgress { BlocksPinned = dto.Progress.Value });
                }
                if (dto.Pins != null)
                {
                    foreach (var p in dto.Pins)
                    {
                        pinned.Add((Cid)p);
                    }
                }
            }
            return pinned;
        }

        public async IAsyncEnumerable<PinListItem> ListAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancel = default)
        {
            // Default non-streaming, no names
            foreach (var item in await ListItemsOnceAsync(null, new List<string>(), cancel))
            {
                yield return item;
            }
        }

        public async IAsyncEnumerable<PinListItem> ListAsync(PinType type, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancel = default)
        {
            var opts = new List<string> { $"type={type.ToString().ToLowerInvariant()}" };
            foreach (var item in await ListItemsOnceAsync(null, opts, cancel))
            {
                yield return item;
            }
        }

        public async IAsyncEnumerable<PinListItem> ListAsync(PinListOptions options, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancel = default)
        {
            options ??= new PinListOptions();
            var opts = new List<string>();
            if (options.Type != PinType.All)
                opts.Add($"type={options.Type.ToString().ToLowerInvariant()}");
            if (!string.IsNullOrEmpty(options.Name))
            {
                opts.Add($"name={options.Name}");
                opts.Add("names=true");
            }
            else if (options.Names)
            {
                opts.Add("names=true");
            }

            if (options.Stream)
            {
                await foreach (var item in ListItemsStreamAsync(null, opts, options.Names, cancel))
                {
                    yield return item;
                }
            }
            else
            {
                foreach (var item in await ListItemsOnceAsync(null, opts, cancel))
                {
                    yield return item;
                }
            }
        }

        public async Task<IEnumerable<Cid>> RemoveAsync(Cid id, bool recursive = true, CancellationToken cancel = default(CancellationToken))
        {
            var opts = "recursive=" + recursive.ToString().ToLowerInvariant();
            var json = await ipfs.DoCommandAsync("pin/rm", cancel, id, opts);
            var dto = JsonConvert.DeserializeObject<PinChangeResponseDto>(json);
            var pins = dto?.Pins ?? new List<string>();
            return pins.Select(p => (Cid)p);
        }

    // Internal helper used by ListAsync overloads

        async IAsyncEnumerable<PinListItem> ListItemsStreamAsync(string? path, List<string> opts, bool includeNames, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancel)
        {
            opts = new List<string>(opts) { "stream=true" };
            var stream = await ipfs.PostDownloadAsync("pin/ls", cancel, path, opts.ToArray());
            using var sr = new StreamReader(stream);
            while (!sr.EndOfStream && !cancel.IsCancellationRequested)
            {
                var line = await sr.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                var dto = JsonConvert.DeserializeObject<PinLsObjectDto>(line);
                if (dto is null || string.IsNullOrEmpty(dto.Cid))
                    continue;
                yield return new PinListItem
                {
                    Cid = (Cid)dto.Cid!,
                    Type = ParseType(dto.Type),
                    Name = dto.Name
                };
            }
        }

        async Task<IEnumerable<PinListItem>> ListItemsOnceAsync(string? path, List<string> opts, CancellationToken cancel)
        {
            var json = await ipfs.DoCommandAsync("pin/ls", cancel, path, opts.ToArray());
            var root = JsonConvert.DeserializeObject<PinListResponseDto>(json);
            var list = new List<PinListItem>();
            if (root?.Keys != null)
            {
                foreach (var kv in root.Keys)
                {
                    list.Add(new PinListItem
                    {
                        Cid = (Cid)kv.Key!,
                        Type = ParseType(kv.Value?.Type),
                        Name = string.IsNullOrEmpty(kv.Value?.Name) ? null : kv.Value!.Name
                    });
                }
            }
            return list;
        }

        static PinType ParseType(string? t)
        {
            return t?.ToLowerInvariant() switch
            {
                "direct" => PinType.Direct,
                "indirect" => PinType.Indirect,
                "recursive" => PinType.Recursive,
                "all" => PinType.All,
                _ => PinType.All
            };
        }

    }

}
