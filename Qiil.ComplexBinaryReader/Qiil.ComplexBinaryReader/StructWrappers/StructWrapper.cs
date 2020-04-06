using Newtonsoft.Json;
using Qiil.IO.Enums;
using System;
using System.Runtime.InteropServices;

namespace Qiil.IO
{
    public interface IStructWrapper { }

    public abstract class StructWrapper<T> : StructWrapper<ComplexBinaryReader, T>, IStructWrapper
            where T : struct
        
    {
        protected StructWrapper(
            ComplexBinaryReader reader,
            long ptr = StructWrapperConst.DEFAULT_CURRENT_POSITION,
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

        public long BasePtr { get; }
        public static int StructSize { get; } = Marshal.SizeOf(typeof(T));

        public T Base { get; }

        protected StructWrapper(
            R reader,
            long ptr = StructWrapperConst.DEFAULT_CURRENT_POSITION,
            PtrType ptrType = PtrType.FileOffset)
        {
            Reader = reader;

            if (ptr != StructWrapperConst.DEFAULT_CURRENT_POSITION)
                BasePtr = ptr;

            Base = reader.Get<T>(ptr, ptrType);
        }

        protected StructWrapper(
            R reader)
        {
            Reader = reader;

            BasePtr = reader.Stream.Position;
            
            Base = reader.Get<T>();
        }

        /// <summary>
        /// Get a struct at the reader stream's position.
        /// </summary>
        /// <typeparam name="K">Struct wrapper type.</typeparam>
        /// <param name="reader">Reader.</param>
        /// <returns></returns>
        public static K Get<K>(
            R reader) where K : StructWrapper<R, T>
        {
            return (K)Activator.CreateInstance(typeof(K), new object[] { reader });
        }

        public static K Get<K>(
            R reader,
            long ptr,
            PtrType ptrType = PtrType.FileOffset) where K: StructWrapper<R, T>
        {
            return (K)Activator.CreateInstance(typeof(K), new object[] { reader, ptr, ptrType });
        }

    }
}
