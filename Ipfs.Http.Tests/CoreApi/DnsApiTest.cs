﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Http
{
    [TestClass]
    public class DnsApiTest
    {
        [TestMethod]
        public void Api_Exists()
        {
            IpfsClient ipfs = TestFixture.Ipfs;
            Assert.IsNotNull(ipfs.Dns);
        }

        [TestMethod]
        public async Task Resolve()
        {
            IpfsClient ipfs = TestFixture.Ipfs;
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var path = await ipfs.Dns.ResolveAsync("ipfs.io", recursive: true, cancel: cts.Token);
            StringAssert.StartsWith(path, "/ipfs/");
        }

        [TestMethod]
        [Ignore("takes forever")]
        public async Task Publish()
        {
            var ipfs = TestFixture.Ipfs;
            var cs = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            var content = await ipfs.FileSystem.AddTextAsync("hello world");
            var key = await ipfs.Key.CreateAsync("name-publish-test", "rsa", 1024);
            try
            {
                var result = await ipfs.Name.PublishAsync(content.Id, key.Name, cancel: cs.Token);
                Assert.IsNotNull(result);
                StringAssert.EndsWith(result.NamePath, key.Id.ToString());
                StringAssert.EndsWith(result.ContentPath, content.Id.Encode());
            }
            finally
            {
                await ipfs.Key.RemoveAsync(key.Name);
            }
        }
    }
}
