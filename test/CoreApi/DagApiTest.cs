using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Threading.Tasks;

namespace Ipfs.Http
{
    [TestClass]
    public class DagApiTest
    {
        class Name
        {
            public string First { get; set; }

            public string Last { get; set; }
        }

        [TestMethod]
        public async Task PutAndGet_JSON()
        {
            var ipfs = TestFixture.Ipfs;
            var expected = new JObject();
            expected["a"] = "alpha";
            var expectedId = "bafyreigdhej736dobd6z3jt2vxsxvbwrwgyts7e7wms6yrr46rp72uh5bu";
            var id = await ipfs.Dag.PutAsync(expected);
            Assert.IsNotNull(id);
            Assert.AreEqual(expectedId, (string)id);

            var actual = await ipfs.Dag.GetAsync(id);
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected["a"], actual["a"]);

            var value = (string)await ipfs.Dag.GetAsync(expectedId + "/a");
            Assert.AreEqual(expected["a"], value);
        }

        [TestMethod]
        public async Task PutAndGet_POCO()
        {
            var ipfs = TestFixture.Ipfs;
            var expected = new Name { First = "John", Last = "Smith" };
            var id = await ipfs.Dag.PutAsync(expected);
            Assert.IsNotNull(id);

            var actual = await ipfs.Dag.GetAsync<Name>(id);
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.First, actual.First);
            Assert.AreEqual(expected.Last, actual.Last);

            var value = (string)await ipfs.Dag.GetAsync(id.Encode() + "/Last");
            Assert.AreEqual(expected.Last, value);
        }

        [TestMethod]
        public async Task Import_Default_Pins_Roots()
        {
            var ipfs = TestFixture.Ipfs;

            var node = await ipfs.FileSystem.AddTextAsync("car import default pin");
            await using var car = await ipfs.Dag.ExportAsync(node.Id);

            // ensure unpinned first
            await ipfs.Pin.RemoveAsync(node.Id);

            var result = await ipfs.Dag.ImportAsync(car, pinRoots: null, stats: false);
            Assert.IsNotNull(result.Root);

            var pins = await ipfs.Pin.ListAsync().ToArrayAsync();
            Assert.IsTrue(pins.Any(p => p.Cid == node.Id));
        }

        [TestMethod]
        public async Task Import_PinRoots_False_Does_Not_Pin()
        {
            var ipfs = TestFixture.Ipfs;

            var node = await ipfs.FileSystem.AddTextAsync("car import nopin");
            await using var car = await ipfs.Dag.ExportAsync(node.Id);

            // ensure unpinned first
            await ipfs.Pin.RemoveAsync(node.Id);

            var result = await ipfs.Dag.ImportAsync(car, pinRoots: false, stats: false);
            // Some Kubo versions emit no Root output when pin-roots=false; allow null.

            var pins = await ipfs.Pin.ListAsync().ToArrayAsync();
            Assert.IsFalse(pins.Any(p => p.Cid == node.Id));
        }

        [TestMethod]
        public async Task Export_Then_Import_Roundtrip_Preserves_Root()
        {
            var ipfs = TestFixture.Ipfs;

            var node = await ipfs.FileSystem.AddTextAsync("car export roundtrip");

            // ensure unpinned first so import with pinRoots=true creates a new pin
            try { await ipfs.Pin.RemoveAsync(node.Id); } catch { }

            await using var car = await ipfs.Dag.ExportAsync(node.Id);
            Assert.IsNotNull(car);

            var result = await ipfs.Dag.ImportAsync(car, pinRoots: true, stats: false);
            Assert.IsNotNull(result.Root);
            Assert.AreEqual(node.Id.ToString(), result.Root!.Cid.ToString());

            // Verify it is pinned now
            var pins = await ipfs.Pin.ListAsync().ToArrayAsync();
            Assert.IsTrue(pins.Any(p => p.Cid == node.Id));
        }
    }
}

