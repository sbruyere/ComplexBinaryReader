using Newtonsoft.Json;
using Qiil.IO.Enums;

namespace Qiil.IO
{
    public abstract class StructWrapper<T> : StructWrapper<ComplexBinaryReader, T>
            where T : struct
    {
        protected StructWrapper(
            ComplexBinaryReader reader,
            ulong ptr = StructWrapperConst.DEFAULT_CURRENT_POSITION,
            PtrType ptrType = PtrType.FileOffset)
            : base(reader, ptr, ptrType)
        {
        }
    }

    public abstract class StructWrapper<R, T>
            where R : ComplexBinaryReader
            where T : struct
        {
            [JsonIgnore]
            public R Reader { get; }

            public ulong BasePtr { get; }
            public static int StructSize { get; } = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));

            public T Base { get; }

            protected StructWrapper(
                R reader,
                ulong ptr = StructWrapperConst.DEFAULT_CURRENT_POSITION,
                PtrType ptrType = PtrType.FileOffset)
            {
                Reader = reader;

                if (ptr != 0xFFFFFFFF)
                    BasePtr = ptr;

                Base = reader.Get<T>(ptr, ptrType);
            }
        }
}
