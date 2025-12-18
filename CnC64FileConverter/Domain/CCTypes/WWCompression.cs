using System;

namespace RApp.Compression
{
    public static class WWCompression
    {
        ////////////////////////////////////////////////////////////////////////////////
        //  Copyright Notice
        ////////////////////////////////////////////////////////////////////////////////
        // This code is free software: you can redistribute it and/or modify
        // it under the terms of the GNU General Public License as published by
        // the Free Software Foundation, either version 2 of the License, or
        // (at your option) any later version.
        // 
        // This code is distributed in the hope that it will be useful,
        // but WITHOUT ANY WARRANTY; without even the implied warranty of
        // MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
        // GNU General Public License for more details.
        // 
        // You should have received a copy of the GNU General Public License
        // along with this code.  If not, see <http://www.gnu.org/licenses/>.
        ////////////////////////////////////////////////////////////////////////////////

        ////////////////////////////////////////////////////////////////////////////////
        //  Notes
        ////////////////////////////////////////////////////////////////////////////////
        //
        // LCW streams should always start and end with the fill command (& 0x80) though
        // the decompressor doesn't strictly require that it start with one the ability
        // to use the offset commands in place of the RLE command early in the stream
        // relies on it. Streams larger than 64k that need the relative versions of the
        // 3 and 5 byte commands should start with a null byte before the first 0x80
        // command to flag that they are relative compressed.
        //
        // LCW uses the following rules to decide which command to use:
        // 1. Runs of the same colour should only use 4 byte RLE command if longer than
        //    64 bytes. 2 and 3 byte offset commands are more efficient otherwise.
        // 2. Runs of less than 3 should just be stored as is with the one byte fill
        //    command.
        // 3. Runs greater than 10 or if the relative offset is greater than 
        //    4095 use an absolute copy. Less than 64 bytes uses 3 byte command, else it
        //    uses the 5 byte command.
        // 4. If Absolute rule isn't met then copy from a relative offset with 2 byte
        //    command.
        //
        // Absolute LCW can efficiently compress data that is 64k in size, much greater
        // and relative offsets for the 3 and 5 byte commands are needed.
        //
        // The XOR delta generator code works to the following assumptions
        //
        // 1. Any skip command is preferable if source and base are same
        // 2. Fill is preferable to XOR if 4 or larger, XOR takes same data plus at 
        //    least 1 byte
        //
        ////////////////////////////////////////////////////////////////////////////////

        ////////////////////////////////////////////////////////////////////////////////
        //  Some defines used by the encoders
        ////////////////////////////////////////////////////////////////////////////////
        public const Byte XOR_SMALL = 0x7F;
        public const Byte XOR_MED = 0xFF;
        public const Int32 XOR_LARGE = 0x3FFF;
        public const Int32 XOR_MAX = 0x7FFF;

        ////////////////////////////////////////////////////////////////////////////////
        //  Some utility functions to get worst case sizes for buffer allocation
        ////////////////////////////////////////////////////////////////////////////////

        public static int LCWWorstCase(int datasize)
        {
            return datasize + (datasize / 63) + 1;
        }

        public static int XORWorstCase(int datasize)
        {
            return datasize + ((datasize / 63) * 3) + 4;
        }

