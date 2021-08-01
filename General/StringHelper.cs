using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElementEngine
{
    public static class StringHelper
    {
        public static unsafe string GetString(byte* startPtr)
        {
            int length = 0;

            while (startPtr[length] != 0)
                length += 1;

            return Encoding.UTF8.GetString(startPtr, length);
        }

    } // StringHelper
}
