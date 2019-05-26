using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Core.Server
{
    public class BinaryContentCreator
    {
        protected List<byte> content = new List<byte>();

        public BinaryContentCreator(byte[] content = null)
        {
            if (content != null)
                this.content.AddRange(content);
        }

        public void AddDouble(double value, int nrBytes, long multiplier)
        {
            if (nrBytes <= 4)
                AddInt((int)(value * multiplier), nrBytes);
            else
                AddLong((long)(value * multiplier), nrBytes);
        }

        public void AddLong(long value, int nrBytes = 8)
        {
            while (nrBytes-- > 0)
            {
                content.Add((byte)(value & 0xFF));
                value >>= 8;
            }
        }

        public void AddInt(int value, int nrBytes = 4)
        {
            while (nrBytes-- > 0)
            {
                content.Add((byte)(value & 0xFF));
                value >>= 8;
            }
        }

        public void AddString(string value)
        {
            content.AddRange(Encoding.UTF8.GetBytes(value));
            content.Add((byte)'\0');
        }

        private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public void AddDateTime(DateTime? value)
        {
            if (value.HasValue)
                AddLong(Convert.ToInt64((value.Value - epoch).TotalMilliseconds), 8);
            else
                AddLong(0, 8);
        }

        public void AddRawBytes(byte[] value)
        {
            content.AddRange(value);
        }

        public void AddByteArray(byte[] value)
        {
            AddInt(value.Length);
            content.AddRange(value);
        }

        public byte[] GetContent()
        {
            return content.ToArray();
        }

        public void AddBytesCRC(byte[] value)
        {
            content.Add(BinaryContentCRC.Instance.ComputeChecksum(value));
        }

        public void AddLongCRC(long value, int nrBytes = 8)
        {
            byte[] vals = new byte[nrBytes];

            for (int i = 0; i < vals.Length; i++)
            {
                vals[i] = (byte)(value & 0xFF);
                value >>= 8;
            }

            AddBytesCRC(vals);
        }

        public void AddDateTimeCRC(DateTime? value)
        {
            if (value.HasValue)
                AddLongCRC(Convert.ToInt64((value.Value - epoch).TotalMilliseconds));
            else
                AddEmptyCRC();
        }

        public void AddEmptyCRC()
        {
            content.Add(0);
        }
    }
}
