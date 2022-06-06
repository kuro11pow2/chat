using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Common
{
    using Interface;

    public class Utf8Message : IMessage
    {
        public static IEncoder Encoder = new Utf8PayloadEncoder();
        private string Str = "";
        private ReadOnlyMemory<byte> Bytes;

        public void SetString(string str)
        {
            Str = str;
            Bytes = Encoder.Encode(str);
        }

        public void SetBytes(byte[] bytes, int index, int count)
        {
            Str = Encoder.Decode(bytes, index, count);
            Bytes = bytes;
        }


        public ReadOnlySpan<byte> GetBytes()
        {
            return Bytes.Span;
        }

        public string GetString() 
        {
            return Str;
        }

        public override string ToString()
        {
            return GetString();
        }

        public string GetInfo()
        {
            return $"Utf8Message: {Str}, Bytes: {Convert.ToHexString(Bytes.Span)}";
        }
    }
}
