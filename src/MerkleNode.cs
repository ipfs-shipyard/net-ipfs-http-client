using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Ipfs.Http
{
    /// <summary>
    ///   The IPFS <see href="https://github.com/ipfs/specs/blob/main/MERKLE_DAG.md">MerkleDag</see> is the datastructure at the heart of IPFS.
    ///   It is an acyclic directed graph whose edges are hashes.
    /// </summary>
    /// <remarks>
    ///   Initially an <b>MerkleNode</b> is just constructed with its Cid.
    /// </remarks>
    [DataContract]
    public class MerkleNode : IMerkleNode<IMerkleLink>, IEquatable<MerkleNode>
    {
        ulong blockSize;
        string name;
        IEnumerable<IMerkleLink> links = [];

        /// <summary>
        ///   Creates a new instance of the <see cref="MerkleNode"/> with the specified
        ///   <see cref="Cid"/> and optional <see cref="Name"/>.
        /// </summary>
        /// <param name="id">
        ///   The <see cref="Cid"/> of the node.
        /// </param>
        /// <param name="name">A name for the node.</param>
        public MerkleNode(Cid id, string name = null)
        {
            if (id == null)
                throw new ArgumentNullException("id");

            Id = id;
            Name = name;
        }

        /// <summary>
        ///   Creates a new instance of the <see cref="MerkleNode"/> with the specified
        ///   <see cref="Id">cid</see> and optional <see cref="Name"/>.
        /// </summary>
        /// <param name="path">
        ///   The string representation of a <see cref="Cid"/> of the node or "/ipfs/cid".
        /// </param>
        /// <param name="name">A name for the node.</param>
        public MerkleNode(string path, string name = null)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException("path");

            if (path.StartsWith("/ipfs/"))
                path = path.Substring(6);

            Id = Cid.Decode(path);
            Name = name;
        }

        /// <summary>
        ///   Creates a new instance of the <see cref="MerkleNode"/> from the
        ///   <see cref="IMerkleLink"/>.
        /// </summary>
        /// <param name="link">The link to a node.</param>
        public MerkleNode(IMerkleLink link)
        {
            Id = link.Id;
            Name = link.Name;
            blockSize = link.Size;
        }

        /// <inheritdoc />
        [DataMember]
        public Cid Id { get; private set; }

        /// <summary>
        ///   The name for the node.  If unknown it is "" (not null).
        /// </summary>
        [DataMember]
        public string Name
        {
            get { return name; }
            set { name = value ?? string.Empty; }
        }

        /// <inheritdoc />
        /// <seealso cref="blockSize"/>
        [DataMember]
        public ulong Size => blockSize;

        /// <inheritdoc />
        [DataMember]
        public IEnumerable<IMerkleLink> Links => links;

        /// <inheritdoc />
        public IMerkleLink ToLink(string name = null)
        {
            return new DagLink(name ?? Name, Id, Size);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            var that = obj as MerkleNode;
            return that != null && this.Id == that.Id;
        }

        /// <inheritdoc />
        public bool Equals(MerkleNode that)
        {
            return that != null && this.Id == that.Id;
        }

        /// <summary>
        ///  TODO
        /// </summary>
        public static bool operator ==(MerkleNode a, MerkleNode b)
        {
            if (object.ReferenceEquals(a, b)) return true;
            if (object.ReferenceEquals(a, null)) return false;
            if (object.ReferenceEquals(b, null)) return false;

            return a.Equals(b);
        }

        /// <summary>
        ///  TODO
        /// </summary>
        public static bool operator !=(MerkleNode a, MerkleNode b)
        {
            if (object.ReferenceEquals(a, b)) return false;
            if (object.ReferenceEquals(a, null)) return true;
            if (object.ReferenceEquals(b, null)) return true;

            return !a.Equals(b);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return "/ipfs/" + Id;
        }
    }
}
