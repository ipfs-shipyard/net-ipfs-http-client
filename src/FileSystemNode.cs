using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Ipfs.Http
{
    /// <inheritdoc />
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
        public long Size { get; set; }

        /// <summary>
        ///   Determines if the link is a directory (folder).
        /// </summary>
        /// <value>
        ///   <b>true</b> if the link is a directory; Otherwise <b>false</b>,
        ///   the link is some type of a file.
        /// </value>
        [DataMember]
        public bool IsDirectory { get; set; }

        /// <summary>
        ///   The file name of the IPFS node.
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <inheritdoc />
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
