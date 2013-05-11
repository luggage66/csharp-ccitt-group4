using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace CodecTests
{
    [TestClass]
    public class EncoderTests
    {
        [TestMethod]
        [DeploymentItem("sample1.bmp")]
        [DeploymentItem("sample1.group4")]
        public void EncoderTest1()
        {
            byte[] bmpData;
            byte[] referenceData;

            using (FileStream fs = new FileStream("sample1.bmp", FileMode.Open))
            {
                bmpData = new byte[fs.Length];
                fs.Read(bmpData, 0, bmpData.Length);
                fs.Close();
            }

            using (FileStream fs = new FileStream("sample1.group4", FileMode.Open))
            {
                referenceData = new byte[fs.Length];
                fs.Read(referenceData, 0, referenceData.Length);
                fs.Close();
            }

            var encoder = new CCITTCodecs.CCITTEncoder();

            var outputStreamMock = new Moq.Mock<Stream>(Moq.MockBehavior.Strict);

            //we do NOT setup .Seek() as the encoder should not be calling it.

            //the position property is used int he encoder (to return bytes written) and by the below .WriteByte() test
            outputStreamMock.SetupProperty(o => o.Position, 0);

            // this compares each byte as it's written to the reference encoding. It failed immadiately if the wrong byte is written
            //it also causes the position to be incremented and it there for essential to the operation of the encoder
            outputStreamMock.Setup(o => o.WriteByte(Moq.It.Is<byte>(b => TestByte(b, referenceData, outputStreamMock.Object))));

            
            using (var inputStream = new MemoryStream(bmpData, 62, bmpData.Length - 62, false))
            {
                encoder.Encode(inputStream, 0, 1702, 746, outputStreamMock.Object);
            }

            Assert.AreEqual(referenceData.LongLength, outputStreamMock.Object.Position, "Incorrect number of bytes encoded.");


        }

        bool TestByte(byte b, byte[] buffer, Stream moqStream)
        {
            //putting this in the same line as the comparison can lead to problems when in the debugger (increments multiple times).
            var position = moqStream.Position++;

            return b == buffer[position];
        }
    }
}
