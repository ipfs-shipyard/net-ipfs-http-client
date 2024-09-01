using Ipfs.CoreApi;
using Newtonsoft.Json;

namespace Ipfs.Http.CoreApi
{
    /// <summary>
    /// Model for the hold filestore key
    /// </summary>
    public class Key : IFilesStoreKey
    {
        /// <summary>
        /// Key value.
        /// </summary>
        [JsonProperty("/")]
        public string Value { get; set; }
    }

}