        /// <summary>
        ///    Compresses data to the proprietary LCW format used in
        ///    many games developed by Westwood Studios. Compression is better
        ///    than that achieved by popular community tools. This is a new
        ///    implementation based on understanding of the compression gained from
        ///    the reference code.
        /// </summary>
        /// <param name="input">Array of the data to compress.</param>
        /// <returns>The compressed data</returns>
        /// <remarks>Commonly known in the community as "format80"</remarks>
        public static Byte[] LcwCompress(Byte[] input)
        {
            if (input == null || input.Length == 0)
                return new Byte[0];

            //Decide if we are going to do relative offsets for 3 and 5 byte commands
            Boolean relative = input.Length > UInt16.MaxValue;

            // Nyer's C# conversion: replacements for write and read for pointers.
            Int32 getp = 0;
            Int32 putp = 0;
            // Input length. Used commonly enough to warrant getting it out in advance I guess.
            Int32 getend = input.Length;
            // "Worst case length" code by OmniBlade. We'll just use a buffer of
            // that max length and cut it down to the actual used size at the end.
            Byte[] output = new Byte[LCWWorstCase(getend)];
            // relative LCW starts with 0 as flag to decoder.
            // this is only used by later games for decoding hi-color vqa files.
            if (relative)
                output[putp++] = 0;            

            //Implementations that properly conform to the WestWood encoder should
            //write a starting cmd1. It's important for using the offset copy commands
            //to do more efficient RLE in some cases than the cmd4.

            //we also set bool to flag that we have an on going cmd1.
            Int32 cmd_onep = putp;
            output[putp++] = 0x81;
            output[putp++] = input[getp++];
            bool cmd_one = true;

            //Compress data until we reach end of input buffer.
            while (getp < getend)
            {
                //Is RLE encode (4bytes) worth evaluating?
                if (getend - getp > 64 && input[getp] == input[getp + 64])
                {
                    //RLE run length is encoded as a short so max is UINT16_MAX
                    Int32 rlemax = (getend - getp) < UInt16.MaxValue ? getend : getp + UInt16.MaxValue;
                    Int32 rlep = getp + 1;
                    while (input[rlep] == input[getp] && rlep < rlemax)
                        rlep++;

                    UInt16 run_length = (UInt16)(rlep - getp);

                    //If run length is long enough, write the command and start loop again
                    if (run_length >= 0x41)
                    {
                        //write 4byte command 0b11111110
                        cmd_one = false;
                        output[putp++] = 0xFE;
                        output[putp++] = (Byte)(run_length & 0xFF);
                        output[putp++] = (Byte)((run_length >> 8) & 0xFF);
                        output[putp++] = input[getp];
                        getp = rlep;
                        continue;
                    }
                }

                //current block size for an offset copy
                UInt16 block_size = 0;
                //Set where we start looking for matching runs.
                Int32 offstart = relative ? getp < UInt16.MaxValue ? 0 : getp - UInt16.MaxValue : 0;

                //Look for matching runs
                Int32 offchk = offstart;
                Int32 offsetp = getp;
                while (offchk < getp)
                {
                    //Move offchk to next matching position
                    while (offchk < getp && input[offchk] != input[getp])
                        offchk++;

                    //If the checking pointer has reached current pos, break
                    if (offchk >= getp)
                        break;

                    //find out how long the run of matches goes for
                    int i;
                    for (i = 1; input[getp] < getend; ++i)
                        if (input[offchk] != input[getp])
                            break;
                    if (i >= block_size)
                    {
                        block_size = (UInt16)i;
                        offsetp = offchk;
                    }
                    offchk++;
                }

                //decide what encoding to use for current run
                //If its less than 2 bytes long, we store as is with cmd1
                if (block_size <= 2)
                {
                    //short copy 0b10??????
                    //check we have an existing 1 byte command and if its value is still
                    //small enough to handle additional bytes
                    //start a new command if current one doesn't have space or we don't
                    //have one to continue
                    if (cmd_one && output[cmd_onep] < 0xBF)
                    {
                        //increment command value
                        ++cmd_onep;
                        output[putp++] = input[getp++];
                    }
                    else
                    {
                        cmd_onep = putp;
                        output[putp++] = 0x81;
                        output[putp++] = input[getp++];
                        cmd_one = true;
                    }
                    //Otherwise we need to decide what relative copy command is most efficient
                }
                else
                {
                    Int32 offset;
                    Int32 rel_offset = getp - offsetp;
                    if (block_size > 0xA || ((rel_offset) > 0xFFF))
                    {
                        //write 5 byte command 0b11111111
                        if (block_size > 0x40)
                        {
                            output[putp++] = 0xFF;
                            output[putp++] = (Byte)(block_size & 0xFF);
                            output[putp++] = (Byte)((block_size >> 8) & 0xFF);
                            //write 3 byte command 0b11??????
                        }
                        else
                        {
                            output[putp++] = (Byte)((block_size - 3) | 0xC0);
                        }

                        offset = relative ? rel_offset : offsetp;
                        //write 2 byte command? 0b0???????
                    }
                    else
                    {
                        offset = rel_offset << 8 | (16 * (block_size - 3) + (rel_offset >> 8));
                    }
                    output[putp++] = (Byte)(offset & 0xFF);
                    output[putp++] = (Byte)((offset >> 8) & 0xFF);
                    getp += block_size;
                    cmd_one = false;
                }
            }

            //write final 0x80, basically an empty cmd1 to signal the end of the stream.
            output[putp++] = 0x80;

            Byte[] finalOutput = new Byte[putp];
            Array.Copy(output, 0, finalOutput, 0, putp);
            // Return the final compressed data.
            return finalOutput;
        }

