using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ipfs.Http
{
    [TestClass]
    public class BlockTest
    {
        private byte[] someBytes = new byte[] { 1, 2, 3 };

        [TestMethod]
        public void DataBytes()
        {
            var block = new Block
            {
                DataBytes = someBytes
            };
            CollectionAssert.AreEqual(someBytes, block.DataBytes);
        }

    }
}
