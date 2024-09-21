using Ipfs.CoreApi;
using Newtonsoft.Json;

namespace Ipfs.Http
{
    /// <summary>
    /// Model for the hold filestore key
    /// </summary>
    public class FilestoreKey : IFilesStoreKey
    {
        /// <summary>
        /// Key value.
        /// </summary>
        [JsonProperty("/")]
        public string _ { get; set; }
    }

}
