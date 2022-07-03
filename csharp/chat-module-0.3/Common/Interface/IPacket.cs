using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Interface
{
    public interface IPacket
    {
        public void Set(Message message);
        public void Set(string str);
        public void Set(byte[] bytes, int messageLength);
        public string GetRawString();
        public Memory<byte> GetFullBytes();
        public int GetFullBytesLength();
        public string GetInfo();
    }
}
