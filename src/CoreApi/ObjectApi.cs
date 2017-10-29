﻿using Common.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Api
{

    /// <summary>
    ///   Manages the Directed Acrylic Graph.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   This API is accessed via the <see cref="IpfsClient.Object"/> property.
    ///   </para>
    /// </remarks>
    /// <seealso href="https://github.com/ipfs/interface-ipfs-core/tree/master/API/object">Object API</seealso>
    public class ObjectApi
    {
        static ILog log = LogManager.GetLogger<ObjectApi>();

        IpfsClient ipfs;

        /// <summary>
        ///  TODO
        /// </summary>
        public class DagInfo
        {
            /// <summary>
            ///  TODO
            /// </summary>
            public string Hash { get; set; }
            /// <summary>
            ///  TODO
            /// </summary>
            public int NumLinks { get; set; }
            /// <summary>
            ///  TODO
            /// </summary>
            public long BlockSize { get; set; }
            /// <summary>
            ///  TODO
            /// </summary>
            public long LinksSize { get; set; }
            /// <summary>
            ///  TODO
            /// </summary>
            public long DataSize { get; set; }
            /// <summary>
            ///  TODO
            /// </summary>
            public long CumulativeSize { get; set; }
        }

        internal ObjectApi(IpfsClient ipfs)
        {
            this.ipfs = ipfs;
        }

        /// <summary>
        ///   Creates a new file directory in IPFS.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        ///   Equivalent to <c>NewAsync("unixfs-dir")</c>.
        /// </remarks>
        public Task<DagNode> NewDirectoryAsync(CancellationToken cancel = default(CancellationToken))
        {
            return NewAsync("unixfs-dir", cancel);
        }

        /// <summary>
        ///   Create a new MerkleDAG node, using a specific layout.
        /// </summary>
        /// <param name="template"><b>null</b> or "unixfs-dir".</param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns></returns>
        /// <remarks>
        ///  Caveat: So far, only UnixFS object layouts are supported.
        /// </remarks>
        public async Task<DagNode> NewAsync(string template = null, CancellationToken cancel = default(CancellationToken))
        {
            var json = await ipfs.PostCommandAsync("object/new", cancel, template);
            var hash = (string) (JObject.Parse(json)["Hash"]);
            return await GetAsync(hash);
        }

        /// <summary>
        ///   Fetch a MerkleDAG node.
        /// </summary>
        /// <param name="hash">
        ///   The <see cref="string"/> representation of an encoded <see cref="Ipfs.MultiHash"/>.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns></returns>
        public async Task<DagNode> GetAsync(string hash, CancellationToken cancel = default(CancellationToken))
        {
            var json = await ipfs.DoCommandAsync("object/get", cancel, hash);
            return GetDagFromJson(json);
        }

        /// <summary>
        ///   Store a MerkleDAG node.
        /// </summary>
        /// <param name="data">
        ///   The opaque data, can be <b>null</b>.
        /// </param>
        /// <param name="links">
        ///   The links to other nodes.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        public Task<DagNode> PutAsync(byte[] data, IEnumerable<IMerkleLink> links = null, CancellationToken cancel = default(CancellationToken))
        {
            return PutAsync(new DagNode(data, links), cancel);
        }

        /// <summary>
        ///   Store a MerkleDAG node.
        /// </summary>
        /// <param name="node">A merkle dag</param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        public async Task<DagNode> PutAsync(DagNode node, CancellationToken cancel = default(CancellationToken))
        {
            var json = await ipfs.UploadAsync("object/put", cancel, node.ToArray(), "inputenc=protobuf");
            return node;
        }

        /// <summary>
        ///   Get the data of a MerkleDAG node.
        /// </summary>
        /// <param name="hash">
        ///   The <see cref="string"/> representation of an encoded <see cref="Ipfs.MultiHash"/>.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns></returns>
        /// <remarks>
        ///   The caller must dispose the returned <see cref="Stream"/>.
        /// </remarks>
        public Task<Stream> DataAsync(string hash, CancellationToken cancel = default(CancellationToken))
        {
            return ipfs.DownloadAsync("object/data", cancel, hash);
        }

        /// <summary>
        ///   Get the links of a MerkleDAG node.
        /// </summary>
        /// <param name="hash">
        ///   The <see cref="string"/> representation of an encoded <see cref="Ipfs.MultiHash"/>.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>A sequence of links</returns>
        public async Task<IEnumerable<IMerkleLink>> LinksAsync(string hash, CancellationToken cancel = default(CancellationToken))
        {
            var json = await ipfs.DoCommandAsync("object/links", cancel, hash);
            return GetDagFromJson(json).Links;
        }

        /// <summary>
        ///   Get the statistics of a MerkleDAG node.
        /// </summary>
        /// <param name="hash">
        ///   The <see cref="string"/> representation of an encoded <see cref="Ipfs.MultiHash"/>.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns></returns>
        public Task<DagInfo> StatAsync(string hash, CancellationToken cancel = default(CancellationToken))
        {
            return ipfs.DoCommandAsync<DagInfo>("object/stat", cancel, hash);
        }

        // TOOD: patch sub API

        DagNode GetDagFromJson(string json)
        {
            var result = JObject.Parse(json);
            byte[] data = null;
            var stringData = (string)result["Data"];
            if (stringData != null)
                data = Encoding.UTF8.GetBytes(stringData);
            var links = ((JArray)result["Links"])
                .Select(link => new DagLink(
                    (string)link["Name"],
                    (string)link["Hash"],
                    (long)link["Size"]));
            return new DagNode(data, links);
        }
    }
}