        /// <summary>
        ///     Decompresses data in the proprietary LCW format used in many games 
        ///     developed by Westwood Studios.
        /// </summary>
        /// <param name="input">The data to decompress.</param>
        /// <param name="output">The buffer to store the decompressed data. This is assumed to be initialized to the correct size.</param>
        /// <returns>Length of the decompressed data in bytes.</returns>
        public static Int32 LcwUncompress(Byte[] input, Byte[] output)
        {
            if (input == null || input.Length == 0 || output == null || output.Length == 0)
                return 0;
	        Boolean relative = false;
            // Nyer's C# conversion: replacements for write and read for pointers.
	        Int32 putp = 0;
	        Int32 getp = 0;
            // Output length should be part of the information given in the file format using LCW.
            // Techncically it can just be cropped at the end, though this value is used to
            // automatically cut off repeat-commands that go too far.
            Int32 putend = output.Length;
            
            //Decide if the stream uses relative 3 and 5 byte commands
	        //Extension allows effective compression of data > 64k
	        //https://github.com/madmoose/scummvm/blob/bladerunner/engines/bladerunner/decompress_lcw.cpp
            // this is only used by later games for decoding hi-color vqa files.
            // For other stuff (like shp), just check in advance to decide if the data is too big.
	        if (input[getp] == 0)
	        {
		        relative = true;
		        getp++;
	        }
	        //DEBUG_SAY("LCW Decompression... \n");
	        while (putp < putend)
	        {
		        Byte flag = input[getp++];
		        UInt16 cpysize;
                UInt16 offset;

		        if ((flag & 0x80) != 0)
		        {
			        if ((flag & 0x40) != 0)
			        {
                        cpysize = (UInt16)((flag & 0x3F) + 3);
				        //long set 0b11111110
				        if (flag == 0xFE)
				        {
                            cpysize = input[getp++];
                            cpysize += (UInt16)((input[getp++]) << 8);

					        if (cpysize > putend - putp)
                                cpysize = (UInt16)(putend - putp);

					        //DEBUG_SAY("0b11111110 Source Pos %ld, Dest Pos %ld, Count %d\n", source - sstart - 3, dest - start, cpysize);
                            for (int i=putp; i < putp+cpysize; i++)
                                output[i] = input[getp];
                            getp++;
					        putp += cpysize;
				        }
				        else
				        {
					        Int32 s;
					        //long move, abs 0b11111111
					        if (flag == 0xFF)
					        {
                                cpysize = input[getp++];
						        cpysize += (UInt16)((input[getp++]) << 8);

						        if (cpysize > putend - putp)
						        {
							        cpysize = (UInt16)(putend - putp);
						        }

                                offset = input[getp++];
						        offset += (UInt16)((input[getp++]) << 8);

						        //extended format for VQA32
						        if (relative)
						        {
							        s = putp - offset;
						        }
						        else
						        {
							        s = offset;
						        }

						        //DEBUG_SAY("0b11111111 Source Pos %ld, Dest Pos %ld, Count %d, Offset %d\n", source - sstart - 5, dest - start, cpysize, offset);
						        for (; cpysize > 0; --cpysize)
						        {
							        output[putp++] = output[s++];
						        }
					        //short move abs 0b11??????
					        }
					        else
					        {
						        if (cpysize > putend - putp)
						        {
							        cpysize = (UInt16)(putend - putp);
						        }

                                offset = input[getp++];
						        offset += (UInt16)((input[getp++]) << 8);

						        //extended format for VQA32
						        if (relative)
						        {
							        s = putp - offset;
						        }
						        else
						        {
							        s = offset;
						        }

						        //DEBUG_SAY("0b11?????? Source Pos %ld, Dest Pos %ld, Count %d, Offset %d\n", source - sstart - 3, dest - start, cpysize, offset);
						        for (; cpysize > 0; --cpysize)
						        {
                                    output[putp++] = output[s++];
						        }
					        }
				        }
			        //short copy 0b10??????
			        }
			        else
			        {
				        if (flag == 0x80)
				        {
					        //DEBUG_SAY("0b10?????? Source Pos %ld, Dest Pos %ld, Count %d\n", source - sstart - 1, dest - start, 0);
					        return putp;
				        }

				        cpysize = (UInt16)(flag & 0x3F);

				        if (cpysize > putend - putp)
				        {
					        cpysize = (UInt16)(putend - putp);
				        }

				        //DEBUG_SAY("0b10?????? Source Pos %ld, Dest Pos %ld, Count %d\n", source - sstart - 1, dest - start, cpysize);
				        for (; cpysize > 0; --cpysize)
				        {
					        output[putp++]= input[getp++];
				        }
			        }
		        //short move rel 0b0???????
		        }
		        else
		        {
			        cpysize = (UInt16)((flag >> 4) + 3);

			        if (cpysize > putend - putp)
			        {
				        cpysize = (UInt16)(putend - putp);
			        }

			        offset = (UInt16)(((flag & 0xF) << 8) + input[getp++]);
			        //DEBUG_SAY("0b0??????? Source Pos %ld, Dest Pos %ld, Count %d, Offset %d\n", source - sstart - 2, dest - start, cpysize, offset);
			        for (; cpysize > 0; --cpysize)
			        {
				        output[putp] = output[putp - offset];
				        putp++;
			        }
		        }
	        }
	        return putp;
        }

