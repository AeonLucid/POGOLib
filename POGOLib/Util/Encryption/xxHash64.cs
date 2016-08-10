// xxHash64 port by https://github.com/AeonLucid
// Ported from https://github.com/Cyan4973/xxHash/blob/16bbb84a69d7560fdff1c0560e39537045148cbe/xxhash.c#L436-L502

using System;

namespace POGOLib.Util.Encryption
{
    public class xxHash64
    {
        public struct XXH64_State
        {
            public ulong total_len;
            public ulong seed;
            public ulong v1;
            public ulong v2;
            public ulong v3;
            public ulong v4;
            public int memsize;
            public byte[] memory;
        };

        const ulong PRIME64_1 = 11400714785074694791UL;
        const ulong PRIME64_2 = 14029467366897019727UL;
        const ulong PRIME64_3 = 1609587929392839161UL;
        const ulong PRIME64_4 = 9650029242287828579UL;
        const ulong PRIME64_5 = 2870177450012600261UL;

        protected XXH64_State _state;
        public xxHash64()
        {

        }

        public static ulong CalculateHash(byte[] buf, int len = -1, ulong seed = 0)
        {
            ulong h64;
            int index = 0;
            if (len == -1)
            {
                len = buf.Length;
            }


            if (len >= 32)
            {
                int limit = len - 32;
                ulong v1 = seed + PRIME64_1 + PRIME64_2;
                ulong v2 = seed + PRIME64_2;
                ulong v3 = seed + 0;
                ulong v4 = seed - PRIME64_1;

                do
                {
                    v1 = XXH64_round(v1, buf, index);
                    index += 8;
                    v2 = XXH64_round(v2, buf, index);
                    index += 8;
                    v3 = XXH64_round(v3, buf, index);
                    index += 8;
                    v4 = XXH64_round(v4, buf, index);
                    index += 8;
                } while (index <= limit);

                h64 = XXH_rotl64(v1, 1) + XXH_rotl64(v2, 7) + XXH_rotl64(v3, 12) + XXH_rotl64(v4, 18);
                h64 = XXH64_mergeRound(h64, v1);
                h64 = XXH64_mergeRound(h64, v2);
                h64 = XXH64_mergeRound(h64, v3);
                h64 = XXH64_mergeRound(h64, v4);
            }
            else
            {
                h64 = seed + PRIME64_5;
            }

            h64 += (ulong)len;

            while (index <= len - 8)
            {
                ulong k1 = XXH64_round(0, buf, index);
                h64 ^= k1;
                h64 = XXH_rotl64(h64, 27) * PRIME64_1 + PRIME64_4;
                index += 8;
            }

            if (index <= len - 4)
            {
                uint why = BitConverter.ToUInt32(buf, index);
                h64 ^= why*PRIME64_1;
                h64 = XXH_rotl64(h64, 23)*PRIME64_2 + PRIME64_3;
                index += 4;
            }

            while (index < len)
            {
                h64 ^= buf[index] * PRIME64_5;
                h64 = XXH_rotl64(h64, 11) * PRIME64_1;
                index++;
            }

            h64 ^= h64 >> 33;
            h64 *= PRIME64_2;
            h64 ^= h64 >> 29;
            h64 *= PRIME64_3;
            h64 ^= h64 >> 32;

            return h64;
        }

        private static ulong XXH_rotl64(ulong value, int count)
        {
            return (value << count) | (value >> (64 - count));
        }

        private static ulong XXH64_round(ulong value, byte[] buf, int index)
        {
            ulong input = BitConverter.ToUInt64(buf, index);
            return XXH64_round(value, input);
        }

        private static ulong XXH64_round(ulong acc, ulong input)
        {
            acc += input * PRIME64_2;
            acc = XXH_rotl64(acc, 31);
            acc *= PRIME64_1;
            return acc;
        }

        private static ulong XXH64_mergeRound(ulong acc, ulong val)
        {
            val = XXH64_round(0, val);
            acc ^= val;
            acc = acc * PRIME64_1 + PRIME64_4;
            return acc;
        }

    }
}