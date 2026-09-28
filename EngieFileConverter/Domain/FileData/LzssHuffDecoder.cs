// The LZHUF compression algorithm implementation is based on lzhuf.c by Haruyasu Yoshizaki (1988),
// with comments translated by Haruhiko Okumura and subsequent modifications by Paul Edwards.
// Source: https://github.com/pzgnss/snippets/blob/master/lzhuf.c
// Although the original authors are known, the original source file contains no explicit license
// or copyright notice. The licensing status of this particular version is therefore unclear.

using System;

namespace Nyerguds.FileData.Compression
{
    /// <summary>
    /// Decoder only. Need to get my hands on an encoder for this.
    /// Information from:
    /// https://www.vogons.org/viewtopic.php?p=306003#p306003
    /// https://people.cs.umu.se/isak/snippets/lzhuf.c
    /// </summary>
    public class LzssHuffDecoder
    {
        /**************************************************************
        lzhuf.c
        written by Haruyasu Yoshizaki 1988/11/20
        some minor changes 1989/04/06
        comments translated by Haruhiko Okumura 1989/04/07
        getbit and getbyte modified 1990/03/23 by Paul Edwards
          so that they would work on machines where integers are
          not necessarily 16 bits (although ANSI guarantees a
          minimum of 16).  This program has compiled and run with
          no errors under Turbo C 2.0, Power C, and SAS/C 4.5
          (running on an IBM mainframe under MVS/XA 2.2).  Could
          people please use YYYY/MM/DD date format so that everyone
          in the world can know what format the date is in?
        external storage of filesize changed 1990/04/18 by Paul Edwards to
          Intel's "little endian" rather than a machine-dependant style so
          that files produced on one machine with lzhuf can be decoded on
          any other.  "little endian" style was chosen since lzhuf
          originated on PC's, and therefore they should dictate the
          standard.
        initialization of something predicting spaces changed 1990/04/22 by
          Paul Edwards so that when the compressed file is taken somewhere
          else, it will decode properly, without changing ascii spaces to
          ebcdic spaces.  This was done by changing the ' ' (space literal)
          to 0x20 (which is the far most likely character to occur, if you
          don't know what environment it will be running on.
        **************************************************************/

        // Thanks to NewRisingSun for identifying this as the format used in the Dynamix games.

        public static byte[] LzssDecode(byte[] buffer, int? startOffset, int? endOffset, int decompressedSize)
        {
            LzssHuffDecoder lzhDec = new LzssHuffDecoder();
            byte[] outputBuffer = lzhDec.Decode(buffer, startOffset, endOffset, decompressedSize);
            if (decompressedSize < outputBuffer.Length)
                throw new ArgumentException("Decompression failed.");
            if (decompressedSize > outputBuffer.Length)
            {
                byte[] output = new byte[decompressedSize];
                Array.Copy(outputBuffer, output, outputBuffer.Length);
                return output;
            }
            return outputBuffer;
        }


/********** LZSS compression **********/

        private const int N = 4096; /* buffer size */
        private const int F = 60; /* lookahead buffer size */
        private const int Threshold = 2;
        private const int Nil = N; /* leaf of tree */

        private byte[] _textBuf = new byte[N + F - 1];
        private int _matchPosition;
        private int _matchLength;
        private int[] _lson = new int[N + 1];
        private int[] _rson = new int[N + 257];
        private int[] _dad = new int[N + 1];

        private void InitTree() /* initialize trees */
        {
            int i;

            for (i = N + 1; i <= N + 256; ++i)
                _rson[i] = Nil; /* root */
            for (i = 0; i < N; ++i)
                _dad[i] = Nil; /* node */
        }

