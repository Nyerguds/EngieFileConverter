using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nyerguds.GameData.Westwood
{
    public class CnCShp
    {
        //C++ TO C# CONVERTER WARNING: The following constructor is declared outside of its associated class:
        private Byte[] shp_file_write(Byte[] s, int cx, int cy, int c_images)
        {
	        Cvirtual_binary d = new Cvirtual_binary();
        //C++ TO C# CONVERTER TODO TASK: Pointer arithmetic is detected on this variable, so pointers on this variable are left unchanged:
	        @byte * r = s;
        //C++ TO C# CONVERTER TODO TASK: Pointer arithmetic is detected on this variable, so pointers on this variable are left unchanged:
	        @byte * w = d.write_start(sizeof(t_shp_ts_header) + (sizeof(t_shp_ts_image_header) + cx * cy) * c_images);
        //C++ TO C# CONVERTER TODO TASK: There is no equivalent to 'reinterpret_cast' in C#:
	        t_shp_header header = reinterpret_cast<t_shp_header*>(w);
	        header.c_images = c_images;
	        header.xpos = 0;
	        header.ypos = 0;
	        header.cx = cx;
	        header.cy = cy;
	        header.delta = 0;
	        header.flags = 0;
	        w += sizeof(t_shp_header);
        //C++ TO C# CONVERTER TODO TASK: Pointer arithmetic is detected on this variable, so pointers on this variable are left unchanged:
        //C++ TO C# CONVERTER TODO TASK: There is no equivalent to 'reinterpret_cast' in C#:
	        int * index = reinterpret_cast<int*>(w);
	        w += 8 * (c_images + 2);

        //C++ TO C# CONVERTER TODO TASK: C# does not have an equivalent to pointers to variables (in C#, the variable no longer points to the original when the original variable is re-assigned):
        //ORIGINAL LINE: const byte* last = r;
	        @byte last = r;
	        @byte last40 = null;
        //C++ TO C# CONVERTER TODO TASK: C# does not have an equivalent to pointers to variables (in C#, the variable no longer points to the original when the original variable is re-assigned):
        //ORIGINAL LINE: const byte* last80 = r;
	        @byte last80 = r;
        //C++ TO C# CONVERTER TODO TASK: C# does not have an equivalent to pointers to variables (in C#, the variable no longer points to the original when the original variable is re-assigned):
        //ORIGINAL LINE: byte* last80w = w;
	        @byte last80w = w;
	        int count20 = 0;
	        int deltaframe = 0;
	        int largest = 0;

	        //first frame is always format80(LCW)
	        *index++= 0x80000000 | w - d.data();
	        *index++= 0;
	        w += encode80(r, w, cx * cy);
	        r += cx * cy;
	        largest = w - last80w;

	        for (int i = 1; i < c_images; i++)
	        {
		        int size80;
		        int size40;
		        int size20;

		        // do test encodes of the 3 possible frame formats to see which is
		        // smaller.
		        if (last40 != null)
		        {
			        size20 = encode40(last, r, w, cx * cy);
		        }
		        else
		        {
			        size20 = 0x7FFFFFFF;
		        }

		        size40 = encode40(last80, r, w, cx * cy);
		        size80 = encode80(r, w, cx * cy);

		        // if format80 is smallest or equal, do format80
		        if (size80 <= size40 != 0 && size80 <= size20)
		        {
			        *index++= 0x80000000 | w - d.data();
			        *index++= 0;
        //C++ TO C# CONVERTER TODO TASK: C# does not have an equivalent to pointers to variables (in C#, the variable no longer points to the original when the original variable is re-assigned):
        //ORIGINAL LINE: last80 = r;
			        last80 = r;
			        last40 = null;
        //C++ TO C# CONVERTER TODO TASK: C# does not have an equivalent to pointers to variables (in C#, the variable no longer points to the original when the original variable is re-assigned):
        //ORIGINAL LINE: last = r;
			        last = r;
        //C++ TO C# CONVERTER TODO TASK: C# does not have an equivalent to pointers to variables (in C#, the variable no longer points to the original when the original variable is re-assigned):
        //ORIGINAL LINE: last80w = w;
			        last80w = w;
			        w += encode80(r, w, cx * cy);
			        r += cx * cy;

			        if (size80 > largest)
			        {
				        largest = size80;
			        }
		        }
		        else if (size40 <= size20)
		        {
			        *index++= 0x40000000 | w - d.data();
			        *index++= 0x80000000 | last80w - d.data();
        //C++ TO C# CONVERTER TODO TASK: C# does not have an equivalent to pointers to variables (in C#, the variable no longer points to the original when the original variable is re-assigned):
        //ORIGINAL LINE: last = r;
			        last = r;
        //C++ TO C# CONVERTER TODO TASK: C# does not have an equivalent to pointers to variables (in C#, the variable no longer points to the original when the original variable is re-assigned):
        //ORIGINAL LINE: last40 = r;
			        last40 = r;
			        deltaframe = i;
			        w += encode40(last80, r, w, cx * cy);
			        r += cx * cy;

			        if (size40 > largest)
			        {
				        largest = size40;
			        }
		        }
		        else
		        {
			        *index++= 0x20000000 | w - d.data();
			        *index++= 0x48000000 | deltaframe;
			        w += encode40(last, r, w, cx * cy);
        //C++ TO C# CONVERTER TODO TASK: C# does not have an equivalent to pointers to variables (in C#, the variable no longer points to the original when the original variable is re-assigned):
        //ORIGINAL LINE: last = r;
			        last = r;
			        r += cx * cy;

			        if (size20 > largest)
			        {
				        largest = size20;
			        }
		        }
	        }

	        header.delta = largest + 37;

	        *index++= w - d.data();
	        *index++= 0;
	        *index++= 0;
	        *index++= 0;
	        d.size(w - d.data());
	        return d;
        }

    }
}
