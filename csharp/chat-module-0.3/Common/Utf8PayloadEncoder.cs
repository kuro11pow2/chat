using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    

    public class Utf8PayloadEncoder : Interface.IEncoder
    {

        public string Decode(byte[] bytes, int index, int count)
        {
            if (bytes.Length > PayloadProtocol.MAX_MESSAGE_BYTES_LENGTH)
                throw new ProtocolBufferOverflowException($"{PayloadProtocol.MAX_MESSAGE_BYTES_LENGTH} bytes 초과");
            return Encoding.UTF8.GetString(bytes, index, count);
        }

        public byte[] Encode(string str)
        {
            byte[] tmp = Encoding.UTF8.GetBytes(str);
            if (tmp.Length > PayloadProtocol.MAX_MESSAGE_BYTES_LENGTH)
                throw new ProtocolBufferOverflowException($"{PayloadProtocol.MAX_MESSAGE_BYTES_LENGTH} bytes 초과");
            return Encoding.UTF8.GetBytes(str);
        }
    }
}
