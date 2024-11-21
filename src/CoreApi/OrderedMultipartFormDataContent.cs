using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Ipfs.Http
{
    /// <summary>
    /// An ordered <see cref="MultipartFormDataContent"/>.
    /// </summary>
    internal class OrderedMultipartFormDataContent : MultipartFormDataContent
    {
        private const string CrLf = "\r\n";
        
        /// <summary>
        /// Creates a new instance of <see cref="OrderedMultipartFormDataContent"/>.
        /// </summary>
        /// <param name="boundary"></param>
        public OrderedMultipartFormDataContent(string boundary)
            : base(boundary)
        {
            Boundary = boundary;
        
            // Remove the default Content-Type header set by MultipartFormDataContent
            Headers.Remove("Content-Type");
        
            // Add the Content-Type header without quotes around the boundary
            Headers.TryAddWithoutValidation("Content-Type", $"multipart/form-data; boundary={boundary}");
        }
        
        /// <summary>
        /// The boundary for this <see cref="OrderedMultipartFormDataContent"/>.
        /// </summary>
        public string Boundary { get; set; }
        
        /// <inheritdoc />
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            // Write start boundary.
            await EncodeStringToStreamAsync(stream, "--" + Boundary + CrLf, default).ConfigureAwait(false);

            // Write each nested content.
            var output = new StringBuilder();
            var items = this.ToList();
            for (var contentIndex = 0; contentIndex < items.Count; contentIndex++)
            {
                // Write divider, headers, and content.
                var content = items[contentIndex];
                await EncodeStringToStreamAsync(stream, SerializeHeadersToString(output, contentIndex, content), default).ConfigureAwait(false);
                await content.CopyToAsync(stream, context).ConfigureAwait(false);
            }

            // Write footer boundary.
            await EncodeStringToStreamAsync(stream, CrLf + "--" + Boundary + "--" + CrLf, default).ConfigureAwait(false);
        }
        
        private static ValueTask EncodeStringToStreamAsync(Stream stream, string input, CancellationToken cancellationToken)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(input);
            return new ValueTask(stream.WriteAsync(buffer, 0, buffer.Length, cancellationToken));
        }

        private string SerializeHeadersToString(StringBuilder scratch, int contentIndex, HttpContent content)
        {
            scratch.Clear();

            // Add divider.
            if (contentIndex != 0) // Write divider for all but the first content.
            {
                scratch.Append(CrLf + "--"); // const strings
                scratch.Append(Boundary);
                scratch.Append(CrLf);
            }

            // Add headers.
            foreach (KeyValuePair<string, IEnumerable<string>> headerPair in content.Headers.OrderBy(x=> x.Key))
            {
                scratch.Append(headerPair.Key);
                scratch.Append(": ");
                string delim = string.Empty;
                foreach (string value in headerPair.Value)
                {
                    scratch.Append(delim);
                    scratch.Append(value);
                    delim = ", ";
                }
                scratch.Append(CrLf);
            }

            // Extra CRLF to end headers (even if there are no headers).
            scratch.Append(CrLf);

            return scratch.ToString();
        }
    }
}
