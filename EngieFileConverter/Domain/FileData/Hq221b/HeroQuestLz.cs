using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nyerguds.FileData.Hq221b
{
    public static class HeroQuestLz
    {

        // int Sub_145D1(int len, byte* out, byte* inp, int inplen )
        public static byte[] Decompress(Byte[] input)
        {
            UInt16 readVal; // ax
            int outPtr = 0;
            int len = 0xffff;
            Byte[] out0 = new Byte[0x1000000];
            int inp = 0;
            int inpend = input.Length;

            /*

            11LLLLDD DDDDDDDD -- match, mindist=1, minlen=3 (dist to end of match!)
            000LLLLL          -- literal span, len=0 impossible because of EOF
            001LLLLL LLLLLLLL -- literal span, minlen=31

            00000000          -- EOF
            010LLLLL xxxxxxxx -- run of x, minlen=3 (3..18)
            011LLLLL LLLLLLLL xxxxxxxx -- run of x, minlen=36 (36..8227)
            10LDDDDD          -- match, len=2: dist=2..33, len=3: dist=3..34

            */
            while (true)
            {
                inp++; if (inp > inpend) break;
                readVal = input[inp]; 
                //printf( "v4=%02X\n", v4 );
                if (readVal != 0x00)
                {
                    //printf( "!1!\n" );
                    if ((readVal & 0x80) != 0)
                    {
                        readVal = (UInt16)(readVal & 0x7F);
                        if ((readVal & 0x40) != 0)
                        {
                            readVal = (UInt16)(readVal & 0xBF);                            
                            inp++; if (inp > inpend) break;
                            //v11 = ReplaceHighByte(v11, v4);
                            //v11 = ReplaceLowByte(v11, input[inp]);
                            int v11 = (Int16)(((readVal & 0xFF) << 8) | input[inp]);
                            int v12 = (Int16)((4 >> 2) + 3);
                            int v13 = outPtr -1 - (v11 & 0x03FF) - v12;
                            //DBG printf( "match: %i,%i\n", (v4 >> 2) + 3 + 1, (v11 & 0x3FF) + v12 + 1 );
                            while (true)
                            {
                                //v4 = ReplaceLowByte(v4, out0[v13]);
                                out0[outPtr++] = out0[v13++];
                                if (--len == 0) break;
                                if (--v12 < 0) continue;
                            }
                        }
                        else
                        {
                            int amount = (Int16)((readVal & 0x20) == 0 ? 1 : 2);
                            readVal = (UInt16)(readVal & 0xDF);
                            int start = outPtr - 1 - readVal - amount;
                            while (true)
                            {
                                //v4 = ReplaceLowByte(v4, out0[v9]);
                                out0[outPtr++] = out0[start++];
                                if (--len == 0) break;
                                if (--amount < 0) continue;
                            }
                        }
                    }
                    else if ((readVal & 0x40) != 0)
                    {
                        readVal = (UInt16)(readVal & 0xBF);
                        if ((readVal & 0x20) != 0)
                        {
                            inp++; if (inp > inpend) break;
                            //v6 = ReplaceHighByte(v6, v4 & 0xDF);
                            //v6 = ReplaceLowByte(v6, input[inp]);
                            //v4 = (UInt16)(v6 + 33);
                            readVal = (UInt16)((readVal & 0xDF << 8 | input[inp]) + 33);
                        }
                        readVal += 2;
                        inp++; if (inp > inpend) break;
                        byte rleVal = input[inp];
                        //DBG printf( "RLErun: l=%i, c=%02X\n", v4 + 1, v7 );
                        while (true)
                        {
                            out0[outPtr++] = rleVal;
                            if (--len == 0)
                                break;
                            if ((--readVal & 0x8000u) != 0)
                                continue;
                        }
                    }
                    else
                    {
                        if ((readVal & 0x20) != 0)
                        {
                            inp++; if (inp > inpend) break;
                            //v5 = ReplaceHighByte(v5, v4 & 0xDF);
                            //v5 = ReplaceLowByte(v5, input[inp]);
                            //v4 = (UInt16)(v5 + 32);
                            readVal = (UInt16)(((readVal & 0xDF) << 8 | input[inp]) + 32);
                        }
                        --readVal;
                        while (true)
                        {
                            inp++; if (inp > inpend) break;
                            out0[outPtr++] = input[inp];
                            if (--len == 0) break;
                            if ((--readVal & 0x8000u) != 0) continue;
                        }
                    }
                }
            }
            // Trim down to used size.
            byte[] output = new byte[outPtr];
            Array.Copy(out0, 0, output, 0, outPtr);
            return output;
        }

        // #define LOBYTE(x) (*((byte*)&x))
        // #define HIBYTE(x) (*(((byte*)&x)+1))
        // #define getbyte() input[inp++]; if(inp>inpend) break;

        private static Int16 ReplaceLowByte(Int16 input, int replacement)
        {
            return (Int16)((input & 0xFF00) | (replacement & 0xFF));
        }

        private static UInt16 ReplaceLowByte(UInt16 input, int replacement)
        {
            return (UInt16)((input & 0xFF00) | (replacement & 0xFF));
        }

        private static Int16 ReplaceHighByte(Int16 input, int replacement)
        {
            return (Int16)((input & 0xFF) | ((replacement & 0xFF) << 8));
        }

        private static UInt16 ReplaceHighByte(UInt16 input, int replacement)
        {
            return (UInt16)((input & 0xFF) | ((replacement & 0xFF) << 8));
        }

    }
}
