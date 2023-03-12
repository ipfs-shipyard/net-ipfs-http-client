﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;

namespace Ipfs.Http
{
    [TestClass]
    public class FileSystemNodeTest
    {
        [TestMethod]
        public async Task Serialization()
        {
            var ipfs = TestFixture.Ipfs;
            var a = await ipfs.FileSystem.AddTextAsync("hello world");
            Assert.AreEqual("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", (string)a.Id);

            var b = await ipfs.FileSystem.ListFileAsync(a.Id);
            var json = JsonConvert.SerializeObject(b);
            var c = JsonConvert.DeserializeObject<FileSystemNode>(json);
            Assert.AreEqual(b.Id, c.Id);
            Assert.AreEqual(b.IsDirectory, c.IsDirectory);
            Assert.AreEqual(b.Size, c.Size);
            CollectionAssert.AreEqual(b.Links.ToArray(), c.Links.ToArray());
        }
    }
}
