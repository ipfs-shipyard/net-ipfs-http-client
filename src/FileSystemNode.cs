using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace Ipfs.Http
{
    /// <inheritdoc />
    [DataContract]
    public class FileSystemNode : IFileSystemNode
    {
        private IpfsClient? ipfsClient;
        private IEnumerable<IFileSystemLink>? links;
        private long? size;
        private bool? isDirectory;
        private Cid? id;

        /// <inheritdoc />
        public byte[] DataBytes
        {
            get
            {
                using (var stream = DataStream)
                {
                    if (stream is null)
                        return Array.Empty<byte>();

                    using (var data = new MemoryStream())
                    {
                        stream.CopyTo(data);
                        return data.ToArray();
                    }
                }
            }
        }

        /// <inheritdoc />
        public Stream DataStream => IpfsClient.FileSystem.ReadFileAsync(Id).GetAwaiter().GetResult();

        /// <inheritdoc />
        [DataMember]
        public Cid Id
        {
            get => id ?? throw new InvalidDataException("Field mus be initialized");
            set => id = value;
        }

        /// <inheritdoc />
        [DataMember]
        public IEnumerable<IFileSystemLink> Links
        {
            get
            {
                if (links is null) GetInfo();
                return links!;
            }
            set
            {
                links = value;
            }
        }

        /// <summary>
        ///   Size of the file contents.
        /// </summary>
        /// <value>
        ///   This is the size of the file not the raw encoded contents
        ///   of the block.
        /// </value>
        [DataMember]
        public long Size
        {
            get
            {
                if (!size.HasValue) GetInfo();
                return size!.Value;
            }
            set
            {
                size = value;
            }
        }

        /// <summary>
        ///   Determines if the link is a directory (folder).
        /// </summary>
        /// <value>
        ///   <b>true</b> if the link is a directory; Otherwise <b>false</b>,
        ///   the link is some type of a file.
        /// </value>
        [DataMember]
        public bool IsDirectory
        {
            get
            {
                if (!isDirectory.HasValue) GetInfo();
                return isDirectory!.Value;
            }
            set
            {
                isDirectory = value;
            }
        }

        /// <summary>
        ///   The file name of the IPFS node.
        /// </summary>
        [DataMember]
        public string? Name { get; set; }

        /// <inheritdoc />
        public IFileSystemLink ToLink(string name = "")
        {
            var link = new FileSystemLink(Id)
            {
                Name = string.IsNullOrWhiteSpace(name) ? Name : name,
                Id = Id,
                Size = Size,
            };
            return link;
        }

        /// <summary>
        ///   The client to IPFS.
        /// </summary>
        /// <value>
        ///   Used to fetch additional information on the node.
        /// </value>
        public IpfsClient IpfsClient
        {
            get
            {
                if (ipfsClient is null)
                {
                    lock (this)
                    {
                        ipfsClient ??= new IpfsClient();
                    }
                }
                return ipfsClient;
            }
            set
            {
                ipfsClient = value;
            }
        }

        void GetInfo()
        {
            var node = IpfsClient.FileSystem.ListFileAsync(Id).Result;
            this.IsDirectory = node.IsDirectory;
            this.Links = node.Links;
            this.Size = node.Size;
        }
    }
}
