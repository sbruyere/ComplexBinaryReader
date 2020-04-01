using Newtonsoft.Json;
using Qiil.IO.Enums;
using System;

namespace Qiil.IO
{
    public interface IStructWrapper { }

    public abstract class StructWrapper<T> : StructWrapper<ComplexBinaryReader, T>, IStructWrapper
            where T : struct
        
    {
        public const uint DEFAULT_CURRENT_POSITION = 0xFFFFFFFF;
        protected StructWrapper(
            ComplexBinaryReader reader,
            ulong ptr = StructWrapperConst.DEFAULT_CURRENT_POSITION,
            PtrType ptrType = PtrType.FileOffset)
            : base(reader, ptr, ptrType)
        {
        }

        protected StructWrapper(
            ComplexBinaryReader reader)
            : base(reader)
        {
        }
    }

    public abstract class StructWrapper<R, T> : IStructWrapper
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

        protected StructWrapper(
            R reader)
        {
            Reader = reader;
            
            Base = reader.Get<T>();
        }

        /// <summary>
        /// Get a struct at the reader stream's position.
        /// </summary>
        /// <typeparam name="K">Struct wrapper type.</typeparam>
        /// <param name="reader">Reader.</param>
        /// <returns></returns>
        public static K Get<K>(
            R reader) where K : StructWrapper<R, T>, new()
        {
            return (K)Activator.CreateInstance(typeof(K), new object[] { });
        }

        public static K Get<K>(
            R reader,
            ulong ptr = StructWrapperConst.DEFAULT_CURRENT_POSITION,
            PtrType ptrType = PtrType.FileOffset) where K: StructWrapper<R, T>, new()
        {
            return (K)Activator.CreateInstance(typeof(K), new object[] { reader, ptr, ptrType });
        }

    }
}
