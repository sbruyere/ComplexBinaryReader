using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Qiil.Binary.Utils
{
    public static class LargeObjectBinarySearch
    {
        public static List<long> GetPatternPositions(Stream stream, byte[] pattern, int bufferSize = 400_000)
        {
            List<long> searchResults = new List<long>(); //The results as offsets within the file

            int realBufferSize = (int)Math.Min(stream.Length, 400_000);

            byte[] buffer = new byte[realBufferSize];
            int readCount = 0;
            int patternSize = pattern.Length;

            long posFile = 0;

            while (((readCount = stream.Read(buffer, 0, realBufferSize))) > 0 && (posFile + patternSize) < stream.Length)
            {
                List<long> finds = FindPatternSequences(buffer, pattern, 0);

                for (int i = 0; i < finds.Count; i++)
                {
                    searchResults.Add(finds[i] + posFile);
                }

                stream.Seek(1 - patternSize, SeekOrigin.Current);
                posFile = stream.Position;
            }

            return searchResults;
        }

        public static List<long> FindPatternSequences(this byte[] buffer, byte[] pattern, int startIndex)
        {
            List<long> positions = new List<long>();
            byte pat0 = pattern[0];
            int i = Array.IndexOf<byte>(buffer, pat0, startIndex);
            int patternLength = pattern.Length;

            while (i >= 0 && i <= buffer.Length - patternLength)
            {
                byte[] segment = new byte[patternLength];
                Buffer.BlockCopy(buffer, i, segment, 0, patternLength);
                if (segment.SequenceEqual<byte>(pattern))
                    positions.Add(i);
                i = Array.IndexOf<byte>(buffer, pat0, i + patternLength);
            }

            return positions;
        }

        static readonly int[] Empty = new int[0];

        public static int[] Locate(this byte[] self, byte[] candidate)
        {
            if (IsEmptyLocate(self, candidate))
                return Empty;

            var list = new List<int>();

            for (int i = 0; i < self.Length; i++)
            {
                if (!IsMatch(self, i, candidate))
                    continue;

                list.Add(i);
            }

            return list.Count == 0 ? Empty : list.ToArray();
        }

        static bool IsMatch(byte[] array, int position, byte[] candidate)
        {
            if (candidate.Length > (array.Length - position))
                return false;

            for (int i = 0; i < candidate.Length; i++)
                if (array[position + i] != candidate[i])
                    return false;

            return true;
        }

        static bool IsEmptyLocate(byte[] array, byte[] candidate)
        {
            return array == null
                   || candidate == null
                   || array.Length == 0
                   || candidate.Length == 0
                   || candidate.Length > array.Length;
        }
    }
}

