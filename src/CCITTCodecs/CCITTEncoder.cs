using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace CCITTCodecs
{
    public class CCITTEncoder
    {
        public CCITTEncoder()
        {
        }

        public int Encode(Stream input, long inputStreamStartOffset, int width, int height, Stream output)
        {
            var rowLengthInBytes = (((int)width + 31) / 32) * 4; //bmp row data is in a multiple of 4 bytes

            // do not .Seek() the output. The caller may want us to append to the end. We store the starting position
            //here so we can return how many bytes were written.
            var outputStartPosition = output.Position;

            var state = new EncoderState()
            {
                CodingReader = new BitReader(new byte[rowLengthInBytes], width), //this will get swapped to referenceReader on the first run
                ReferenceReader = new BitReader(new byte[rowLengthInBytes], width),
                Writer = new BitWriter(output),
                Width = width
            };

            //fill the current row with all 1's. It'll be used as the reference row for the first real image row.
            for (int i = 0; i < state.CodingReader.Buffer.Length; i++)
            {
                state.CodingReader.Buffer[i] = 0xff;
            }

            for (int row = 0; row < height; row++)
            {
                //swap current into previous and reuse the previous as the next current
                //for the first iteration of this loop the current row is all 1's
                var tempReader = state.ReferenceReader;
                state.ReferenceReader = state.CodingReader;
                state.CodingReader = tempReader;

                //fill the current row data from the stream
                input.Seek(inputStreamStartOffset, SeekOrigin.Begin);
                input.Seek(rowLengthInBytes * (height - 1 - row), SeekOrigin.Current);
                input.Read(state.CodingReader.Buffer, 0, rowLengthInBytes);

                EncodeRowGroup4(state);
            }

            //EOFB. Not really needed for files but it doesn't hurt and some encoders write it.
            state.Writer.Write(0, 8);
            state.Writer.Write(16, 8);
            state.Writer.Write(1, 8);

            state.Writer.Flush();

            //return # of bytes written to output stream.
            return (int)(output.Position - outputStartPosition);
        }

        private void EncodeRowGroup4(EncoderState state)
        {
            int a0 = -1; //start just before the first element
            bool a0Color = true;

            while (a0 < state.Width)
            {
                if (a0 > -1)
                    a0Color = state.CodingReader.GetBit(a0);

                int a1 = state.CodingReader.GetNextMatchingBit(a0, !a0Color);
                int b1 = state.ReferenceReader.GetNextMatchingBit(a0, !a0Color);
                int b2 = state.ReferenceReader.GetNextMatchingBit(b1, a0Color);

                if (a0 == -1)
                    a0 = 0; // for the rest of the math to work.

                if (b2 < a1) //pass mode
                {
                    WritePassCode(state);
                    a0 = b2;
                }
                else //horizontal or vertical
                {
                    int deltab1a1 = b1 - a1;

                    if (-3 <= deltab1a1 && deltab1a1 <= 3) //vertical
                    {
                        WriteVerticalCode(state, deltab1a1);
                        a0 = a1;
                    }
                    else //horizontal
                    {
                        int a2 = state.CodingReader.GetNextMatchingBit(a1, a0Color);
                        WriteHorizontalCodes(state, a0Color, a1 - a0, a2 - a1);

                        a0 = a2;
                    }
                }
            }
        }

        private void WritePassCode(EncoderState state)
        {
            state.Writer.Write(PassCodes[0], PassCodes[1]);
        }

        private void WriteHorizontalCodes(EncoderState state, bool startingColor, int firstSpan, int secondSpan)
        {
            state.Writer.Write(HorizontalCodes[0], HorizontalCodes[1]);

            WriteHorizontalSpan(state, startingColor, firstSpan);
            WriteHorizontalSpan(state, !startingColor, secondSpan);
        }

        private void WriteHorizontalSpan(EncoderState state, bool color, int spanLength)
        {
            uint[] terminatingCodes = color ? WhiteTerminatingCodes : BlackTerminatingCodes;
            uint[] makeUpCodes = color ? WhiteMakeUpCodes : BlackMakeUpCodes;

            int count = spanLength;

            // The make-up code for 2560 will be written as often as required:
            while (count >= 2624)
            {
                state.Writer.Write(makeUpCodes[39 * 2], makeUpCodes[39 * 2 + 1]); // Magic: 2560
                count -= 2560;
            }
            // A make-up code for a multiple of 64 will be written if required:
            if (count > 63)
            {
                int line = count / 64 - 1;
                state.Writer.Write(makeUpCodes[line * 2], makeUpCodes[line * 2 + 1]);
                count -= (line + 1) * 64;
            }
            // And finally the terminating code for the remaining value (0 through 63):
            state.Writer.Write(terminatingCodes[count * 2], terminatingCodes[count * 2 + 1]);
        }

        private void WriteVerticalCode(EncoderState state, int deltab1a1)
        {
            var verticalIndex = deltab1a1 + 3;

            state.Writer.Write(VerticalCodes[verticalIndex * 2], VerticalCodes[verticalIndex * 2 + 1]);
        }

        private class EncoderState
        {
            public BitWriter Writer { get; set; }
            public BitReader CodingReader { get; set; }
            public BitReader ReferenceReader { get; set; }
            public int Width { get; set; }
        }



        internal readonly static uint[] WhiteTerminatingCodes =
    {
      0x35, 8, //00110101 // 0
      0x07, 6, //000111
      0x07, 4, //0111
      0x08, 4, //1000
      0x0b, 4, //1011
      0x0c, 4, //1100
      0x0e, 4, //1110
      0x0f, 4, //1111
      0x13, 5, //10011
      0x14, 5, //10100
      0x07, 5, //00111    // 10
      0x08, 5, //01000
      0x08, 6, //001000
      0x03, 6, //000011
      0x34, 6, //110100
      0x35, 6, //110101
      0x2a, 6, //101010   // 16
      0x2b, 6, //101011
      0x27, 7, //0100111
      0x0c, 7, //0001100
      0x08, 7, //0001000  // 20
      0x17, 7, //0010111
      0x03, 7, //0000011
      0x04, 7, //0000100
      0x28, 7, //0101000
      0x2b, 7, //0101011
      0x13, 7, //0010011
      0x24, 7, //0100100
      0x18, 7, //0011000
      0x02, 8, //00000010
      0x03, 8, //00000011 // 30
      0x1a, 8, //00011010
      0x1b, 8, //00011011 // 32
      0x12, 8, //00010010
      0x13, 8, //00010011
      0x14, 8, //00010100
      0x15, 8, //00010101
      0x16, 8, //00010110
      0x17, 8, //00010111
      0x28, 8, //00101000
      0x29, 8, //00101001 // 40
      0x2a, 8, //00101010
      0x2b, 8, //00101011
      0x2c, 8, //00101100
      0x2d, 8, //00101101
      0x04, 8, //00000100
      0x05, 8, //00000101
      0x0a, 8, //00001010
      0x0b, 8, //00001011 // 48
      0x52, 8, //01010010
      0x53, 8, //01010011 // 50
      0x54, 8, //01010100
      0x55, 8, //01010101
      0x24, 8, //00100100
      0x25, 8, //00100101
      0x58, 8, //01011000
      0x59, 8, //01011001
      0x5a, 8, //01011010
      0x5b, 8, //01011011
      0x4a, 8, //01001010
      0x4b, 8, //01001011 // 60
      0x32, 8, //00110010
      0x33, 8, //00110011
      0x34, 8, //00110100 // 63
    };

        internal readonly static uint[] BlackTerminatingCodes =
    {
      0x37, 10, //0000110111   // 0
      0x02,  3, //010
      0x03,  2, //11
      0x02,  2, //10
      0x03,  3, //011
      0x03,  4, //0011
      0x02,  4, //0010
      0x03,  5, //00011
      0x05,  6, //000101
      0x04,  6, //000100
      0x04,  7, //0000100
      0x05,  7, //0000101
      0x07,  7, //0000111
      0x04,  8, //00000100
      0x07,  8, //00000111
      0x18,  9, //000011000
      0x17, 10, //0000010111   // 16
      0x18, 10, //0000011000
      0x08, 10, //0000001000
      0x67, 11, //00001100111
      0x68, 11, //00001101000
      0x6c, 11, //00001101100
      0x37, 11, //00000110111
      0x28, 11, //00000101000
      0x17, 11, //00000010111
      0x18, 11, //00000011000
      0xca, 12, //000011001010
      0xcb, 12, //000011001011
      0xcc, 12, //000011001100
      0xcd, 12, //000011001101
      0x68, 12, //000001101000 // 30
      0x69, 12, //000001101001
      0x6a, 12, //000001101010 // 32
      0x6b, 12, //000001101011
      0xd2, 12, //000011010010
      0xd3, 12, //000011010011
      0xd4, 12, //000011010100
      0xd5, 12, //000011010101
      0xd6, 12, //000011010110
      0xd7, 12, //000011010111
      0x6c, 12, //000001101100
      0x6d, 12, //000001101101
      0xda, 12, //000011011010
      0xdb, 12, //000011011011
      0x54, 12, //000001010100
      0x55, 12, //000001010101
      0x56, 12, //000001010110
      0x57, 12, //000001010111
      0x64, 12, //000001100100 // 48
      0x65, 12, //000001100101
      0x52, 12, //000001010010
      0x53, 12, //000001010011
      0x24, 12, //000000100100
      0x37, 12, //000000110111
      0x38, 12, //000000111000
      0x27, 12, //000000100111
      0x28, 12, //000000101000
      0x58, 12, //000001011000
      0x59, 12, //000001011001
      0x2b, 12, //000000101011
      0x2c, 12, //000000101100
      0x5a, 12, //000001011010
      0x66, 12, //000001100110
      0x67, 12, //000001100111 // 63
    };

        internal readonly static uint[] WhiteMakeUpCodes =
    {
      0x1b,  5, //11011 64          // 0
      0x12,  5, //10010 128
      0x17,  6, //010111 192
      0x37,  7, //0110111 256
      0x36,  8, //00110110 320
      0x37,  8, //00110111 384
      0x64,  8, //01100100 448
      0x65,  8, //01100101 512
      0x68,  8, //01101000 576
      0x67,  8, //01100111 640
      0xcc,  9, //011001100 704     // 10
      0xcd,  9, //011001101 768
      0xd2,  9, //011010010 832
      0xd3,  9, //011010011 896
      0xd4,  9, //011010100 960
      0xd5,  9, //011010101 1024
      0xd6,  9, //011010110 1088    // 16
      0xd7,  9, //011010111 1152
      0xd8,  9, //011011000 1216
      0xd9,  9, //011011001 1280
      0xda,  9, //011011010 1344
      0xdb,  9, //011011011 1408
      0x98,  9, //010011000 1472
      0x99,  9, //010011001 1536
      0x9a,  9, //010011010 1600
      0x18,  6, //011000    1664
      0x9b,  9, //010011011 1728
      // Common codes for white and black:
      0x08, 11, //00000001000 1792
      0x0c, 11, //00000001100 1856
      0x0d, 11, //00000001101 1920
      0x12, 12, //000000010010 1984
      0x13, 12, //000000010011 2048
      0x14, 12, //000000010100 2112 // 32
      0x15, 12, //000000010101 2176
      0x16, 12, //000000010110 2240
      0x17, 12, //000000010111 2304
      0x1c, 12, //000000011100 2368
      0x1d, 12, //000000011101 2432
      0x1e, 12, //000000011110 2496
      0x1f, 12, //000000011111 2560
      0x01, 12, //000000000001 EOL  // 40
    };

        internal readonly static uint[] BlackMakeUpCodes =
    {
      0x0f, 10, //0000001111    64   // 0
      0xc8, 12, //000011001000  128
      0xc9, 12, //000011001001  192
      0x5b, 12, //000001011011  256
      0x33, 12, //000000110011  320
      0x34, 12, //000000110100  384
      0x35, 12, //000000110101  448
      0x6c, 13, //0000001101100 512
      0x6d, 13, //0000001101101 576
      0x4a, 13, //0000001001010 640
      0x4b, 13, //0000001001011 704
      0x4c, 13, //0000001001100 768
      0x4d, 13, //0000001001101 832
      0x72, 13, //0000001110010 896
      0x73, 13, //0000001110011 960
      0x74, 13, //0000001110100 1024
      0x75, 13, //0000001110101 1088 // 16
      0x76, 13, //0000001110110 1152
      0x77, 13, //0000001110111 1216
      0x52, 13, //0000001010010 1280
      0x53, 13, //0000001010011 1344
      0x54, 13, //0000001010100 1408
      0x55, 13, //0000001010101 1472
      0x5a, 13, //0000001011010 1536
      0x5b, 13, //0000001011011 1600
      0x64, 13, //0000001100100 1664
      0x65, 13, //0000001100101 1728
      // Common codes for white and black:
      0x08, 11, //00000001000 1792
      0x0c, 11, //00000001100 1856
      0x0d, 11, //00000001101 1920
      0x12, 12, //000000010010 1984
      0x13, 12, //000000010011 2048
      0x14, 12, //000000010100 2112  // 32
      0x15, 12, //000000010101 2176
      0x16, 12, //000000010110 2240
      0x17, 12, //000000010111 2304
      0x1c, 12, //000000011100 2368
      0x1d, 12, //000000011101 2432
      0x1e, 12, //000000011110 2496
      0x1f, 12, //000000011111 2560
      0x01, 12, //000000000001 EOL   // 40
    };

        internal readonly static uint[] HorizontalCodes = { 0x1, 3 }; /* 001 */
        internal readonly static uint[] PassCodes = { 0x1, 4, }; /* 0001 */
        internal readonly static uint[] VerticalCodes =
    {
      0x03, 7, /* 0000 011 */
      0x03, 6, /* 0000 11 */
      0x03, 3, /* 011 */
      0x1,  1, /* 1 */
      0x2,  3, /* 010 */
      0x02, 6, /* 0000 10 */
      0x02, 7, /* 0000 010 */
    };

    }
}
