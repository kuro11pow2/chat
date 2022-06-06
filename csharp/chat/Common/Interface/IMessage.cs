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
        public void SetBytes(byte[] bytes, int index, int count);
        public string GetString();
        public ReadOnlySpan<byte> GetBytes();
        public string GetInfo();

    }
}
