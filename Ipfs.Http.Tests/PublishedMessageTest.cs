﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace Ipfs.Http
{
    [TestClass]
    public partial class PublishedMessageTest
    {
        private const string json = @"{
            ""from"":""EiDzOYdzT4BE42JXwxVM8Q19w6tx30Bp2N3T7tOH/a2nCw=="",
            ""data"":""aGVsbG8gd29ybGQ="",
            ""seqno"":""FPBVj+oTUug="",
            ""topicIDs"":[""net-ipfs-http-client-test""]
            }";

        [TestMethod]
        public void FromJson()
        {
            var msg = new PublishedMessage(json);
            Assert.AreEqual("Qmei6fBYij8gjbetgHLXmoR54iRc9hioPR7dtmBTNG3oWa", msg.Sender);
            Assert.AreEqual("14f0558fea1352e8", msg.SequenceNumber.ToHexString());
            Assert.AreEqual("68656c6c6f20776f726c64", msg.DataBytes.ToHexString());
            Assert.AreEqual("hello world", msg.DataString);
            CollectionAssert.Contains(msg.Topics.ToArray(), "net-ipfs-http-client-test");

            var data = msg.DataBytes;
            var streamData = new MemoryStream();
            msg.DataStream.CopyTo(streamData);
            CollectionAssert.AreEqual(data, streamData.ToArray());
        }

        [TestMethod]
        public void Id_NotSupported()
        {
            var msg = new PublishedMessage(json);
            ExceptionAssert.Throws<NotSupportedException>(() =>
            {
                var _ = msg.Id;
            });
        }

    }
}
