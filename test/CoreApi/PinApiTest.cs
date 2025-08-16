using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ipfs.CoreApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ipfs.Http
{
    [TestClass]
    public class PinApiTest
    {
        [TestMethod]
        public void List()
        {
            var ipfs = TestFixture.Ipfs;
            var pins = ipfs.Pin.ListAsync().ToEnumerable().ToArray();
            Assert.IsNotNull(pins);
            Assert.IsTrue(pins.Length > 0);
        }

        [TestMethod]
        public async Task List_WithType_All()
        {
            var ipfs = TestFixture.Ipfs;
            var pins = await ipfs.Pin.ListAsync(PinType.All).ToArrayAsync();
            Assert.IsNotNull(pins);
            Assert.IsTrue(pins.Length > 0);
        }

        [TestMethod]
        public async Task Add_Remove()
        {
            var ipfs = TestFixture.Ipfs;
            var result = await ipfs.FileSystem.AddTextAsync("I am pinned");
            var id = result.Id;

            var pins = await ipfs.Pin.AddAsync(id, new PinAddOptions { Recursive = true });
            Assert.IsTrue(pins.Any(pin => pin == id));
            var all = await ipfs.Pin.ListAsync().ToArrayAsync();
            Assert.IsTrue(all.Any(pin => pin.Cid == id));

            var removed = await ipfs.Pin.RemoveAsync(id);
            Assert.IsTrue(removed.Any(pin => pin == id));
            all = await ipfs.Pin.ListAsync().ToArrayAsync();
            Assert.IsFalse(all.Any(pin => pin.Cid == id));
        }

        [TestMethod]
        public async Task Add_WithName()
        {
            var ipfs = TestFixture.Ipfs;
            var result = await ipfs.FileSystem.AddTextAsync("I am pinned with a name");
            var id = result.Id;
            var name = $"tdd-{System.Guid.NewGuid()}";

            var pins = await ipfs.Pin.AddAsync(id, new PinAddOptions { Name = name, Recursive = true });
            Assert.IsTrue(pins.Any(pin => pin == id));

            var all = await ipfs.Pin.ListAsync().ToArrayAsync();
            Assert.IsTrue(all.Any(pin => pin.Cid == id));

            // cleanup
            await ipfs.Pin.RemoveAsync(id);
        }

        [TestMethod]
        public async Task Add_WithName_NonRecursive()
        {
            var ipfs = TestFixture.Ipfs;
            var result = await ipfs.FileSystem.AddTextAsync("I am pinned non-recursive with a name", new Ipfs.CoreApi.AddFileOptions { Pin = false });
            var id = result.Id;
            var name = $"tdd-nr-{System.Guid.NewGuid()}";

            var pins = await ipfs.Pin.AddAsync(id, new PinAddOptions { Name = name, Recursive = false });
            Assert.IsTrue(pins.Any(pin => pin == id));

            // cleanup
            await ipfs.Pin.RemoveAsync(id, recursive: false);
        }

        [TestMethod]
        public async Task Add_WithProgress_Reports_And_Pins()
        {
            var ipfs = TestFixture.Ipfs;
            // Create a larger object (>256 KiB) to ensure multiple blocks
            var data = new byte[300_000];
            var node = await ipfs.FileSystem.AddAsync(new System.IO.MemoryStream(data, writable: false), "big.bin", new Ipfs.CoreApi.AddFileOptions { Pin = false });
            var id = node.Id;

            var progressValues = new List<int>();
            var progress = new Progress<BlocksPinnedProgress>(p => progressValues.Add(p.BlocksPinned));

            var pins = await ipfs.Pin.AddAsync(id, new PinAddOptions { Recursive = true }, progress);

            Assert.IsTrue(pins.Any(pin => pin == id), "Expected returned pins to contain the root CID");
            Assert.IsTrue(progressValues.Count >= 1, "Expected at least one progress report");
            Assert.IsTrue(progressValues[progressValues.Count - 1] >= 1, "Expected final progress to be >= 1");
            // Monotonic non-decreasing
            for (int i = 1; i < progressValues.Count; i++)
            {
                Assert.IsTrue(progressValues[i] >= progressValues[i - 1], "Progress should be non-decreasing");
            }

            // cleanup
            await ipfs.Pin.RemoveAsync(id);
        }

        [TestMethod]
        public async Task List_NonStreaming_Default()
        {
            var ipfs = TestFixture.Ipfs;
            var items = await ipfs.Pin.ListAsync(new PinListOptions { Stream = false }).ToArrayAsync();
            Assert.IsTrue(items.Length > 0);
            Assert.IsTrue(items.All(i => i.Cid != null));
        }

        [TestMethod]
        public async Task List_Streaming_WithNames()
        {
            var ipfs = TestFixture.Ipfs;
            // Ensure at least one named pin exists
            var n = await ipfs.FileSystem.AddTextAsync("named pin", new Ipfs.CoreApi.AddFileOptions { Pin = false });
            var id = n.Id;
            var name = $"tdd-name-{Guid.NewGuid()}";
            await ipfs.Pin.AddAsync(id, new PinAddOptions { Name = name });

            var items = new List<PinListItem>();
            await foreach (var item in ipfs.Pin.ListAsync(new PinListOptions { Stream = true, Names = true }))
            {
                items.Add(item);
            }
            Assert.IsTrue(items.Count > 0);
            Assert.IsTrue(items.Any(i => i.Cid == id));

            // cleanup
            await ipfs.Pin.RemoveAsync(id);
        }

    }
}
