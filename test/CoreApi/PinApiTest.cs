using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ipfs.CoreApi;
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
            var pins = ipfs.Pin.ListAsync().Result;
            Assert.IsNotNull(pins);
            Assert.IsTrue(pins.Count() > 0);
        }

        [TestMethod]
        public async Task List_WithType_All()
        {
            var ipfs = TestFixture.Ipfs;
            var pins = await ipfs.Pin.ListAsync(PinType.All);
            Assert.IsNotNull(pins);
            Assert.IsTrue(pins.Count() > 0);
        }

        [TestMethod]
        public async Task Add_Remove()
        {
            var ipfs = TestFixture.Ipfs;
            var result = await ipfs.FileSystem.AddTextAsync("I am pinned");
            var id = result.Id;

            var pins = await ipfs.Pin.AddAsync(id, new PinAddOptions { Recursive = true });
            Assert.IsTrue(pins.Any(pin => pin == id));
            var all = await ipfs.Pin.ListAsync();
            Assert.IsTrue(all.Any(pin => pin == id));

            pins = await ipfs.Pin.RemoveAsync(id);
            Assert.IsTrue(pins.Any(pin => pin == id));
            all = await ipfs.Pin.ListAsync();
            Assert.IsFalse(all.Any(pin => pin == id));
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

            var all = await ipfs.Pin.ListAsync();
            Assert.IsTrue(all.Any(pin => pin == id));

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

    }
}
