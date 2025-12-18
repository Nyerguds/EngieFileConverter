using System;
using System.IO;

namespace Nyerguds.GameData.Westwood
{
    public class CnCMap
    {
        public const Int32 LENGTH = 0x1000;
        public const Int32 FILELENGTH = LENGTH * 2;

        public CnCMapCell[] Cells;

        public CnCMapCell this[Int32 index]
        {
            get { return this.Cells[index]; }
            set { this.Cells[index] = value; }
        }

        public CnCMap(Byte[] buffer)
        {
            this.FillFromBuffer(buffer);
        }

        public CnCMap(String filename)
        {
            this.ReadFromFile(filename);
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
            foreach (CnCMapCell cell in this.Cells)
            {
                stream.WriteByte(cell.HighByte);
                stream.WriteByte(cell.LowByte);
            }
        }

        private void ReadFromFile(String filename)
        {
            if (String.IsNullOrEmpty(filename))
                throw new ArgumentNullException("filename", "No filename given!");
            Byte[] buffer;
            using (FileStream fs = File.OpenRead(filename))
            {
                if (fs.Length != FILELENGTH)
                    throw new ArgumentException("File must be " + FILELENGTH + " bytes long.");
                buffer = new Byte[FILELENGTH];
                fs.Read(buffer, 0, FILELENGTH);
            }
            this.FillFromBuffer(buffer);
        }

        private void FillFromBuffer(Byte[] buffer)
        {
            if (buffer.Length != FILELENGTH)
                throw new ArgumentException("Buffer must be " + FILELENGTH + " bytes long.");
            this.Cells = new CnCMapCell[LENGTH];
            for (Int32 i = 0; i < LENGTH; i++)
            {
                Int32 pos = i * 2;
                this.Cells[i] = new CnCMapCell(buffer[pos], buffer[pos + 1]);
            }
        }
    }

    public class CnCMapCell : IComparable<CnCMapCell>, IComparable
    {
        public Byte HighByte { get; set; }
        public Byte LowByte { get; set; }
        public Int32 Value { get { return this.HighByte << 8 | this.LowByte; } }

        public CnCMapCell(Byte highByte, Byte lowByte)
        {
            this.HighByte = highByte;
            this.LowByte = lowByte;
        }

        public CnCMapCell(Int32 value)
        {
            if (value > 0xFFFF)
                throw new ArgumentOutOfRangeException("value");
            this.HighByte = (Byte)((value >> 8) & 0xFF);
            this.LowByte = (Byte)(value & 0xFF);
        }

        public Boolean Equals(CnCMapCell cell)
        {
            return ((cell.HighByte == this.HighByte) && (cell.LowByte == this.LowByte));
        }

        public override String ToString()
        {
            return this.Value.ToString("X4");
        }

        public Int32 CompareTo(CnCMapCell other)
        {
            return this.Value.CompareTo(other.Value);
        }

        public Int32 CompareTo(Object obj)
        {
            CnCMapCell cell = obj as CnCMapCell;
            if (cell != null)
                return this.CompareTo(cell);
            return this.Value.CompareTo(obj);
        }

        public override Int32 GetHashCode()
        {
            return this.Value;
        }
    }
}
