using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Interface
{
    public interface IMessage
    {
        public void SetString(string str);
        public void SetBytes(byte[] bytes, int messageLength);
        public string GetString();
        public ReadOnlyMemory<byte> GetFullBytes();
        public int GetFullBytesLength();
        public string GetInfo();

    }
}
