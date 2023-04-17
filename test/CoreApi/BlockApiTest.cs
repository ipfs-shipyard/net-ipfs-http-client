using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Http
{
    [TestClass]
    public class BlockApiTest
    {
        private readonly IpfsClient ipfs = TestFixture.Ipfs;
        private const string id = "bafkreiaxnnnb7qz2focittuqq3ya25q7rcv3bqynnczfzako47346wosmu";
        private readonly byte[] blob = Encoding.UTF8.GetBytes("blorb");

        [TestMethod]
        public async Task Put_Bytes()
        {
            var cid = await ipfs.Block.PutAsync(blob);
            Assert.AreEqual(id, (string)cid);

            var data = await ipfs.Block.GetAsync(cid);
            Assert.AreEqual(blob.Length, data.Size);
            CollectionAssert.AreEqual(blob, data.DataBytes);
        }

        [TestMethod]
        public async Task Put_Bytes_ContentType()
        {
            var cid = await ipfs.Block.PutAsync(blob, contentType: "raw");
            Assert.AreEqual("bafkreiaxnnnb7qz2focittuqq3ya25q7rcv3bqynnczfzako47346wosmu", (string)cid);

            var data = await ipfs.Block.GetAsync(cid);
            Assert.AreEqual(blob.Length, data.Size);
            CollectionAssert.AreEqual(blob, data.DataBytes);
        }

        [TestMethod]
        public async Task Put_Bytes_Hash()
        {
            var cid = await ipfs.Block.PutAsync(blob, "raw", "sha2-512");
            Assert.AreEqual("bafkrgqelljziv4qfg5mefz36m2y3h6voaralnw6lwb4f53xcnrf4mlsykkn7vt6eno547tw5ygcz62kxrle45wnbmpbofo5tvu57jvuaf7k7e", (string)cid);

            var data = await ipfs.Block.GetAsync(cid);
            Assert.AreEqual(blob.Length, data.Size);
            CollectionAssert.AreEqual(blob, data.DataBytes);
        }

        [TestMethod]
        public async Task Put_Bytes_Pinned()
        {
            var data1 = new byte[] { 23, 24, 127 };
            var cid1 = await ipfs.Block.PutAsync(data1, contentType: "raw", pin: true);
            var pins = await ipfs.Pin.ListAsync();
            Assert.IsTrue(pins.Any(pin => pin == cid1));

            var data2 = new byte[] { 123, 124, 27 };
            var cid2 = await ipfs.Block.PutAsync(data2, contentType: "raw", pin: false);
            pins = await ipfs.Pin.ListAsync();
            Assert.IsFalse(pins.Any(pin => pin == cid2));
        }

        [TestMethod]
        public async Task Put_Stream()
        {
            var cid = await ipfs.Block.PutAsync(new MemoryStream(blob));
            Assert.AreEqual(id, (string)cid);

            var data = await ipfs.Block.GetAsync(cid);
            Assert.AreEqual(blob.Length, data.Size);
            CollectionAssert.AreEqual(blob, data.DataBytes);
        }

        [TestMethod]
        public async Task Put_Stream_ContentType()
        {
            var cid = await ipfs.Block.PutAsync(new MemoryStream(blob), contentType: "raw");
            Assert.AreEqual("bafkreiaxnnnb7qz2focittuqq3ya25q7rcv3bqynnczfzako47346wosmu", (string)cid);

            var data = await ipfs.Block.GetAsync(cid);
            Assert.AreEqual(blob.Length, data.Size);
            CollectionAssert.AreEqual(blob, data.DataBytes);
        }

        [TestMethod]
        public async Task Put_Stream_Hash()
        {
            var cid = await ipfs.Block.PutAsync(new MemoryStream(blob), "raw", "sha2-512");
            Assert.AreEqual("bafkrgqelljziv4qfg5mefz36m2y3h6voaralnw6lwb4f53xcnrf4mlsykkn7vt6eno547tw5ygcz62kxrle45wnbmpbofo5tvu57jvuaf7k7e", (string)cid);

            var data = await ipfs.Block.GetAsync(cid);
            Assert.AreEqual(blob.Length, data.Size);
            CollectionAssert.AreEqual(blob, data.DataBytes);
        }

        [TestMethod]
        public async Task Put_Stream_Pinned()
        {
            var data1 = new MemoryStream(new byte[] { 23, 24, 127 });
            var cid1 = await ipfs.Block.PutAsync(data1, contentType: "raw", pin: true);
            var pins = await ipfs.Pin.ListAsync();
            Assert.IsTrue(pins.Any(pin => pin == cid1));

            var data2 = new MemoryStream(new byte[] { 123, 124, 27 });
            var cid2 = await ipfs.Block.PutAsync(data2, contentType: "raw", pin: false);
            pins = await ipfs.Pin.ListAsync();
            Assert.IsFalse(pins.Any(pin => pin == cid2));
        }

        [TestMethod]
        public async Task Get()
        {
            var _ = await ipfs.Block.PutAsync(blob);
            var block = await ipfs.Block.GetAsync(id);
            Assert.AreEqual(id, (string)block.Id);
            CollectionAssert.AreEqual(blob, block.DataBytes);
            var blob1 = new byte[blob.Length];
            block.DataStream.Read(blob1, 0, blob1.Length);
            CollectionAssert.AreEqual(blob, blob1);
        }

        [TestMethod]
        public async Task Stat()
        {
            var _ = await ipfs.Block.PutAsync(blob);
            var info = await ipfs.Block.StatAsync(id);
            Assert.AreEqual(id, (string)info.Id);
            Assert.AreEqual(5, info.Size);
        }

        [TestMethod]
        public async Task Remove()
        {
            var _ = await ipfs.Block.PutAsync(blob);
            var cid = await ipfs.Block.RemoveAsync(id);
            Assert.IsNotNull(cid);
            Assert.AreEqual(id, (string)cid!);
        }

        [TestMethod]
        public void Remove_Unknown()
        {
            ExceptionAssert.Throws<Exception>(() => { var _ = ipfs.Block.RemoveAsync("QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rFF").GetAwaiter().GetResult(); });
        }

        [TestMethod]
        public async Task Remove_Unknown_OK()
        {
            var cid = await ipfs.Block.RemoveAsync("QmPv52ekjS75L4JmHpXVeuJ5uX2ecSfSZo88NSyxwA3rFF", ignoreNonexistent: true);
            Assert.IsNull(cid);
        }
    }
}