        private void InsertNode(int r) /* insert to tree */
        {
            int cmp = 1;
            int key = r;
            int p = N + 1 + _textBuf[key];
            _rson[r] = _lson[r] = Nil;
            _matchLength = 0;
            for (;;)
            {
                if (cmp >= 0)
                {
                    if (_rson[p] != Nil)
                        p = _rson[p];
                    else
                    {
                        _rson[p] = r;
                        _dad[r] = p;
                        return;
                    }
                }
                else
                {
                    if (_lson[p] != Nil)
                        p = _lson[p];
                    else
                    {
                        _lson[p] = r;
                        _dad[r] = p;
                        return;
                    }
                }
                int i;
                for (i = 1; i < F; ++i)
                    if ((cmp = _textBuf[key + i] - _textBuf[p + i]) != 0)
                        break;
                if (i > Threshold)
                {
                    if (i > _matchLength)
                    {
                        _matchPosition = ((r - p) & (N - 1)) - 1;
                        if ((_matchLength = i) >= F)
                            break;
                    }
                    if (i == _matchLength)
                    {
                        uint c;
                        if ((c = (uint) ((r - p) & (N - 1)) - 1) < (uint) _matchPosition)
                        {
                            _matchPosition = (int) c;
                        }
                    }
                }
            }
            _dad[r] = _dad[p];
            _lson[r] = _lson[p];
            _rson[r] = _rson[p];
            _dad[_lson[p]] = r;
            _dad[_rson[p]] = r;
            if (_rson[_dad[p]] == p)
                _rson[_dad[p]] = r;
            else
                _lson[_dad[p]] = r;
            _dad[p] = Nil; /* remove p */
        }

        private void DeleteNode(int p) /* remove from tree */
        {
            int q;

            if (_dad[p] == Nil)
                return; /* not registered */
            if (_rson[p] == Nil)
                q = _lson[p];
            else if (_lson[p] == Nil)
                q = _rson[p];
            else
            {
                q = _lson[p];
                if (_rson[q] != Nil)
                {
                    do
                    {
                        q = _rson[q];
                    } while (_rson[q] != Nil);
                    _rson[_dad[q]] = _lson[q];
                    _dad[_lson[q]] = _dad[q];
                    _lson[q] = _lson[p];
                    _dad[_lson[p]] = q;
                }
                _rson[q] = _rson[p];
                _dad[_rson[p]] = q;
            }
            _dad[q] = _dad[p];
            if (_rson[_dad[p]] == p)
                _rson[_dad[p]] = q;
            else
                _lson[_dad[p]] = q;
            _dad[p] = Nil;
        }

/* Huffman coding */

        private const int NChar = (256 - Threshold + F);
        /* kinds of characters (character code = 0..N_CHAR-1) */
        private const int T = (NChar * 2 - 1); /* size of table */
        private const int R = (T - 1); /* position of root */
        private const int MaxFreq = 0x8000; /* updates tree when the */

/* table for decoding the upper 6 bits of position */

/* for decoding */

        private byte[] _dCode =
        {
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
            0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
            0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02,
            0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02,
            0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03,
            0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03,
            0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04,
            0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05,
            0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06,
            0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07,
            0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08,
            0x09, 0x09, 0x09, 0x09, 0x09, 0x09, 0x09, 0x09,
            0x0A, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A,
            0x0B, 0x0B, 0x0B, 0x0B, 0x0B, 0x0B, 0x0B, 0x0B,
            0x0C, 0x0C, 0x0C, 0x0C, 0x0D, 0x0D, 0x0D, 0x0D,
            0x0E, 0x0E, 0x0E, 0x0E, 0x0F, 0x0F, 0x0F, 0x0F,
            0x10, 0x10, 0x10, 0x10, 0x11, 0x11, 0x11, 0x11,
            0x12, 0x12, 0x12, 0x12, 0x13, 0x13, 0x13, 0x13,
            0x14, 0x14, 0x14, 0x14, 0x15, 0x15, 0x15, 0x15,
            0x16, 0x16, 0x16, 0x16, 0x17, 0x17, 0x17, 0x17,
            0x18, 0x18, 0x19, 0x19, 0x1A, 0x1A, 0x1B, 0x1B,
            0x1C, 0x1C, 0x1D, 0x1D, 0x1E, 0x1E, 0x1F, 0x1F,
            0x20, 0x20, 0x21, 0x21, 0x22, 0x22, 0x23, 0x23,
            0x24, 0x24, 0x25, 0x25, 0x26, 0x26, 0x27, 0x27,
            0x28, 0x28, 0x29, 0x29, 0x2A, 0x2A, 0x2B, 0x2B,
            0x2C, 0x2C, 0x2D, 0x2D, 0x2E, 0x2E, 0x2F, 0x2F,
            0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37,
            0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F,
        };

