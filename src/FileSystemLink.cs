namespace Ipfs.Http
{
    /// <summary>
    ///     A link to another file system node in IPFS.
    /// </summary>
    public class FileSystemLink : IFileSystemLink
    {
        /// <summary>
        /// Creates a new instance of <see cref="FileSystemLink"/>.
        /// </summary>
        /// <param name="id"></param>
        public FileSystemLink(Cid id)
        {
            Id = id;
        }

        /// <inheritdoc />
        public string? Name { get; set; }

        /// <inheritdoc />
        public Cid Id { get; set; }

        /// <inheritdoc />
        public long Size { get; set; }
    }
}
