using System;
using System.Collections.Generic;
using System.Text;

namespace Qiil.Binary.Utils
{
    public static class CInteropUtils
    {
        public static bool IS_INTRESOURCE(uint resource) => (resource >> 16) == 0;
        public static bool IS_INTRESOURCE(int resource) => (resource >> 16) == 0;
        public static int MAKE_INT32(ushort low, ushort high) => (high << 16) + low;
        public static int MAKE_INT32(short low, short high) => (high << 16) + low;
    }
}
