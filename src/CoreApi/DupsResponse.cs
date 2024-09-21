using Ipfs.CoreApi;

namespace Ipfs.Http.CoreApi
{
    /// <summary>
    /// Model holding response from <see cref="FilestoreApi"/> Dups command.
    /// </summary>
    public class DupsResponse : IDupsResponse
    {
        /// <summary>
        /// Any error in the <see cref="IFilestoreApi"/> Dups response.
        /// </summary>
        public string Err { get; set; }

        /// <summary>
        /// The error in the <see cref="IFilestoreApi"/> Dups response.
        /// </summary>
        public string Ref { get; set; }
    }

}
