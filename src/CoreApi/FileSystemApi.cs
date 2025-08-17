using Ipfs.CoreApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Ipfs.Http
{

    class FileSystemApi : IFileSystemApi
    {
        private IpfsClient ipfs;

        internal FileSystemApi(IpfsClient ipfs)
        {
            this.ipfs = ipfs;
        }

        public async Task<IFileSystemNode> AddFileAsync(string path, AddFileOptions? options = null, CancellationToken cancel = default)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var node = await AddAsync(stream, Path.GetFileName(path), options, cancel);
                return node;
            }
        }

        public Task<IFileSystemNode> AddTextAsync(string text, AddFileOptions? options = null, CancellationToken cancel = default)
        {
            return AddAsync(new MemoryStream(Encoding.UTF8.GetBytes(text), false), "", options, cancel);
        }

        public async Task<IFileSystemNode> AddAsync(Stream stream, string name = "", AddFileOptions? options = null, CancellationToken cancel = default)
        {
            var filePart = new FilePart { Name = name, Data = stream };
            await foreach (var item in AddAsync([filePart], [], options, cancel))
                return item;

            throw new InvalidOperationException("No file nodes were provided");
        }

        public async IAsyncEnumerable<IFileSystemNode> AddAsync(FilePart[] fileParts, FolderPart[] folderParts, AddFileOptions? options = default, [EnumeratorCancellation] CancellationToken cancel = default)
        {
            string boundary = $"{Guid.NewGuid()}";
            var content = new OrderedMultipartFormDataContent(boundary);

            foreach (var folderPart in folderParts)
                AddApiHeader(content, folderPart);

            foreach (var filePart in fileParts)
                AddApiHeader(content, filePart);

            var url = ipfs.BuildCommand("add", null, ToApiOptions(options));

            // Create the request message
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };

            // Enable chunked transfer encoding
            request.Headers.TransferEncodingChunked = true;

            // Remove the Content-Length header if it exists
            request.Content.Headers.ContentLength = null;
            
            using var response = await ipfs.Api().SendAsync(request, cancel);
            await ipfs.ThrowOnErrorAsync(response);
            
             // The result is a stream of LDJSON objects.
             // See https://github.com/ipfs/go-ipfs/issues/4852
             using var stream = await response.Content.ReadAsStreamAsync();
             using var sr = new StreamReader(stream);

             using var jr = new JsonTextReader(sr) { SupportMultipleContent = true };

             while (await jr.ReadAsync(cancel))
             {
                 cancel.ThrowIfCancellationRequested();
                 var r = await JObject.LoadAsync(jr, cancel);

                 // For the filestore, the hash can be output instead of the bytes. Verified with small files.
                 var isFilestoreProgressOutput = !r.TryGetValue("Hash", out _);

                 // For uploads, bytes are output to report progress.
                 var isUploadProgressOutput = r.TryGetValue("Bytes", out var bytes);
                 
                 if (isUploadProgressOutput)
                 {
                     options?.Progress?.Report(new TransferProgress
                     {
                         Name = r["Name"]?.ToString() ?? throw new InvalidDataException("The response did not contain a name."),
                         Bytes = bytes?.ToObject<ulong>() ?? 0,
                     });
                 }
                 else if (!isFilestoreProgressOutput)
                 {
                     var name = r["Name"]?.ToString() ?? throw new InvalidDataException("The response did not contain a name.");
                     yield return new FileSystemNode
                     {
                         Name = name,
                         Id = r["Hash"]?.ToString() ??
                              throw new InvalidDataException("The response did not contain a hash."),
                         Size = r["Size"] is { } sz
                             ? sz.ToObject<ulong>()
                             : throw new InvalidDataException("The response did not contain a size."),
                         IsDirectory = folderParts.Any(x => x.Name == name),
                     };
                 }
             }
        }

        /// <summary>
        ///   Reads the content of an existing IPFS file as text.
        /// </summary>
        /// <param name="path">
        ///   A path to an existing file, such as "QmXarR6rgkQ2fDSHjSY5nM2kuCXKYGViky5nohtwgF65Ec/about"
        ///   or "QmZTR5bcpQD7cFgTorqxZDYaew1Wqgfbd2ud9QqGPAkK2V"
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   The contents of the <paramref name="path"/> as a <see cref="string"/>.
        /// </returns>
        public async Task<String> ReadAllTextAsync(string path, CancellationToken cancel = default)
        {
            using (var data = await ReadFileAsync(path, cancel))
            using (var text = new StreamReader(data))
            {
                return await text.ReadToEndAsync();
            }
        }

        /// <summary>
        ///   Opens an existing IPFS file for reading.
        /// </summary>
        /// <param name="path">
        ///   A path to an existing file, such as "QmXarR6rgkQ2fDSHjSY5nM2kuCXKYGViky5nohtwgF65Ec/about"
        ///   or "QmZTR5bcpQD7cFgTorqxZDYaew1Wqgfbd2ud9QqGPAkK2V"
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A <see cref="Stream"/> to the file contents.
        /// </returns>
        public Task<Stream> ReadFileAsync(string path, CancellationToken cancel = default)
        {
            return ipfs.PostDownloadAsync("cat", cancel, path);
        }

        public Task<Stream> ReadFileAsync(string path, long offset, long length = 0, CancellationToken cancel = default)
        {
            return ipfs.PostDownloadAsync("cat", cancel, path,
                $"offset={offset}",
                $"length={length}");
        }

        /// <inheritdoc cref="ListAsync"/>
        public Task<IFileSystemNode> ListFileAsync(string path, CancellationToken cancel = default)
        {
            return ListAsync(path, cancel);
        }

        /// <summary>
        ///   Get information about the directory.
        /// </summary>
        /// <param name="path">
        ///   A path to an existing directory, such as "QmZTR5bcpQD7cFgTorqxZDYaew1Wqgfbd2ud9QqGPAkK2V"
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task. When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns></returns>
        public async Task<IFileSystemNode> ListAsync(string path, CancellationToken cancel = default)
        {
            var json = await ipfs.DoCommandAsync("ls", cancel, path);
            var r = JObject.Parse(json);
            var o = (JObject?)r["Objects"]?[0];
            var h = (o?["Hash"])?.ToString() ?? throw new InvalidDataException("The response did not contain a hash.");

            var node = new FileSystemNode()
            {
                Id = h,
                IsDirectory = true,
                Links = Array.Empty<FileSystemLink>(),
            };

            if (o["Links"] is JArray links)
            {
                node.Links = links
                    .Select(l => new FileSystemLink()
                    {
                        Name = l["Name"]?.ToString() ?? throw new InvalidDataException("The response did not contain a name."),
                        Id = l["Hash"]?.ToString() ?? throw new InvalidDataException("The response did not contain a hash."),
                        Size = l["Size"] is { } sz ? sz.ToObject<ulong>() : throw new InvalidDataException("The response did not contain a size."),
                    })
                    .ToArray();
            }

            return node;
        }

        public Task<Stream> GetAsync(string path, bool compress = false, CancellationToken cancel = default)
        {
            return ipfs.PostDownloadAsync("get", cancel, path, $"compress={compress}");
        }

        public void AddApiHeader(MultipartFormDataContent content, FolderPart folderPart)
        {
            // Use a ByteArrayContent with an empty byte array to signify no content
            var folderContent = new ByteArrayContent([]); // Empty content
            folderContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-directory");
            folderContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = "\"file\"",
                FileName = $"\"{WebUtility.UrlEncode(folderPart.Name)}\""
            };

            // Add the content part to the multipart content
            content.Add(folderContent);
        }

        public void AddApiHeader(MultipartFormDataContent content, FilePart filePart)
        {
            var streamContent = new StreamContent(filePart.Data ?? new MemoryStream());
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            
            if (filePart.AbsolutePath is not null)
                streamContent.Headers.Add("Abspath-Encoded", WebUtility.UrlEncode(filePart.AbsolutePath));
            
            streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = "\"file\"",
                FileName = $"\"{WebUtility.UrlEncode(filePart.Name)}\""
            };

            content.Add(streamContent);
        }

        private string[] ToApiOptions(AddFileOptions? options)
        {
            var opts = new List<string>();

            if (options is null)
                return opts.ToArray();

            if (options.CidVersion is not null)
                opts.Add($"cid-version={options.CidVersion}");

            if (options.Inline is not null)
                opts.Add($"inline={options.Inline.ToString().ToLowerInvariant()}");

            if (options.InlineLimit is not null)
                opts.Add($"inline-limit={options.InlineLimit}");

            if (options.NoCopy is not null)
                opts.Add($"nocopy={options.NoCopy.ToString().ToLowerInvariant()}");

            if (options.Pin is not null)
                opts.Add($"pin={options.Pin.ToString().ToLowerInvariant()}");
                
            if (!string.IsNullOrEmpty(options.PinName))
                opts.Add($"pin-name={options.PinName}");

            if (options.Wrap is not null)
                opts.Add($"wrap-with-directory={options.Wrap.ToString().ToLowerInvariant()}");

            if (options.RawLeaves is not null)
                opts.Add($"raw-leaves={options.RawLeaves.ToString().ToLowerInvariant()}");

            if (options.OnlyHash is not null)
                opts.Add($"only-hash={options.OnlyHash.ToString().ToLowerInvariant()}");

            if (options.Trickle is not null)
                opts.Add($"trickle={options.Trickle.ToString().ToLowerInvariant()}");

            if (options.Chunker is not null)
                opts.Add($"chunker={options.Chunker}");

            if (options.Progress is not null)
                opts.Add("progress=true");

            if (options.Hash is not null)
                opts.Add($"hash={options.Hash}");

            if (options.FsCache is not null)
                opts.Add($"fscache={options.FsCache.ToString().ToLowerInvariant()}");

            if (options.ToFiles is not null)
                opts.Add($"to-files={options.ToFiles}");

            if (options.PreserveMode is not null)
                opts.Add($"preserve-mode={options.PreserveMode.ToString().ToLowerInvariant()}");

            if (options.PreserveMtime is not null)
                opts.Add($"preserve-mtime={options.PreserveMtime.ToString().ToLowerInvariant()}");

            if (options.Mode is not null)
                opts.Add($"mode={options.Mode}");

            if (options.Mtime is not null)
                opts.Add($"mtime={options.Mtime}");

            if (options.MtimeNsecs is not null)
                opts.Add($"mtime-nsecs={options.MtimeNsecs}");

            return opts.ToArray();
        }
    }
}
