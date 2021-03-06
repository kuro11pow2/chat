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

    public static class Utf8PayloadProtocol
    {
        /// <summary>
        /// 메시지 크기 버퍼 최대 길이. 4 bytes 이하 (int 제약)
        /// </summary>
        public const int SIZE_BYTES_LENGTH = 2;

        public const uint MAX_SIZE_BYTES_MASK = 0xFFFF_FFFF - ((1 << (SIZE_BYTES_LENGTH * 8)) - 1);
        /// <summary>
        /// 메시지 버퍼 최대 길이. 2^(8 * SIZE_BYTES_LENGTH)-1
        /// </summary>
        public const int MAX_MESSAGE_BYTES_LENGTH = 1 << (8 * SIZE_BYTES_LENGTH) - 1;

        public static void GetPayloadBytes(string str, out byte[] sizeBytes, out byte[] messageBytes)
        {
            messageBytes = EncodePayload(str);
            sizeBytes = EncodeSizeBytes(messageBytes.Length);
        }

        private static void MergeBytes(out byte[] fullBytes, byte[] sizeBytes, byte[] messageBytes)
        {
            fullBytes = new byte[sizeBytes.Length + messageBytes.Length];

            Buffer.BlockCopy(sizeBytes, 0, fullBytes, 0, sizeBytes.Length);
            Buffer.BlockCopy(messageBytes, 0, fullBytes, sizeBytes.Length, messageBytes.Length);
        }

        public static byte[] EncodePayload(string str)
        {
            byte[] tmp = Encoding.UTF8.GetBytes(str);
            if (tmp.Length > MAX_MESSAGE_BYTES_LENGTH)
                throw new ProtocolBufferOverflowException($"MESSAGE_BYTES {MAX_MESSAGE_BYTES_LENGTH} bytes 초과");
            return Encoding.UTF8.GetBytes(str);
        }

        public static string DecodePayload(byte[] bytes, int start, int length)
        {
            if (length - start > MAX_MESSAGE_BYTES_LENGTH)
                throw new ProtocolBufferOverflowException($"MESSAGE_BYTES {MAX_MESSAGE_BYTES_LENGTH} bytes 초과");
            return Encoding.UTF8.GetString(new Span<byte>(bytes).Slice(start, length));
        }

        public static byte[] EncodeSizeBytes(int num)
        {
            if (0 != (num & MAX_SIZE_BYTES_MASK))
                throw new ProtocolBufferOverflowException($"SIZE_BYTES {SIZE_BYTES_LENGTH} bytes 초과");

            return BitConverter.GetBytes(num)[..SIZE_BYTES_LENGTH];
        }

        public static int DecodeSizeBytes(byte[] sizeBytes, int start, int length)
        {
            if (length - start != SIZE_BYTES_LENGTH)
                throw new ProtocolBufferOverflowException($"SIZE_BYTES {SIZE_BYTES_LENGTH} bytes 아님");

            int ret = 0;
            for (int i = start + length - 1; i >= start ; i--)
            {
                ret <<= 8;
                ret += sizeBytes[i];
            }
            return ret;
        }

        public static byte[] Encode(string str)
        {
            GetPayloadBytes(str, out byte[] sizeBytes, out byte[] messageBytes);
            MergeBytes(out byte[] fullBytes, sizeBytes, messageBytes);

            return fullBytes;
        }

        public static string Decode(byte[] fullBytes, int bytesLength)
        {
            return DecodePayload(fullBytes, SIZE_BYTES_LENGTH, bytesLength - SIZE_BYTES_LENGTH);
        }
    }
}
