using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Ipfs.Http
{
    [TestClass]
    public class TlsReproTest
    {
        [TestMethod]
        public void Real_Tls_MultiAddress_Should_Fail()
        {
            // Real multiaddr from long-running node that contains TLS
            var realTlsAddr = "/dns4/45-86-153-40.k51qzi5uqu5dhssh49wibkxi3yw56ihw9uo7cxctyiqbfxpodudqo09swsxp1s.libp2p.direct/tcp/4001/tls/ws/p2p/12D3KooWEBaJd7msGiDjA5ATyMpVSEgja4xc6FXkxddaSM9DtHCT/p2p-circuit/p2p/12D3KooWEPx5LSWNGRtQFweBG9KMsfNjdogoeRisF9cU2s5RcrsJ";
            
            var addr = new MultiAddress(realTlsAddr);
            OwlCore.Diagnostics.Logger.LogInformation($"Successfully parsed TLS multiaddr: {addr}");
            
            // Check if it contains the protocols we expect
            var protocols = addr.Protocols.ToList();
            OwlCore.Diagnostics.Logger.LogInformation($"Protocol count: {protocols.Count}");
            
            foreach(var protocol in protocols)
            {
                OwlCore.Diagnostics.Logger.LogInformation($"Protocol: {protocol.Name} (code: {protocol.Code})");
            }
            
            Assert.IsNotNull(addr);
        }
    }
}
