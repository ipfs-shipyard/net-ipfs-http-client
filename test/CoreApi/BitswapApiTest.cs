using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Ipfs.Http
{
    [TestClass]
    public class BitswapApiTest
    {
        private IpfsClient ipfs = TestFixture.Ipfs;

        [TestMethod]
        public async Task Ledger()
        {
            var peer = new Peer { Id = "QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3" };
            var ledger = await ipfs.Bitswap.LedgerAsync(peer);
            Assert.IsNotNull(ledger);
            Assert.AreEqual(peer.Id, ledger.Peer.Id);
        }
    }
}