        /// <summary>
        /// Generates a binary delta between two buffers. Mainly used for image data.
        /// </summary>
        /// <param name="source">Buffer containing data to generate the delta for.</param>
        /// <param name="base">Buffer containing data that is the base for the delta.</param>
        /// <returns>The generated delta as bytes array</returns>
        /// <remarks>Commonly known in the community as "format40"</remarks>
        public static Byte[] GenerateXorDelta(Byte[] source, Byte[] @base)
        {
            // Nyer's C# conversion: replacements for write and read for pointers.
            // -for our delta (output)
            Int32 putp = 0;
            // -for the image we go to
            Int32 getsp = 0;
            // -for the image we come from
            Int32 getbp = 0;
            //Length to process
            Int32 getsendp = Math.Min(source.Length, @base.Length);
            Byte[] dest = new Byte[XORWorstCase(getsendp)];

            //Only check getsp to save a redundant check. 
            //Both source and base should be same size and both pointers should be 
            //incremented at the same time.
            while (getsp < getsendp)
            {
                UInt32 fillcount = 0;
                UInt32 xorcount = 0;
                UInt32 skipcount = 0;
                Byte lastxor = (Byte)(source[getsp] ^ @base[getbp]);
                Int32 testsp = getsp;
                Int32 testbp = getbp;

                //Only evaluate other options if we don't have a matched pair
                while (source[testsp] != @base[testbp] && testsp < getsendp)
                {
                    if ((source[testsp] ^  @base[testbp]) == lastxor)
                    {
                        ++fillcount;
                        ++xorcount;
                    }
                    else
                    {
                        if (fillcount > 3)
                        {
                            break;
                        }
                        else
                        {
                            lastxor = (Byte)(source[testsp] ^ @base[testbp]);
                            fillcount = 1;
                            ++xorcount;
                        }
                    }
                    testsp++;
                    testbp++;
                }

                //fillcount should always be lower than xorcount and should be greater
                //than 3 to warrant using the fill commands.
                fillcount = fillcount > 3 ? fillcount : 0;

                //Okay, lets see if we have any xor bytes we need to handle
                xorcount -= fillcount;
                while (xorcount != 0)
                {
                    UInt16 count = 0;
                    //Its cheaper to do the small cmd twice than do the large cmd once 
                    //for data that can be handled by two small cmds.
                    //cmd 0???????
                    if (xorcount < XOR_MED)
                    {
                        count = (UInt16)(xorcount <= XOR_SMALL ? xorcount : XOR_SMALL);
                        dest[putp++] = (Byte)count;
                        //cmd 10000000 10?????? ??????
                    }
                    else
                    {
                        count = (UInt16)(xorcount <= XOR_LARGE ? xorcount : XOR_LARGE);
                        dest[putp++] = 0x80;
                        dest[putp++] = (Byte)(count & 0xFF);
                        dest[putp++] = (Byte)(((count >> 8) & 0xFF) | 0x80);
                    }

                    while (count != 0)
                    {
                        dest[putp++] = (Byte)(source[getsp++] ^ @base[getbp++]);
                        count--;
                        xorcount--;
                    }
                }

                //lets handle the bytes that are best done as xorfill
                while (fillcount != 0)
                {
                    UInt16 count = 0;
                    //cmd 00000000 ????????
                    if (fillcount <= XOR_MED)
                    {
                        count = (UInt16)fillcount;
                        dest[putp++] = 0;
                        dest[putp++] = (Byte)(count & 0xFF);
                        //cmd 10000000 11?????? ??????
                    }
                    else
                    {
                        count = (UInt16)(fillcount <= XOR_LARGE ? fillcount : XOR_LARGE);
                        dest[putp++] = 0x80;
                        dest[putp++] = (Byte)(count & 0xFF);
                        dest[putp++] = (Byte)(((count >> 8) & 0xFF) | 0xC0);
                    }
                    dest[putp++] = (Byte)(source[getsp] ^ @base[getbp]);
                    fillcount -= count;
                    getsp += count;
                    getbp += count;
                }

                //Handle regions that match exactly
                while (source[testsp] == @base[testbp] && testsp < getsendp)
                {
                    skipcount++;
                    testsp++;
                    testbp++;
                }

                while (skipcount != 0)
                {
                    UInt16 count = 0;
                    //Again its cheaper to do the small cmd twice than do the large cmd 
                    //once for data that can be handled by two small cmds.
                    //cmd 1???????
                    if (skipcount < XOR_MED)
                    {
                        count = (Byte)(skipcount <= XOR_SMALL ? skipcount : XOR_SMALL);
                        dest[putp++] = (Byte)(count | 0x80);
                        //cmd 10000000 0??????? ????????
                    }
                    else
                    {
                        count = (UInt16)(skipcount <= XOR_MAX ? skipcount : XOR_MAX);
                        dest[putp++] = 0x80;
                        dest[putp++] = (Byte)(count & 0xFF);
                        dest[putp++] = (Byte)((count >> 8) & 0xFF);
                    }
                    skipcount -= count;
                    getsp += count;
                    getbp += count;
                }
            }

            //final skip command of 0 to signal end of stream.
            dest[putp++] = 0x80;
            dest[putp++] = 0;
            dest[putp++] = 0;
            
            Byte[] finalOutput = new Byte[putp];
            Array.Copy(dest, 0, finalOutput, 0, putp);
            // Return the final data
            return finalOutput;
        }

