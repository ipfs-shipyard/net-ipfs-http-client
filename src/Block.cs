using System;
using System.IO;
using System.Runtime.Serialization;

namespace Ipfs.Http
{
    /// <inheritdoc />
    [DataContract]
    public class Block : IDataBlock
    {
        private long? size;
        private Cid? id;

        /// <inheritdoc />
        [DataMember]
        public Cid Id
        {
            get => id ?? throw new InvalidDataException("Value mus be initialized");
            set => id = value;
        }

        /// <inheritdoc />
        [DataMember]
        public byte[] DataBytes { get; set; } = Array.Empty<byte>();

        /// <inheritdoc />
        public Stream DataStream => new MemoryStream(DataBytes, false);

        /// <inheritdoc />
        [DataMember]
        public long Size
        {
            get
            {
                if (size.HasValue)
                {
                    return size.Value;
                }
                return DataBytes.Length;
            }
            set
            {
                size = value;
            }
        }
    }
}
