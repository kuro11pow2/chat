using System;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Benchmarks
{
    public class Md5VsSha256
    {
        private const int N = 10000;
        private readonly byte[] data;

        private readonly SHA256 sha256 = SHA256.Create();
        private readonly MD5 md5 = MD5.Create();

        public Md5VsSha256()
        {
            data = new byte[N];
            new Random(42).NextBytes(data);
        }

        [Benchmark]
        public byte[] Sha256() => sha256.ComputeHash(data);

        [Benchmark]
        public byte[] Md5() => md5.ComputeHash(data);
    }

    public class Program
    {
        public static void Main(string[] args)
        {
#if RELEASE      
            BenchmarkDotNet.Reports.Summary sum;
            sum = BenchmarkRunner.Run<PayloadProtocolEncodeBenchmark>();
            //sum = BenchmarkRunner.Run<PayloadProtocolDecodeBenchmark>();
#elif DEBUG
            var encode = new Utf8PayloadProtocolEncodeBenchmark();
            var encode_out = encode.Encode();
            var decode = new Utf8PayloadProtocolDecodeBenchmark();
            var decode_out = decode.Decode();
#endif
        }
    }
}