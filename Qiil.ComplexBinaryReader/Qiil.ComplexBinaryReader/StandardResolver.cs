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
