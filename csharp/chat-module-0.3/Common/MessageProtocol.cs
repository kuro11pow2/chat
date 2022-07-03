using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    using Interface;
    public enum MessageType
    {
        REQUEST_OFFSET,
        PING,
        BROADCAST,

        RESPONSE_OFFSET = 100,
        SUCCESS,
        FAILURE,
    }
    public static class MessageProtocol
    {
        /// <summary>
        /// 타입 표시 버퍼 최대 길이. 4 bytes 이하 (int 제약)
        /// </summary>
        public const int TYPE_BYTES_LENGTH = 1;

        public const uint TYPE_BYTES_MASK = 0xFFFF_FFFF - ((1 << (TYPE_BYTES_LENGTH * 8)) - 1);

        public static void GetPayloadBytes(MessageType type, string str, out byte[] sizeBytes, out byte[] typeBytes, out byte[] messageBytes)
        {
            messageBytes = Utf8PayloadProtocol.EncodePayload(str);
            typeBytes = EncodeMessageType(type);
            sizeBytes = Utf8PayloadProtocol.EncodeSizeBytes(messageBytes.Length);
        }


        private static void MergeBytes(out byte[] fullBytes, byte[] sizeBytes, byte[] typeBytes, byte[] messageBytes)
        {
            fullBytes = new byte[sizeBytes.Length + typeBytes.Length + messageBytes.Length];

            Buffer.BlockCopy(sizeBytes, 0, fullBytes, 0, sizeBytes.Length);
            Buffer.BlockCopy(typeBytes, 0, fullBytes, sizeBytes.Length, typeBytes.Length);
            Buffer.BlockCopy(messageBytes, 0, fullBytes, sizeBytes.Length + typeBytes.Length, messageBytes.Length);
        }

        public static IMessage GetMessage(MessageType type, string str="")
        {
            GetPayloadBytes(type, str, out byte[] sizeBytes, out byte[] typeBytes, out byte[] messageBytes);
            MergeBytes(out byte[] fullBytes, sizeBytes, typeBytes, messageBytes);

            IMessage message = new Utf8Message();
            message.SetBytes(fullBytes, fullBytes.Length);

            return message;
        }

        public static MessageType GetType(IMessage message)
        {
            return DecodeMessageType(message.GetFullBytes().Span, 0, TYPE_BYTES_LENGTH);
        }

        private static byte[] EncodeMessageType(MessageType type)
        {
            if (0 != ((uint)type & TYPE_BYTES_MASK))
                throw new ProtocolBufferOverflowException($"TYPE_BYTES_MASK {TYPE_BYTES_MASK} bytes 초과");

            return BitConverter.GetBytes((uint)type);
        }

        private static MessageType DecodeMessageType(Span<byte> bytes, int start, int length)
        {
            if (length - start > TYPE_BYTES_LENGTH)
                throw new ProtocolBufferOverflowException($"TYPE_BYTES_LENGTH {TYPE_BYTES_LENGTH} bytes 초과");

            return (MessageType)BitConverter.ToUInt32(bytes.Slice(start, length));
        }
    }
}
