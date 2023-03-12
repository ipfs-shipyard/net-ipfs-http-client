﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Http
{
    [TestClass]
    public class PubSubApiTest
    {
        [TestMethod]
        public void Api_Exists()
        {
            IpfsClient ipfs = TestFixture.Ipfs;
            Assert.IsNotNull(ipfs.PubSub);
        }

        [TestMethod]
        public async Task Peers()
        {
            var ipfs = TestFixture.Ipfs;
            var topic = "net-ipfs-http-client-test-" + Guid.NewGuid();
            var cs = new CancellationTokenSource();
            try
            {
                await ipfs.PubSub.SubscribeAsync(topic, msg => { }, cs.Token);
                var peers = ipfs.PubSub.PeersAsync().Result.ToArray();
                Assert.IsTrue(peers.Length > 0);
            }
            finally
            {
                cs.Cancel();
            }
        }

        [TestMethod]
        public void Peers_Unknown_Topic()
        {
            var ipfs = TestFixture.Ipfs;
            var topic = "net-ipfs-http-client-test-unknown" + Guid.NewGuid();
            var peers = ipfs.PubSub.PeersAsync(topic).Result.ToArray();
            Assert.AreEqual(0, peers.Length);
        }

        [TestMethod]
        public async Task Subscribed_Topics()
        {
            var ipfs = TestFixture.Ipfs;
            var topic = "net-ipfs-http-client-test-" + Guid.NewGuid();
            var cs = new CancellationTokenSource();
            try
            {
                await ipfs.PubSub.SubscribeAsync(topic, msg => { }, cs.Token);
                var topics = ipfs.PubSub.SubscribedTopicsAsync().Result.ToArray();
                Assert.IsTrue(topics.Length > 0);
                CollectionAssert.Contains(topics, topic);
            }
            finally
            {
                cs.Cancel();
            }
        }

        volatile int messageCount = 0;

        [TestMethod]
        public async Task Subscribe()
        {
            messageCount = 0;
            var ipfs = TestFixture.Ipfs;
            var topic = "net-ipfs-http-client-test-" + Guid.NewGuid();
            var cs = new CancellationTokenSource();
            try
            {
                await ipfs.PubSub.SubscribeAsync(topic, msg =>
                {
                    Interlocked.Increment(ref messageCount);
                }, cs.Token);
                await ipfs.PubSub.PublishAsync(topic, "hello world!");

                await Task.Delay(1000);
                Assert.AreEqual(1, messageCount);
            }
            finally
            {
                cs.Cancel();
            }
        }

        [TestMethod]
        public async Task Subscribe_Mutiple_Messages()
        {
            messageCount = 0;
            var messages = "hello world this is pubsub".Split();
            var ipfs = TestFixture.Ipfs;
            var topic = "net-ipfs-http-client-test-" + Guid.NewGuid();
            var cs = new CancellationTokenSource();
            try
            {
                await ipfs.PubSub.SubscribeAsync(topic, msg =>
                {
                    Interlocked.Increment(ref messageCount);
                }, cs.Token);
                foreach (var msg in messages)
                {
                    await ipfs.PubSub.PublishAsync(topic, msg);
                }

                await Task.Delay(1000);
                Assert.AreEqual(messages.Length, messageCount);
            }
            finally
            {
                cs.Cancel();
            }
        }

        [TestMethod]
        public async Task Multiple_Subscribe_Multiple_Messages()
        {
            messageCount = 0;
            var messages = "hello world this is pubsub".Split();
            var ipfs = TestFixture.Ipfs;
            var topic = "net-ipfs-http-client-test-" + Guid.NewGuid();
            var cs = new CancellationTokenSource();
            Action<IPublishedMessage> processMessage = (msg) =>
            {
                Interlocked.Increment(ref messageCount);
            };
            try
            {
                await ipfs.PubSub.SubscribeAsync(topic, processMessage, cs.Token);
                await ipfs.PubSub.SubscribeAsync(topic, processMessage, cs.Token);
                foreach (var msg in messages)
                {
                    await ipfs.PubSub.PublishAsync(topic, msg);
                }

                await Task.Delay(1000);
                Assert.AreEqual(messages.Length * 2, messageCount);
            }
            finally
            {
                cs.Cancel();
            }
        }

        volatile int messageCount1 = 0;

        [TestMethod]
        public async Task Unsubscribe()
        {
            messageCount1 = 0;
            var ipfs = TestFixture.Ipfs;
            var topic = "net-ipfs-http-client-test-" + Guid.NewGuid();
            var cs = new CancellationTokenSource();
            await ipfs.PubSub.SubscribeAsync(topic, msg =>
            {
                Interlocked.Increment(ref messageCount1);
            }, cs.Token);
            await ipfs.PubSub.PublishAsync(topic, "hello world!");
            await Task.Delay(1000);
            Assert.AreEqual(1, messageCount1);

            cs.Cancel();
            await ipfs.PubSub.PublishAsync(topic, "hello world!!!");
            await Task.Delay(1000);
            Assert.AreEqual(1, messageCount1);
        }

        [TestMethod]
        public async Task Subscribe_BinaryMessage()
        {
            var messages = new List<IPublishedMessage>();
            var expected = new byte[] { 0, 1, 2, 4, (byte)'a', (byte)'b', 0xfe, 0xff };
            var ipfs = TestFixture.Ipfs;
            var topic = "net-ipfs-http-client-test-" + Guid.NewGuid();
            var cs = new CancellationTokenSource();
            try
            {
                await ipfs.PubSub.SubscribeAsync(topic, msg =>
                {
                    messages.Add(msg);
                }, cs.Token);
                await ipfs.PubSub.PublishAsync(topic, expected);

                await Task.Delay(1000);
                Assert.AreEqual(1, messages.Count);
                CollectionAssert.AreEqual(expected, messages[0].DataBytes);
            }
            finally
            {
                cs.Cancel();
            }
        }

        [TestMethod]
        public async Task Subscribe_StreamMessage()
        {
            var messages = new List<IPublishedMessage>();
            var expected = new byte[] { 0, 1, 2, 4, (byte)'a', (byte)'b', 0xfe, 0xff };
            var ipfs = TestFixture.Ipfs;
            var topic = "net-ipfs-http-client-test-" + Guid.NewGuid();
            var cs = new CancellationTokenSource();
            try
            {
                await ipfs.PubSub.SubscribeAsync(topic, msg =>
                {
                    messages.Add(msg);
                }, cs.Token);
                var ms = new MemoryStream(expected, false);
                await ipfs.PubSub.PublishAsync(topic, ms);

                await Task.Delay(1000);
                Assert.AreEqual(1, messages.Count);
                CollectionAssert.AreEqual(expected, messages[0].DataBytes);
            }
            finally
            {
                cs.Cancel();
            }
        }
    }
}
