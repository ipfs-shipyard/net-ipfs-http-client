using System.Collections.Generic;

#nullable enable
namespace Ipfs.Http
{
    /// <summary>
    ///   Non-streaming response DTO for /api/v0/pin/ls.
    /// </summary>
    internal record PinListResponseDto
    {
        public Dictionary<string, PinInfoDto>? Keys { get; init; }
    }

    /// <summary>
    ///   DTO for entry value in PinListResponseDto.Keys.
    /// </summary>
    internal record PinInfoDto
    {
        public string? Name { get; init; }
        public string? Type { get; init; }
    }

    /// <summary>
    ///   Streaming response DTO for /api/v0/pin/ls?stream=true.
    /// </summary>
    internal record PinLsObjectDto
    {
        public string? Cid { get; init; }
        public string? Name { get; init; }
        public string? Type { get; init; }
    }

    /// <summary>
    ///   Response DTO for /api/v0/pin/add and /api/v0/pin/rm which both return a Pins array.
    /// </summary>
    internal record PinChangeResponseDto
    {
    public int? Progress { get; init; }
        public List<string>? Pins { get; init; }
    }
}
