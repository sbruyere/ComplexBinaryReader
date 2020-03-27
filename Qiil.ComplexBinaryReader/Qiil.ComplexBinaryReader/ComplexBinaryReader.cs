using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Qiil.Binary.Utils;
using Qiil.IO.Enums;

namespace Qiil.IO
{
    public abstract class ComplexBinaryReader
    {
        #region Private Fields

        #endregion Private Fields

        #region Public Methods

        protected ComplexBinaryReader(string filePath)
            : this(new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
        }

        protected ComplexBinaryReader(Stream stream)
        {
            Stream = stream;
            Reader = new BinaryReader(Stream);

            SeekReset(stream);

            ComputeSHA256(stream);

            OnReadStream();

        }

        [JsonIgnore]
        public Stream Stream { get; set; }

        private void ComputeSHA256(Stream stream)
        {
            var fileBytes = Reader.ReadBytes((int)stream.Length);
            stream.Seek(0, SeekOrigin.Begin);
            SHA256 = GetSHA256(ref fileBytes);
        }

        public void SeekReset(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
        }

        protected abstract void OnReadStream();


        public string SHA256 { get; set; }

        [JsonIgnore]
        public BinaryReader Reader { get; set; }


        public abstract ulong Resolve(ulong virtAddress, PtrType ptrType = PtrType.VA);


        /// <summary>
        /// Reads in a block from a file and converts it to the struct
        /// type specified by the template parameter
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <param name="structPtr"></param>
        /// <returns></returns>
        public static T FromBinaryReaderFromPtr<T>(BinaryReader reader, ulong structPtr)
        {
            reader.BaseStream.Seek((long)structPtr, SeekOrigin.Begin);

            return Get<T>(reader);
        }

        /// <summary>
        /// Reads in a block from a file and converts it to the struct
        /// type specified by the template parameter
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="structRVA"></param>
        /// <returns></returns>
        public T Get<T>(ulong structRVA, PtrType ptrType = PtrType.VA)
        {
            ulong structPtr = Resolve(structRVA, ptrType);

            return FromBinaryReaderFromPtr<T>(Reader, structPtr);
        }

        public T Get<T>()
        {
            return Get<T>(Reader);
        }

        public IEnumerable<T> GetArrayOf<T>(uint count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return Get<T>();
            }
        }

        public IEnumerable<T> GetArrayOf<T>(uint ptr, uint count, PtrType ptrType = PtrType.VA)
        {
            if (ptr == 0)
                return new T[0];

            Seek((long)Resolve(ptr, ptrType), SeekOrigin.Begin);

            return GetArrayOf<T>(count);
        }

        /// <summary>
        /// Reads in a block from a file and converts it to the struct
        /// type specified by the template parameter
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static T Get<T>(BinaryReader reader)
        {
            bool isEnum = typeof(T).IsEnum;

            Type marshalType = typeof(T).IsEnum
                ? Enum.GetUnderlyingType(typeof(T))
                : typeof(T);

            // Read in a byte array
            byte[] bytes = null;
            bytes = reader.ReadBytes(Marshal.SizeOf(marshalType));

            // Pin the managed memory while, copy it out the data, then unpin it
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            T theStructure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), marshalType);
            handle.Free();

            return theStructure;
        }

        public void Get<T>(ulong structRVA, ref T destination, PtrType ptrType = PtrType.VA)
        {
            ulong structPtr = Resolve(structRVA, ptrType);
            Reader.BaseStream.Seek((long)structPtr, SeekOrigin.Begin);

            Get(Reader, ref destination);
        }

        public string GetASCIIStr(int size = -1)
        {
            if (size == -1)
                size = Get<ushort>();


            if (size == 0)
                (0).ToString();
            // Read in a byte array
            byte[] bytes = Reader.ReadBytes(size);

            byte zero = Reader.ReadByte();

            return Encoding.ASCII.GetString(bytes);
        }


        public string GetStr(int size = -1, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.ASCII;

            if (size == -1)
                size = Get<ushort>();

            // Read in a byte array
            byte[] bytes = Reader.ReadBytes(size + 1);

            return encoding.GetString(bytes);
        }


        public string GetBStr()
        {
            ushort size = Get<ushort>();

            // Read in a byte array
            byte[] bytes = Reader.ReadBytes((size + 1) * 2);

            return Encoding.Unicode.GetString(bytes);
        }


        public static void Get<T>(BinaryReader reader, ref T destination)
        {
            // Read in a byte array
            byte[] bytes = reader.ReadBytes(Marshal.SizeOf(destination));

            // Pin the managed memory while, copy it out the data, then unpin it
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            destination = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();
        }

        #endregion Public Methods

        #region Properties

        #endregion Properties
        public static string ReadNullTerminatedString(BinaryReader stream, Encoding encoding = null)
        {
            Encoding enc = encoding ?? Encoding.ASCII;
            return stream.ReadStringNull(enc);
        }


        public string RGetStr(ulong structPtr, Encoding encoding = null)
        {
            if (structPtr == 0) return string.Empty;

            Reader.BaseStream.Seek((long)structPtr, SeekOrigin.Begin);

            return ReadNullTerminatedString(Reader, encoding);
        }

        public string VGetStr(ulong structRVA, Encoding encoding = null, PtrType ptrType = PtrType.VA)
        {
            var ptr = Resolve(structRVA, ptrType);
            return RGetStr(ptr, encoding);
        }

        public Guid RGetGuid(ulong structPtr)
        {
            if (structPtr == 0) return Guid.Empty;

            Reader.BaseStream.Seek((long)structPtr, SeekOrigin.Begin);

            return Get<Guid>(Reader);
        }

        public Guid VGetGuid(ulong structRVA, PtrType ptrType = PtrType.VA)
        {
            return RGetGuid(Resolve(structRVA, ptrType));
        }

        public static string GetSHA256(ref byte[] bytes)
        {
            SHA256Managed hashstring = new SHA256Managed();
            byte[] hash = hashstring.ComputeHash(bytes);
            string hashString = string.Empty;
            foreach (byte x in hash)
            {
                hashString += $"{x:x2}";
            }
            return hashString;
        }

        public long Seek(long offset, SeekOrigin seekOrigin)
        {
            return Stream.Seek(offset, seekOrigin);
        }
    }
}
