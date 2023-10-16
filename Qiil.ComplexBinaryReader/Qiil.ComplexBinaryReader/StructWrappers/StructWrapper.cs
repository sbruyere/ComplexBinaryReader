using Newtonsoft.Json;
using System;
using System.Runtime.InteropServices;

namespace Qiil.IO
{
    public interface IStructWrapper { }

    public abstract class StructWrapper<TStruct> : StructWrapper<ComplexBinaryReader, TStruct>, IStructWrapper
            where TStruct : struct
        
    {
        protected StructWrapper(
            ComplexBinaryReader reader,
            IPtrResolver ptrResolver,
            long ptr = StructWrapperConst.DEFAULT_CURRENT_POSITION) 
            : base(reader, ptrResolver, ptr)
        {
        }

        protected StructWrapper(
            ComplexBinaryReader reader)
            : base(reader)
        {
        }
    }

    public abstract class StructWrapper<TReader, TStruct> : IStructWrapper
            where TReader : ComplexBinaryReader
            where TStruct : struct
        
    {
        [JsonIgnore]
        public TReader Reader { get; }

        public long BasePtr { get; }
        public static int StructSize { get; } = Marshal.SizeOf(typeof(TStruct));

        public TStruct Base { get; }

        protected StructWrapper(
            TReader reader,
            IPtrResolver ptrResolver,
            long ptr = StructWrapperConst.DEFAULT_CURRENT_POSITION)
        {
            Reader = reader;

            if (ptr != StructWrapperConst.DEFAULT_CURRENT_POSITION)
                BasePtr = ptr;

            Base = reader.Get<TStruct>(ptr, ptrResolver);
        }

        protected StructWrapper(
            TReader reader)
        {
            Reader = reader;

            BasePtr = reader.Stream.Position;
            
            Base = reader.Get<TStruct>();
        }

        /// <summary>
        /// Get a struct at the reader stream's position.
        /// </summary>
        /// <typeparam name="TRet">Struct wrapper type.</typeparam>
        /// <param name="reader">Reader.</param>
        /// <returns></returns>
        public static TRet Get<TRet>(
            TReader reader) where TRet : StructWrapper<TReader, TStruct>
        {
            return (TRet)Activator.CreateInstance(typeof(TRet), new object[] { reader });
        }

        public static TRet Get<TRet>(
            TReader reader,
            long ptr,
            PtrResolver<TReader> ptrResolver) where TRet
            : StructWrapper<TReader, TStruct>
        {
            return (TRet)Activator.CreateInstance(typeof(TRet), new object[] { reader, ptr, ptrResolver });
        }

    }
}
