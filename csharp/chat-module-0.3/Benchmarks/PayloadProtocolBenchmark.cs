using System;
using System.Text;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Benchmarks
{
    using Common;

    static class StringGenerator
    {
        public static string GetKorean(int length)
        {
            Random random = new Random();
            StringBuilder stringBuilder = new StringBuilder();
            int rangeStart = 0xAC00, rangeEnd = 0xD7A3;

            for (int i = 0; i < length; i++)
            {
                Rune rune = new Rune(random.Next(rangeStart, rangeEnd));
                stringBuilder.Append(rune);
            }

            return stringBuilder.ToString();
        }

        public static string GetJapanese(int length)
        {
            Random random = new Random();
            StringBuilder stringBuilder = new StringBuilder();
            int rangeStart = 0x4E00, rangeEnd = 0x9fBF;

            for (int i = 0; i < length; i++)
            {
                Rune rune = new Rune(random.Next(rangeStart, rangeEnd));
                stringBuilder.Append(rune);
            }

            return stringBuilder.ToString();
        }
    }

    static class Utf8BytesGenerator
    {

        public static byte[] GetKorean(int length)
        {
            if (length % 3 != 0)
                throw new InvalidDataException($"UTF8 한글은 글자당 3바이트이므로, 바이트는 3의 배수로 입력되어야 한다. current value : {length}");
            return Encoding.UTF8.GetBytes(StringGenerator.GetKorean(length / 3));
        }


        public static byte[] GetJapanese(int length)
        {
            if (length % 3 != 0)
                throw new InvalidDataException($"UTF8 한자는 글자당 3바이트이므로, 바이트는 3의 배수로 입력되어야 한다. current value : {length}");
            return Encoding.UTF8.GetBytes(StringGenerator.GetJapanese(length / 3));
        }
    }


    public class PayloadProtocolEncodeBenchmark
    {
        private const int STRING_LENGTH = 10000;

        private readonly string data;

        public PayloadProtocolEncodeBenchmark()
        {
            data = StringGenerator.GetKorean(STRING_LENGTH);
        }

        [Benchmark]
        public byte[] Utf8Encode() => Utf8PayloadProtocol.Encode(data);
    }

    public class PayloadProtocolDecodeBenchmark
    {
        private const int BYTES_LENGTH = 30000;

        private const int OFFSET = Utf8PayloadProtocol.SIZE_BYTES_LENGTH;
        private const int UTF8_DATA_SIZE = OFFSET + BYTES_LENGTH;
        private readonly byte[] utf8_data;

        public PayloadProtocolDecodeBenchmark()
        {
            utf8_data = new byte[UTF8_DATA_SIZE];
            Buffer.BlockCopy(Utf8BytesGenerator.GetKorean(BYTES_LENGTH), 0, utf8_data, OFFSET, BYTES_LENGTH);
        }

        [Benchmark]
        public string Utf8Decode() => Utf8PayloadProtocol.Decode(utf8_data, UTF8_DATA_SIZE);
    }
}
