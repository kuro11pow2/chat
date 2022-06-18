﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Common
{
    using Interface;

    public class Utf8Message : IMessage
    {
        private string Str = "";
        private Memory<byte> FullBytes;

        public void SetMessage(string str)
        {
            Str = str;
            FullBytes = Utf8PayloadProtocol.Encode(str);
        }

        public void SetBytes(byte[] fullBytes, int messageLength)
        {
            Str = Utf8PayloadProtocol.Decode(fullBytes, messageLength);
            FullBytes = new Memory<byte>(fullBytes, 0, messageLength);
        }


        public ReadOnlyMemory<byte> GetFullBytes()
        {
            return FullBytes;
        }

        public int GetFullBytesLength()
        {
            return FullBytes.Length;
        }

        public string GetMessage() 
        {
            return Str;
        }

        public override string ToString()
        {
            return GetMessage();
        }

        public string GetInfo()
        {
            return $"Utf8Message: {Str}, Bytes: {Convert.ToHexString(FullBytes.ToArray())}, BytesLength: {GetFullBytesLength()}";
        }
    }
}
