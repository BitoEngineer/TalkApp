using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Core.Server
{
    public class BinaryContentCRC
    {
        static byte[] table = new byte[256];
        const byte poly = 0x31;

        private static BinaryContentCRC _instance = new BinaryContentCRC();

        public static BinaryContentCRC Instance { get { return _instance; } }

        private BinaryContentCRC()
        {
            for (int i = 0; i < 256; ++i)
            {
                int temp = i;
                for (int j = 0; j < 8; ++j)
                {
                    if ((temp & 0x80) != 0)
                    {
                        temp = (temp << 1) ^ poly;
                    }
                    else
                    {
                        temp <<= 1;
                    }
                }
                table[i] = (byte)temp;
            }
        }

        public byte ComputeChecksum(params byte[] bytes)
        {
            byte crc = 0x56;
            if (bytes != null && bytes.Length > 0)
            {
                foreach (byte b in bytes)
                {
                    crc = table[crc ^ b];
                }
            }
            return crc;
        }
    }
}

