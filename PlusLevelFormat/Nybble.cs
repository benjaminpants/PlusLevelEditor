﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PlusLevelFormat
{
    // TODO: Write Nybble array writer and Nybble array reader for BinaryReader and BinaryWriter
    public static class NybbleExtensions
    {
        public static byte MergeWith(this Nybble left, Nybble right)
        {
            return Nybble.MergeIntoByte(left, right);
        }
        public static Nybble[] Split(this byte me)
        {
            return Nybble.SplitIntoNybbles(me);
        }
        public static void Write(this BinaryWriter writer, Nybble[] nybbles)
        {
            writer.Write(nybbles.Length);
            for (int i = 0; i < nybbles.Length; i += 2)
            {
                if (i + 1 < nybbles.Length)
                {
                    byte mergedByte = Nybble.MergeIntoByte(nybbles[i], nybbles[i + 1]);
                    writer.Write(mergedByte);
                }
                else
                {
                    // If the array has an odd length, write the last Nybble as a single byte
                    writer.Write((byte)(((byte)nybbles[i]) << 4));
                }
            }
        }

        public static Nybble[] ReadNybbles(this BinaryReader reader)
        {
            int nybbleCount = reader.ReadInt32();
            List<Nybble> nybbles = new List<Nybble>();
            for (int i = 0; i < nybbleCount; i += 2)
            {
                Nybble[] pair = reader.ReadByte().Split();
                if ((i + 1) < nybbleCount)
                {
                    nybbles.AddRange(pair);
                }
                else
                {
                    nybbles.Add(pair[0]);
                }
            }
            return nybbles.ToArray();
        }
    }


    // a nybble represents one half of a byte (4 bits).
    // BB+ uses nybbles to store the state of each wall, one bit determining if a wall is on or off.
    public struct Nybble
    {
        private byte _internal;

        public Nybble(int value)
        {
            _internal = (byte)(value & 0b_0000_1111);
        }

        public static byte MergeIntoByte(Nybble left, Nybble right)
        {
            return (byte)((left << 4) + (right));
        }

        public static Nybble[] SplitIntoNybbles(byte toSplit)
        {
            Nybble[] result = new Nybble[2];
            result[0] = new Nybble((toSplit & 0b_1111_0000) >> 4);
            result[1] = new Nybble(toSplit & 0b_0000_1111);
            return result;
        }

        public override int GetHashCode()
        {
            return _internal.GetHashCode();
        }

        public override string ToString()
        {
            return _internal.ToString();
        }

        public static Nybble operator +(Nybble a, Nybble b) => new Nybble(a._internal + b._internal);

        public static Nybble operator -(Nybble a, Nybble b) => new Nybble(a._internal - b._internal);

        public static Nybble operator &(Nybble a, Nybble b) => new Nybble(a._internal & b._internal);

        public static Nybble operator |(Nybble a, Nybble b) => new Nybble(a._internal | b._internal);

        public static implicit operator byte(Nybble a) => a._internal;

        public static implicit operator int(Nybble a) => a._internal;

        public static explicit operator Nybble(byte v) => new Nybble(v);
    }
}
