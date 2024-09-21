using Ipfs.CoreApi;

namespace Ipfs.Http
{
    /// <summary>
    /// Model holding response to <see cref="IFilestoreApi"/>.
    /// </summary>
    public class FilestoreObjectResponse : IFilestoreApiObjectResponse
    {
        /// <summary>
        /// Holds any error message.
        /// </summary>
        public string ErrorMsg { get; set; }

        /// <summary>
        /// Path to the file
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// The key to the Filestore.
        /// </summary>
        
        public FilestoreKey Key { get; set; }

        /// <summary>
        /// The response offset.
        /// </summary>
        public string Offset { get; set; }

        /// <summary>
        /// The size of the object.
        /// </summary>
        public string Size { get; set; }

        /// <summary>
        /// The object status.k
        /// </summary>
        public string Status { get; set; }
    }

}
