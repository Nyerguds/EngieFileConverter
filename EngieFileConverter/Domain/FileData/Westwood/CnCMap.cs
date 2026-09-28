using Nyerguds.Util;
using System;
using System.IO;

namespace Nyerguds.FileData.Westwood
{
    public class CnCMap
    {
        public const int LENGTH_TD = 0x1000;
        public const int FILELENGTH_TD = LENGTH_TD * 2;

        public const int LENGTH_RA = 0x4000;
        public const int FILELENGTH_RA = LENGTH_RA * 3;

        public CnCMapCell[] Cells;
        public bool IsRaType { get; private set; }

        public CnCMapCell this[int index]
        {
            get { return this.Cells[index]; }
            set { this.Cells[index] = value; }
        }

        public CnCMap(byte[] buffer, bool raFormat)
        {
            this.IsRaType = raFormat;
            this.FillFromBuffer(buffer, raFormat);
        }

        public CnCMap(string filename)
        {
            this.IsRaType = false;
            this.ReadTdMapFromFile(filename);
        }

        public void WriteToFile(string filename)
        {
            if (String.IsNullOrEmpty(filename))
                throw new ArgumentNullException("filename", "No filename given.");
            using (FileStream fs = File.Create(filename))
                this.WriteToStream(fs);
        }

        public byte[] GetAsBytes()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                this.WriteToStream(ms);
                ms.Flush();
                return ms.ToArray();
            }
        }

        public void WriteToStream(Stream stream)
        {
            if (!IsRaType)
            {
                for (int i = 0; i < LENGTH_TD; ++i)
                {
                    CnCMapCell cell = this.Cells[i];
                    stream.WriteByte((byte)(cell.TemplateType & 0xFF));
                    stream.WriteByte(cell.Icon);
                }
            }
            else
            {
                for (int i = 0; i < LENGTH_RA; ++i)
                {
                    CnCMapCell cell = this.Cells[i];
                    stream.WriteByte((byte)(cell.TemplateType & 0xFF));
                    stream.WriteByte((byte)((cell.TemplateType >> 8) & 0xFF));
                }
                for (int i = 0; i < LENGTH_RA; ++i)
                {
                    stream.WriteByte(this.Cells[i].Icon);
                }
            }
        }

        private void ReadTdMapFromFile(string filename)
        {
            if (String.IsNullOrEmpty(filename))
                throw new ArgumentNullException("filename", "No filename given.");
            byte[] buffer;
            using (FileStream fs = File.OpenRead(filename))
            {
                if (fs.Length != FILELENGTH_TD)
                    throw new ArgumentException("File must be " + FILELENGTH_TD + " bytes long.");
                buffer = new byte[FILELENGTH_TD];
                fs.Read(buffer, 0, FILELENGTH_TD);
            }
            this.FillFromBuffer(buffer, false);
        }

        private void FillFromBuffer(byte[] buffer, bool raFormat)
        {
            int dataLength = raFormat ? FILELENGTH_RA : FILELENGTH_TD;
            int cells = raFormat ? LENGTH_RA : LENGTH_TD;
            if (buffer.Length != dataLength)
                throw new ArgumentException("Buffer must be " + dataLength + " bytes long.");
            this.Cells = new CnCMapCell[cells];
            if (!raFormat)
            {
                int pos = 0;
                for (int i = 0; i < cells; ++i)
                {
                    this.Cells[i] = new CnCMapCell(buffer[pos], buffer[pos + 1], false);
                    pos += 2;
                }
            }
            else
            {
                int pos1 = 0;
                int pos2 = LENGTH_RA * 2;
                for (int i = 0; i < cells; ++i)
                {
                    this.Cells[i] = new CnCMapCell(ArrayUtils.ReadUInt16FromByteArrayLe(buffer, pos1), buffer[pos2], true);
                    pos1 += 2;
                    pos2++;
                }
            }
        }
    }

    public class CnCMapCell : IComparable<CnCMapCell>, IComparable
    {
        public ushort TemplateType { get; set; }
        public byte Icon { get; set; }
        public bool RaFormat { get; private set; }
        public int ValueTD { get { return this.TemplateType << 8 | this.Icon; } }

        public CnCMapCell(ushort templateType, byte icon, bool raFormat)
        {
            this.TemplateType = templateType;
            this.Icon = icon;
            this.RaFormat = raFormat;
        }

        public CnCMapCell(int value)
        {
            if (value > 0xFFFF)
                throw new ArgumentOutOfRangeException("value");
            this.TemplateType = (byte)((value >> 8) & 0xFF);
            this.Icon = (byte)(value & 0xFF);
        }

        public bool Equals(CnCMapCell cell)
        {
            return ((cell.TemplateType == this.TemplateType) && (cell.Icon == this.Icon));
        }

        public override string ToString()
        {
            return this.ValueTD.ToString("X4");
        }

        public int CompareTo(CnCMapCell other)
        {
            return this.ValueTD.CompareTo(other.ValueTD);
        }

        public int CompareTo(object obj)
        {
            CnCMapCell cell = obj as CnCMapCell;
            if (cell != null)
                return this.CompareTo(cell);
            return this.ValueTD.CompareTo(obj);
        }

        public override int GetHashCode()
        {
            return this.ValueTD;
        }
    }
}
