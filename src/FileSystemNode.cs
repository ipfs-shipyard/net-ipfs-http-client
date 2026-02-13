#nullable enable
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Ipfs.Http
{
    /// <summary>
    ///   Implementation of <see cref="IFileSystemNode"/> for the HTTP client.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   This class represents a decoded UnixFS DAG node. See <see cref="IFileSystemNode"/> for
    ///   details on the UnixFS specification alignment.
    ///   </para>
    ///   <para>
    ///   The <see cref="Mode"/>, <see cref="Mtime"/>, and <see cref="MtimeNsecs"/> properties
    ///   are only populated when the node is retrieved via an API that decodes the UnixFS <c>Data</c>
    ///   blob (e.g., <c>files/stat</c>). Listing APIs do not populate these fields.
    ///   </para>
    /// </remarks>
    [DataContract]
    public class FileSystemNode : IFileSystemNode
    {
        /// <inheritdoc />
        [DataMember]
        public required Cid Id { get; set; }

        /// <inheritdoc />
        [DataMember]
        public IEnumerable<IFileSystemLink> Links { get; set; } = [];

        /// <summary>
        ///   Size of the file contents.
        /// </summary>
        /// <value>
        ///   This is the size of the file not the raw encoded contents
        ///   of the block.
        /// </value>
        [DataMember]
        public ulong Size { get; set; }

        /// <inheritdoc />
        [DataMember]
        public bool IsDirectory { get; set; }

        /// <inheritdoc />
        [DataMember]
        public string Name { get; set; } = string.Empty;

        /// <inheritdoc />
        [DataMember]
        public string? Mode { get; set; }

        /// <inheritdoc />
        [DataMember]
        public long? Mtime { get; set; }

        /// <inheritdoc />
        [DataMember]
        public int? MtimeNsecs { get; set; }

        /// <summary>
        ///   Converts this node to a <see cref="FileSystemLink"/>.
        /// </summary>
        /// <param name="name">Optional name override for the link.</param>
        /// <returns>A <see cref="FileSystemLink"/> representing this node.</returns>
        public IFileSystemLink ToLink(string name = "")
        {
            var link = new FileSystemLink
            {
                Name = string.IsNullOrWhiteSpace(name) ? Name : name,
                Id = Id,
                Size = Size,
            };
            return link;
        }
    }
}
