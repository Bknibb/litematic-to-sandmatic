using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace litematic_to_sandmatic.LitematicaCS
{
    internal class LitematicaBitArray : IEnumerable<long>
    {
        public int size;
        public int nbits;
        public long[] array;
        private long mask;
        public LitematicaBitArray(int size, int nbits)
        {
            this.size = size;
            this.nbits = nbits;
            int s = (int)Math.Ceiling((double)nbits * size / 64.0);
            array = new long[s];
            mask = (1L << nbits) - 1;
        }

        public long this[int index]
        {
            get
            {
                if (index < 0 || index >= size)
                    throw new IndexOutOfRangeException($"Invalid index {index}");

                int startOffset = index * nbits;
                int startArrIndex = startOffset >> 6;
                int endArrIndex = ((index + 1) * nbits - 1) >> 6;
                int startBitOffset = startOffset & 0x3F;

                ulong shiftedPart = ((ulong)array[startArrIndex] >> startBitOffset);
                if (startArrIndex == endArrIndex)
                {
                    return (long)(shiftedPart & (ulong)mask);
                }
                else
                {
                    int endOffset = 64 - startBitOffset;
                    ulong val = shiftedPart | ((ulong)array[endArrIndex] << endOffset);
                    return (long)(val & (ulong)mask);
                }
            }
            set
            {
                if (index < 0 || index >= size)
                    throw new IndexOutOfRangeException($"Invalid index {index}");

                if (value < 0 || value > mask)
                    throw new ArgumentOutOfRangeException($"Invalid value {value}, maximum value is {mask}");

                int startOffset = index * nbits;
                int startArrIndex = startOffset >> 6;
                int endArrIndex = ((index + 1) * nbits - 1) >> 6;
                int startBitOffset = startOffset & 0x3F;
                long m = -1L; // 0xFFFFFFFFFFFFFFFF

                array[startArrIndex] = (array[startArrIndex] & ~(mask << startBitOffset)) |
                                        ((value & mask) << startBitOffset);
                array[startArrIndex] &= m;

                if (startArrIndex != endArrIndex)
                {
                    int endOffset = 64 - startBitOffset;
                    int j1 = nbits - endOffset;
                    array[endArrIndex] = ((array[endArrIndex] >> j1) << j1) |
                                         ((value & mask) >> endOffset);
                    array[endArrIndex] &= m;
                }
            }
        }

        public int Length => size;

        public static LitematicaBitArray fromNbtLongArray(long[] arr, int size, int nbits)
        {
            int expected_len = (int)Math.Ceiling((double)size * nbits / 64.0);
            if (expected_len != arr.Length)
            {
                throw new ArgumentException($"long array length does not match bit array size and nbits, expected {expected_len}, not {arr.Length}");
            }
            LitematicaBitArray r = new LitematicaBitArray(size, nbits);
            //long m = (1L << 64) - 1;
            //r.array = arr.Select(i => (int)i & m).ToArray();
            r.array = new long[arr.Length];
            Array.Copy(arr, r.array, arr.Length);
            return r;
        }

        public bool Contains(long value)
        {
            foreach (var v in this)
            {
                if (v == value)
                    return true;
            }
            return false;
        }

        public IEnumerator<long> GetEnumerator()
        {
            for (int i = 0; i < size; i++)
            {
                yield return this[i];
            }
        }

        public List<long> toLongList()
        {
            List<long> list_of_longs = new List<long>();
            long m1 = 1L << 63;
            long m2 = (1L << 64) - 1;
            foreach (long i in this.array)
            {
                long val = (i & m1) > 0
                    ? unchecked((long)(i | ~m2))
                    : unchecked((long)i);
                list_of_longs.Add(val);
            }
            return list_of_longs;
        }
        public long[] toNbtLongArray()
        {
            return toLongList().ToArray();
        }
        public LitematicaBitArray Reversed()
        {
            var arr = new LitematicaBitArray(size, nbits);
            for (int i = 0; i < size; i++)
            {
                arr[i] = this[size - i - 1];
            }
            return arr;
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
