using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Core.Server
{
    public class BinaryContentReader
    {
        protected byte[] data;
        public int CurrentIndex { get; protected set; }

        public BinaryContentReader(byte[] data)
        {
            this.data = data;
            this.CurrentIndex = 0;
        }

        public double ReadDouble(int nrBytes, long multiplier)
        {
            double v;

            if (nrBytes <= 4)
                v = ReadInt(nrBytes);
            else
                v = ReadLong(nrBytes);

            return v / multiplier;
        }

        public int ReadInt(int nrBytes)
        {
            int ret = 0;
            int maxBytes = 4;

            for (int i = 0; i < maxBytes; i++)
            {
                ret >>= 8;
                if (i < nrBytes)
                {
                    ret = (ret & 0x00FFFFFF) | (data[CurrentIndex++] << ((maxBytes - 1) * 8));
                }
            }

            return ret;
        }

        public long ReadLong(int nrBytes)
        {
            long ret = 0;
            int maxBytes = 8;

            for (int i = 0; i < maxBytes; i++)
            {
                ret >>= 8;
                if (i < nrBytes)
                {
                    ret = (ret & 0x00FFFFFFFFFFFFFF) | (((long)data[CurrentIndex++]) << ((maxBytes - 1) * 8));
                }
            }

            return ret;
        }

        public string ReadString()
        {
            int endi = CurrentIndex;

            while (data[endi] != (byte)'\0')
            {
                endi++;
                if (endi >= data.Length)
                    return "";
            }

            string ret = Encoding.UTF8.GetString(data, CurrentIndex, endi - CurrentIndex);

            CurrentIndex = endi + 1;

            return ret;
        }

        private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public DateTime? ReadDateTime()
        {
            long millis = ReadLong(8);

            if (millis == 0) return null;

            return epoch.AddMilliseconds(millis);
        }

        public byte[] ReadRawBytes(int nrBytes)
        {
            byte[] ret = new byte[nrBytes];

            Array.Copy(data, CurrentIndex, ret, 0, ret.Length);

            CurrentIndex += nrBytes;

            return ret;
        }

        public byte[] ReadByteArray()
        {
            byte[] ret = new byte[ReadInt(4)];

            Array.Copy(data, CurrentIndex, ret, 0, ret.Length);

            CurrentIndex += ret.Length;

            return ret;
        }

        public bool ReadAndVerifyBytesCRC(byte[] values)
        {
            byte crc = data[CurrentIndex++];
            if (crc == 0) return true;
            return crc == BinaryContentCRC.Instance.ComputeChecksum(values);
        }

        public bool ReadAndVerifyLongCRC(long value, int nrBytes = 8)
        {
            byte[] vals = new byte[nrBytes];

            for (int i = 0; i < vals.Length; i++)
            {
                vals[i] = (byte)(value & 0xFF);
                value >>= 8;
            }

            return ReadAndVerifyBytesCRC(vals);
        }

        public bool ReadAndVerifyDateTimeCRC(DateTime value)
        {
            return ReadAndVerifyLongCRC(Convert.ToInt64((value - epoch).TotalMilliseconds));
        }

        public bool HasBytes(int nrBytes)
        {
            return (data.Length - CurrentIndex) > 0;
        }
    }
}
