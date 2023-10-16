using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Qiil.IO
{
    public interface IPtrResolver
    {
        long Resolve(long ptr);
        long Seek(long ptr, SeekOrigin seekOrigin = SeekOrigin.Begin);
    }

    public abstract class PtrResolver<T> : IPtrResolver where T 
        : ComplexBinaryReader
    {
        public T Reader { get; }

        public PtrResolver(T reader)
        {
            Reader = reader;
        }

        public abstract long Resolve(long ptr);

        public long Seek(long ptr, SeekOrigin seekOrigin = SeekOrigin.Begin)
        {
            return Reader.Seek(Resolve(ptr), seekOrigin);
        }

    }
}