        private byte[] _dLen =
        {
            0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03,
            0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03,
            0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03,
            0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03,
            0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04,
            0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04,
            0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04,
            0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04,
            0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04,
            0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04,
            0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05,
            0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05,
            0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05,
            0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05,
            0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05,
            0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05,
            0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05,
            0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05,
            0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06,
            0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06,
            0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06,
            0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06,
            0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06,
            0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06,
            0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07,
            0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07,
            0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07,
            0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07,
            0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07,
            0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07,
            0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08,
            0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08,
        };

        private uint[] _freq = new uint[T + 1]; /* frequency table */

        private int[] _prnt = new int[T + NChar]; /* pointers to parent nodes, except for the */
        /* elements [T..T + N_CHAR - 1] which are used to get */
        /* the positions of leaves corresponding to the codes. */

        private int[] _son = new int[T]; /* pointers to child nodes (son[], son[] + 1) */

        private uint _getbuf;
        private byte _getlen;

        private int GetBit() /* get one bit */
        {
            uint i;
            while (_getlen <= 8)
            {
                if ((int) (i = get_bits_left(8)) < 0)
                    i = 0;
                _getbuf |= (i << (8 - _getlen));
                _getlen += 8;
            }
            i = _getbuf;
            _getbuf <<= 1;
            _getlen--;
            return (int) ((i & 0x8000) >> 15);
        }

        private int GetByte() /* get one byte */
        {
            uint i;

            while (_getlen <= 8)
            {
                if ((int) (i = get_bits_left(8)) < 0) i = 0;
                _getbuf |= i << (8 - _getlen);
                _getlen += 8;
            }
            i = _getbuf;
            _getbuf <<= 8;
            _getlen -= 8;
            return (int) ((i & 0xff00) >> 8);
        }


/* initialization of tree */

        private void StartHuff()
        {
            for (int index = 0; index < NChar; ++index)
            {
                _freq[index] = 1;
                _son[index] = index + T;
                _prnt[index + T] = index;
            }
            int i = 0;
            int j = NChar;
            while (j <= R)
            {
                _freq[j] = _freq[i] + _freq[i + 1];
                _son[j] = i;
                _prnt[i] = _prnt[i + 1] = j;
                i += 2;
                j++;
            }
            _freq[T] = 0xffff;
            _prnt[R] = 0;
        }


/* reconstruction of tree */

        private void Reconst()
        {
            /* collect leaf nodes in the first half of the table */
            /* and replace the freq by (freq + 1) / 2. */

            int j = 0;
            int k;
            for (int i = 0; i < T; ++i)
            {
                if (_son[i] >= T)
                {
                    _freq[j] = (_freq[i] + 1) / 2;
                    _son[j] = _son[i];
                    j++;
                }
            }
            /* begin constructing tree by connecting sons */
            j = NChar;
            for (int i = 0; j < T; i += 2, ++j)
            {
                k = i + 1;
                uint f = _freq[j] = _freq[i] + _freq[k];
                for (k = j - 1; f < _freq[k]; --k) ;
                k++;
                uint l = (uint) (j - k) * 2;
                Array.Copy(_freq, k, _freq, k + 1, l);
                _freq[k] = f;
                Array.Copy(_son, k, _son, k + 1, l);
                _son[k] = i;
            }
            /* connect prnt */
            for (int i = 0; i < T; ++i)
            {
                if ((k = _son[i]) >= T)
                {
                    _prnt[k] = i;
                }
                else
                {
                    _prnt[k] = _prnt[k + 1] = i;
                }
            }
        }

/* increment frequency of given code by one, and update tree */

