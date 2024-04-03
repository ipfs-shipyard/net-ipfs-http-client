using System.Runtime.Serialization;

namespace Ipfs.Http
{
    /// <inheritdoc />
    [DataContract]
    public class Block : IDataBlock
    {
        /// <summary>
        ///  The data of the block.
        /// </summary>
        public byte[] DataBytes { get; set; }

        /// <inheritdoc />
        [DataMember]
        public required Cid Id { get; set; }

        /// <inheritdoc />
        [DataMember]
        public required long Size { get; set; }
    }

}
