using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Ipfs.Http
{
    /// <inheritdoc />
    [DataContract]
    public record Block : IBlockStat
    {
        /// <inheritdoc />
        [DataMember]
        [JsonProperty("Key")]
        public required Cid Id { get; set; }

        /// <inheritdoc />
        [DataMember]
        public required int Size { get; set; }
    }
}
