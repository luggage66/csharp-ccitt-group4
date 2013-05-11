using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using CCITTCodecs;

namespace CodecTests
{
    [TestClass]
    public class BitWriterTests
    {
        [TestMethod]
        public void BitWriterTest()
        {
            byte[] correctValues = new byte[]
            {
                //8 1's
                0xff, //1111 1111

                //2x 4bit words
                0xA5, //1010 0101

                //1x 16bit word
                0x6d, //0110 1101
                0x68, //0110 1010

                //5 bits + 5bits + 6bits = 16bits
                0xdb,
                0x6d,

                //3 bits + 3bits + 2bits
                0x24, //0010 0100

                //unused
                0,
                0,

                //end on a partial 3bit word, 101, and the rest should be blank
                0xa0 //1010 000
            };

            byte[] testValues = new byte[10];

            var ms = new MemoryStream(testValues);

            var writer = new BitWriter(ms);
            writer.Write(255, 8); //11111111

            writer.Write(10, 4); //1010
            writer.Write(5, 4); //0101

            writer.Write(28008, 16); //0110 1101 0110 1010

            //5+5+6
            writer.Write(27, 5);
            writer.Write(13, 5);
            writer.Write(45, 6);

            //3+3+2
            writer.Write(1, 3);
            writer.Write(1, 3);
            writer.Write(0, 2);

            //placeholder for the unused set;
            writer.Write(0, 16);

            //partial byte at the last one.
            writer.Write(5, 3);

            writer.Flush();


            Assert.IsTrue(Validate(ms, correctValues), "Incorrect Values in stream");
        }

        private bool Validate(Stream s, byte[] byteArray)
        {
            Assert.IsTrue(s.Length == byteArray.Length, "Output stream the wrong length.");

            s.Seek(0, SeekOrigin.Begin);

            int b;
            int index = 0;
            while ((b = s.ReadByte()) != -1)
            {
                if (b != byteArray[index])
                    return false;

                index++;
            }

            return true;
        }
    }
}

