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
        [JsonIgnore]
        public Stream Stream { get; set; }

        [JsonIgnore]
        public BinaryReader Reader { get; set; }

        public string SHA256 { get; set; }
        #endregion

        #region Abstract methods
        protected abstract void OnReadStream();

        public abstract long Resolve(long virtAddress, PtrType ptrType = PtrType.VA);

        #endregion


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

        /// <summary>
        /// Reads in a block from a file and converts it to the struct
        /// type specified by the template parameter
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <param name="structPtr"></param>
        /// <returns></returns>
        public static T FromBinaryReaderFromPtr<T>(BinaryReader reader, long structPtr)
        {
            reader.BaseStream.Seek(structPtr, SeekOrigin.Begin);

            return Get<T>(reader);
        }


        public static T FromBinaryReaderFromPtr<T>(ComplexBinaryReader reader, long structPtr)
        {
            reader.Seek(structPtr, SeekOrigin.Begin);

            return Get<T>(reader);
        }

        /// <summary>
        /// Reads in a block from a file and converts it to the struct
        /// type specified by the template parameter
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="structRVA"></param>
        /// <returns></returns>
        public T Get<T>(long structRVA, PtrType ptrType = PtrType.VA)
        {
            long structPtr = Resolve(structRVA, ptrType);

            return FromBinaryReaderFromPtr<T>(this, structPtr);
        }

        public T Get<T>()
        {
            return Get<T>(this);
        }

        public T[] GetArrayOf<T>(int count)
        {
            if (typeof(T) == typeof(byte))
                return (T[])(object)Reader.ReadBytes(count);


            List<T> lstRet = new List<T>();

            for (int i = 0; i < count; i++)
                lstRet.Add(Get<T>());

            return lstRet.ToArray();
        }

        public T[] GetArrayOf<T>(long ptr, int count, PtrType ptrType = PtrType.VA)
        {
            if (ptr == 0)
                return new T[0];

            Seek(Resolve(ptr, ptrType), SeekOrigin.Begin);

            return GetArrayOf<T>(count);
        }

        public static T Get<T>(ComplexBinaryReader reader)
        {
            if (typeof(IStructWrapper).IsAssignableFrom(typeof(T)))
                return (T)Activator.CreateInstance(typeof(T), (object)reader);

            return Get<T>(reader.Reader);
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
            Type genType = typeof(T);
            bool isEnum = genType.IsEnum;

            genType = isEnum
                ? Enum.GetUnderlyingType(genType)
                : genType;

            // Read in a byte array
            byte[] bytes = reader.ReadBytes(Marshal.SizeOf(genType));

            // Pin the managed memory while, copy it out the data, then unpin it
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);

            T theStructure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), genType);
            handle.Free();

            return theStructure;
        }

        public void Get<T>(long structRVA, ref T destination, PtrType ptrType = PtrType.VA)
        {
            long structPtr = Resolve(structRVA, ptrType);
            Seek(structPtr, SeekOrigin.Begin);

            Get(this, ref destination);
        }

        //public string GetASCIIStr(int size = -1)
        //{
        //    if (size == -1)
        //        size = Get<ushort>();

        //    if (size == 0)
        //        return string.Empty;

        //    // Read in a byte array
        //    byte[] bytes = Reader.ReadBytes(size);

        //    byte zero = Reader.ReadByte();

        //    return Encoding.ASCII.GetString(bytes);
        //}


        //public string GetStr(int size = -1, Encoding encoding = null)
        //{
        //    if (encoding == null)
        //        encoding = Encoding.ASCII;

        //    if (size == -1)
        //        size = Get<ushort>();

        //    // Read in a byte array
        //    byte[] bytes = Reader.ReadBytes(size + 1);

        //    return encoding.GetString(bytes);
        //}

        public string GetStr(
            long ptr, 
            PtrType ptrType, 
            int size = -1, 
            Encoding encoding = null, 
            bool nullTerminated = false)
        {
            Seek(Resolve(ptr, ptrType), SeekOrigin.Begin);

            return GetStr(size, encoding, nullTerminated);
        }


        public string GetStr(
            int size,
            Encoding encoding = null,
            bool nullTerminated = false)
        {
            if (encoding == null)
                encoding = Encoding.ASCII;

            BinaryReader textReader = new BinaryReader(this.Stream, encoding);

            if (size == -1)
                size = Get<ushort>();

            var chars = textReader.ReadChars(size);

            if (nullTerminated)
                Seek(1, SeekOrigin.Current);

            return new string(chars);
        }


        public string GetStr(
            Encoding encoding = null,
            bool nullTerminated = false)
        {
            return GetStr(-1, encoding, nullTerminated);
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
            int size = -1;
            if (destination is Array)
            {
                var elementType = destination.GetType().GetElementType();

                if (elementType == typeof(byte))
                    destination = (T)(object)reader.ReadBytes((destination as Array).Length);
            } 

            if (size == -1)
                size = Marshal.SizeOf(destination);


            // Read in a byte array
            byte[] bytes = reader.ReadBytes(size);

            
            // Pin the managed memory while, copy it out the data, then unpin it
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            destination = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();
        }

        public static void Get<T>(ComplexBinaryReader reader, ref T destination)
        {
            Get(reader.Reader, ref destination);
        }


        public static string ReadNullTerminatedString(BinaryReader stream, Encoding encoding = null)
        {
            return stream.ReadStringNull(encoding ?? Encoding.ASCII);
        }


        public string RGetStr(long structPtr, Encoding encoding = null)
        {
            if (structPtr == 0) return string.Empty;

            Reader.BaseStream.Seek(structPtr, SeekOrigin.Begin);

            return ReadNullTerminatedString(Reader, encoding ?? Encoding.ASCII);
        }

        public string VGetStr(long structRVA, Encoding encoding = null, PtrType ptrType = PtrType.VA)
        {
            var ptr = Resolve(structRVA, ptrType);
            return RGetStr(ptr, encoding ?? Encoding.ASCII);
        }

        public Guid RGetGuid(long structPtr)
        {
            if (structPtr == 0) return Guid.Empty;

            Reader.BaseStream.Seek(structPtr, SeekOrigin.Begin);

            return Get<Guid>(Reader);
        }

        public Guid VGetGuid(long structRVA, PtrType ptrType = PtrType.VA)
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
        #endregion Public Methods
    }
}
