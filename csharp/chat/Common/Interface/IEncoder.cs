using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Interface
{
    public interface IEncoder
    {
        string Decode(byte[] bytes, int index, int count);

        byte[] Encode(string str);
    }
}
