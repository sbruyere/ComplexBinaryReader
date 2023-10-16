using Qiil.IO;
using System;
using System.Collections.Generic;
using System.Text;

namespace Qiil.IO
{
    public class StandardPtrResolver : PtrResolver<ComplexBinaryReader>
    {
        public StandardPtrResolver(ComplexBinaryReader reader)
            : base(reader)
        {
        }

        public override long Resolve(long ptr)
        {
            return ptr;
        }
    }
}
