using System.Runtime.Serialization;

namespace Ipfs.Http;

/// <summary>
/// IpfsFile
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