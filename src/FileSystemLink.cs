using Newtonsoft.Json;

namespace Ipfs.Http
{
    /// <summary>
    ///     A link to another file system node in IPFS.
    /// </summary>
    public class FileSystemLink : IFileSystemLink
    {
        /// <inheritdoc />
        public string Name { get; set; }

        /// <inheritdoc />
        [JsonProperty("Hash")]
        public Cid Id { get; set; }

        /// <inheritdoc />
        public ulong Size { get; set; }
    }
}
