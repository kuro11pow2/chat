using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class ProtocolBufferOverflowException : Exception
    {
        public ProtocolBufferOverflowException()
        {
        }

        public ProtocolBufferOverflowException(string message)
            : base(message)
        {
        }

        public ProtocolBufferOverflowException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public static class PayloadProtocol
    {
        /// <summary>
        /// 메시지 크기 버퍼 최대 길이. 4 bytes 이하 (int 제약)
        /// </summary>
        public const int MAX_SIZE_BYTES_LENGTH = 1;
        /// <summary>
        /// 메시지 버퍼 최대 길이. 2^(8 * MAX_SIZE_BYTES_LENGTH)-1
        /// </summary>
        public const int MAX_MESSAGE_BYTES_LENGTH = 1 << (8 * MAX_SIZE_BYTES_LENGTH) - 1;

        public static byte[] EncodeSizeBytes(int num)
        {
            return BitConverter.GetBytes(num)[..MAX_SIZE_BYTES_LENGTH];
        }

        public static int DecodeSizeBytes(byte[] sizeBytes)
        {
            int ret = 0;
            for (int i = 0; i < sizeBytes.Length; i++)
            {
                ret <<= 1;
                ret += sizeBytes[i];
            }
            return ret;
        }
    }
}
