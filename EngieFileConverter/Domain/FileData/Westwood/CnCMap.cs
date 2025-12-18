using Nyerguds.Util;
using System;
using System.IO;

namespace Nyerguds.FileData.Westwood
{
    public class CnCMap
    {
        public const Int32 LENGTH_TD = 0x1000;
        public const Int32 FILELENGTH_TD = LENGTH_TD * 2;

        public const Int32 LENGTH_RA = 0x4000;
        public const Int32 FILELENGTH_RA = LENGTH_RA * 3;

        public CnCMapCell[] Cells;
        public Boolean IsRaType { get; private set; }

        public CnCMapCell this[Int32 index]
        {
            get { return this.Cells[index]; }
            set { this.Cells[index] = value; }
        }

        public CnCMap(Byte[] buffer, bool raFormat)
        {
            this.IsRaType = raFormat;
            this.FillFromBuffer(buffer, raFormat);
        }

        public CnCMap(String filename)
        {
            this.IsRaType = false;
            this.ReadTdMapFromFile(filename);
        }

        public void WriteToFile(String filename)
        {
            if (String.IsNullOrEmpty(filename))
                throw new ArgumentNullException("filename", "No filename given!");
            using (FileStream fs = File.Create(filename))
                this.WriteToStream(fs);
        }

        public Byte[] GetAsBytes()
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
                for (Int32 i = 0; i < LENGTH_TD; ++i)
                {
                    CnCMapCell cell = this.Cells[i];
                    stream.WriteByte((byte)(cell.TemplateType & 0xFF));
                    stream.WriteByte(cell.Icon);
                }
            }
            else
            {
                for (Int32 i = 0; i < LENGTH_RA; ++i)
                {
                    CnCMapCell cell = this.Cells[i];
                    stream.WriteByte((byte)(cell.TemplateType & 0xFF));
                    stream.WriteByte((byte)((cell.TemplateType >> 8) & 0xFF));
                }
                for (Int32 i = 0; i < LENGTH_RA; ++i)
                {
                    stream.WriteByte(this.Cells[i].Icon);
                }
            }
        }

        private void ReadTdMapFromFile(String filename)
        {
            if (String.IsNullOrEmpty(filename))
                throw new ArgumentNullException("filename", "No filename given!");
            Byte[] buffer;
            using (FileStream fs = File.OpenRead(filename))
            {
                if (fs.Length != FILELENGTH_TD)
                    throw new ArgumentException("File must be " + FILELENGTH_TD + " bytes long.");
                buffer = new Byte[FILELENGTH_TD];
                fs.Read(buffer, 0, FILELENGTH_TD);
            }
            this.FillFromBuffer(buffer, false);
        }

        private void FillFromBuffer(Byte[] buffer, Boolean raFormat)
        {
            int dataLength = raFormat ? FILELENGTH_RA : FILELENGTH_TD;
            int cells = raFormat ? LENGTH_RA : LENGTH_TD;
            if (buffer.Length != dataLength)
                throw new ArgumentException("Buffer must be " + dataLength + " bytes long.");
            this.Cells = new CnCMapCell[cells];
            if (!raFormat)
            {
                Int32 pos = 0;
                for (Int32 i = 0; i < cells; ++i)
                {
                    this.Cells[i] = new CnCMapCell(buffer[pos], buffer[pos + 1], false);
                    pos += 2;
                }
            }
            else
            {
                Int32 pos1 = 0;
                Int32 pos2 = LENGTH_RA * 2;
                for (Int32 i = 0; i < cells; ++i)
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
        public UInt16 TemplateType { get; set; }
        public Byte Icon { get; set; }
        public Boolean RaFormat { get; private set; }
        public Int32 ValueTD { get { return this.TemplateType << 8 | this.Icon; } }

        public CnCMapCell(UInt16 templateType, Byte icon, bool raFormat)
        {
            this.TemplateType = templateType;
            this.Icon = icon;
            this.RaFormat = raFormat;
        }

        public CnCMapCell(Int32 value)
        {
            if (value > 0xFFFF)
                throw new ArgumentOutOfRangeException("value");
            this.TemplateType = (Byte)((value >> 8) & 0xFF);
            this.Icon = (Byte)(value & 0xFF);
        }

        public Boolean Equals(CnCMapCell cell)
        {
            return ((cell.TemplateType == this.TemplateType) && (cell.Icon == this.Icon));
        }

        public override String ToString()
        {
            return this.ValueTD.ToString("X4");
        }

        public Int32 CompareTo(CnCMapCell other)
        {
            return this.ValueTD.CompareTo(other.ValueTD);
        }

        public Int32 CompareTo(Object obj)
        {
            CnCMapCell cell = obj as CnCMapCell;
            if (cell != null)
                return this.CompareTo(cell);
            return this.ValueTD.CompareTo(obj);
        }

        public override Int32 GetHashCode()
        {
            return this.ValueTD;
        }
    }
}
