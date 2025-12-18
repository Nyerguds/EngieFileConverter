using System;
using System.Collections.Generic;
using System.Linq;

namespace CnC64FileConverter.Domain.CCTypes
{
    public class Ra1TemplateTypeClass
    {

        [Flags]
        public enum Ra1Theater
        {
            TEM_THR_NONE = 0,
            TEM_THR_TEMPERAT = 1,
            TEM_THR_SNOW = 2,
            TEM_THR_INTERIOR = 4,
        }

        public enum TemplateType
        {
            TEMPLATE_CLEAR = 0,
            TEMPLATE_WATER = 1,
            TEMPLATE_WATER2 = 2,
            TEMPLATE_SHORE01 = 3,
            TEMPLATE_SHORE02 = 4,
            TEMPLATE_SHORE03 = 5,
            TEMPLATE_SHORE04 = 6,
            TEMPLATE_SHORE05 = 7,
            TEMPLATE_SHORE06 = 8,
            TEMPLATE_SHORE07 = 9,
            TEMPLATE_SHORE08 = 10,
            TEMPLATE_SHORE09 = 11,
            TEMPLATE_SHORE10 = 12,
            TEMPLATE_SHORE11 = 13,
            TEMPLATE_SHORE12 = 14,
            TEMPLATE_SHORE13 = 15,
            TEMPLATE_SHORE14 = 16,
            TEMPLATE_SHORE15 = 17,
            TEMPLATE_SHORE16 = 18,
            TEMPLATE_SHORE17 = 19,
            TEMPLATE_SHORE18 = 20,
            TEMPLATE_SHORE19 = 21,
            TEMPLATE_SHORE20 = 22,
            TEMPLATE_SHORE21 = 23,
            TEMPLATE_SHORE22 = 24,
            TEMPLATE_SHORE23 = 25,
            TEMPLATE_SHORE24 = 26,
            TEMPLATE_SHORE25 = 27,
            TEMPLATE_SHORE26 = 28,
            TEMPLATE_SHORE27 = 29,
            TEMPLATE_SHORE28 = 30,
            TEMPLATE_SHORE29 = 31,
            TEMPLATE_SHORE30 = 32,
            TEMPLATE_SHORE31 = 33,
            TEMPLATE_SHORE32 = 34,
            TEMPLATE_SHORE33 = 35,
            TEMPLATE_SHORE34 = 36,
            TEMPLATE_SHORE35 = 37,
            TEMPLATE_SHORE36 = 38,
            TEMPLATE_SHORE37 = 39,
            TEMPLATE_SHORE38 = 40,
            TEMPLATE_SHORE39 = 41,
            TEMPLATE_SHORE40 = 42,
            TEMPLATE_SHORE41 = 43,
            TEMPLATE_SHORE42 = 44,
            TEMPLATE_SHORE43 = 45,
            TEMPLATE_SHORE44 = 46,
            TEMPLATE_SHORE45 = 47,
            TEMPLATE_SHORE46 = 48,
            TEMPLATE_SHORE47 = 49,
            TEMPLATE_SHORE48 = 50,
            TEMPLATE_SHORE49 = 51,
            TEMPLATE_SHORE50 = 52,
            TEMPLATE_SHORE51 = 53,
            TEMPLATE_SHORE52 = 54,
            TEMPLATE_SHORE53 = 55,
            TEMPLATE_SHORE54 = 56,
            TEMPLATE_SHORE55 = 57,
            TEMPLATE_SHORE56 = 58,
            TEMPLATE_SHORECLIFF01 = 59,
            TEMPLATE_SHORECLIFF02 = 60,
            TEMPLATE_SHORECLIFF03 = 61,
            TEMPLATE_SHORECLIFF04 = 62,
            TEMPLATE_SHORECLIFF05 = 63,
            TEMPLATE_SHORECLIFF06 = 64,
            TEMPLATE_SHORECLIFF07 = 65,
            TEMPLATE_SHORECLIFF08 = 66,
            TEMPLATE_SHORECLIFF09 = 67,
            TEMPLATE_SHORECLIFF10 = 68,
            TEMPLATE_SHORECLIFF11 = 69,
            TEMPLATE_SHORECLIFF12 = 70,
            TEMPLATE_SHORECLIFF13 = 71,
            TEMPLATE_SHORECLIFF14 = 72,
            TEMPLATE_SHORECLIFF15 = 73,
            TEMPLATE_SHORECLIFF16 = 74,
            TEMPLATE_SHORECLIFF17 = 75,
            TEMPLATE_SHORECLIFF18 = 76,
            TEMPLATE_SHORECLIFF19 = 77,
            TEMPLATE_SHORECLIFF20 = 78,
            TEMPLATE_SHORECLIFF21 = 79,
            TEMPLATE_SHORECLIFF22 = 80,
            TEMPLATE_SHORECLIFF23 = 81,
            TEMPLATE_SHORECLIFF24 = 82,
            TEMPLATE_SHORECLIFF25 = 83,
            TEMPLATE_SHORECLIFF26 = 84,
            TEMPLATE_SHORECLIFF27 = 85,
            TEMPLATE_SHORECLIFF28 = 86,
            TEMPLATE_SHORECLIFF29 = 87,
            TEMPLATE_SHORECLIFF30 = 88,
            TEMPLATE_SHORECLIFF31 = 89,
            TEMPLATE_SHORECLIFF32 = 90,
            TEMPLATE_SHORECLIFF33 = 91,
            TEMPLATE_SHORECLIFF34 = 92,
            TEMPLATE_SHORECLIFF35 = 93,
            TEMPLATE_SHORECLIFF36 = 94,
            TEMPLATE_SHORECLIFF37 = 95,
            TEMPLATE_SHORECLIFF38 = 96,
            TEMPLATE_BOULDER1 = 97,
            TEMPLATE_BOULDER2 = 98,
            TEMPLATE_BOULDER3 = 99,
            TEMPLATE_BOULDER4 = 100,
            TEMPLATE_BOULDER5 = 101,
            TEMPLATE_BOULDER6 = 102,
            TEMPLATE_PATCH01 = 103,
            TEMPLATE_PATCH02 = 104,
            TEMPLATE_PATCH03 = 105,
            TEMPLATE_PATCH04 = 106,
            TEMPLATE_PATCH07 = 107,
            TEMPLATE_PATCH08 = 108,
            TEMPLATE_PATCH13 = 109,
            TEMPLATE_PATCH14 = 110,
            TEMPLATE_PATCH15 = 111,
            TEMPLATE_RIVER01 = 112,
            TEMPLATE_RIVER02 = 113,
            TEMPLATE_RIVER03 = 114,
            TEMPLATE_RIVER04 = 115,
            TEMPLATE_RIVER05 = 116,
            TEMPLATE_RIVER06 = 117,
            TEMPLATE_RIVER07 = 118,
            TEMPLATE_RIVER08 = 119,
            TEMPLATE_RIVER09 = 120,
            TEMPLATE_RIVER10 = 121,
            TEMPLATE_RIVER11 = 122,
            TEMPLATE_RIVER12 = 123,
            TEMPLATE_RIVER13 = 124,
            TEMPLATE_FALLS1 = 125,
            TEMPLATE_FALLS1A = 126,
            TEMPLATE_FALLS2 = 127,
            TEMPLATE_FALLS2A = 128,
            TEMPLATE_FORD1 = 129,
            TEMPLATE_FORD2 = 130,
            TEMPLATE_BRIDGE1 = 131,
            TEMPLATE_BRIDGE1D = 132,
            TEMPLATE_BRIDGE2 = 133,
            TEMPLATE_BRIDGE2D = 134,
            TEMPLATE_SLOPE01 = 135,
            TEMPLATE_SLOPE02 = 136,
            TEMPLATE_SLOPE03 = 137,
            TEMPLATE_SLOPE04 = 138,
            TEMPLATE_SLOPE05 = 139,
            TEMPLATE_SLOPE06 = 140,
            TEMPLATE_SLOPE07 = 141,
            TEMPLATE_SLOPE08 = 142,
            TEMPLATE_SLOPE09 = 143,
            TEMPLATE_SLOPE10 = 144,
            TEMPLATE_SLOPE11 = 145,
            TEMPLATE_SLOPE12 = 146,
            TEMPLATE_SLOPE13 = 147,
            TEMPLATE_SLOPE14 = 148,
            TEMPLATE_SLOPE15 = 149,
            TEMPLATE_SLOPE16 = 150,
            TEMPLATE_SLOPE17 = 151,
            TEMPLATE_SLOPE18 = 152,
            TEMPLATE_SLOPE19 = 153,
            TEMPLATE_SLOPE20 = 154,
            TEMPLATE_SLOPE21 = 155,
            TEMPLATE_SLOPE22 = 156,
            TEMPLATE_SLOPE23 = 157,
            TEMPLATE_SLOPE24 = 158,
            TEMPLATE_SLOPE25 = 159,
            TEMPLATE_SLOPE26 = 160,
            TEMPLATE_SLOPE27 = 161,
            TEMPLATE_SLOPE28 = 162,
            TEMPLATE_SLOPE29 = 163,
            TEMPLATE_SLOPE30 = 164,
            TEMPLATE_SLOPE31 = 165,
            TEMPLATE_SLOPE32 = 166,
            TEMPLATE_SLOPE33 = 167,
            TEMPLATE_SLOPE34 = 168,
            TEMPLATE_SLOPE35 = 169,
            TEMPLATE_SLOPE36 = 170,
            TEMPLATE_SLOPE37 = 171,
            TEMPLATE_SLOPE38 = 172,
            TEMPLATE_ROAD01 = 173,
            TEMPLATE_ROAD02 = 174,
            TEMPLATE_ROAD03 = 175,
            TEMPLATE_ROAD04 = 176,
            TEMPLATE_ROAD05 = 177,
            TEMPLATE_ROAD06 = 178,
            TEMPLATE_ROAD07 = 179,
            TEMPLATE_ROAD08 = 180,
            TEMPLATE_ROAD09 = 181,
            TEMPLATE_ROAD10 = 182,
            TEMPLATE_ROAD11 = 183,
            TEMPLATE_ROAD12 = 184,
            TEMPLATE_ROAD13 = 185,
            TEMPLATE_ROAD14 = 186,
            TEMPLATE_ROAD15 = 187,
            TEMPLATE_ROAD16 = 188,
            TEMPLATE_ROAD17 = 189,
            TEMPLATE_ROAD18 = 190,
            TEMPLATE_ROAD19 = 191,
            TEMPLATE_ROAD20 = 192,
            TEMPLATE_ROAD21 = 193,
            TEMPLATE_ROAD22 = 194,
            TEMPLATE_ROAD23 = 195,
            TEMPLATE_ROAD24 = 196,
            TEMPLATE_ROAD25 = 197,
            TEMPLATE_ROAD26 = 198,
            TEMPLATE_ROAD27 = 199,
            TEMPLATE_ROAD28 = 200,
            TEMPLATE_ROAD29 = 201,
            TEMPLATE_ROAD30 = 202,
            TEMPLATE_ROAD31 = 203,
            TEMPLATE_ROAD32 = 204,
            TEMPLATE_ROAD33 = 205,
            TEMPLATE_ROAD34 = 206,
            TEMPLATE_ROAD35 = 207,
            TEMPLATE_ROAD36 = 208,
            TEMPLATE_ROAD37 = 209,
            TEMPLATE_ROAD38 = 210,
            TEMPLATE_ROAD39 = 211,
            TEMPLATE_ROAD40 = 212,
            TEMPLATE_ROAD41 = 213,
            TEMPLATE_ROAD42 = 214,
            TEMPLATE_ROAD43 = 215,
            TEMPLATE_ROUGH01 = 216,
            TEMPLATE_ROUGH02 = 217,
            TEMPLATE_ROUGH03 = 218,
            TEMPLATE_ROUGH04 = 219,
            TEMPLATE_ROUGH05 = 220,
            TEMPLATE_ROUGH06 = 221,
            TEMPLATE_ROUGH07 = 222,
            TEMPLATE_ROUGH08 = 223,
            TEMPLATE_ROUGH09 = 224,
            TEMPLATE_ROUGH10 = 225,
            TEMPLATE_ROUGH11 = 226,
            TEMPLATE_ROAD44 = 227,
            TEMPLATE_ROAD45 = 228,
            TEMPLATE_RIVER14 = 229,
            TEMPLATE_RIVER15 = 230,
            TEMPLATE_RIVERCLIFF01 = 231,
            TEMPLATE_RIVERCLIFF02 = 232,
            TEMPLATE_RIVERCLIFF03 = 233,
            TEMPLATE_RIVERCLIFF04 = 234,
            TEMPLATE_BRIDGE1A = 235,
            TEMPLATE_BRIDGE1B = 236,
            TEMPLATE_BRIDGE1C = 237,
            TEMPLATE_BRIDGE2A = 238,
            TEMPLATE_BRIDGE2B = 239,
            TEMPLATE_BRIDGE2C = 240,
            TEMPLATE_BRIDGE3A = 241,
            TEMPLATE_BRIDGE3B = 242,
            TEMPLATE_BRIDGE3C = 243,
            TEMPLATE_BRIDGE3D = 244,
            TEMPLATE_BRIDGE3E = 245,
            TEMPLATE_BRIDGE3F = 246,
            TEMPLATE_F01 = 247,
            TEMPLATE_F02 = 248,
            TEMPLATE_F03 = 249,
            TEMPLATE_F04 = 250,
            TEMPLATE_F05 = 251,
            TEMPLATE_F06 = 252,
            TEMPLATE_ARRO0001 = 253,
            TEMPLATE_ARRO0002 = 254,
            TEMPLATE_ARRO0003 = 255,
            TEMPLATE_ARRO0004 = 256,
            TEMPLATE_ARRO0005 = 257,
            TEMPLATE_ARRO0006 = 258,
            TEMPLATE_ARRO0007 = 259,
            TEMPLATE_ARRO0008 = 260,
            TEMPLATE_ARRO0009 = 261,
            TEMPLATE_ARRO0010 = 262,
            TEMPLATE_ARRO0011 = 263,
            TEMPLATE_ARRO0012 = 264,
            TEMPLATE_ARRO0013 = 265,
            TEMPLATE_ARRO0014 = 266,
            TEMPLATE_ARRO0015 = 267,
            TEMPLATE_FLOR0001 = 268,
            TEMPLATE_FLOR0002 = 269,
            TEMPLATE_FLOR0003 = 270,
            TEMPLATE_FLOR0004 = 271,
            TEMPLATE_FLOR0005 = 272,
            TEMPLATE_FLOR0006 = 273,
            TEMPLATE_FLOR0007 = 274,
            TEMPLATE_GFLR0001 = 275,
            TEMPLATE_GFLR0002 = 276,
            TEMPLATE_GFLR0003 = 277,
            TEMPLATE_GFLR0004 = 278,
            TEMPLATE_GFLR0005 = 279,
            TEMPLATE_GSTR0001 = 280,
            TEMPLATE_GSTR0002 = 281,
            TEMPLATE_GSTR0003 = 282,
            TEMPLATE_GSTR0004 = 283,
            TEMPLATE_GSTR0005 = 284,
            TEMPLATE_GSTR0006 = 285,
            TEMPLATE_GSTR0007 = 286,
            TEMPLATE_GSTR0008 = 287,
            TEMPLATE_GSTR0009 = 288,
            TEMPLATE_GSTR0010 = 289,
            TEMPLATE_GSTR0011 = 290,
            TEMPLATE_LWAL0001 = 291,
            TEMPLATE_LWAL0002 = 292,
            TEMPLATE_LWAL0003 = 293,
            TEMPLATE_LWAL0004 = 294,
            TEMPLATE_LWAL0005 = 295,
            TEMPLATE_LWAL0006 = 296,
            TEMPLATE_LWAL0007 = 297,
            TEMPLATE_LWAL0008 = 298,
            TEMPLATE_LWAL0009 = 299,
            TEMPLATE_LWAL0010 = 300,
            TEMPLATE_LWAL0011 = 301,
            TEMPLATE_LWAL0012 = 302,
            TEMPLATE_LWAL0013 = 303,
            TEMPLATE_LWAL0014 = 304,
            TEMPLATE_LWAL0015 = 305,
            TEMPLATE_LWAL0016 = 306,
            TEMPLATE_LWAL0017 = 307,
            TEMPLATE_LWAL0018 = 308,
            TEMPLATE_LWAL0019 = 309,
            TEMPLATE_LWAL0020 = 310,
            TEMPLATE_LWAL0021 = 311,
            TEMPLATE_LWAL0022 = 312,
            TEMPLATE_LWAL0023 = 313,
            TEMPLATE_LWAL0024 = 314,
            TEMPLATE_LWAL0025 = 315,
            TEMPLATE_LWAL0026 = 316,
            TEMPLATE_LWAL0027 = 317,
            TEMPLATE_STRP0001 = 318,
            TEMPLATE_STRP0002 = 319,
            TEMPLATE_STRP0003 = 320,
            TEMPLATE_STRP0004 = 321,
            TEMPLATE_STRP0005 = 322,
            TEMPLATE_STRP0006 = 323,
            TEMPLATE_STRP0007 = 324,
            TEMPLATE_STRP0008 = 325,
            TEMPLATE_STRP0009 = 326,
            TEMPLATE_STRP0010 = 327,
            TEMPLATE_STRP0011 = 328,
            TEMPLATE_WALL0001 = 329,
            TEMPLATE_WALL0002 = 330,
            TEMPLATE_WALL0003 = 331,
            TEMPLATE_WALL0004 = 332,
            TEMPLATE_WALL0005 = 333,
            TEMPLATE_WALL0006 = 334,
            TEMPLATE_WALL0007 = 335,
            TEMPLATE_WALL0008 = 336,
            TEMPLATE_WALL0009 = 337,
            TEMPLATE_WALL0010 = 338,
            TEMPLATE_WALL0011 = 339,
            TEMPLATE_WALL0012 = 340,
            TEMPLATE_WALL0013 = 341,
            TEMPLATE_WALL0014 = 342,
            TEMPLATE_WALL0015 = 343,
            TEMPLATE_WALL0016 = 344,
            TEMPLATE_WALL0017 = 345,
            TEMPLATE_WALL0018 = 346,
            TEMPLATE_WALL0019 = 347,
            TEMPLATE_WALL0020 = 348,
            TEMPLATE_WALL0021 = 349,
            TEMPLATE_WALL0022 = 350,
            TEMPLATE_WALL0023 = 351,
            TEMPLATE_WALL0024 = 352,
            TEMPLATE_WALL0025 = 353,
            TEMPLATE_WALL0026 = 354,
            TEMPLATE_WALL0027 = 355,
            TEMPLATE_WALL0028 = 356,
            TEMPLATE_WALL0029 = 357,
            TEMPLATE_WALL0030 = 358,
            TEMPLATE_WALL0031 = 359,
            TEMPLATE_WALL0032 = 360,
            TEMPLATE_WALL0033 = 361,
            TEMPLATE_WALL0034 = 362,
            TEMPLATE_WALL0035 = 363,
            TEMPLATE_WALL0036 = 364,
            TEMPLATE_WALL0037 = 365,
            TEMPLATE_WALL0038 = 366,
            TEMPLATE_WALL0039 = 367,
            TEMPLATE_WALL0040 = 368,
            TEMPLATE_WALL0041 = 369,
            TEMPLATE_WALL0042 = 370,
            TEMPLATE_WALL0043 = 371,
            TEMPLATE_WALL0044 = 372,
            TEMPLATE_WALL0045 = 373,
            TEMPLATE_WALL0046 = 374,
            TEMPLATE_WALL0047 = 375,
            TEMPLATE_WALL0048 = 376,
            TEMPLATE_WALL0049 = 377,
            TEMPLATE_BRIDGE1H = 378,
            TEMPLATE_BRIDGE2H = 379,
            TEMPLATE_BRIDGE1AX = 380,
            TEMPLATE_BRIDGE2AX = 381,
            TEMPLATE_BRIDGE1X = 382,
            TEMPLATE_BRIDGE2X = 383,
            TEMPLATE_XTRA0001 = 384,
            TEMPLATE_XTRA0002 = 385,
            TEMPLATE_XTRA0003 = 386,
            TEMPLATE_XTRA0004 = 387,
            TEMPLATE_XTRA0005 = 388,
            TEMPLATE_XTRA0006 = 389,
            TEMPLATE_XTRA0007 = 390,
            TEMPLATE_XTRA0008 = 391,
            TEMPLATE_XTRA0009 = 392,
            TEMPLATE_XTRA0010 = 393,
            TEMPLATE_XTRA0011 = 394,
            TEMPLATE_XTRA0012 = 395,
            TEMPLATE_XTRA0013 = 396,
            TEMPLATE_XTRA0014 = 397,
            TEMPLATE_XTRA0015 = 398,
            TEMPLATE_XTRA0016 = 399,
            TEMPLATE_ANTHILL = 400,
        }

