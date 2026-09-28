using Nyerguds.ImageManipulation;
using Nyerguds.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace EngieFileConverter.Domain.FileTypes
{
    public class FileMapHqQuest : SupportedFileType
    {
        public override FileClass FileClass { get { return FileClass.Image8Bit; } }
        public override FileClass InputFileClass { get { return FileClass.Image8Bit; } }
        public override int Width { get { return this.m_LoadedImage?.Width ?? 0; } }
        public override int Height { get { return this.m_LoadedImage?.Height ?? 0; } }
        protected const int mapWidth = 26;
        protected const int mapHeight = 19;
        protected const int _cellSize = 12;
        protected const int _wallThickness = 2;
        public override string IdCode { get { return "HqBin"; } }
        /// <summary>Very short code name for this type.</summary>
        public override string ShortTypeName { get { return "HeroQuest Quest"; } }
        public override string[] FileExtensions { get { return new string[] { "bin" }; } }
        public override string LongTypeName { get { return "HeroQuest Quest File"; } }
        public override bool NeedsPalette { get { return false; } }
        public override int BitsPerPixel { get { return 8; } }

        /// <summary>True if this type can save. Defaults to true.</summary>
        public override bool CanSave { get { return false; } }

        public override void LoadFile(byte[] fileData)
        {
            this.LoadFile(fileData, null);
        }

        byte[] defaultRooms =
        {
            0x01, 0x01, 0x01, 0x01, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x05, 0x05, 0x05, 0x05,
            0x01, 0x15, 0x15, 0x15, 0x15, 0x16, 0x16, 0x16, 0x16, 0x17, 0x17, 0x17, 0x03, 0x03, 0x18, 0x18, 0x18, 0x19, 0x19, 0x19, 0x19, 0x1A, 0x1A, 0x1A, 0x1A, 0x05,
            0x01, 0x15, 0x15, 0x15, 0x15, 0x16, 0x16, 0x16, 0x16, 0x17, 0x17, 0x17, 0x03, 0x03, 0x18, 0x18, 0x18, 0x19, 0x19, 0x19, 0x19, 0x1A, 0x1A, 0x1A, 0x1A, 0x05,
            0x01, 0x15, 0x15, 0x15, 0x15, 0x16, 0x16, 0x16, 0x16, 0x17, 0x17, 0x17, 0x03, 0x03, 0x18, 0x18, 0x18, 0x19, 0x19, 0x19, 0x19, 0x1A, 0x1A, 0x1A, 0x1A, 0x05,
            0x01, 0x1B, 0x1B, 0x1B, 0x1B, 0x1C, 0x1C, 0x1C, 0x1C, 0x17, 0x17, 0x17, 0x03, 0x03, 0x18, 0x18, 0x18, 0x19, 0x19, 0x19, 0x19, 0x1A, 0x1A, 0x1A, 0x1A, 0x05,
            0x06, 0x1B, 0x1B, 0x1B, 0x1B, 0x1C, 0x1C, 0x1C, 0x1C, 0x17, 0x17, 0x17, 0x08, 0x08, 0x18, 0x18, 0x18, 0x1D, 0x1D, 0x1D, 0x1D, 0x1E, 0x1E, 0x1E, 0x1E, 0x0A,
            0x06, 0x1B, 0x1B, 0x1B, 0x1B, 0x1C, 0x1C, 0x1C, 0x1C, 0x07, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x09, 0x1D, 0x1D, 0x1D, 0x1D, 0x1E, 0x1E, 0x1E, 0x1E, 0x0A,
            0x06, 0x1B, 0x1B, 0x1B, 0x1B, 0x1C, 0x1C, 0x1C, 0x1C, 0x07, 0x1F, 0x1F, 0x1F, 0x1F, 0x1F, 0x1F, 0x09, 0x1D, 0x1D, 0x1D, 0x1D, 0x1E, 0x1E, 0x1E, 0x1E, 0x0A,
            0x06, 0x1B, 0x1B, 0x1B, 0x1B, 0x1C, 0x1C, 0x1C, 0x1C, 0x07, 0x1F, 0x1F, 0x1F, 0x1F, 0x1F, 0x1F, 0x09, 0x1D, 0x1D, 0x1D, 0x1D, 0x1E, 0x1E, 0x1E, 0x1E, 0x0A,
            0x06, 0x06, 0x06, 0x06, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x1F, 0x1F, 0x1F, 0x1F, 0x1F, 0x1F, 0x09, 0x09, 0x09, 0x09, 0x09, 0x09, 0x0A, 0x0A, 0x0A, 0x0A,
            0x0B, 0x20, 0x20, 0x20, 0x20, 0x21, 0x21, 0x22, 0x22, 0x0C, 0x1F, 0x1F, 0x1F, 0x1F, 0x1F, 0x1F, 0x0E, 0x23, 0x23, 0x23, 0x23, 0x24, 0x24, 0x24, 0x24, 0x0F,
            0x0B, 0x20, 0x20, 0x20, 0x20, 0x21, 0x21, 0x22, 0x22, 0x0C, 0x1F, 0x1F, 0x1F, 0x1F, 0x1F, 0x1F, 0x0E, 0x23, 0x23, 0x23, 0x23, 0x24, 0x24, 0x24, 0x24, 0x0F,
            0x0B, 0x20, 0x20, 0x20, 0x20, 0x21, 0x21, 0x22, 0x22, 0x0C, 0x0D, 0x0D, 0x0D, 0x0D, 0x0D, 0x0D, 0x0E, 0x23, 0x23, 0x23, 0x23, 0x24, 0x24, 0x24, 0x24, 0x0F,
            0x0B, 0x20, 0x20, 0x20, 0x20, 0x26, 0x26, 0x26, 0x26, 0x27, 0x27, 0x27, 0x0D, 0x0D, 0x28, 0x28, 0x28, 0x28, 0x23, 0x23, 0x23, 0x24, 0x24, 0x24, 0x24, 0x0F,
            0x0B, 0x25, 0x25, 0x25, 0x25, 0x26, 0x26, 0x26, 0x26, 0x27, 0x27, 0x27, 0x0D, 0x0D, 0x28, 0x28, 0x28, 0x28, 0x29, 0x29, 0x29, 0x2A, 0x2A, 0x2A, 0x2A, 0x0F,
            0x10, 0x25, 0x25, 0x25, 0x25, 0x26, 0x26, 0x26, 0x26, 0x27, 0x27, 0x27, 0x12, 0x12, 0x28, 0x28, 0x28, 0x28, 0x29, 0x29, 0x29, 0x2A, 0x2A, 0x2A, 0x2A, 0x14,
            0x10, 0x25, 0x25, 0x25, 0x25, 0x26, 0x26, 0x26, 0x26, 0x27, 0x27, 0x27, 0x12, 0x12, 0x28, 0x28, 0x28, 0x28, 0x29, 0x29, 0x29, 0x2A, 0x2A, 0x2A, 0x2A, 0x14,
            0x10, 0x25, 0x25, 0x25, 0x25, 0x26, 0x26, 0x26, 0x26, 0x27, 0x27, 0x27, 0x12, 0x12, 0x28, 0x28, 0x28, 0x28, 0x29, 0x29, 0x29, 0x2A, 0x2A, 0x2A, 0x2A, 0x14,
            0x10, 0x10, 0x10, 0x10, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x13, 0x13, 0x13, 0x13, 0x13, 0x13, 0x14, 0x14, 0x14, 0x14,
        };

        byte[] defaultWalls =
        {
            0xC0, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
            0x40, 0xC0, 0x80, 0x80, 0x80, 0xC0, 0x80, 0x80, 0x80, 0xC0, 0x80, 0x80, 0x41, 0x00, 0xC0, 0x80, 0x80, 0xC0, 0x80, 0x80, 0x80, 0xC0, 0x80, 0x80, 0x80, 0x40,
            0x40, 0x40, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x41, 0x00, 0x40, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x41,
            0x40, 0x40, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x41, 0x00, 0x40, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x41,
            0x40, 0xC0, 0x80, 0x80, 0x80, 0xC0, 0x80, 0x80, 0x80, 0x40, 0x00, 0x00, 0x41, 0x00, 0x40, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x41,
            0x40, 0x40, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x40, 0x00, 0x40, 0x00, 0x00, 0xC0, 0x80, 0x80, 0x80, 0xC0, 0x80, 0x80, 0x80, 0x40,
            0x40, 0x40, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0xC0, 0x80, 0x80, 0x00, 0x00, 0x81, 0x81, 0x80, 0x40, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x40,
            0x40, 0x40, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x40, 0xC0, 0x80, 0x80, 0x80, 0x80, 0x80, 0x40, 0x40, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x40,
            0x40, 0x40, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x40, 0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x40, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x40,
            0x40, 0x81, 0x81, 0x81, 0x80, 0x80, 0x80, 0x80, 0x80, 0x00, 0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x81, 0x81, 0x81, 0x81, 0x81, 0x80, 0x80, 0x80, 0x00,
            0x40, 0xC0, 0x80, 0x80, 0x80, 0xC0, 0x80, 0xC0, 0x80, 0x40, 0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0xC0, 0x80, 0x80, 0x80, 0xC0, 0x80, 0x80, 0x80, 0x40,
            0x40, 0x40, 0x00, 0x00, 0x00, 0x40, 0x00, 0x40, 0x00, 0x40, 0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x40, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x40,
            0x40, 0x40, 0x00, 0x00, 0x00, 0x40, 0x00, 0x40, 0x00, 0x40, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x00, 0x40, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x40,
            0x40, 0x40, 0x00, 0x00, 0x00, 0xC0, 0x80, 0x80, 0x80, 0xC0, 0x80, 0x80, 0x41, 0x00, 0xC0, 0x80, 0x80, 0x80, 0x41, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x40,
            0x40, 0xC0, 0x80, 0x80, 0x80, 0x40, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x41, 0x00, 0x40, 0x00, 0x00, 0x00, 0xC0, 0x80, 0x80, 0xC0, 0x80, 0x80, 0x80, 0x40,
            0x40, 0x40, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x40, 0x00, 0x40, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x40,
            0x40, 0x40, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x40, 0x00, 0x40, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x40,
            0x40, 0x40, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x40, 0x00, 0x40, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x40,
            0x40, 0x81, 0x81, 0x81, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x00, 0x00, 0x81, 0x81, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x00,
        };

        // Alphabet icons.
        private static readonly string iconLetterA = " XX |X  X|XXXX|X  X|X  X";
        private static readonly string iconLetterB = "XXX |X  X|XXX |X  X|XXX ";
        private static readonly string iconLetterC = " XX |X  X|X   |X  X| XX ";
        private static readonly string iconLetterD = "XXX |X  X|X  X|X  X|XXX ";
        private static readonly string iconLetterE = "XXXX|X   |XXX |X   |XXXX";
        private static readonly string iconLetterF = "XXXX|X   |XXXX|X   |X   ";
        private static readonly string iconLetterG = " XX |X   |X XX|X  X| XX ";
        private static readonly string iconLetterH = "X  X|X  X|XXXX|X  X|X  X";
        private static readonly string iconLetterI = " X  | X  | X  | X  | X  ";
        private static readonly string iconLetterJ = "   X|   X|   X|X  X| XX ";
        private static readonly string iconLetterK = "X  X|X X |XX  |X X |X  X";
        private static readonly string iconLetterL = "X   |X   |X   |X   |XXXX";
        private static readonly string iconLetterM = "X   X|XX XX|X X X|X   X|X   X";
        private static readonly string iconLetterN = "X  X|XX X|X XX|X  X|X  X";
        private static readonly string iconLetterO = " XX |X  X|X  X|X  X| XX ";
        private static readonly string iconLetterP = "XXX |X  X|XXX |X   |X   ";
        private static readonly string iconLetterQ = " XX |X  X|X  X|X  X| XXX";
        private static readonly string iconLetterR = "XXX |X  X|XXX |X  X|X  X";
        private static readonly string iconLetterS = " XXX|X   | XX |   X|XXX ";
        private static readonly string iconLetterT = "XXX | X | X | X | X ";
        private static readonly string iconLetterU = "X  X|X  X|X  X|X  X| XX ";
        private static readonly string iconLetterV = "X  X|X  X| XX |XX  | X  ";
        private static readonly string iconLetterW = "X   X|X   X|X X X|X X X| X X ";
        private static readonly string iconLetterX = "X   X| X X |  X  | X X |X   X";
        private static readonly string iconLetterY = "X X|X X| X | X |X  ";
        private static readonly string iconLetterZ = "XXXX|   X| XX |X   |XXXX";
        private static readonly string iconNumber0 = " XX |X XX|XXXX|XX X| XX ";
        private static readonly string iconNumber1 = "  X | XX |X X |  X |  X ";
        private static readonly string iconNumber2 = " XX |X  X|  X | X  |XXXX";
        private static readonly string iconNumber3 = "XXX |   X|  X |   X|XXX ";
        private static readonly string iconNumber4 = "  X | X  |X X |XXXX|  X ";
        private static readonly string iconNumber5 = "XXXX|X   |XXX |   X|XXX ";
        private static readonly string iconNumber6 = " XX |X   |XXX |X  X| XX ";
        private static readonly string iconNumber7 = "XXXX|   X|  X | X  | X  ";
        private static readonly string iconNumber8 = " XX |X  X| XX |X  X| XX ";
        private static readonly string iconNumber9 = " XX |X  X| XXX|   X| XX ";
        private static readonly string iconDash =    "    |    |XXXX|    |    ";
        private static readonly string iconQuestion =" XX |   X| XX |    | X  ";

        // Staircase icons
        private static readonly string iconSt1 = "            |            |  XXXXXXXXXX|  XXXXXXXXXX|  XXXXXX    |  XXXXXX    |  XX  XXXXXX|  XX  XXXXXX|  XX  XX  XX|  XX  XX  XX|  XX  XX  XX|  XX  XX  XX";
        private static readonly string iconSt2 = "  XX  XX  XX|  XX  XX  XX|  XX  XX  XX|  XX  XX  XX|  XX  XX  XX|  XX  XX  XX|  XX  XX  XX|  XX  XX  XX|  XX  XX  XX|  XX  XX  XX|  XXXXXXXXXX|  XXXXXXXXXX";
        private static readonly string iconSt3 = "            |            |XXXXXXXXXXXX|XXXXXXXXXXXX|          XX|          XX|          XX|          XX|XXXX      XX|XXXX      XX|  XXXXXX  XX|  XXXXXX  XX";
        private static readonly string iconSt4 = "  XX  XXXXXX|  XX  XXXXXX|  XX  XX  XX|  XX  XX  XX|  XX  XX  XX|  XX  XX  XX|  XX  XX  XX|  XX  XX  XX|  XX  XX  XX|  XX  XX  XX|XXXXXXXXXXXX|XXXXXXXXXXXX";
        // object icons.
        private static readonly string iconTel = "  XX  | X  X |X XX X|X XX X| X  X |  XX  ";
        private static readonly string iconTrp = "      | X   X|  X X |   X  |  X X | X   X";
        private static readonly string iconChs = "      |  XXX | XXXXX| X X X| X   X| XXXXX";
        private static readonly string iconRoc = "      |  XXX | XX  X| XXX X| XXXXX|  XXX ";
        private static readonly string iconThr = "      | XX   | XX   | XXXXX| XX XX| XX XX";
        private static readonly string iconAlc = "      |      | XXXXX| X X X| XXXXX| X   X";
        private static readonly string iconTom = "      |   X  |  XXX |   X  |   X  | XXXXX";
        private static readonly string iconClo = "      | XXXXX| X X X| X X X| XXXXX| X   X";
        private static readonly string iconLib = "      | XXXXX| X X X| XX XX| XXXXX| X   X";
        private static readonly string iconArm = "      |   X  |   X  |  XXX |   X  | XXXXX";
        private static readonly string iconTab = "      |      |      |      | XXXXX| X   X";
        private static readonly string iconFir = "      |      | XXXXX| X   X| X X X| XXXXX";
        private static readonly string iconTor = "      |      |      | X X X| XXXXX| X   X";

        public override void LoadFile(byte[] fileData, string filename)
        {
            const int roomSize = 0x200;
            const int mapSize = mapWidth * mapHeight;
            const int heroesSize = 0x0C;
            const int roomIds = 0x2B;
            const int popHeadersize = 0x0C;

            const int roomsOffset = 0;
            const int wallsOffset = roomsOffset + roomSize;
            const int heroesOffset = wallsOffset + roomSize;
            const int eventTreasureOffset = heroesOffset + heroesSize;
            const int eventHiddenOffset = eventTreasureOffset + roomIds;
            const int popSectionOffset = eventHiddenOffset + roomIds;

            if (fileData.Length < popSectionOffset + popHeadersize)
                throw new FileTypeLoadException(ERR_FILE_TOO_SMALL);
            // check on padding bytes in header.
            if (fileData.Skip(mapSize).Take(roomSize - mapSize).Any(b => b != 0))
                throw new FileTypeLoadException(ERR_BAD_HEADER_DATA);
            if (fileData.Skip(roomSize + mapSize).Take(roomSize - mapSize).Any(b => b != 0))
                throw new FileTypeLoadException(ERR_BAD_HEADER_DATA);
            byte[] rooms = new byte[mapSize];
            Array.Copy(fileData, roomsOffset, rooms, 0, mapSize);
            int maxroom = 0x2A;
            bool[] foundRooms = new bool[maxroom + 1];
            bool[] foundRoomsSeen = new bool[maxroom + 1];
            bool[] foundRoomsClosed = new bool[maxroom + 1];
            int cellCount = 0;
            int cellCountBigRooms = 0;
            int cellCountCorridors = 0;
            for (int i = 0; i < rooms.Length; ++i)
            {
                byte data = rooms[i];
                byte roomnr = (byte)(data & 0x3F);
                bool tileSeen = (data & 0x40) == 0x40;
                //bool wallOpen = (data & 0x80) == 0x80;
                if (roomnr <= maxroom)
                {
                    foundRooms[roomnr] = true;
                    bool[] toMark = tileSeen ? foundRoomsSeen : foundRoomsClosed;
                    toMark[roomnr] = true;
                }
                if (roomnr != 0)
                {
                    cellCount++;
                    if (roomnr < 21)
                    {
                        cellCountCorridors++;
                    }
                    else
                    {
                        cellCountBigRooms++;
                    }
                }
            }
            byte[] usedRooms = Enumerable.Range(1, foundRooms.Length - 1).Where(rn => foundRooms[rn]).Select(rn => (byte)rn).ToArray();
            byte[] seenRooms = Enumerable.Range(1, foundRoomsSeen.Length - 1).Where(rn => foundRoomsSeen[rn]).Select(rn => (byte)rn).ToArray();
            int roomsCount = usedRooms.Count();
            int roomsCountBig = usedRooms.Count(r => r >= 21);
            int roomsCountCor = usedRooms.Count(r => r < 21);
            //rooms = defaultRooms;
            byte[] walls = new byte[mapSize];
            Array.Copy(fileData, wallsOffset, walls, 0, mapSize);
            List<(Point, char)> heroes = new List<(Point, char)>() {
                ( new Point(fileData[heroesOffset + 0], fileData[heroesOffset + 1]), 'B' ), // Barbarian
                ( new Point(fileData[heroesOffset + 3], fileData[heroesOffset + 4]), 'D' ), // Dwarf
                ( new Point(fileData[heroesOffset + 6], fileData[heroesOffset + 7]), 'E' ), // Elf
                ( new Point(fileData[heroesOffset + 9], fileData[heroesOffset + 10]), 'W' ) // Wizard
            };
            byte[] eventsTreasure = new byte[roomIds];
            Array.Copy(fileData, eventTreasureOffset, eventsTreasure, 0, roomIds);
            int[] treasureRooms = Enumerable.Range(0, roomIds).Where(i => eventsTreasure[i] != 0).ToArray();
            byte[] eventsHidden = new byte[roomIds];
            Array.Copy(fileData, eventHiddenOffset, eventsHidden, 0, roomIds);
            int[] hiddenRooms = Enumerable.Range(0, roomIds).Where(i => eventsHidden[i] != 0).ToArray();

            List<(Point, MonsterType)> monsters = null;
            MonsterType wanderingMonster = MonsterType.None;
            Dictionary<Point, ObjectType> objects = null;
            List<string> texts = null;
            this.SetFileNames(filename);
            int questNr = -1;
            Match match = new Regex("^quest(\\d\\d?)\\.bin$", RegexOptions.IgnoreCase).Match(Path.GetFileName(filename));
            if (match.Success)
            {
                questNr = Int32.Parse(match.Groups[1].Value);
            }
            this.ReadPopulation(fileData, popSectionOffset, out monsters, out wanderingMonster, out objects, out texts);
            List<string> extrainfo = new List<string>();
            if (texts != null && texts.Count > 0) extrainfo.Add(texts[0]);
            extrainfo.Add("Wandering monster: " + wanderingMonster.ToString());
            extrainfo.Add("Cells: " + cellCount + " (corridor: " + cellCountCorridors + "; rooms: " + cellCountBigRooms + ")");
            extrainfo.Add("Rooms: " + roomsCount + " (corridor: " + roomsCountCor + "; rooms: " + roomsCountBig + ")");
            if (foundRoomsSeen.Any())
            {
                extrainfo.Add("Open rooms: " + String.Join(", ",
                    seenRooms.Select(or => or.ToString() + (foundRoomsSeen[or] && foundRoomsClosed[or] ? " (partial)" : String.Empty)).ToArray()));
            }
            extrainfo.Add("Search: " + (treasureRooms.Length == 0 ? "-" : String.Join(", ", treasureRooms.Select(i => "r" + i + ": " + ((EventType)eventsTreasure[i]).ToString()).ToArray())));
            extrainfo.Add("Scan: " + (hiddenRooms.Length == 0 ? "-" : String.Join(", ", hiddenRooms.Select(i => "r" + i + ": " + ((EventType)eventsHidden[i]).ToString()).ToArray())));
            int width;
            int height;
            int stride;
            byte[] imageData = GenerateImageData(_cellSize, _wallThickness, rooms, walls, heroes, monsters, objects, out width, out height, out stride);
            // Paints a dot on all rooms that are inaccessible.
            //ScanInaccessible(imageData, _cellSize, stride, rooms, walls, heroes, objects, questNr, true, false);
            if (texts != null && texts.Count > 1)
            {
                extrainfo.Add("\n" + String.Join("\n", texts.Skip(1).ToArray()));
            }
            this.ExtraInfo = String.Join("\n", extrainfo.ToArray());
            this.m_LoadedImage = GenerateImage(imageData, width, height, stride);
        }

        /// <summary>
        /// This function is meant for a map editor, to automatically suggest rooms to exclude if they are unused.
        /// </summary>
        /// <param name="imageData"></param>
        /// <param name="cellSize"></param>
        /// <param name="stride"></param>
        /// <param name="rooms"></param>
        /// <param name="walls"></param>
        /// <param name="heroes"></param>
        /// <param name="monsters"></param>
        /// <param name="objects"></param>
        /// <param name="questNr"></param>
        private void ScanInaccessible(byte[] imageData, int cellSize, int stride, byte[] rooms, byte[] walls, Dictionary<Point, char> heroes, Dictionary<Point, ObjectType> objects, int questNr, bool assessObjects, bool assessRoomZero)
        {
            int scanWidth;
            int scanHeight;
            int scanStride;
            /*/
            // Not hardcoded; defined in special event blocks.
            if (questNr == 5)
            {
                Point chairPoint = new Point(20, 3);
                int chairPos = chairPoint.Y * mapWidth + chairPoint.X;
                if (rooms[chairPos] == 25 && objects.TryGetValue(chairPoint, out ObjectType chair) && IsObstruction(chair))
                {
                    objects.Remove(chairPoint);
                    walls[chairPos + 1] = (byte)(WallInfo.DoorWest | WallInfo.DoorSecret);
                }
            }
            //*/
            // This system uses an image of the map with 3x3 cell as cell size to scan for passability.
            const int scanCellSize = 2;
            byte[] scanData = GenerateImageData(scanCellSize, 1, rooms, walls, null, null, null, out scanWidth, out scanHeight, out scanStride);
            /*/
            // Debug
            using (Bitmap bm = GenerateImage(scanData, scanWidth, scanHeight, scanStride))
            {
                bm.Save("quest" + questNr + "_bare.png");
            }
            //*/
            Point[] fullFill = new Point[scanCellSize * scanCellSize];
            int cellIndex = 0;
            for (int y = 0; y < scanCellSize; ++y)
            {
                for (int x = 0; x < scanCellSize; ++x)
                {
                    fullFill[cellIndex++] = new Point(x, y);
                }
            }
            // Amount of bytes to skip to skip an entire row of cells on the map image.
            int cellLineSize = scanCellSize * scanStride;
            List<Point> nullAreas = new List<Point>();
            if (assessRoomZero)
            {
                int lineOffs = 0;
                for (int y = 0; y < mapHeight; ++y)
                {
                    int curOffs = lineOffs;
                    for (int x = 0; x < mapWidth; ++x)
                    {
                        if (rooms[curOffs] == 0)
                        {
                            nullAreas.Add(new Point(x * scanCellSize + 1, y * scanCellSize + 1));
                            PaintPoints(scanData, cellLineSize * y + x * scanCellSize, scanStride, (byte)SpecialPalette.Wall, fullFill);
                        }
                        curOffs++;
                    }
                    lineOffs += mapWidth;
                }
            }
            List<Point> obstructedAreas = new List<Point>();
            if (assessObjects)
            {
                foreach (KeyValuePair<Point, ObjectType> objInfo in objects)
                {
                    if (IsObstruction(objInfo.Value))
                    {
                        obstructedAreas.Add(objInfo.Key);
                        PaintPoints(scanData, objInfo.Key.Y * cellLineSize + objInfo.Key.X * scanCellSize, scanStride, (byte)SpecialPalette.Wall, fullFill);
                    }
                }
            }
            /*/
            // Debug
            using (Bitmap bm = GenerateImage(scanData, scanWidth, scanHeight, scanStride))
            {
                bm.Save("quest" + questNr + ".png");
            }
            //*/
            Func<byte[], int, int, bool> detectFunc = (imgData, yVal, xVal) =>
            {
                byte val = imgData[yVal * scanWidth + xVal];
                return val != (int)SpecialPalette.Wall && val != (int)SpecialPalette.WallHidden;
            };
            List<List<Point>> blobs = BlobDetection.FindBlobs(scanData, scanWidth, scanHeight, detectFunc, false, false);
            HashSet<Point> heroStartPoints = new HashSet<Point>(heroes.Select(kvp => new Point(kvp.Key.X * scanCellSize + 1, kvp.Key.Y * scanCellSize + 1)));
            List<List<Point>> deadAreas = blobs.Where(pts => !pts.Any(pt => heroStartPoints.Contains(pt))).ToList();
            List<Point> allDead = new List<Point>();
            foreach (List<Point> deadArea in deadAreas)
                allDead.AddRange(deadArea.Where(p => p.X % scanCellSize == 1 && p.Y % scanCellSize == 1).Select(p => new Point(p.X / scanCellSize, p.Y / scanCellSize)));
            allDead = allDead.Distinct().ToList();
            //allDead.AddRange(nullAreas);
            allDead.AddRange(obstructedAreas);
            List<int> isDead = new List<int>();
            Dictionary<int, List<Point>> roomLocations = new Dictionary<int, List<Point>>();
            int lineStart = 0;
            byte[] roomsToScan = rooms; // defaultRooms;
            for (int y = 0; y < mapHeight; ++y)
            {
                int offset = lineStart;
                for (int x = 0; x < mapWidth; ++x)
                {
                    int room = roomsToScan[offset];
                    List<Point> points;
                    if (!roomLocations.TryGetValue(room, out points))
                    {
                        points = new List<Point>();
                        roomLocations[room] = points;
                    }
                    points.Add(new Point(x, y));
                    offset++;
                }
                lineStart += mapWidth;
            }
            int maxRoom = 42;
            for (int i = 1; i <= maxRoom; ++i)
            {
                List<Point> points;
                if (!roomLocations.TryGetValue(i, out points) || points.All(p => allDead.Contains(p)))
                {
                    isDead.Add(i);
                }
            }
            int lineSize = cellSize * stride;
            int halfCellSize = cellSize / 2;
            int halfLineSize = halfCellSize * stride;
            this.ExtraInfo += "\nDead rooms: " + String.Join(", ", isDead.Select(i => i.ToString()).ToArray());
            foreach (Point p in allDead)
            {
                int offs = halfLineSize + lineSize * p.Y + halfCellSize + cellSize * p.X;
                imageData[offs] = (byte)SpecialPalette.Flag1;
            }
        }

        private bool IsObstruction(ObjectType value)
        {
            if (value < ObjectType.Rock01)
            {
                // Just assume these are all impassable.
                return true;
            }
            switch (value)
            {
                case ObjectType.Rock01:
                case ObjectType.Rock02:
                //case ObjectType.FallingRockTrap:
                case ObjectType.Teleport:
                case ObjectType.StairsBlock1:
                case ObjectType.StairsBlock2:
                case ObjectType.StairsBlock3:
                case ObjectType.StairsBlock4:
                case ObjectType.TableFiller:
                case ObjectType.Object67:
                case ObjectType.TortureFiller:
                case ObjectType.TombFiller:
                case ObjectType.AlchemyFiller:
                case ObjectType.FireplaceFiller:
                case ObjectType.ClosetFiller:
                case ObjectType.LibraryFiller:
                case ObjectType.ArmourRackFiller:
                case ObjectType.Table2x3Origin:
                case ObjectType.Table3x2Origin:
                case ObjectType.Torture3x2Origin:
                case ObjectType.Torture2x3Origin:
                case ObjectType.Alchemy2x3Origin:
                case ObjectType.Alchemy3x2Origin:
                case ObjectType.Tomb3x2Origin:
                case ObjectType.Closet1x3Origin:
                case ObjectType.ArmourRack3x1Origin:
                case ObjectType.Library1x3Origin:
                case ObjectType.Fireplace3x1Origin:
                case ObjectType.Library3x1Origin:
                case ObjectType.Fireplace1x3Origin:
                case ObjectType.Closet3x1Origin:
                case ObjectType.ArmourRack1x3Origin:
                case ObjectType.ChestSouth:
                case ObjectType.ChestEast:
                case ObjectType.ThroneSouth:
                case ObjectType.ThroneEast:
                    return true;
            }
            return false;
        }

        private byte[] GenerateImageData(int cellSize, int wallThickness, byte[] rooms, byte[] walls, List<(Point, char)> heroes, List<(Point, MonsterType)> monsters, Dictionary<Point, ObjectType> objects, out int width, out int height, out int stride)
        {
            width = mapWidth * cellSize;
            stride = width;
            height = mapHeight * cellSize;
            byte[] mapImage = new byte[width * height];

            int readOffs = 0;
            int cellOffs = 0;
            int lineblockSize = cellSize * cellSize * mapWidth;
            Point[] northWall = MakeWall(cellSize, 0, cellSize, 0, wallThickness, cellSize, true, false, false);
            Point[] westWall = MakeWall(cellSize, 0, cellSize, 0, wallThickness, cellSize, false, false, false);
            Point[] northWSeen = MakeWall(cellSize, 0, cellSize, 0, wallThickness, cellSize, true, false, true);
            Point[] westWSeen = MakeWall(cellSize, 0, cellSize, 0, wallThickness, cellSize, false, false, true);
            Point[] northGrid = MakeWall(cellSize, 0, cellSize, 0, 1, cellSize, true, false, false);
            Point[] westGrid = MakeWall(cellSize, 0, cellSize, 0, 1, cellSize, false, false, false);
            Point[][] wallTypes = new[] { northWall, westWall, northWSeen, westWSeen, northGrid, westGrid };
            int startPoint = cellSize < 2 ? 0 : Math.Max(1, cellSize / 6);
            int doorSize = Math.Max(1, cellSize - startPoint * 2);
            Point[] northDoor = MakeWall(cellSize, startPoint, doorSize, 0, wallThickness, cellSize, true, false, false);
            Point[] westDoor = MakeWall(cellSize, startPoint, doorSize, 0, wallThickness, cellSize, false, false, false);
            Point[] northDSeen = MakeWall(cellSize, startPoint, doorSize, 0, wallThickness, cellSize, true, false, true);
            Point[] westDSeen = MakeWall(cellSize, startPoint, doorSize, 0, wallThickness, cellSize, false, false, true);
            Point[][] doorTypes = new[] { northDoor, westDoor, northDSeen, westDSeen };
            int offset = wallThickness;
            int centerSize = cellSize - wallThickness;
            for (int y = 0; y < mapHeight; ++y)
            {
                int lineOffs = readOffs;
                int lineCellOffs = cellOffs;
                for (int x = 0; x < mapWidth; ++x)
                {
                    byte room = rooms[lineOffs];
                    bool tileSeen = (room & 0x40) == 0x40;
                    bool wallSeen = (room & 0x80) == 0x80;
                    bool wallSeenN = tileSeen || (wallSeen && (y > 0 && (rooms[lineOffs - mapWidth] & 0x40) == 0x40));
                    bool wallSeenW = tileSeen || (wallSeen && (x > 0 && (rooms[lineOffs - 1] & 0x40) == 0x40));
                    room = (byte)(room & 0x3F);
                    byte wallValue = walls[lineOffs];
                    Point cur = new Point(x, y);
                    WallInfo wall = (WallInfo)wallValue;
                    PaintWallsInfo(mapImage, lineCellOffs, width, cellSize, room, tileSeen, wallSeenN, wallSeenW, wall, wallTypes, doorTypes, false);
                    //*/
                    if (heroes != null)
                    {
                        foreach((Point, char) hero in heroes)
                        {
                            if (hero.Item1.X == x && hero.Item1.Y == y)
                            {
                                Point[] letterIcon = GetLetterIcon(hero.Item2, offset, offset, centerSize, centerSize);
                                PaintPoints(mapImage, lineCellOffs, width, (int)SpecialPalette.Heroes, letterIcon);
                            }
                        }
                    }
                    if (monsters != null)
                    {
                        foreach ((Point, MonsterType) monster in monsters)
                        {
                            if (monster.Item1.X == x && monster.Item1.Y == y)
                            {
                                Point[] monsterIcon = GetMonsterIcon(monster.Item2, offset, offset, centerSize, centerSize);
                                PaintPoints(mapImage, lineCellOffs, width, (int)SpecialPalette.Monsters, monsterIcon);
                            }
                        }
                    }
                    if (objects != null && objects.TryGetValue(cur, out ObjectType obj))
                    {
                        Point[] objectIcon = GetObjectIcon(obj, 0, 0, cellSize, cellSize);
                        PaintPoints(mapImage, lineCellOffs, width, (int)SpecialPalette.Objects, objectIcon);
                    }
                    //*/
                    lineOffs++;
                    lineCellOffs += cellSize;
                }
                readOffs += mapWidth;
                cellOffs += lineblockSize;
            }
            return mapImage;
        }

        private Bitmap GenerateImage(byte[] mapImage, int imgWidth, int imgHeight, int imgStride)
        {
            Color[] palette = Enumerable.Repeat(Color.Black, 0x100).ToArray();
            Color[] grayPal = PaletteUtils.GenerateGrayPalette(8, null, false);
            // Take 20 colors from the mid-range of the generated palette: use 50%, start at 1/4th.
            int step = 0x100 / 2 / 20;
            int srcndex = 0x100 / 4;
            for (int i = 1; i < 21; ++i)
            {
                palette[i] = grayPal[srcndex];
                srcndex += step;
            }
            // rooms
            Color[] rainbow = PaletteUtils.GenerateRainbowPalette(8, -1, null, false);
            // Old logic: takes 20 colors from the mid-range of the generated palette: use 50%, start at 1/4th.
            //step = 0x100 / 2 / 20;
            //srcndex = 0x100 / 4;
            // Use entire rotation, but offset by a bit.
            step = 0x100 / 21;
            srcndex = 20;
            for (int i = 21; i < 43; ++i)
            {
                Color c = rainbow[srcndex];
                // Alternate between dark and bright to bring more contrast into the room colours.
                int div = 2 + (i % 2);
                palette[i] = Color.FromArgb(c.R / div, c.G / div, c.B / div);
                srcndex += step;
                if (srcndex > 255)
                    srcndex -= 256;
            }
            // Unused room area
            palette[(int)SpecialPalette.RoomZero] = Color.FromArgb(0x00, 0x00, 0x00);
            // Wall
            palette[(int)SpecialPalette.Wall] = Color.FromArgb(0xC0, 0x00, 0x00);
            // Draw-suppressed wall
            palette[(int)SpecialPalette.WallHidden] = Color.FromArgb(0xFF, 0x80, 0x00);
            // Door
            palette[(int)SpecialPalette.Door] = Color.FromArgb(0xFF, 0xFF, 0xFF);
            // Secret door
            palette[(int)SpecialPalette.DoorSecret] = Color.FromArgb(0xFF, 0x00, 0xFF);
            // Open door
            palette[(int)SpecialPalette.DoorOpened] = Color.FromArgb(0x80, 0x80, 0x80);
            
            // Hero
            palette[(int)SpecialPalette.Heroes] = Color.FromArgb(0xFF, 0x00, 0x00);
            // Creature
            palette[(int)SpecialPalette.Monsters] = Color.FromArgb(0x00, 0xFF, 0x00);
            // Object
            palette[(int)SpecialPalette.Objects] = Color.FromArgb(0xA0, 0x50, 0x50);
            // special bit flag indicators (for testing only)
            palette[(int)SpecialPalette.Flag1] = Color.FromArgb(0xFF, 0xFF, 0xC0);
            palette[(int)SpecialPalette.Flag2] = Color.FromArgb(0xFF, 0xC0, 0xFF);
            palette[(int)SpecialPalette.Flag3] = Color.FromArgb(0x0C, 0xFF, 0xFF);
            palette[(int)SpecialPalette.Flag4] = Color.FromArgb(0xFF, 0xC0, 0xC0);
            palette[(int)SpecialPalette.Grid] = Color.FromArgb(0x80, 0x00, 0x80);
            palette[(int)SpecialPalette.RoomOpened] = Color.FromArgb(0x00, 0x50, 0x00);

            return ImageUtils.BuildImage(mapImage, imgWidth, imgHeight, imgStride, PixelFormat.Format8bppIndexed, palette, Color.Black);
        }

        private Point[] GetLetterIcon(char letter, int offsetX, int offsetY, int centerX, int centerY)
        {

            bool[][] iconData = GetLetterIconData(letter);
            return GetIconPoints(iconData, offsetX, offsetY, centerX, centerY);
        }

        protected bool[][] GetLetterIconData(char letter)
        {
            string iconStr;
            switch (letter.ToString().ToUpperInvariant()[0])
            {
                case 'A': iconStr = iconLetterA; break;
                case 'B': iconStr = iconLetterB; break;
                case 'C': iconStr = iconLetterC; break;
                case 'D': iconStr = iconLetterD; break;
                case 'E': iconStr = iconLetterE; break;
                case 'F': iconStr = iconLetterF; break;
                case 'G': iconStr = iconLetterG; break;
                case 'H': iconStr = iconLetterH; break;
                case 'I': iconStr = iconLetterI; break;
                case 'J': iconStr = iconLetterJ; break;
                case 'K': iconStr = iconLetterK; break;
                case 'L': iconStr = iconLetterL; break;
                case 'M': iconStr = iconLetterM; break;
                case 'N': iconStr = iconLetterN; break;
                case 'O': iconStr = iconLetterO; break;
                case 'P': iconStr = iconLetterP; break;
                case 'Q': iconStr = iconLetterQ; break;
                case 'R': iconStr = iconLetterR; break;
                case 'S': iconStr = iconLetterS; break;
                case 'T': iconStr = iconLetterT; break;
                case 'U': iconStr = iconLetterU; break;
                case 'V': iconStr = iconLetterV; break;
                case 'W': iconStr = iconLetterW; break;
                case 'X': iconStr = iconLetterX; break;
                case 'Y': iconStr = iconLetterY; break;
                case 'Z': iconStr = iconLetterZ; break;
                case '0': iconStr = iconNumber0; break;
                case '1': iconStr = iconNumber1; break;
                case '2': iconStr = iconNumber2; break;
                case '3': iconStr = iconNumber3; break;
                case '4': iconStr = iconNumber4; break;
                case '5': iconStr = iconNumber5; break;
                case '6': iconStr = iconNumber6; break;
                case '7': iconStr = iconNumber7; break;
                case '8': iconStr = iconNumber8; break;
                case '9': iconStr = iconNumber9; break;
                case '-': iconStr = iconDash; break;
                default: iconStr = iconQuestion; break;
            }
            return GetIconData(iconStr);
        }

        protected static bool[][] GetIconData(string iconStr)
        {
            string[] lines = iconStr.Split('|');
            int iconW = lines.Max(l => l.Length);
            int iconH = lines.Length;
            bool[][] pixels = new bool[iconH][];
            for (int y = 0; y < iconH; ++y)
            {
                string line = lines[y];
                bool[] bline = new bool[iconW];
                for (int x = 0; x < line.Length; ++x)
                {
                    bline[x] = line[x] != ' ';
                }
                pixels[y] = bline;
            }
            return pixels;
        }

        enum SpecialPalette : byte
        {
            RoomZero = 0,
            Wall = 48, // Row3Start
            WallHidden,
            Door,
            DoorSecret,
            DoorOpened,
            Heroes,
            Monsters,
            Objects,
            Flag1,
            Flag2,
            Flag3,
            Flag4,
            Grid,
            RoomOpened,
            // Row4Start = 64,
        }

        private void ReadPopulation(byte[] fileData, int popSectionOffset, out List<(Point, MonsterType)> monsters, out MonsterType wanderingMonster, out Dictionary<Point, ObjectType> objects, out List<string> texts)
        {
            ushort textBlockStart = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, popSectionOffset + 0);
            ushort textBlockSize = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, popSectionOffset + 2);
            ushort monstersBlockStart = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, popSectionOffset + 4);
            ushort monstersBlockSize = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, popSectionOffset + 6);
            ushort objectsBlockStart = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, popSectionOffset + 8);
            ushort nrOfObjects = ArrayUtils.ReadUInt16FromByteArrayLe(fileData, popSectionOffset + 10);
            int len = fileData.Length;
            if (len < popSectionOffset + monstersBlockStart + monstersBlockSize ||
               len < popSectionOffset + textBlockStart + textBlockSize ||
                len < popSectionOffset + objectsBlockStart + nrOfObjects * 3)
                throw new FileTypeLoadException(ERR_FILE_TOO_SMALL);
            monsters = new List<(Point, MonsterType)>();
            wanderingMonster = MonsterType.None;
            objects = new Dictionary<Point, ObjectType>();
            texts = new List<string>();
            int monsterEnd = popSectionOffset + monstersBlockStart + monstersBlockSize;
            for (int offs = popSectionOffset + monstersBlockStart; offs < monsterEnd; offs += 10)
            {
                Point monsterLoc = new Point(fileData[offs + 01], fileData[offs + 02]);
                int monsterId = fileData[offs + 08];
                MonsterType monster = (MonsterType)monsterId;
                if (fileData[offs] != 0)
                {
                    monsters.Add((monsterLoc, monster));
                }
                if (wanderingMonster == MonsterType.None)
                {
                    wanderingMonster = monster;
                }
            }
            int objectsEnd = popSectionOffset + objectsBlockStart + nrOfObjects * 3;
            for (int i = popSectionOffset + objectsBlockStart; i < objectsEnd; i += 3)
            {
                Point objectLoc = new Point(fileData[i + 00], fileData[i + 01]);
                int objId = fileData[i + 02];
                ObjectType obj = (ObjectType)objId;
                objects.Add(objectLoc, obj);
            }
            int txtPtr = popSectionOffset + textBlockStart;
            int txtEnd = popSectionOffset + textBlockStart + textBlockSize;
            Encoding dosEnc = Encoding.GetEncoding(437);
            while (txtPtr < txtEnd)
            {
                if (fileData[txtPtr++] != 0x11)
                    throw new FileLoadException("Text entry does not start with required value.");
                //Point txtPoint = new Point(fileData[txtPtr + 1], fileData[txtPtr]);
                // These aren't map locations. Ignore them for now.
                txtPtr += 2;
                int txtStart = txtPtr;
                while (txtPtr < txtEnd && fileData[txtPtr] != 0x11 && fileData[txtPtr] != 0x00)
                    txtPtr++;
                int txtlen = txtPtr - txtStart;
                byte[] text = new byte[txtlen];
                Array.Copy(fileData, txtStart, text, 0, txtlen);
                texts.Add(dosEnc.GetString(text));
                if (fileData[txtPtr] == 0x00)
                    break;
            }
        }

        private static Point[] GetIconPoints(string iconDataStr, int offsetX, int offsetY, int centerX, int centerY)
        {
            return GetIconPoints(GetIconData(iconDataStr), offsetX, offsetY, centerX, centerY);
        }

        private static Point[] GetIconPoints(bool[][] iconData, int offsetX, int offsetY, int centerX, int centerY)
        {
            int iconH = iconData.Length;
            int iconW = iconData.Max(l => l.Length);
            List<Point> points = new List<Point>();
            int lenY = Math.Min(iconH, centerY);
            if (centerX == -1) centerX = iconW;
            if (centerY == -1) centerY = iconW;
            int offsX = offsetX + (centerX - iconW) / 2;
            int offsY = offsetY + (centerY - iconH) / 2;

            for (int y = 0; y < iconH; ++y)
            {
                bool[] line = iconData[y];
                // Cut off if too long.
                int lenX = Math.Min(line.Length, centerX);
                for (int x = 0; x < iconW; ++x)
                {
                    if (line[x])
                    {
                        points.Add(new Point(offsX + x, offsY + y));
                    }
                }
            }
            return points.ToArray();
        }

        private Point[] GetMonsterIcon(MonsterType monster, int offsetX, int offsetY, int centerX, int centerY)
        {
            int monsterId = (int)monster;
            string iconStr = null;
            if (monsterId >= 0 && monsterId < monsterNames.Length)
            {
                iconStr = monsterNames[monsterId];
            }
            if (iconStr == null)
            {
                iconStr = ((int)monster).ToString("D2");
            }
            iconStr = iconStr.ToUpperInvariant();
            char iconLetter1 = String.IsNullOrEmpty(iconStr) ? '?' : iconStr[0];
            char iconLetter2 = String.IsNullOrEmpty(iconStr) || iconStr.Length < 2 ? '\0' : iconStr[1];

            if (iconLetter2 == '\0')
            {
                return GetLetterIcon(iconLetter1, offsetX, offsetY, centerX, centerY);
            }
            int offset1x = offsetX;
            int offset_y = offsetY;
            int center = centerX / 2;
            int offset2x = offsetX + center;
            Point[] points1 = GetLetterIcon(iconLetter1, offset1x, offset_y, center, centerY);
            // If it touches. Larger and we'll allow it to touch, smaller and it'll center itself away anyway.
            bool[][] letter2 = GetLetterIconData(iconLetter2);
            if (points1.Any(p => p.X + 1 >= offset2x) && letter2.Max(p => p.Length) == center - 1)
                offset2x++;
            Point[] points2 = GetIconPoints(letter2, offset2x, offset_y, center, centerY);
            return points1.Union(points2).ToArray();
        }

        private Point[] GetObjectIcon(ObjectType item, int offsetX, int offsetY, int centerX, int centerY)
        {
            bool isFiller = false;
            string iconStr;
            switch (item)
            {
                case ObjectType.StairsBlock1: iconStr = iconSt1; break;
                case ObjectType.StairsBlock2: iconStr = iconSt2; break;
                case ObjectType.StairsBlock3: iconStr = iconSt3; break;
                case ObjectType.StairsBlock4: iconStr = iconSt4; break;
                case ObjectType.Teleport:
                    iconStr = iconTel; break;
                case ObjectType.Rock01:
                case ObjectType.Rock02:
                    iconStr = iconRoc; break;
                case ObjectType.ThroneSouth:
                case ObjectType.ThroneEast:
                    iconStr = iconThr; break;
                case ObjectType.Table2x3Origin:
                case ObjectType.Table3x2Origin:
                    iconStr = iconTab; break;
                case ObjectType.TableFiller:
                    iconStr = iconTab;
                    isFiller = true;
                    break;
                case ObjectType.Torture3x2Origin:
                case ObjectType.Torture2x3Origin:
                    iconStr = iconTor; break;
                case ObjectType.TortureFiller:
                    iconStr = iconTor;
                    isFiller = true;
                    break;
                case ObjectType.Tomb3x2Origin:
                    iconStr = iconTom; break;
                case ObjectType.TombFiller:
                    iconStr = iconTom;
                    isFiller = true;
                    break;
                case ObjectType.Alchemy2x3Origin:
                case ObjectType.Alchemy3x2Origin:
                    iconStr = iconAlc; break;
                case ObjectType.AlchemyFiller:
                    iconStr = iconAlc;
                    isFiller = true;
                    break;
                case ObjectType.Closet1x3Origin:
                case ObjectType.Closet3x1Origin:
                    iconStr = iconClo; break;
                case ObjectType.ClosetFiller:
                    iconStr = iconClo;
                    isFiller = true;
                    break;
                case ObjectType.Library1x3Origin:
                case ObjectType.Library3x1Origin:
                    iconStr = iconLib; break;
                case ObjectType.LibraryFiller:
                    iconStr = iconLib;
                    isFiller = true;
                    break;
                case ObjectType.ArmourRack3x1Origin:
                case ObjectType.ArmourRack1x3Origin:
                    iconStr = iconArm; break;
                case ObjectType.ArmourRackFiller:
                    iconStr = iconArm;
                    isFiller = true;
                    break;
                case ObjectType.Fireplace3x1Origin:
                case ObjectType.Fireplace1x3Origin:
                    iconStr = iconFir; break;
                case ObjectType.FireplaceFiller:
                    iconStr = iconFir;
                    isFiller = true;
                    break;
                case ObjectType.FallingRockTrap:
                case ObjectType.SpearTrap:
                case ObjectType.PitTrapHidden:
                    iconStr = iconTrp; break;
                case ObjectType.PitTrapOpened:
                    iconStr = iconTrp; break;
                case ObjectType.ChestEast:
                case ObjectType.ChestSouth:
                    iconStr = iconChs; break;
                default:
                    iconStr = iconQuestion; break;
            }
            Point[] points = GetIconPoints(iconStr, offsetX, offsetY, centerX, centerY);
            if (!isFiller)
                return points;
            //Point[] fillerPoints = iconFil;
            Point[] fillerPoints = MakeWall(centerX, 2, 10, 2, 1, 12, true, false, false)
                            .Union(MakeWall(centerX, 2, 10, 0, 1, 12, true, true, false))
                            .Union(MakeWall(centerY, 2, 10, 2, 1, 12, false, false, false))
                            .Union(MakeWall(centerY, 2, 10, 0, 1, 12, false, true, false)).Distinct().ToArray();
            return points.Union(fillerPoints).Distinct().ToArray();            
        }

        private static readonly Point dot1 = new Point(2, 2);
        private static readonly Point dot2 = new Point(3, 2);
        private static readonly Point dot3 = new Point(2, 3);
        private static readonly Point dot4 = new Point(3, 3);

        private static Point[] MakeWall(int cellSize, int lenStart, int length, int thicknessStart, int thickness, int divider, bool horizontal, bool opposite, bool gridify)
        {
            List<Point> points = new List<Point>();
            int realFullLength = horizontal ? cellSize : cellSize;
            int realFullThickness = horizontal ? cellSize : cellSize;
            int realWallStart = lenStart * realFullLength / divider;
            int realWallLength = length * realFullLength / divider;
            int realWallThicknessStart = thicknessStart * realFullThickness / divider;
            int realWallThickness = thickness * realFullThickness / divider;
            for (int th = 0; th < realWallThickness; ++th)
            {
                for (int len = 0; len < realWallLength; ++len)
                {
                    int l = realWallStart + len;
                    int t = realWallThicknessStart + th;
                    if (opposite)
                    {
                        t = realFullThickness - t - 1;
                    }
                    int x = horizontal ? l : t;
                    int y = horizontal ? t : l;
                    if (!gridify || Gridify(x, y, false, true))
                    {
                        points.Add(new Point(x, y));
                    }
                }
            }
            return points.ToArray();
        }

        private void PaintWallsInfo(byte[] mapImage, int startPoint, int stride, int cellSize, byte room, bool tileSeen, bool wallSeenN, bool wallSeenW,
            WallInfo wallInfo, Point[][] wallTypes, Point[][] doorTypes, bool paintGrid)
        {
            Point[] northWall = wallTypes[0];
            Point[] westWall = wallTypes[1];
            Point[] northWSeen = wallTypes[2];
            Point[] westWSeen = wallTypes[3];
            Point[] northGrid = wallTypes[4];
            Point[] westGrid = wallTypes[5];
            Point[] northDoor = doorTypes[0];
            Point[] westDoor = doorTypes[1];
            Point[] northDSeen = doorTypes[2];
            Point[] westDSeen = doorTypes[3];
            int writeOffs = startPoint;
            for (int y = 0; y < cellSize; ++y)
            {
                int lineOffs = writeOffs;
                for (int x = 0; x < cellSize; ++x)
                {
                    byte toPaint = tileSeen ? Gridify(x, y, room, (byte)SpecialPalette.RoomOpened) : room;
                    mapImage[lineOffs++] = toPaint;
                }
                writeOffs += stride;
            }
            bool notDrawn = (wallInfo & WallInfo.DontDraw) != 0;
            HashSet<Point> wallsExist = new HashSet<Point>();
            HashSet<Point> wallsGrid = paintGrid ? new HashSet<Point>() : null;
            if ((wallInfo & (WallInfo.WallNorth | WallInfo.DoorNorth)) != 0)
            {
                // North wall is only ever marked as visible if the cell itself is visible.
                wallsExist.UnionWith(wallSeenN ? northWSeen : northWall);
            }
            else if (wallsGrid != null)
            {
                wallsGrid.UnionWith(northGrid);
            }
            if ((wallInfo & (WallInfo.WallWest | WallInfo.DoorWest)) != 0)
            {
                wallsExist.UnionWith(wallSeenW ? westWSeen : westWall);
            }
            else if (wallsGrid != null)
            {
                wallsGrid.UnionWith(westGrid);
            }
            if (wallsGrid != null)
            {
                PaintPoints(mapImage, startPoint, stride, (byte)SpecialPalette.Grid, wallsGrid.ToArray());
            }
            PaintPoints(mapImage, startPoint, stride, (byte)(notDrawn ? SpecialPalette.WallHidden : SpecialPalette.Wall), wallsExist.ToArray());
            // doors
            List<Point[]> doors = new List<Point[]>();
            byte doorColor = (byte)SpecialPalette.Door;
            if ((wallInfo & WallInfo.DoorOpen) != 0)
                doorColor = (byte)SpecialPalette.DoorOpened;
            else if ((wallInfo & WallInfo.DoorSecret) != 0)
                doorColor = (byte)SpecialPalette.DoorSecret;
            if ((wallInfo & WallInfo.DoorNorth) != 0)
                doors.Add(wallSeenN ? northDSeen : northDoor);
            if ((wallInfo & WallInfo.DoorWest) != 0)
                doors.Add(wallSeenW ? westDSeen : westDoor);
            foreach (Point[] paintDoor in doors)
            {
                PaintPoints(mapImage, startPoint, stride, doorColor, paintDoor);
            }
        }

        private static T Gridify<T>(int x, int y, T fill1, T fill2)
        {
            return (x + y) % 2 == 0 ? fill1 : fill2;
        }

        private void PaintPoints(byte[] mapImage, int startPoint, int stride, byte color, params Point[] pixels)
        {
            PaintPoints(mapImage, startPoint, stride, 0, 0, color, pixels);
        }

        private void PaintPoints(byte[] mapImage, int startPoint, int stride, int offsX, int offsY, byte color, params Point[] pixels)
        {
            foreach (Point pixel in pixels)
            {
                mapImage[startPoint + pixel.X + offsX + stride * (pixel.Y + offsY)] = color;
            }
        }

        [Flags]
        protected enum WallInfo
        {
            None         = 0x00, // 00000000
            DontDraw     = 0x01, // 00000001
            Unknown02    = 0x02, // 00000010
            DoorOpen     = 0x04, // 00000100
            DoorSecret   = 0x08, // 00001000
            DoorWest     = 0x10, // 00010000
            DoorNorth    = 0x20, // 00100000
            WallWest     = 0x40, // 01000000
            WallNorth    = 0x80, // 10000000
        }

        protected enum MonsterType
        {
            None = -1, // Local program constant; not used in the game (would be byte 255 anyway, not Int32 '-1').
            Goblin = 00, // Goblin
            Orc = 01, // Orc
            Fimir = 02, // Fimir
            ChaosWarrior = 03, // Chaos Warrior
            Skeleton = 04, // Skeleton
            Zombie = 05, // Zombie
            Mummy = 06, // Mummy
            Gargoyle = 07, // Gargoyle
            OrcWarlordUlag = 08, // Orc Warlord Ulag (appearance: gargoyle)
            OrcLordGrak = 09, // Orc Lord Grak (appearance: orc)
            FireMageBalur = 10, // Balur the Fire Mage (appearance: gargoyle)
            StoneChaosWarrior = 11, // Stone Chaos Warrior
            WitchLord = 12, // The Witch Lord
            SpiritRider = 20, // Spirit Rider (appearance: gargoyle)
            DeathMist = 21, // Unique invisible monster in Quest 17. Due to its special code, it seems to only work in that level.
            Bellthor = 22, // The Guardian Bellthor (appearance: gargoyle)
            Skulmar = 23, // Elite Army Leader Skulmar (appearance: gargoyle)
            Doomguard = 24, // Kessandria's Doom Guard (appearance: gargoyle)
            WitchKessandria = 25, // Witch Kessandria (appearance: gargoyle)
            WitchlordExp = 26, // Alternate version of the Witch Lord, used in the expansion.
        }

        protected static string[] monsterNames =
        {
            "GB", // 00 Goblin
            "OR", // 01 Orc
            "FM", // 02 Fimir
            "CW", // 03 ChaosWarrior
            "SK", // 04 Skeleton
            "ZB", // 05 Zombie
            "MU", // 06 Mummy
            "GA", // 07 Gargoyle
            "UL", // 08 OrcWarlordUlag
            "GR", // 09 OrcLordGrak
            "BA", // 10 FireMageBalur
            "SW", // 11 StoneChaosWarrior
            "WL", // 12 WitchLord
            null, // 13
            null, // 14
            null, // 15
            null, // 16
            null, // 17
            null, // 18
            null, // 19
            "SR", // 20 SpiritRider
            "DM", // 21 DeathMist
            "BE", // 22 Bellthor
            "SM", // 23 Skulmar
            "DG", // 24 Doomguard
            "KE", // 25 WitchKessandria
            "W2"  // 26 WitchlordExp
        };

        protected enum ObjectType
        {
            Rock01 = 48,
            Rock02 = 49,
            PitTrapHidden = 50,
            PitTrapOpened = 51,
            FallingRockTrap = 52,
            Item53 = 53,
            SpearTrap = 54,
            Teleport = 55,
            StairsBlock1 = 56,
            StairsBlock2 = 57,
            StairsBlock3 = 58,
            StairsBlock4 = 59,
            Object60 = 60,
            Object61 = 61,
            Object62 = 62,
            Object63 = 63,
            Object64 = 64,
            Object65 = 65,
            TableFiller = 66,
            Object67 = 67,
            TortureFiller = 68,
            TombFiller = 69,
            AlchemyFiller = 70,
            FireplaceFiller = 71,
            ClosetFiller = 72,
            LibraryFiller = 73,
            ArmourRackFiller = 74,
            Table2x3Origin = 75,
            Table3x2Origin = 76,
            Torture3x2Origin = 77,
            Torture2x3Origin = 78,
            Alchemy2x3Origin = 79,
            Alchemy3x2Origin = 80,
            Object81 = 81,
            Object82 = 82,
            Tomb3x2Origin = 83,
            Closet1x3Origin = 84,
            ArmourRack3x1Origin = 85,
            Library1x3Origin = 86,
            Fireplace3x1Origin = 87,
            Library3x1Origin = 88,
            Fireplace1x3Origin = 89,
            Closet3x1Origin = 90,
            ArmourRack1x3Origin = 91,
            ChestSouth = 92,
            ChestEast = 93,
            ThroneSouth = 94,
            ThroneEast = 95,
        }

        protected enum EventType
        {
            TrChest0gDis1 = 01, // 01 - Chest, Trapped, you disarm it
            TrChest0gDmg1 = 02, // 02 - Chest, Trapped, -1 body point
            Cupboard30gHeal = 03, // 03 - Cupboard , 30 coins + potion of healing
            TrChest100gDmg = 04, // 04 - Chest, Trapped, -1 body point, +100 coins
            WpRackSpear = 05, // 05 - Weapon rack, Spear
            Chest50gHeal = 06, // 06 - Chest, 50 coins + potion of healing
            Chest250gSlowMagnus = 07, // 07 - Chest, 250 coins of prince magnus, the weight slows you
            ThroneDoorOpen = 08, // 08 - Throne, Melar's key (removes object, spawns throne on new location, creates open hidden door. Todo: check if relative to room, or absolute.)
            TalismanOfLore = 09, // 09 - Talisman of lore
            WpRackBorinsArmour = 10, // 0A - Borin's armour
            Chest200gKarlen = 11, // 0B - Chest, Karlen's 200 coins
            Chest150WandOfRecall = 12, // 0C - Chest, 150 coins + wand of recall
            Chest100gVanishes = 13, // 0D - Chest, 100 coins (and the chest vanishes. Todo: check if fixed location gets cleared, or if it looks for a chest to wipe)
            WpRackShield = 14, // 0E - Shield
            TrChestBeware = 15, // 0F - Chest, Trapped, beware
            TrChestGargoyle = 16, // 10 - Chest, Trapped, gargoyle awakes
            SpiritBlade = 17, // 11 - Spirit blade
            Chest200g1 = 18, // 12 - Chest, 200 coins
            Chest200g2 = 19, // 13 - Chest, 200 coins
            Chest300g = 20, // 14 - Chest, 300 coins
            Chest100gHeal = 21, // 15 - Chest, 100 coins + potion of healing
            TrChest0gDmg2 = 22, // 16 - Chest, Trapped, -1 body point
            TrChest0gDis2 = 23, // 17 - Chest, Trapped, you disarm it
            Table100gAgrainsKeys = 24, // 18 - Table, Agrain's keys (100 coins)
            Chest200g3 = 25, // 19 - Chest, 200 coins
            Chest350g = 26, // 1A - Chest, 350 coins
            Chest250g = 27, // 1B - Chest, 250 coins
            Throne500g = 28, // 1C - Throne, 500 coins
            Chest0gHeal = 29, // 1D - Chest, Potion of healing
            MineEntrance5000g = 30, // 1E - Mine entrance + 5000 coins, cannot attack. When used in the original "castle of mystery" quest, the coins are fake and taken away from the player at the end of the quest.
        }

        public override byte[] SaveToBytesAsThis(SupportedFileType fileToSave, Option[] saveOptions)
        {
            throw new NotImplementedException();
        }
    }
}