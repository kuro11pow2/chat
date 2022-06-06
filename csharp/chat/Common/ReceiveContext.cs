using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class ReceiveContext
    {
        public int expectedMessageBytesLength { get; set; }
        public byte[] sizeBytes { get; set; }
        public byte[] messageBytes { get; set; }
        public byte[] fullBytes { get; set; }
        public string messageStr { get; set; }

        public ReceiveContext()
        {
            expectedMessageBytesLength = 0;
            sizeBytes = new byte[PayloadProtocol.MAX_SIZE_BYTES_LENGTH];
            messageBytes = new byte[PayloadProtocol.MAX_MESSAGE_BYTES_LENGTH];
            fullBytes = new byte[PayloadProtocol.MAX_SIZE_BYTES_LENGTH + PayloadProtocol.MAX_MESSAGE_BYTES_LENGTH];
            messageStr = "";
        }

        private string GetBytes2HexStr(byte[] bytes, int len)
        {
            if (bytes.Length < len)
                return $"GetBytes2HexStr: 주어진 바이트 배열의 길이가 최대 길이를 초과함. {bytes.Length} < {len}";

            StringBuilder sb = new StringBuilder();
            sb.Append(BitConverter.ToString(bytes[..len]));

            return sb.ToString();
        }

        public override string ToString()
        {
            return $" [{nameof(ReceiveContext)}]\n{nameof(expectedMessageBytesLength)}: {expectedMessageBytesLength}\n{nameof(sizeBytes)}: {GetBytes2HexStr(sizeBytes, PayloadProtocol.MAX_SIZE_BYTES_LENGTH)}\n{nameof(messageBytes)}: {GetBytes2HexStr(messageBytes, expectedMessageBytesLength)}\n{nameof(fullBytes)}: {GetBytes2HexStr(fullBytes, PayloadProtocol.MAX_SIZE_BYTES_LENGTH + expectedMessageBytesLength)}\n{nameof(messageStr)}: {messageStr}";
        }
    }
}