        private void Update(int c)
        {
            if (_freq[R] == MaxFreq)
            {
                Reconst();
            }
            c = _prnt[c + T];
            do
            {
                int k = (int) (++_freq[c]);
                /* if the order is disturbed, exchange nodes */
                int l;
                if ((uint) k <= _freq[l = c + 1])
                    continue;
                while ((uint) k > _freq[++l]) ;
                l--;
                _freq[c] = _freq[l];
                _freq[l] = (uint) k;

                int i = _son[c];
                _prnt[i] = l;
                if (i < T) _prnt[i + 1] = l;

                int j = _son[l];
                _son[l] = i;

                _prnt[j] = c;
                if (j < T) _prnt[j + 1] = c;
                _son[c] = j;

                c = l;
            } while ((c = _prnt[c]) != 0); /* repeat up to root */
        }


        private int DecodeByte()
        {
            uint c = (uint) _son[R];

            /* travel from root to leaf, */
            /* choosing the smaller child node (son[]) if the read bit is 0, */
            /* the bigger (son[]+1} if 1 */
            while (c < T)
            {
                c += (uint) GetBit();
                c = (uint) _son[c];
            }
            c -= T;
            Update((int) c);
            return (int) c;
        }


        private int DecodePosition()
        {
            uint i, j, c;

            /* recover upper 6 bits from table */
            i = (uint) GetByte();
            c = (uint) _dCode[i] << 6;
            j = _dLen[i];

            /* read lower 6 bits verbatim */
            j -= 2;
            while (j-- != 0)
            {
                i = (uint) ((i << 1) + GetBit());
            }
            return (int) (c | (i & 0x3f));
        }


        private void Reset()
        {
            InitTree();
            _getlen = 0;
            _getbuf = 0;
        }

        public byte[] Decode(byte[] input, int? startOffset, int? endOffset, int decompressedSize)
        {
            buf_in = input;
            buf_ptr = startOffset ?? 0;
            buf_end = endOffset ?? input.Length;
            bits_size = 0;
            bits_data = 0;
            uint outPtr = 0;
            uint len = (uint) decompressedSize;
            byte[] bufOut = new byte[decompressedSize];
            if (len == 0)
                return new byte[0];
            Reset();
            StartHuff();
            for (int i = 0; i < N - F; ++i)
                _textBuf[i] = 0x20;
            int r = N - F;
            uint count = 0;
            while (count < len)
            {
                int c = DecodeByte();
                if (c < 256)
                {
                    bufOut[outPtr++] = (byte) c;

                    _textBuf[r++] = (byte) c;
                    r &= (N - 1);
                    count++;
                }
                else
                {
                    int i = (r - DecodePosition() - 1) & (N - 1);
                    int j = c - 255 + Threshold;
                    int k;
                    for (k = 0; k < j; ++k)
                    {
                        c = _textBuf[(i + k) & (N - 1)];

                        bufOut[outPtr++] = (byte) c;
                        _textBuf[r++] = (byte) c;
                        r &= (N - 1);
                        count++;
                    }
                }
            }
            return bufOut;
        }

        private byte[] buf_in;
        private int buf_ptr;
        private int buf_end;
        private byte bits_size;
        private byte bits_data;

        private uint get_bits_left(uint totalBits)
        {
            byte[] bitsMask =
            {
                0x00, 0x01, 0x03, 0x07, 0x0f,
                0x1f, 0x3f, 0x7f, 0xff
            };

            uint numBits = totalBits;
            uint data = 0;

            while (numBits > 0)
            {
                // ERROR!
                if (buf_ptr >= buf_end)
                    return uint.MaxValue;
                // 8-bit buffer
                if (bits_size == 0)
                {
                    bits_size = 8;
                    bits_data = buf_in[buf_ptr++];
                }
                // consume cached bits
                uint useBits = numBits;
                if (useBits > 8) useBits = 8;
                if (useBits > bits_size)
                    useBits = bits_size;

                // tack on bits
                data <<= (int) useBits;
                data |= (uint) ((bits_data >> (int) (bits_size - useBits)) & bitsMask[useBits]);

                // update cache data
                numBits -= useBits;
                bits_size -= (byte) useBits;
            }
            return data;
        }
    }
}