using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ipfs.Http
{
    [TestClass]
    public sealed class BitswapApiTest
    {
        private readonly IpfsClient ipfs = TestFixture.Ipfs;

        [TestMethod]
        public async Task Wants()
        {
            var block = new DagNode(Encoding.UTF8.GetBytes("BitswapApiTest unknown block"));
            await RunAsyncTaskAndTestAsync(
                ct => ipfs.Bitswap.GetAsync(block.Id, ct),
                async () =>
                {
                    var endTime = DateTime.Now.AddSeconds(10);
                    while (DateTime.Now < endTime)
                    {
                        await Task.Delay(100);
                        var wants = await ipfs.Bitswap.WantsAsync();
                        if (wants.Contains(block.Id))
                        {
                            return;
                        }
                    }
                    Assert.Fail("wanted block is missing");
                });
        }

        [TestMethod]
        [Ignore("https://github.com/ipfs/go-ipfs/issues/5295")]
        public async Task Unwant()
        {
            var block = new DagNode(Encoding.UTF8.GetBytes("BitswapApiTest unknown block 2"));
            await RunAsyncTaskAndTestAsync(
                ct => ipfs.Bitswap.GetAsync(block.Id, ct),
                async () =>
                {
                    var endTime = DateTime.Now.AddSeconds(10);
                    while (true)
                    {
                        if (DateTime.Now > endTime)
                            Assert.Fail("wanted block is missing");
                        await Task.Delay(100);
                        var wants = await ipfs.Bitswap.WantsAsync();
                        if (wants.Contains(block.Id))
                            break;
                    }

                    await ipfs.Bitswap.UnwantAsync(block.Id);
                    endTime = DateTime.Now.AddSeconds(10);
                    while (true)
                    {
                        if (DateTime.Now > endTime)
                            Assert.Fail("unwanted block is present");
                        await Task.Delay(100);
                        var wants = await ipfs.Bitswap.WantsAsync();
                        if (!wants.Contains(block.Id))
                            break;
                    }
                });
        }

        [TestMethod]
        public async Task Ledger()
        {
            var peer = new Peer { Id = "QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3" };
            var ledger = await ipfs.Bitswap.LedgerAsync(peer);
            Assert.IsNotNull(ledger);
            Assert.IsNotNull(ledger.Peer);
            Assert.AreEqual(peer.Id, ledger.Peer!.Id);
        }

        private static async Task RunAsyncTaskAndTestAsync(Func<CancellationToken, Task> asyncTaskWork, Func<Task> testWork)
        {
            var cts = new CancellationTokenSource();
            var asyncTask = Task.Run(async () => await asyncTaskWork(cts.Token));
            try
            {
                await testWork();
            }
            finally
            {
                cts.Cancel();
                try
                {
                    await asyncTask;
                }
                catch
                {
                }
            }
        }
    }
}
