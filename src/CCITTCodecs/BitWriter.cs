using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace CCITTCodecs
{
    /// <summary>
    /// Writes bits to a stream. Once 8 bits are received (in one or more .Write() calls) a complete byte is written to the stream. .Flush() Writes any remaining bits to the stream as a 0-padded byte.
    /// </summary>
    public class BitWriter
    {
        Stream stream;
        byte buffer;
        int bitsInBuffer;

        public BitWriter(Stream stream)
        {
            this.stream = stream;
        }

        public void Write(uint value, uint countOfBits)
        {
            var bitsLeftToWrite = (int)countOfBits;

            while ((bitsLeftToWrite + bitsInBuffer) >= 8)
            {
                //push bits into buffer
                var roomInBuffer = 8 - bitsInBuffer;

                buffer = (byte)(buffer << roomInBuffer);
                buffer += (byte)((value >> (bitsLeftToWrite - roomInBuffer)) & masks[roomInBuffer]);

                //value = value >> roomInBuffer;

                stream.WriteByte(buffer);
                bitsInBuffer = 0;

                bitsLeftToWrite -= roomInBuffer;
            }

            //at this point the value is whatever has NOT bee written and it's smaller than a bytes.
            buffer = (byte)(buffer << bitsLeftToWrite);
            buffer += (byte)(value & masks[bitsLeftToWrite]);
            bitsInBuffer += bitsLeftToWrite;
        }

        //writes any remaining partial byte
        public void Flush()
        {
            if (bitsInBuffer != 0)
            {
                var roomInBuffer = 8 - bitsInBuffer;
                buffer = (byte)(buffer << roomInBuffer); ;

                stream.WriteByte(buffer);
                bitsInBuffer = 0;
            }
        }

        static readonly byte[] masks = { 0, 1, 3, 7, 15, 31, 63, 127, 255 };
    }
}