        protected static Ra1TemplateTypeClass TemplateEmpty = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_CLEAR, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW | Ra1Theater.TEM_THR_INTERIOR), "CLEAR1", 29);
        protected static Ra1TemplateTypeClass TemplateClear = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_CLEAR, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW | Ra1Theater.TEM_THR_INTERIOR), "CLEAR1", 29);
        protected static Ra1TemplateTypeClass TemplateRoad01 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD01, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D01", 31);
        protected static Ra1TemplateTypeClass TemplateRoad02 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD02, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D02", 31);
        protected static Ra1TemplateTypeClass TemplateRoad03 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD03, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D03", 31);
        protected static Ra1TemplateTypeClass TemplateRoad04 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD04, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D04", 31);
        protected static Ra1TemplateTypeClass TemplateRoad05 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD05, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D05", 31);
        protected static Ra1TemplateTypeClass TemplateRoad06 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD06, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D06", 31);
        protected static Ra1TemplateTypeClass TemplateRoad07 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD07, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D07", 31);
        protected static Ra1TemplateTypeClass TemplateRoad08 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD08, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D08", 31);
        protected static Ra1TemplateTypeClass TemplateRoad09 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD09, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D09", 31);
        protected static Ra1TemplateTypeClass TemplateRoad10 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD10, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D10", 31);
        protected static Ra1TemplateTypeClass TemplateRoad11 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD11, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D11", 31);
        protected static Ra1TemplateTypeClass TemplateRoad12 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD12, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D12", 31);
        protected static Ra1TemplateTypeClass TemplateRoad13 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD13, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D13", 31);
        protected static Ra1TemplateTypeClass TemplateRoad14 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD14, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D14", 31);
        protected static Ra1TemplateTypeClass TemplateRoad15 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD15, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D15", 31);
        protected static Ra1TemplateTypeClass TemplateRoad16 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD16, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D16", 31);
        protected static Ra1TemplateTypeClass TemplateRoad17 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD17, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D17", 31);
        protected static Ra1TemplateTypeClass TemplateRoad18 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD18, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D18", 31);
        protected static Ra1TemplateTypeClass TemplateRoad19 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD19, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D19", 31);
        protected static Ra1TemplateTypeClass TemplateRoad20 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD20, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D20", 31);
        protected static Ra1TemplateTypeClass TemplateRoad21 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD21, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D21", 31);
        protected static Ra1TemplateTypeClass TemplateRoad22 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD22, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D22", 31);
        protected static Ra1TemplateTypeClass TemplateRoad23 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD23, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D23", 31);
        protected static Ra1TemplateTypeClass TemplateRoad24 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD24, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D24", 31);
        protected static Ra1TemplateTypeClass TemplateRoad25 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD25, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D25", 31);
        protected static Ra1TemplateTypeClass TemplateRoad26 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD26, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D26", 31);
        protected static Ra1TemplateTypeClass TemplateRoad27 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD27, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D27", 31);
        protected static Ra1TemplateTypeClass TemplateRoad28 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD28, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D28", 31);
        protected static Ra1TemplateTypeClass TemplateRoad29 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD29, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D29", 31);
        protected static Ra1TemplateTypeClass TemplateRoad30 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD30, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D30", 31);
        protected static Ra1TemplateTypeClass TemplateRoad31 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD31, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D31", 31);
        protected static Ra1TemplateTypeClass TemplateRoad32 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD32, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D32", 31);
        protected static Ra1TemplateTypeClass TemplateRoad33 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD33, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D33", 31);
        protected static Ra1TemplateTypeClass TemplateRoad34 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD34, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D34", 31);
        protected static Ra1TemplateTypeClass TemplateRoad35 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD35, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D35", 31);
        protected static Ra1TemplateTypeClass TemplateRoad36 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD36, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D36", 31);
        protected static Ra1TemplateTypeClass TemplateRoad37 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD37, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D37", 31);
        protected static Ra1TemplateTypeClass TemplateRoad38 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD38, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D38", 31);
        protected static Ra1TemplateTypeClass TemplateRoad39 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD39, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D39", 31);
        protected static Ra1TemplateTypeClass TemplateRoad40 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD40, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D40", 31);
        protected static Ra1TemplateTypeClass TemplateRoad41 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD41, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D41", 31);
        protected static Ra1TemplateTypeClass TemplateRoad42 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD42, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D42", 31);
        protected static Ra1TemplateTypeClass TemplateRoad43 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD43, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D43", 31);
        protected static Ra1TemplateTypeClass TemplateRoad44 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD44, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D44", 31);
        protected static Ra1TemplateTypeClass TemplateRoad45 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROAD45, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "D45", 31);
        protected static Ra1TemplateTypeClass TemplateWater = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WATER, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "W1", 30);
        protected static Ra1TemplateTypeClass TemplateWater2 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WATER2, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "W2", 30);
        protected static Ra1TemplateTypeClass TemplateShore01 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE01, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH01", 377);
        protected static Ra1TemplateTypeClass TemplateShore02 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE02, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH02", 377);
        protected static Ra1TemplateTypeClass TemplateShore03 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE03, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH03", 377);
        protected static Ra1TemplateTypeClass TemplateShore04 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE04, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH04", 377);
        protected static Ra1TemplateTypeClass TemplateShore05 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE05, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH05", 377);
        protected static Ra1TemplateTypeClass TemplateShore06 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE06, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH06", 377);
        protected static Ra1TemplateTypeClass TemplateShore07 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE07, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH07", 377);
        protected static Ra1TemplateTypeClass TemplateShore08 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE08, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH08", 377);
        protected static Ra1TemplateTypeClass TemplateShore09 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE09, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH09", 377);
        protected static Ra1TemplateTypeClass TemplateShore10 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE10, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH10", 377);
        protected static Ra1TemplateTypeClass TemplateShore11 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE11, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH11", 377);
        protected static Ra1TemplateTypeClass TemplateShore12 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE12, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH12", 377);
        protected static Ra1TemplateTypeClass TemplateShore13 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE13, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH13", 377);
        protected static Ra1TemplateTypeClass TemplateShore14 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE14, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH14", 377);
        protected static Ra1TemplateTypeClass TemplateShore15 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE15, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH15", 377);
        protected static Ra1TemplateTypeClass TemplateShore16 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE16, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH16", 377);
        protected static Ra1TemplateTypeClass TemplateShore17 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE17, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH17", 377);
        protected static Ra1TemplateTypeClass TemplateShore18 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE18, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH18", 377);
        protected static Ra1TemplateTypeClass TemplateShore19 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE19, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH19", 377);
        protected static Ra1TemplateTypeClass TemplateShore20 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE20, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH20", 377);
        protected static Ra1TemplateTypeClass TemplateShore21 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE21, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH21", 377);
        protected static Ra1TemplateTypeClass TemplateShore22 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE22, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH22", 377);
        protected static Ra1TemplateTypeClass TemplateShore23 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE23, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH23", 377);
        protected static Ra1TemplateTypeClass TemplateShore24 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE24, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH24", 377);
        protected static Ra1TemplateTypeClass TemplateShore25 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE25, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH25", 377);
        protected static Ra1TemplateTypeClass TemplateShore26 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE26, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH26", 377);
        protected static Ra1TemplateTypeClass TemplateShore27 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE27, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH27", 377);
        protected static Ra1TemplateTypeClass TemplateShore28 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE28, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH28", 377);
        protected static Ra1TemplateTypeClass TemplateShore29 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE29, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH29", 377);
        protected static Ra1TemplateTypeClass TemplateShore30 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE30, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH30", 377);
        protected static Ra1TemplateTypeClass TemplateShore31 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE31, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH31", 377);
        protected static Ra1TemplateTypeClass TemplateShore32 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE32, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH32", 377);
        protected static Ra1TemplateTypeClass TemplateShore33 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE33, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH33", 377);
        protected static Ra1TemplateTypeClass TemplateShore34 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE34, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH34", 377);
        protected static Ra1TemplateTypeClass TemplateShore35 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE35, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH35", 377);
        protected static Ra1TemplateTypeClass TemplateShore36 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE36, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH36", 377);
        protected static Ra1TemplateTypeClass TemplateShore37 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE37, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH37", 377);
        protected static Ra1TemplateTypeClass TemplateShore38 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE38, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH38", 377);
        protected static Ra1TemplateTypeClass TemplateShore39 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE39, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH39", 377);
        protected static Ra1TemplateTypeClass TemplateShore40 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE40, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH40", 377);
        protected static Ra1TemplateTypeClass TemplateShore41 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE41, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH41", 377);
        protected static Ra1TemplateTypeClass TemplateShore42 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE42, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH42", 377);
        protected static Ra1TemplateTypeClass TemplateShore43 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE43, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH43", 377);
        protected static Ra1TemplateTypeClass TemplateShore44 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE44, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH44", 377);
        protected static Ra1TemplateTypeClass TemplateShore45 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE45, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH45", 377);
        protected static Ra1TemplateTypeClass TemplateShore46 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE46, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH46", 377);
        protected static Ra1TemplateTypeClass TemplateShore47 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE47, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH47", 377);
        protected static Ra1TemplateTypeClass TemplateShore48 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE48, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH48", 377);
        protected static Ra1TemplateTypeClass TemplateShore49 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE49, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH49", 377);
        protected static Ra1TemplateTypeClass TemplateShore50 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE50, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH50", 377);
        protected static Ra1TemplateTypeClass TemplateShore51 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE51, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH51", 377);
        protected static Ra1TemplateTypeClass TemplateShore52 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE52, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH52", 377);
        protected static Ra1TemplateTypeClass TemplateShore53 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE53, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH53", 377);
        protected static Ra1TemplateTypeClass TemplateShore54 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE54, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH54", 377);
        protected static Ra1TemplateTypeClass TemplateShore55 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE55, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH55", 377);
        protected static Ra1TemplateTypeClass TemplateShore56 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORE56, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "SH56", 377);
        protected static Ra1TemplateTypeClass TemplateBoulder1 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_BOULDER1, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "B1", 32);
        protected static Ra1TemplateTypeClass TemplateBoulder2 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_BOULDER2, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "B2", 32);
        protected static Ra1TemplateTypeClass TemplateBoulder3 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_BOULDER3, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "B3", 32);
        protected static Ra1TemplateTypeClass TemplateBoulder4 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_BOULDER4, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "B4", 32);
        protected static Ra1TemplateTypeClass TemplateBoulder5 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_BOULDER5, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "B5", 32);
        protected static Ra1TemplateTypeClass TemplateBoulder6 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_BOULDER6, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "B6", 32);
        protected static Ra1TemplateTypeClass TemplateSlope01 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE01, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S01", 32);
        protected static Ra1TemplateTypeClass TemplateSlope02 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE02, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S02", 32);
        protected static Ra1TemplateTypeClass TemplateSlope03 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE03, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S03", 32);
        protected static Ra1TemplateTypeClass TemplateSlope04 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE04, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S04", 32);
        protected static Ra1TemplateTypeClass TemplateSlope05 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE05, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S05", 32);
        protected static Ra1TemplateTypeClass TemplateSlope06 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE06, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S06", 32);
        protected static Ra1TemplateTypeClass TemplateSlope07 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE07, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S07", 32);
        protected static Ra1TemplateTypeClass TemplateSlope08 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE08, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S08", 32);
        protected static Ra1TemplateTypeClass TemplateSlope09 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE09, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S09", 32);
        protected static Ra1TemplateTypeClass TemplateSlope10 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE10, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S10", 32);
        protected static Ra1TemplateTypeClass TemplateSlope11 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE11, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S11", 32);
        protected static Ra1TemplateTypeClass TemplateSlope12 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE12, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S12", 32);
        protected static Ra1TemplateTypeClass TemplateSlope13 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE13, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S13", 32);
        protected static Ra1TemplateTypeClass TemplateSlope14 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE14, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S14", 32);
        protected static Ra1TemplateTypeClass TemplateSlope15 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE15, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S15", 32);
        protected static Ra1TemplateTypeClass TemplateSlope16 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE16, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S16", 32);
        protected static Ra1TemplateTypeClass TemplateSlope17 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE17, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S17", 32);
        protected static Ra1TemplateTypeClass TemplateSlope18 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE18, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S18", 32);
        protected static Ra1TemplateTypeClass TemplateSlope19 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE19, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S19", 32);
        protected static Ra1TemplateTypeClass TemplateSlope20 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE20, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S20", 32);
        protected static Ra1TemplateTypeClass TemplateSlope21 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE21, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S21", 32);
        protected static Ra1TemplateTypeClass TemplateSlope22 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE22, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S22", 32);
        protected static Ra1TemplateTypeClass TemplateSlope23 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE23, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S23", 32);
        protected static Ra1TemplateTypeClass TemplateSlope24 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE24, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S24", 32);
        protected static Ra1TemplateTypeClass TemplateSlope25 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE25, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S25", 32);
        protected static Ra1TemplateTypeClass TemplateSlope26 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE26, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S26", 32);
        protected static Ra1TemplateTypeClass TemplateSlope27 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE27, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S27", 32);
        protected static Ra1TemplateTypeClass TemplateSlope28 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE28, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S28", 32);
        protected static Ra1TemplateTypeClass TemplateSlope29 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE29, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S29", 32);
        protected static Ra1TemplateTypeClass TemplateSlope30 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE30, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S30", 32);
        protected static Ra1TemplateTypeClass TemplateSlope31 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE31, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S31", 32);
        protected static Ra1TemplateTypeClass TemplateSlope32 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE32, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S32", 32);
        protected static Ra1TemplateTypeClass TemplateSlope33 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE33, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S33", 32);
        protected static Ra1TemplateTypeClass TemplateSlope34 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE34, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S34", 32);
        protected static Ra1TemplateTypeClass TemplateSlope35 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE35, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S35", 32);
        protected static Ra1TemplateTypeClass TemplateSlope36 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE36, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S36", 32);
        protected static Ra1TemplateTypeClass TemplateSlope37 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE37, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S37", 32);
        protected static Ra1TemplateTypeClass TemplateSlope38 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SLOPE38, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "S38", 32);
        protected static Ra1TemplateTypeClass TemplatePatch01 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_PATCH01, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "P01", 33);
        protected static Ra1TemplateTypeClass TemplatePatch02 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_PATCH02, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "P02", 33);
        protected static Ra1TemplateTypeClass TemplatePatch03 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_PATCH03, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "P03", 33);
        protected static Ra1TemplateTypeClass TemplatePatch04 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_PATCH04, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "P04", 33);
        protected static Ra1TemplateTypeClass TemplatePatch07 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_PATCH07, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "P07", 33);
        protected static Ra1TemplateTypeClass TemplatePatch08 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_PATCH08, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "P08", 33);
        protected static Ra1TemplateTypeClass TemplatePatch13 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_PATCH13, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "P13", 33);
        protected static Ra1TemplateTypeClass TemplatePatch14 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_PATCH14, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "P14", 33);
        protected static Ra1TemplateTypeClass TemplatePatch15 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_PATCH15, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "P15", 33);
        protected static Ra1TemplateTypeClass TemplateRiver01 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_RIVER01, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "RV01", 34);
        protected static Ra1TemplateTypeClass TemplateRiver02 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_RIVER02, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "RV02", 34);
        protected static Ra1TemplateTypeClass TemplateRiver03 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_RIVER03, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "RV03", 34);
        protected static Ra1TemplateTypeClass TemplateRiver04 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_RIVER04, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "RV04", 34);
        protected static Ra1TemplateTypeClass TemplateRiver05 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_RIVER05, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "RV05", 34);
        protected static Ra1TemplateTypeClass TemplateRiver06 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_RIVER06, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "RV06", 34);
        protected static Ra1TemplateTypeClass TemplateRiver07 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_RIVER07, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "RV07", 34);
        protected static Ra1TemplateTypeClass TemplateRiver08 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_RIVER08, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "RV08", 34);
        protected static Ra1TemplateTypeClass TemplateRiver09 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_RIVER09, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "RV09", 34);
        protected static Ra1TemplateTypeClass TemplateRiver10 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_RIVER10, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "RV10", 34);
        protected static Ra1TemplateTypeClass TemplateRiver11 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_RIVER11, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "RV11", 34);
        protected static Ra1TemplateTypeClass TemplateRiver12 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_RIVER12, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "RV12", 34);
        protected static Ra1TemplateTypeClass TemplateRiver13 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_RIVER13, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "RV13", 34);
        protected static Ra1TemplateTypeClass TemplateRiver14 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_RIVER14, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "RV14", 34);
        protected static Ra1TemplateTypeClass TemplateRiver15 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_RIVER15, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "RV15", 34);
        protected static Ra1TemplateTypeClass TemplateFord1 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_FORD1, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "FORD1", 34);
        protected static Ra1TemplateTypeClass TemplateFord2 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_FORD2, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "FORD2", 34);
        protected static Ra1TemplateTypeClass TemplateFalls1 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_FALLS1, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "FALLS1", 34);
        protected static Ra1TemplateTypeClass TemplateFalls1a = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_FALLS1A, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "FALLS1A", 34);
        protected static Ra1TemplateTypeClass TemplateFalls2 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_FALLS2, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "FALLS2", 34);
        protected static Ra1TemplateTypeClass TemplateFalls2a = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_FALLS2A, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "FALLS2A", 34);
        protected static Ra1TemplateTypeClass TemplateBridge1x = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_BRIDGE1X, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "BRIDGE1X", 456);
        protected static Ra1TemplateTypeClass TemplateBridge1 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_BRIDGE1, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "BRIDGE1", 456);
        protected static Ra1TemplateTypeClass TemplateBridge1h = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_BRIDGE1H, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "BRIDGE1H", 456);
        protected static Ra1TemplateTypeClass TemplateBridge1d = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_BRIDGE1D, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "BRIDGE1D", 456);
        protected static Ra1TemplateTypeClass TemplateBridge2x = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_BRIDGE2X, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "BRIDGE2X", 456);
        protected static Ra1TemplateTypeClass TemplateBridge2 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_BRIDGE2, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "BRIDGE2", 456);
        protected static Ra1TemplateTypeClass TemplateBridge2h = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_BRIDGE2H, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "BRIDGE2H", 456);
        protected static Ra1TemplateTypeClass TemplateBridge2d = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_BRIDGE2D, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "BRIDGE2D", 456);
        protected static Ra1TemplateTypeClass TemplateBridge1ax = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_BRIDGE1AX, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "BR1X", 456);
        protected static Ra1TemplateTypeClass TemplateBridge1a = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_BRIDGE1A, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "BR1A", 456);
        protected static Ra1TemplateTypeClass TemplateBridge1b = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_BRIDGE1B, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "BR1B", 456);
        protected static Ra1TemplateTypeClass TemplateBridge1c = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_BRIDGE1C, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "BR1C", 456);
        protected static Ra1TemplateTypeClass TemplateBridge2ax = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_BRIDGE2AX, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "BR2X", 456);
        protected static Ra1TemplateTypeClass TemplateBridge2a = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_BRIDGE2A, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "BR2A", 456);
        protected static Ra1TemplateTypeClass TemplateBridge2b = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_BRIDGE2B, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "BR2B", 456);
        protected static Ra1TemplateTypeClass TemplateBridge2c = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_BRIDGE2C, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "BR2C", 456);
        protected static Ra1TemplateTypeClass TemplateBridge3a = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_BRIDGE3A, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "BR3A", 456);
        protected static Ra1TemplateTypeClass TemplateBridge3b = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_BRIDGE3B, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "BR3B", 456);
        protected static Ra1TemplateTypeClass TemplateBridge3c = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_BRIDGE3C, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "BR3C", 456);
        protected static Ra1TemplateTypeClass TemplateBridge3d = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_BRIDGE3D, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "BR3D", 456);
        protected static Ra1TemplateTypeClass TemplateBridge3e = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_BRIDGE3E, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "BR3E", 456);
        protected static Ra1TemplateTypeClass TemplateBridge3f = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_BRIDGE3F, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "BR3F", 456);
        protected static Ra1TemplateTypeClass TemplateShoreCliff01 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF01, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC01", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff02 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF02, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC02", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff03 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF03, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC03", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff04 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF04, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC04", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff05 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF05, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC05", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff06 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF06, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC06", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff07 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF07, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC07", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff08 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF08, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC08", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff09 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF09, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC09", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff10 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF10, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC10", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff11 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF11, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC11", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff12 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF12, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC12", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff13 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF13, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC13", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff14 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF14, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC14", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff15 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF15, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC15", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff16 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF16, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC16", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff17 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF17, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC17", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff18 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF18, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC18", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff19 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF19, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC19", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff20 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF20, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC20", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff21 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF21, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC21", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff22 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF22, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC22", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff23 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF23, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC23", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff24 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF24, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC24", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff25 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF25, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC25", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff26 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF26, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC26", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff27 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF27, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC27", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff28 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF28, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC28", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff29 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF29, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC29", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff30 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF30, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC30", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff31 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF31, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC31", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff32 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF32, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC32", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff33 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF33, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC33", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff34 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF34, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC34", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff35 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF35, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC35", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff36 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF36, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC36", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff37 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF37, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC37", 377);
        protected static Ra1TemplateTypeClass TemplateShoreCliff38 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_SHORECLIFF38, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "WC38", 377);
        protected static Ra1TemplateTypeClass TemplateRough01 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROUGH01, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "RF01", 20);
        protected static Ra1TemplateTypeClass TemplateRough02 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROUGH02, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "RF02", 20);
        protected static Ra1TemplateTypeClass TemplateRough03 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROUGH03, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "RF03", 20);
        protected static Ra1TemplateTypeClass TemplateRough04 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROUGH04, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "RF04", 20);
        protected static Ra1TemplateTypeClass TemplateRough05 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROUGH05, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "RF05", 20);
        protected static Ra1TemplateTypeClass TemplateRough06 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROUGH06, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "RF06", 20);
        protected static Ra1TemplateTypeClass TemplateRough07 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROUGH07, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "RF07", 20);
        protected static Ra1TemplateTypeClass TemplateRough08 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROUGH08, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "RF08", 20);
        protected static Ra1TemplateTypeClass TemplateRough09 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROUGH09, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "RF09", 20);
        protected static Ra1TemplateTypeClass TemplateRough10 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROUGH10, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "RF10", 20);
        protected static Ra1TemplateTypeClass TemplateRough11 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ROUGH11, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "RF11", 20);
        protected static Ra1TemplateTypeClass TemplateRiverCliff01 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_RIVERCLIFF01, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "RC01", 34);
        protected static Ra1TemplateTypeClass TemplateRiverCliff02 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_RIVERCLIFF02, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "RC02", 34);
        protected static Ra1TemplateTypeClass TemplateRiverCliff03 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_RIVERCLIFF03, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "RC03", 34);
        protected static Ra1TemplateTypeClass TemplateRiverCliff04 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_RIVERCLIFF04, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "RC04", 34);
        protected static Ra1TemplateTypeClass TemplateF01 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_F01, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "F01", 34);
        protected static Ra1TemplateTypeClass TemplateF02 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_F02, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "F02", 34);
        protected static Ra1TemplateTypeClass TemplateF03 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_F03, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "F03", 34);
        protected static Ra1TemplateTypeClass TemplateF04 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_F04, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "F04", 34);
        protected static Ra1TemplateTypeClass TemplateF05 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_F05, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "F05", 34);
        protected static Ra1TemplateTypeClass TemplateF06 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_F06, (Ra1Theater.TEM_THR_TEMPERAT | Ra1Theater.TEM_THR_SNOW), "F06", 34);
        protected static Ra1TemplateTypeClass TemplateARRO0001 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ARRO0001, Ra1Theater.TEM_THR_INTERIOR, "ARRO0001", 464);
        protected static Ra1TemplateTypeClass TemplateARRO0002 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ARRO0002, Ra1Theater.TEM_THR_INTERIOR, "ARRO0002", 464);
        protected static Ra1TemplateTypeClass TemplateARRO0003 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ARRO0003, Ra1Theater.TEM_THR_INTERIOR, "ARRO0003", 464);
        protected static Ra1TemplateTypeClass TemplateARRO0004 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ARRO0004, Ra1Theater.TEM_THR_INTERIOR, "ARRO0004", 464);
        protected static Ra1TemplateTypeClass TemplateARRO0005 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ARRO0005, Ra1Theater.TEM_THR_INTERIOR, "ARRO0005", 464);
        protected static Ra1TemplateTypeClass TemplateARRO0006 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ARRO0006, Ra1Theater.TEM_THR_INTERIOR, "ARRO0006", 464);
        protected static Ra1TemplateTypeClass TemplateARRO0007 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ARRO0007, Ra1Theater.TEM_THR_INTERIOR, "ARRO0007", 464);
        protected static Ra1TemplateTypeClass TemplateARRO0008 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ARRO0008, Ra1Theater.TEM_THR_INTERIOR, "ARRO0008", 464);
        protected static Ra1TemplateTypeClass TemplateARRO0009 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ARRO0009, Ra1Theater.TEM_THR_INTERIOR, "ARRO0009", 464);
        protected static Ra1TemplateTypeClass TemplateARRO0010 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ARRO0010, Ra1Theater.TEM_THR_INTERIOR, "ARRO0010", 464);
        protected static Ra1TemplateTypeClass TemplateARRO0011 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ARRO0011, Ra1Theater.TEM_THR_INTERIOR, "ARRO0011", 464);
        protected static Ra1TemplateTypeClass TemplateARRO0012 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ARRO0012, Ra1Theater.TEM_THR_INTERIOR, "ARRO0012", 464);
        protected static Ra1TemplateTypeClass TemplateARRO0013 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ARRO0013, Ra1Theater.TEM_THR_INTERIOR, "ARRO0013", 464);
        protected static Ra1TemplateTypeClass TemplateARRO0014 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ARRO0014, Ra1Theater.TEM_THR_INTERIOR, "ARRO0014", 464);
        protected static Ra1TemplateTypeClass TemplateARRO0015 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ARRO0015, Ra1Theater.TEM_THR_INTERIOR, "ARRO0015", 464);
        protected static Ra1TemplateTypeClass TemplateFLOR0001 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_FLOR0001, Ra1Theater.TEM_THR_INTERIOR, "FLOR0001", 464);
        protected static Ra1TemplateTypeClass TemplateFLOR0002 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_FLOR0002, Ra1Theater.TEM_THR_INTERIOR, "FLOR0002", 464);
        protected static Ra1TemplateTypeClass TemplateFLOR0003 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_FLOR0003, Ra1Theater.TEM_THR_INTERIOR, "FLOR0003", 464);
        protected static Ra1TemplateTypeClass TemplateFLOR0004 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_FLOR0004, Ra1Theater.TEM_THR_INTERIOR, "FLOR0004", 464);
        protected static Ra1TemplateTypeClass TemplateFLOR0005 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_FLOR0005, Ra1Theater.TEM_THR_INTERIOR, "FLOR0005", 464);
        protected static Ra1TemplateTypeClass TemplateFLOR0006 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_FLOR0006, Ra1Theater.TEM_THR_INTERIOR, "FLOR0006", 464);
        protected static Ra1TemplateTypeClass TemplateFLOR0007 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_FLOR0007, Ra1Theater.TEM_THR_INTERIOR, "FLOR0007", 464);
        protected static Ra1TemplateTypeClass TemplateGFLR0001 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_GFLR0001, Ra1Theater.TEM_THR_INTERIOR, "GFLR0001", 464);
        protected static Ra1TemplateTypeClass TemplateGFLR0002 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_GFLR0002, Ra1Theater.TEM_THR_INTERIOR, "GFLR0002", 464);
        protected static Ra1TemplateTypeClass TemplateGFLR0003 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_GFLR0003, Ra1Theater.TEM_THR_INTERIOR, "GFLR0003", 464);
        protected static Ra1TemplateTypeClass TemplateGFLR0004 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_GFLR0004, Ra1Theater.TEM_THR_INTERIOR, "GFLR0004", 464);
        protected static Ra1TemplateTypeClass TemplateGFLR0005 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_GFLR0005, Ra1Theater.TEM_THR_INTERIOR, "GFLR0005", 464);
        protected static Ra1TemplateTypeClass TemplateGSTR0001 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_GSTR0001, Ra1Theater.TEM_THR_INTERIOR, "GSTR0001", 464);
        protected static Ra1TemplateTypeClass TemplateGSTR0002 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_GSTR0002, Ra1Theater.TEM_THR_INTERIOR, "GSTR0002", 464);
        protected static Ra1TemplateTypeClass TemplateGSTR0003 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_GSTR0003, Ra1Theater.TEM_THR_INTERIOR, "GSTR0003", 464);
        protected static Ra1TemplateTypeClass TemplateGSTR0004 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_GSTR0004, Ra1Theater.TEM_THR_INTERIOR, "GSTR0004", 464);
        protected static Ra1TemplateTypeClass TemplateGSTR0005 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_GSTR0005, Ra1Theater.TEM_THR_INTERIOR, "GSTR0005", 464);
        protected static Ra1TemplateTypeClass TemplateGSTR0006 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_GSTR0006, Ra1Theater.TEM_THR_INTERIOR, "GSTR0006", 464);
        protected static Ra1TemplateTypeClass TemplateGSTR0007 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_GSTR0007, Ra1Theater.TEM_THR_INTERIOR, "GSTR0007", 464);
        protected static Ra1TemplateTypeClass TemplateGSTR0008 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_GSTR0008, Ra1Theater.TEM_THR_INTERIOR, "GSTR0008", 464);
        protected static Ra1TemplateTypeClass TemplateGSTR0009 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_GSTR0009, Ra1Theater.TEM_THR_INTERIOR, "GSTR0009", 464);
        protected static Ra1TemplateTypeClass TemplateGSTR0010 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_GSTR0010, Ra1Theater.TEM_THR_INTERIOR, "GSTR0010", 464);
        protected static Ra1TemplateTypeClass TemplateGSTR0011 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_GSTR0011, Ra1Theater.TEM_THR_INTERIOR, "GSTR0011", 464);
        protected static Ra1TemplateTypeClass TemplateLWAL0001 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_LWAL0001, Ra1Theater.TEM_THR_INTERIOR, "LWAL0001", 464);
        protected static Ra1TemplateTypeClass TemplateLWAL0002 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_LWAL0002, Ra1Theater.TEM_THR_INTERIOR, "LWAL0002", 464);
        protected static Ra1TemplateTypeClass TemplateLWAL0003 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_LWAL0003, Ra1Theater.TEM_THR_INTERIOR, "LWAL0003", 464);
        protected static Ra1TemplateTypeClass TemplateLWAL0004 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_LWAL0004, Ra1Theater.TEM_THR_INTERIOR, "LWAL0004", 464);
        protected static Ra1TemplateTypeClass TemplateLWAL0005 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_LWAL0005, Ra1Theater.TEM_THR_INTERIOR, "LWAL0005", 464);
        protected static Ra1TemplateTypeClass TemplateLWAL0006 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_LWAL0006, Ra1Theater.TEM_THR_INTERIOR, "LWAL0006", 464);
        protected static Ra1TemplateTypeClass TemplateLWAL0007 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_LWAL0007, Ra1Theater.TEM_THR_INTERIOR, "LWAL0007", 464);
        protected static Ra1TemplateTypeClass TemplateLWAL0008 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_LWAL0008, Ra1Theater.TEM_THR_INTERIOR, "LWAL0008", 464);
        protected static Ra1TemplateTypeClass TemplateLWAL0009 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_LWAL0009, Ra1Theater.TEM_THR_INTERIOR, "LWAL0009", 464);
        protected static Ra1TemplateTypeClass TemplateLWAL0010 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_LWAL0010, Ra1Theater.TEM_THR_INTERIOR, "LWAL0010", 464);
        protected static Ra1TemplateTypeClass TemplateLWAL0011 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_LWAL0011, Ra1Theater.TEM_THR_INTERIOR, "LWAL0011", 464);
        protected static Ra1TemplateTypeClass TemplateLWAL0012 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_LWAL0012, Ra1Theater.TEM_THR_INTERIOR, "LWAL0012", 464);
        protected static Ra1TemplateTypeClass TemplateLWAL0013 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_LWAL0013, Ra1Theater.TEM_THR_INTERIOR, "LWAL0013", 464);
        protected static Ra1TemplateTypeClass TemplateLWAL0014 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_LWAL0014, Ra1Theater.TEM_THR_INTERIOR, "LWAL0014", 464);
        protected static Ra1TemplateTypeClass TemplateLWAL0015 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_LWAL0015, Ra1Theater.TEM_THR_INTERIOR, "LWAL0015", 464);
        protected static Ra1TemplateTypeClass TemplateLWAL0016 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_LWAL0016, Ra1Theater.TEM_THR_INTERIOR, "LWAL0016", 464);
        protected static Ra1TemplateTypeClass TemplateLWAL0017 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_LWAL0017, Ra1Theater.TEM_THR_INTERIOR, "LWAL0017", 464);
        protected static Ra1TemplateTypeClass TemplateLWAL0018 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_LWAL0018, Ra1Theater.TEM_THR_INTERIOR, "LWAL0018", 464);
        protected static Ra1TemplateTypeClass TemplateLWAL0019 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_LWAL0019, Ra1Theater.TEM_THR_INTERIOR, "LWAL0019", 464);
        protected static Ra1TemplateTypeClass TemplateLWAL0020 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_LWAL0020, Ra1Theater.TEM_THR_INTERIOR, "LWAL0020", 464);
        protected static Ra1TemplateTypeClass TemplateLWAL0021 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_LWAL0021, Ra1Theater.TEM_THR_INTERIOR, "LWAL0021", 464);
        protected static Ra1TemplateTypeClass TemplateLWAL0022 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_LWAL0022, Ra1Theater.TEM_THR_INTERIOR, "LWAL0022", 464);
        protected static Ra1TemplateTypeClass TemplateLWAL0023 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_LWAL0023, Ra1Theater.TEM_THR_INTERIOR, "LWAL0023", 464);
        protected static Ra1TemplateTypeClass TemplateLWAL0024 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_LWAL0024, Ra1Theater.TEM_THR_INTERIOR, "LWAL0024", 464);
        protected static Ra1TemplateTypeClass TemplateLWAL0025 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_LWAL0025, Ra1Theater.TEM_THR_INTERIOR, "LWAL0025", 464);
        protected static Ra1TemplateTypeClass TemplateLWAL0026 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_LWAL0026, Ra1Theater.TEM_THR_INTERIOR, "LWAL0026", 464);
        protected static Ra1TemplateTypeClass TemplateLWAL0027 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_LWAL0027, Ra1Theater.TEM_THR_INTERIOR, "LWAL0027", 464);
        protected static Ra1TemplateTypeClass TemplateSTRP0001 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_STRP0001, Ra1Theater.TEM_THR_INTERIOR, "STRP0001", 464);
        protected static Ra1TemplateTypeClass TemplateSTRP0002 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_STRP0002, Ra1Theater.TEM_THR_INTERIOR, "STRP0002", 464);
        protected static Ra1TemplateTypeClass TemplateSTRP0003 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_STRP0003, Ra1Theater.TEM_THR_INTERIOR, "STRP0003", 464);
        protected static Ra1TemplateTypeClass TemplateSTRP0004 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_STRP0004, Ra1Theater.TEM_THR_INTERIOR, "STRP0004", 464);
        protected static Ra1TemplateTypeClass TemplateSTRP0005 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_STRP0005, Ra1Theater.TEM_THR_INTERIOR, "STRP0005", 464);
        protected static Ra1TemplateTypeClass TemplateSTRP0006 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_STRP0006, Ra1Theater.TEM_THR_INTERIOR, "STRP0006", 464);
        protected static Ra1TemplateTypeClass TemplateSTRP0007 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_STRP0007, Ra1Theater.TEM_THR_INTERIOR, "STRP0007", 464);
        protected static Ra1TemplateTypeClass TemplateSTRP0008 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_STRP0008, Ra1Theater.TEM_THR_INTERIOR, "STRP0008", 464);
        protected static Ra1TemplateTypeClass TemplateSTRP0009 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_STRP0009, Ra1Theater.TEM_THR_INTERIOR, "STRP0009", 464);
        protected static Ra1TemplateTypeClass TemplateSTRP0010 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_STRP0010, Ra1Theater.TEM_THR_INTERIOR, "STRP0010", 464);
        protected static Ra1TemplateTypeClass TemplateSTRP0011 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_STRP0011, Ra1Theater.TEM_THR_INTERIOR, "STRP0011", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0001 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0001, Ra1Theater.TEM_THR_INTERIOR, "WALL0001", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0002 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0002, Ra1Theater.TEM_THR_INTERIOR, "WALL0002", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0003 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0003, Ra1Theater.TEM_THR_INTERIOR, "WALL0003", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0004 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0004, Ra1Theater.TEM_THR_INTERIOR, "WALL0004", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0005 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0005, Ra1Theater.TEM_THR_INTERIOR, "WALL0005", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0006 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0006, Ra1Theater.TEM_THR_INTERIOR, "WALL0006", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0007 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0007, Ra1Theater.TEM_THR_INTERIOR, "WALL0007", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0008 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0008, Ra1Theater.TEM_THR_INTERIOR, "WALL0008", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0009 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0009, Ra1Theater.TEM_THR_INTERIOR, "WALL0009", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0010 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0010, Ra1Theater.TEM_THR_INTERIOR, "WALL0010", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0011 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0011, Ra1Theater.TEM_THR_INTERIOR, "WALL0011", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0012 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0012, Ra1Theater.TEM_THR_INTERIOR, "WALL0012", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0013 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0013, Ra1Theater.TEM_THR_INTERIOR, "WALL0013", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0014 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0014, Ra1Theater.TEM_THR_INTERIOR, "WALL0014", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0015 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0015, Ra1Theater.TEM_THR_INTERIOR, "WALL0015", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0016 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0016, Ra1Theater.TEM_THR_INTERIOR, "WALL0016", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0017 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0017, Ra1Theater.TEM_THR_INTERIOR, "WALL0017", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0018 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0018, Ra1Theater.TEM_THR_INTERIOR, "WALL0018", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0019 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0019, Ra1Theater.TEM_THR_INTERIOR, "WALL0019", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0020 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0020, Ra1Theater.TEM_THR_INTERIOR, "WALL0020", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0021 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0021, Ra1Theater.TEM_THR_INTERIOR, "WALL0021", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0022 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0022, Ra1Theater.TEM_THR_INTERIOR, "WALL0022", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0023 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0023, Ra1Theater.TEM_THR_INTERIOR, "WALL0023", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0024 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0024, Ra1Theater.TEM_THR_INTERIOR, "WALL0024", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0025 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0025, Ra1Theater.TEM_THR_INTERIOR, "WALL0025", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0026 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0026, Ra1Theater.TEM_THR_INTERIOR, "WALL0026", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0027 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0027, Ra1Theater.TEM_THR_INTERIOR, "WALL0027", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0028 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0028, Ra1Theater.TEM_THR_INTERIOR, "WALL0028", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0029 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0029, Ra1Theater.TEM_THR_INTERIOR, "WALL0029", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0030 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0030, Ra1Theater.TEM_THR_INTERIOR, "WALL0030", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0031 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0031, Ra1Theater.TEM_THR_INTERIOR, "WALL0031", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0032 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0032, Ra1Theater.TEM_THR_INTERIOR, "WALL0032", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0033 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0033, Ra1Theater.TEM_THR_INTERIOR, "WALL0033", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0034 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0034, Ra1Theater.TEM_THR_INTERIOR, "WALL0034", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0035 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0035, Ra1Theater.TEM_THR_INTERIOR, "WALL0035", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0036 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0036, Ra1Theater.TEM_THR_INTERIOR, "WALL0036", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0037 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0037, Ra1Theater.TEM_THR_INTERIOR, "WALL0037", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0038 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0038, Ra1Theater.TEM_THR_INTERIOR, "WALL0038", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0039 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0039, Ra1Theater.TEM_THR_INTERIOR, "WALL0039", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0040 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0040, Ra1Theater.TEM_THR_INTERIOR, "WALL0040", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0041 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0041, Ra1Theater.TEM_THR_INTERIOR, "WALL0041", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0042 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0042, Ra1Theater.TEM_THR_INTERIOR, "WALL0042", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0043 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0043, Ra1Theater.TEM_THR_INTERIOR, "WALL0043", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0044 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0044, Ra1Theater.TEM_THR_INTERIOR, "WALL0044", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0045 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0045, Ra1Theater.TEM_THR_INTERIOR, "WALL0045", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0046 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0046, Ra1Theater.TEM_THR_INTERIOR, "WALL0046", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0047 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0047, Ra1Theater.TEM_THR_INTERIOR, "WALL0047", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0048 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0048, Ra1Theater.TEM_THR_INTERIOR, "WALL0048", 464);
        protected static Ra1TemplateTypeClass TemplateWALL0049 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_WALL0049, Ra1Theater.TEM_THR_INTERIOR, "WALL0049", 464);
        protected static Ra1TemplateTypeClass TemplateXtra0001 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_XTRA0001, Ra1Theater.TEM_THR_INTERIOR, "XTRA0001", 464);
        protected static Ra1TemplateTypeClass TemplateXtra0002 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_XTRA0002, Ra1Theater.TEM_THR_INTERIOR, "XTRA0002", 464);
        protected static Ra1TemplateTypeClass TemplateXtra0003 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_XTRA0003, Ra1Theater.TEM_THR_INTERIOR, "XTRA0003", 464);
        protected static Ra1TemplateTypeClass TemplateXtra0004 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_XTRA0004, Ra1Theater.TEM_THR_INTERIOR, "XTRA0004", 464);
        protected static Ra1TemplateTypeClass TemplateXtra0005 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_XTRA0005, Ra1Theater.TEM_THR_INTERIOR, "XTRA0005", 464);
        protected static Ra1TemplateTypeClass TemplateXtra0006 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_XTRA0006, Ra1Theater.TEM_THR_INTERIOR, "XTRA0006", 464);
        protected static Ra1TemplateTypeClass TemplateXtra0007 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_XTRA0007, Ra1Theater.TEM_THR_INTERIOR, "XTRA0007", 464);
        protected static Ra1TemplateTypeClass TemplateXtra0008 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_XTRA0008, Ra1Theater.TEM_THR_INTERIOR, "XTRA0008", 464);
        protected static Ra1TemplateTypeClass TemplateXtra0009 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_XTRA0009, Ra1Theater.TEM_THR_INTERIOR, "XTRA0009", 464);
        protected static Ra1TemplateTypeClass TemplateXtra0010 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_XTRA0010, Ra1Theater.TEM_THR_INTERIOR, "XTRA0010", 464);
        protected static Ra1TemplateTypeClass TemplateXtra0011 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_XTRA0011, Ra1Theater.TEM_THR_INTERIOR, "XTRA0011", 464);
        protected static Ra1TemplateTypeClass TemplateXtra0012 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_XTRA0012, Ra1Theater.TEM_THR_INTERIOR, "XTRA0012", 464);
        protected static Ra1TemplateTypeClass TemplateXtra0013 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_XTRA0013, Ra1Theater.TEM_THR_INTERIOR, "XTRA0013", 464);
        protected static Ra1TemplateTypeClass TemplateXtra0014 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_XTRA0014, Ra1Theater.TEM_THR_INTERIOR, "XTRA0014", 464);
        protected static Ra1TemplateTypeClass TemplateXtra0015 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_XTRA0015, Ra1Theater.TEM_THR_INTERIOR, "XTRA0015", 464);
        protected static Ra1TemplateTypeClass TemplateXtra0016 = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_XTRA0016, Ra1Theater.TEM_THR_INTERIOR, "XTRA0016", 464);
        protected static Ra1TemplateTypeClass TemplateAntHill = new Ra1TemplateTypeClass(TemplateType.TEMPLATE_ANTHILL, Ra1Theater.TEM_THR_TEMPERAT, "HILL01", 20);

        protected static Ra1TemplateTypeClass[] templates = new Ra1TemplateTypeClass[]
        {
            TemplateEmpty,
            TemplateClear,
            TemplateRoad01,
            TemplateRoad02,
            TemplateRoad03,
            TemplateRoad04,
            TemplateRoad05,
            TemplateRoad06,
            TemplateRoad07,
            TemplateRoad08,
            TemplateRoad09,
            TemplateRoad10,
            TemplateRoad11,
            TemplateRoad12,
            TemplateRoad13,
            TemplateRoad14,
            TemplateRoad15,
            TemplateRoad16,
            TemplateRoad17,
            TemplateRoad18,
            TemplateRoad19,
            TemplateRoad20,
            TemplateRoad21,
            TemplateRoad22,
            TemplateRoad23,
            TemplateRoad24,
            TemplateRoad25,
            TemplateRoad26,
            TemplateRoad27,
            TemplateRoad28,
            TemplateRoad29,
            TemplateRoad30,
            TemplateRoad31,
            TemplateRoad32,
            TemplateRoad33,
            TemplateRoad34,
            TemplateRoad35,
            TemplateRoad36,
            TemplateRoad37,
            TemplateRoad38,
            TemplateRoad39,
            TemplateRoad40,
            TemplateRoad41,
            TemplateRoad42,
            TemplateRoad43,
            TemplateRoad44,
            TemplateRoad45,
            TemplateWater,
            TemplateWater2,
            TemplateShore01,
            TemplateShore02,
            TemplateShore03,
            TemplateShore04,
            TemplateShore05,
            TemplateShore06,
            TemplateShore07,
            TemplateShore08,
            TemplateShore09,
            TemplateShore10,
            TemplateShore11,
            TemplateShore12,
            TemplateShore13,
            TemplateShore14,
            TemplateShore15,
            TemplateShore16,
            TemplateShore17,
            TemplateShore18,
            TemplateShore19,
            TemplateShore20,
            TemplateShore21,
            TemplateShore22,
            TemplateShore23,
            TemplateShore24,
            TemplateShore25,
            TemplateShore26,
            TemplateShore27,
            TemplateShore28,
            TemplateShore29,
            TemplateShore30,
            TemplateShore31,
            TemplateShore32,
            TemplateShore33,
            TemplateShore34,
            TemplateShore35,
            TemplateShore36,
            TemplateShore37,
            TemplateShore38,
            TemplateShore39,
            TemplateShore40,
            TemplateShore41,
            TemplateShore42,
            TemplateShore43,
            TemplateShore44,
            TemplateShore45,
            TemplateShore46,
            TemplateShore47,
            TemplateShore48,
            TemplateShore49,
            TemplateShore50,
            TemplateShore51,
            TemplateShore52,
            TemplateShore53,
            TemplateShore54,
            TemplateShore55,
            TemplateShore56,
            TemplateBoulder1,
            TemplateBoulder2,
            TemplateBoulder3,
            TemplateBoulder4,
            TemplateBoulder5,
            TemplateBoulder6,
            TemplateSlope01,
            TemplateSlope02,
            TemplateSlope03,
            TemplateSlope04,
            TemplateSlope05,
            TemplateSlope06,
            TemplateSlope07,
            TemplateSlope08,
            TemplateSlope09,
            TemplateSlope10,
            TemplateSlope11,
            TemplateSlope12,
            TemplateSlope13,
            TemplateSlope14,
            TemplateSlope15,
            TemplateSlope16,
            TemplateSlope17,
            TemplateSlope18,
            TemplateSlope19,
            TemplateSlope20,
            TemplateSlope21,
            TemplateSlope22,
            TemplateSlope23,
            TemplateSlope24,
            TemplateSlope25,
            TemplateSlope26,
            TemplateSlope27,
            TemplateSlope28,
            TemplateSlope29,
            TemplateSlope30,
            TemplateSlope31,
            TemplateSlope32,
            TemplateSlope33,
            TemplateSlope34,
            TemplateSlope35,
            TemplateSlope36,
            TemplateSlope37,
            TemplateSlope38,
            TemplatePatch01,
            TemplatePatch02,
            TemplatePatch03,
            TemplatePatch04,
            TemplatePatch07,
            TemplatePatch08,
            TemplatePatch13,
            TemplatePatch14,
            TemplatePatch15,
            TemplateRiver01,
            TemplateRiver02,
            TemplateRiver03,
            TemplateRiver04,
            TemplateRiver05,
            TemplateRiver06,
            TemplateRiver07,
            TemplateRiver08,
            TemplateRiver09,
            TemplateRiver10,
            TemplateRiver11,
            TemplateRiver12,
            TemplateRiver13,
            TemplateRiver14,
            TemplateRiver15,
            TemplateFord1,
            TemplateFord2,
            TemplateFalls1,
            TemplateFalls1a,
            TemplateFalls2,
            TemplateFalls2a,
            TemplateBridge1x,
            TemplateBridge1,
            TemplateBridge1h,
            TemplateBridge1d,
            TemplateBridge2x,
            TemplateBridge2,
            TemplateBridge2h,
            TemplateBridge2d,
            TemplateBridge1ax,
            TemplateBridge1a,
            TemplateBridge1b,
            TemplateBridge1c,
            TemplateBridge2ax,
            TemplateBridge2a,
            TemplateBridge2b,
            TemplateBridge2c,
            TemplateBridge3a,
            TemplateBridge3b,
            TemplateBridge3c,
            TemplateBridge3d,
            TemplateBridge3e,
            TemplateBridge3f,
            TemplateShoreCliff01,
            TemplateShoreCliff02,
            TemplateShoreCliff03,
            TemplateShoreCliff04,
            TemplateShoreCliff05,
            TemplateShoreCliff06,
            TemplateShoreCliff07,
            TemplateShoreCliff08,
            TemplateShoreCliff09,
            TemplateShoreCliff10,
            TemplateShoreCliff11,
            TemplateShoreCliff12,
            TemplateShoreCliff13,
            TemplateShoreCliff14,
            TemplateShoreCliff15,
            TemplateShoreCliff16,
            TemplateShoreCliff17,
            TemplateShoreCliff18,
            TemplateShoreCliff19,
            TemplateShoreCliff20,
            TemplateShoreCliff21,
            TemplateShoreCliff22,
            TemplateShoreCliff23,
            TemplateShoreCliff24,
            TemplateShoreCliff25,
            TemplateShoreCliff26,
            TemplateShoreCliff27,
            TemplateShoreCliff28,
            TemplateShoreCliff29,
            TemplateShoreCliff30,
            TemplateShoreCliff31,
            TemplateShoreCliff32,
            TemplateShoreCliff33,
            TemplateShoreCliff34,
            TemplateShoreCliff35,
            TemplateShoreCliff36,
            TemplateShoreCliff37,
            TemplateShoreCliff38,
            TemplateRough01,
            TemplateRough02,
            TemplateRough03,
            TemplateRough04,
            TemplateRough05,
            TemplateRough06,
            TemplateRough07,
            TemplateRough08,
            TemplateRough09,
            TemplateRough10,
            TemplateRough11,
            TemplateRiverCliff01,
            TemplateRiverCliff02,
            TemplateRiverCliff03,
            TemplateRiverCliff04,
            TemplateF01,
            TemplateF02,
            TemplateF03,
            TemplateF04,
            TemplateF05,
            TemplateF06,
            TemplateARRO0001,
            TemplateARRO0002,
            TemplateARRO0003,
            TemplateARRO0004,
            TemplateARRO0005,
            TemplateARRO0006,
            TemplateARRO0007,
            TemplateARRO0008,
            TemplateARRO0009,
            TemplateARRO0010,
            TemplateARRO0011,
            TemplateARRO0012,
            TemplateARRO0013,
            TemplateARRO0014,
            TemplateARRO0015,
            TemplateFLOR0001,
            TemplateFLOR0002,
            TemplateFLOR0003,
            TemplateFLOR0004,
            TemplateFLOR0005,
            TemplateFLOR0006,
            TemplateFLOR0007,
            TemplateGFLR0001,
            TemplateGFLR0002,
            TemplateGFLR0003,
            TemplateGFLR0004,
            TemplateGFLR0005,
            TemplateGSTR0001,
            TemplateGSTR0002,
            TemplateGSTR0003,
            TemplateGSTR0004,
            TemplateGSTR0005,
            TemplateGSTR0006,
            TemplateGSTR0007,
            TemplateGSTR0008,
            TemplateGSTR0009,
            TemplateGSTR0010,
            TemplateGSTR0011,
            TemplateLWAL0001,
            TemplateLWAL0002,
            TemplateLWAL0003,
            TemplateLWAL0004,
            TemplateLWAL0005,
            TemplateLWAL0006,
            TemplateLWAL0007,
            TemplateLWAL0008,
            TemplateLWAL0009,
            TemplateLWAL0010,
            TemplateLWAL0011,
            TemplateLWAL0012,
            TemplateLWAL0013,
            TemplateLWAL0014,
            TemplateLWAL0015,
            TemplateLWAL0016,
            TemplateLWAL0017,
            TemplateLWAL0018,
            TemplateLWAL0019,
            TemplateLWAL0020,
            TemplateLWAL0021,
            TemplateLWAL0022,
            TemplateLWAL0023,
            TemplateLWAL0024,
            TemplateLWAL0025,
            TemplateLWAL0026,
            TemplateLWAL0027,
            TemplateSTRP0001,
            TemplateSTRP0002,
            TemplateSTRP0003,
            TemplateSTRP0004,
            TemplateSTRP0005,
            TemplateSTRP0006,
            TemplateSTRP0007,
            TemplateSTRP0008,
            TemplateSTRP0009,
            TemplateSTRP0010,
            TemplateSTRP0011,
            TemplateWALL0001,
            TemplateWALL0002,
            TemplateWALL0003,
            TemplateWALL0004,
            TemplateWALL0005,
            TemplateWALL0006,
            TemplateWALL0007,
            TemplateWALL0008,
            TemplateWALL0009,
            TemplateWALL0010,
            TemplateWALL0011,
            TemplateWALL0012,
            TemplateWALL0013,
            TemplateWALL0014,
            TemplateWALL0015,
            TemplateWALL0016,
            TemplateWALL0017,
            TemplateWALL0018,
            TemplateWALL0019,
            TemplateWALL0020,
            TemplateWALL0021,
            TemplateWALL0022,
            TemplateWALL0023,
            TemplateWALL0024,
            TemplateWALL0025,
            TemplateWALL0026,
            TemplateWALL0027,
            TemplateWALL0028,
            TemplateWALL0029,
            TemplateWALL0030,
            TemplateWALL0031,
            TemplateWALL0032,
            TemplateWALL0033,
            TemplateWALL0034,
            TemplateWALL0035,
            TemplateWALL0036,
            TemplateWALL0037,
            TemplateWALL0038,
            TemplateWALL0039,
            TemplateWALL0040,
            TemplateWALL0041,
            TemplateWALL0042,
            TemplateWALL0043,
            TemplateWALL0044,
            TemplateWALL0045,
            TemplateWALL0046,
            TemplateWALL0047,
            TemplateWALL0048,
            TemplateWALL0049,
            TemplateXtra0001,
            TemplateXtra0002,
            TemplateXtra0003,
            TemplateXtra0004,
            TemplateXtra0005,
            TemplateXtra0006,
            TemplateXtra0007,
            TemplateXtra0008,
            TemplateXtra0009,
            TemplateXtra0010,
            TemplateXtra0011,
            TemplateXtra0012,
            TemplateXtra0013,
            TemplateXtra0014,
            TemplateXtra0015,
            TemplateXtra0016,
            TemplateAntHill,
        };
        protected static List<Ra1TemplateTypeClass> orderedTemplates = new List<Ra1TemplateTypeClass>(templates).OrderBy(x => x.TemplateId).ToList();
        public static List<Ra1TemplateTypeClass> Templates { get { return orderedTemplates.ToList(); } }

        public Int32 TemplateId;
        public Ra1Theater Theaters;
        public string FileName;
        public Int32 NameId;

        public Ra1TemplateTypeClass(TemplateType templateType, Ra1Theater theaters, String fileName, Int32 nameId)
        {
            this.TemplateId = (Int32)templateType;
            this.Theaters = theaters;
            this.FileName = fileName;
            this.NameId = nameId;
        }

        public override String ToString()
        {
            return this.TemplateId + "=" + this.FileName;
        }
    }
}