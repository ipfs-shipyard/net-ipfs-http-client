using System.Runtime.Serialization;

namespace Ipfs.Http;

/// <summary>
/// AddResponse
/// {"Name":"var","Hash":"QmPypQtsKqpUC6QzufU1ZfaPw6Kj4eN459aBMzbcfdnPAN","Size":"288"}
/// </summary>
[DataContract]
public class IpfsFile
{
    /// <summary>
    /// Name
    /// </summary>
    [DataMember(Name = "Name")]
    public string Name { get; set; }

    /// <summary>
    /// Hash
    /// </summary>
    [DataMember(Name = "Hash")]
    public string Hash { get; set; }

    /// <summary>
    /// Size
    /// </summary>
    [DataMember(Name = "Size")]
    public string Size { get; set; }
}