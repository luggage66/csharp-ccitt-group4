using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CCITTCodecs;

namespace CodecTests
{
    [TestClass]
    public class BitReaderTests
    {
        byte[] myBytes;

        [TestInitialize]
        public void Initialize()
        {
            myBytes = new byte[]
            {
                0xff, //11111111
                0x00, //00000000
                0xaa, //10101010
                0x0f, //00001111
                0xf8  //11111000
            };
        }

        [TestMethod]
        public void BitReaderTest()
        {
            var reader = new BitReader(myBytes, myBytes.Length * 8);

            Assert.IsTrue(reader.GetBit(5), "bit 5 should be 1");

            Assert.IsFalse(reader.GetBit(8), "bit 8 should be 0");

            Assert.IsTrue(reader.GetBit(16), "bit 16 should be 1");
            Assert.IsFalse(reader.GetBit(17), "bit 17 should be 0");
        }

        [TestMethod]
        public void BitReaderSpanTest()
        {
            var reader = new BitReader(myBytes, 32); //32 because i'm using the first 4 bytes only. The last partial byte is a separate test

            Assert.AreEqual(16, reader.GetNextMatchingBit(1, true));

            Assert.AreEqual(16, reader.GetNextMatchingBit(0, true));

            Assert.AreEqual(8, reader.GetNextMatchingBit(0, false));

            Assert.AreEqual(16, reader.GetNextMatchingBit(8, true));
            Assert.AreEqual(28, reader.GetNextMatchingBit(27, true));
            Assert.AreEqual(16, reader.GetNextMatchingBit(8, true));

            //i should get 32 (the max legth) when starting at 31 (length - 1) with either value matching
            Assert.AreEqual(32, reader.GetNextMatchingBit(31, true));
            Assert.AreEqual(32, reader.GetNextMatchingBit(31, false));

        }

        [TestMethod]
        public void BitReaderSpanTestWithPadding()
        {
            var reader = new BitReader(myBytes, myBytes.Length * 8 - 3); //the last 3 bits are padding and should NOT effect the outcome


            Assert.AreEqual(8, reader.GetNextMatchingBit(0, false));

            Assert.AreEqual(16, reader.GetNextMatchingBit(8, true));
            Assert.AreEqual(28, reader.GetNextMatchingBit(27, true));
            Assert.AreEqual(16, reader.GetNextMatchingBit(8, true));

            //Assert.AreEqual(32, reader.GetNextMatchingBit(31, true));
            Assert.AreEqual(37, reader.GetNextMatchingBit(31, false));

        }
    }
}
