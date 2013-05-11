using System;
using System.Collections.Generic;
using System.Text;

namespace CCITTCodecs
{
    public class BitReader
    {
        private static readonly byte[] bitMasks = new byte[]
        {
            0x80,
            0x40,
            0x20,
            0x10,
            0x08,
            0x04,
            0x02,
            0x01
        };

        byte[] buffer;
        int length;

        public BitReader(byte[] buffer, int length)
        {
            this.buffer = buffer;
            this.length = length;
        }

        public byte[] Buffer { get { return buffer; } }

        public bool GetBit(int index)
        {
            int byteIndex = index / 8;
            int bitIndex = index % 8;

            return (buffer[byteIndex] & bitMasks[bitIndex]) == bitMasks[bitIndex];
        }

        public int GetNextMatchingBit(int startIndex, bool valueToMatch)
        {
            if (startIndex == length) return startIndex;

            bool referencePixel;
            if (startIndex == -1)
                referencePixel = true;
            else
                referencePixel = GetBit(startIndex);

            bool changed = false;

            int currentIndex = startIndex + 1;

            while (currentIndex < length)
            {
                bool foundBit = GetBit(currentIndex);
                
                if (foundBit != referencePixel)
                    changed = true;

                if (foundBit == valueToMatch && changed)
                    return currentIndex;

                currentIndex++;
            }

            return length;
        }
    }
}