        /// <summary>
        /// Applies a binary delta to a buffer.
        /// </summary>
        /// <param name="data">The data to apply the xor to</param>
        /// <param name="xorSource">The the delta data to apply</param>
        public static void ApplyXorDelta(Byte[] data, Byte[] xorSource)
        {
            // Nyer's C# conversion: replacements for write and read for pointers.
            Int32 putp = 0;
            Int32 getp = 0;
            Byte value = 0;

            while (true)
            {
                //DEBUG_SAY("XOR_Delta Put pos: %u, Get pos: %u.... ", putp - scast<sint8*>(dest), getp - scast<sint8*>(source));
                Byte cmd = xorSource[getp++];
                UInt16 count = cmd;
                Boolean xorval = false;

                if ((cmd & 0x80) == 0)
                {
                    //0b00000000
                    if (cmd == 0)
                    {
                        count = (UInt16)(xorSource[getp++] & 0xFF);
                        value = xorSource[getp++];
                        xorval = true;
                        //DEBUG_SAY("0b00000000 Val Count %d ", count);
                        //0b0???????
                    }
                    else
                    {
                        //DEBUG_SAY("0b0??????? XOR Count %d ", count);
                    }
                }
                else
                {
                    //0b1??????? remove most significant bit
                    count &= 0x7F;
                    if (count != 0)
                    {
                        putp += count;
                        //DEBUG_SAY("0b1??????? Skip Count %d\n", count);
                        continue;
                    }

                    count = (UInt16)((xorSource[getp] & 0xFF) + (xorSource[getp + 1] << 8));
                    getp += 2;

                    //DEBUG_SAY("Eval %u ", count);

                    //0b10000000 0 0
                    if (count == 0)
                    {
                        //DEBUG_SAY("0b10000000 Count %d to end delta\n", count);
                        return;
                    }

                    //0b100000000 0?
                    if ((count & 0x8000) == 0)
                    {
                        putp += count;
                        //DEBUG_SAY("0b100000000 0? Skip Count %d\n", count);
                        continue;
                    }
                    else
                    {
                        //0b10000000 11
                        if ((count & 0x4000) != 0)
                        {
                            count &= 0x3FFF;
                            value = xorSource[getp++];
                            //DEBUG_SAY("0b10000000 11 Val Count %d ", count);
                            xorval = true;
                            //0b10000000 10
                        }
                        else
                        {
                            count &= 0x3FFF;
                            //DEBUG_SAY("0b10000000 10 XOR Count %d ", count);
                        }
                    }
                }

                if (xorval)
                {
                    //DEBUG_SAY("XOR Val %d\n", value);
                    for (; count > 0; --count)
                    {
                        data[putp++] ^= value;
                    }
                }
                else
                {
                    //DEBUG_SAY("XOR Source to Dest\n");
                    for (; count > 0; --count)
                    {
                        data[putp++] ^= xorSource[getp++];
                    }
                }
            }
        }


    }
}