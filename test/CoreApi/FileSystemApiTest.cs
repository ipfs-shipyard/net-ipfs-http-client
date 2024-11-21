using Ipfs.CoreApi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Http
{
    [TestClass]
    public class FileSystemApiTest
    {
        [TestMethod]
        public void AddText()
        {
            var ipfs = TestFixture.Ipfs;
            var result = ipfs.FileSystem.AddTextAsync("hello world").Result;
            Assert.AreEqual("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", (string)result.Id);
        }

        [TestMethod]
        public void ReadText()
        {
            var ipfs = TestFixture.Ipfs;
            var node = ipfs.FileSystem.AddTextAsync("hello world").Result;
            var text = ipfs.FileSystem.ReadAllTextAsync(node.Id).Result;
            Assert.AreEqual("hello world", text);
        }

        [TestMethod]
        public void AddFile()
        {
            var path = Path.GetTempFileName();
            File.WriteAllText(path, "hello world");
            try
            {
                var ipfs = TestFixture.Ipfs;
                var result = ipfs.FileSystem.AddFileAsync(path).Result;
                Assert.AreEqual("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", (string)result.Id);
                Assert.AreEqual(0, result.Links.Count());
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void Read_With_Offset()
        {
            var ipfs = TestFixture.Ipfs;
            var indata = new MemoryStream(new byte[] { 10, 20, 30 });
            var node = ipfs.FileSystem.AddAsync(indata).Result;
            using (var outdata = ipfs.FileSystem.ReadFileAsync(node.Id, offset: 1).Result)
            {
                Assert.AreEqual(20, outdata.ReadByte());
                Assert.AreEqual(30, outdata.ReadByte());
                Assert.AreEqual(-1, outdata.ReadByte());
            }
        }

        [TestMethod]
        public void Read_With_Offset_Length_1()
        {
            var ipfs = TestFixture.Ipfs;
            var indata = new MemoryStream(new byte[] { 10, 20, 30 });
            var node = ipfs.FileSystem.AddAsync(indata).Result;
            using (var outdata = ipfs.FileSystem.ReadFileAsync(node.Id, offset: 1, count: 1).Result)
            {
                Assert.AreEqual(20, outdata.ReadByte());
                Assert.AreEqual(-1, outdata.ReadByte());
            }
        }

        [TestMethod]
        public void Read_With_Offset_Length_2()
        {
            var ipfs = TestFixture.Ipfs;
            var indata = new MemoryStream(new byte[] { 10, 20, 30 });
            var node = ipfs.FileSystem.AddAsync(indata).Result;
            using (var outdata = ipfs.FileSystem.ReadFileAsync(node.Id, offset: 1, count: 2).Result)
            {
                Assert.AreEqual(20, outdata.ReadByte());
                Assert.AreEqual(30, outdata.ReadByte());
                Assert.AreEqual(-1, outdata.ReadByte());
            }
        }

        [TestMethod]
        public void Add_NoPin()
        {
            var ipfs = TestFixture.Ipfs;
            var data = new MemoryStream(new byte[] { 11, 22, 33 });
            var options = new AddFileOptions { Pin = false };
            var node = ipfs.FileSystem.AddAsync(data, "", options).Result;
            var pins = ipfs.Pin.ListAsync().Result;
            Assert.IsFalse(pins.Any(pin => pin == node.Id));
        }

        [TestMethod]
        public async Task Add_Wrap()
        {
            var path = "hello.txt";
            File.WriteAllText(path, "hello world");
            try
            {
                var ipfs = TestFixture.Ipfs;
                var options = new AddFileOptions
                {
                    Wrap = true
                };
                var node = await ipfs.FileSystem.AddFileAsync(path, options);
                Assert.AreEqual("QmNxvA5bwvPGgMXbmtyhxA1cKFdvQXnsGnZLCGor3AzYxJ", (string)node.Id);
                Assert.AreEqual(true, node.IsDirectory);
                Assert.AreEqual(1, node.Links.Count());
                Assert.AreEqual("hello.txt", node.Links.First().Name);
                Assert.AreEqual("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", (string)node.Links.First().Id);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public async Task GetTar_EmptyDirectory()
        {
            var ipfs = TestFixture.Ipfs;
            var temp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(temp);
            try
            {
                IFileSystemNode dir = null;
                await foreach (var item in ipfs.FileSystem.AddAsync([], [], null, default))
                    dir = item;

                var dirid = dir.Id.Encode();

                using (var tar = await ipfs.FileSystem.GetAsync(dir.Id))
                {
                    var buffer = new byte[3 * 512];
                    var offset = 0;
                    while (offset < buffer.Length)
                    {
                        var n = await tar.ReadAsync(buffer, offset, buffer.Length - offset);
                        Assert.IsTrue(n > 0);
                        offset += n;
                    }
                    Assert.AreEqual(-1, tar.ReadByte());
                }
            }
            finally
            {
                DeleteTemp(temp);
            }
        }


        [TestMethod]
        public async Task AddFile_WithProgress()
        {
            var path = Path.GetTempFileName();
            File.WriteAllText(path, "hello world");
            try
            {
                var ipfs = TestFixture.Ipfs;
                var bytesTransferred = 0UL;
                var options = new AddFileOptions
                {
                    Progress = new Progress<TransferProgress>(t =>
                    {
                        bytesTransferred += t.Bytes;
                    })
                };
                var result = await ipfs.FileSystem.AddFileAsync(path, options);
                Assert.AreEqual("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", (string)result.Id);

                // Progress reports get posted on another synchronisation context.
                var stop = DateTime.Now.AddSeconds(3);
                while (DateTime.Now < stop)
                {
                    if (bytesTransferred == 11UL)
                        break;
                    await Task.Delay(10);
                }
                Assert.AreEqual(11UL, bytesTransferred);
            }
            finally
            {
                File.Delete(path);
            }
        }

        void DeleteTemp(string temp)
        {
            while (true)
            {
                try
                {
                    Directory.Delete(temp, true);
                    break;
                }
                catch (Exception)
                {
                    Thread.Sleep(1);
                    continue;  // most likely anti-virus is reading a file
                }
            }
        }

        string MakeTemp()
        {
            var temp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var x = Path.Combine(temp, "x");
            var xy = Path.Combine(x, "y");
            Directory.CreateDirectory(temp);
            Directory.CreateDirectory(x);
            Directory.CreateDirectory(xy);

            File.WriteAllText(Path.Combine(temp, "alpha.txt"), "alpha");
            File.WriteAllText(Path.Combine(temp, "beta.txt"), "beta");
            File.WriteAllText(Path.Combine(x, "x.txt"), "x");
            File.WriteAllText(Path.Combine(xy, "y.txt"), "y");
            return temp;
        }
    }
}
