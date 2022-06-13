using System.Reflection;
using System.Diagnostics;

namespace Common
{
    using Utility;

    internal class Program
    {
        static void Utf8PayloadProtocol_EncodeTest()
        {
            Log.Print($"{MethodBase.GetCurrentMethod()?.Name} 테스트 시작");

            List<(string, string)> parameters = new List<(string, string)>
            {
                (@"0123", @"30313233"),
                (@"abcxyz", @"61626378797a"),
                (@"@!#$%^()[]", @"40212324255e28295b5d"),
                (@"가나다뀕팛", @"eab080eb8298eb8ba4eb8095ed8c9b"),
                (@"凰猫天䬌", @"e587b0e78cabe5a4a9e4ac8c"),
                (@"😂🤣⛴🛬🎁", @"f09f9882f09fa4a3e29bb4f09f9bacf09f8e81"),
            };

            for (int i = 0; i < parameters.Count; i++)
            {
                parameters[i] = (parameters[i].Item1, parameters[i].Item2.ToUpper());
            }

            // Encode
            foreach (var parameter in parameters)
            {
                var input = parameter.Item1;
                var output = Convert.ToHexString(Utf8PayloadProtocol.Encode(input));
                var expected = parameter.Item2;

                Debug.Assert(output == expected, $"테스트 실패, input: {input}, output: {output}, expected: {expected}");
            }

            Log.Print($"{MethodBase.GetCurrentMethod()?.Name} 테스트 통과, 테스트케이스 수: {parameters.Count}");
        }

        static void Utf8PayloadProtocol_DecodeTest()
        {
            Log.Print($"{MethodBase.GetCurrentMethod()?.Name} 테스트 시작");

            List<(string, string)> parameters = new List<(string, string)>
            {
                (@"30313233", @"0123"),
                (@"61626378797a", @"abcxyz"),
                (@"40212324255e28295b5d", @"@!#$%^()[]"),
                (@"eab080eb8298eb8ba4eb8095ed8c9b", @"가나다뀕팛"),
                (@"e587b0e78cabe5a4a9e4ac8c", @"凰猫天䬌"),
                (@"f09f9882f09fa4a3e29bb4f09f9bacf09f8e81", @"😂🤣⛴🛬🎁"),
            };

            for (int i = 0; i < parameters.Count; i++)
            {
                parameters[i] = (parameters[i].Item1.ToUpper(), parameters[i].Item2);
            }

            // Decode
            foreach (var parameter in parameters)
            {
                var input = parameter.Item1;
                var inputHex = Convert.FromHexString(input);
                var output = Utf8PayloadProtocol.Decode(inputHex, 0, inputHex.Length);
                var expected = parameter.Item2;

                Debug.Assert(output == expected, $"테스트 실패, input: {input}, output: {output}, expected: {expected}");
            }

            Log.Print($"{MethodBase.GetCurrentMethod()?.Name} 테스트 통과, 테스트케이스 수: {parameters.Count}");
        }

        static void Utf8PayloadProtocol_EncodeSizeBytesTest()
        {
            Log.Print($"{MethodBase.GetCurrentMethod()?.Name} 테스트 시작");

            List<(int, string)> parameters = new List<(int, string)>
            {
                (4, @"0400"),
                (6, @"0600"),
                (10, @"0a00"),
                (15, @"0f00"),
                (12, @"0c00"),
                (19, @"1300"),
            };

            // Encode
            foreach (var parameter in parameters)
            {
                var input = parameter.Item1;
                var output = Convert.ToHexString(Utf8PayloadProtocol.EncodeSizeBytes(input)).ToLower();
                var expected = parameter.Item2;

                Debug.Assert(output == expected, $"테스트 실패, input: {input}, output: {output}, expected: {expected}");
            }

            Log.Print($"{MethodBase.GetCurrentMethod()?.Name} 테스트 통과, 테스트케이스 수: {parameters.Count}");
        }
        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        static void Utf8PayloadProtocol_DecodeSizeBytesTest()
        {
            Log.Print($"{MethodBase.GetCurrentMethod()?.Name} 테스트 시작");

            List<(string, int) > parameters = new List<(string, int)>
            {
                (@"0400", 4),
                (@"0600", 6),
                (@"0a00", 10),
                (@"0f00", 15),
                (@"0c00", 12),
                (@"1300", 19),
            };

            // Encode
            foreach (var parameter in parameters)
            {
                var input = StringToByteArray(parameter.Item1);
                var output = Utf8PayloadProtocol.DecodeSizeBytes(input);
                var expected = parameter.Item2;

                Debug.Assert(output == expected, $"테스트 실패, input: {input}, output: {output}, expected: {expected}");
            }

            Log.Print($"{MethodBase.GetCurrentMethod()?.Name} 테스트 통과, 테스트케이스 수: {parameters.Count}");
        }

        static void Utf8MessageTest()
        {
            Log.Print($"{MethodBase.GetCurrentMethod()?.Name} 테스트 시작");

            List<(string, string)> parameters = new List<(string, string)>
            {
                (@"01234", @"01234"),
                (@"abcxyz", @"abcxyz"),
                (@"@!#$%^()[]", @"@!#$%^()[]"),
                (@"가나다뀕팛", @"가나다뀕팛"),
                (@"凰猫天䬌", @"凰猫天䬌"),
                (@"😂🤣⛴🛬🎁", @"😂🤣⛴🛬🎁"),
            };

            foreach (var parameter in parameters)
            {
                var input = parameter.Item1;
                var expected = parameter.Item2;

                Utf8Message message = new Utf8Message();
                message.SetString(input);
                var output = message.ToString();
                Debug.Assert(output == expected, $"테스트 실패, input: {input}, output: {output}, expected: {expected}");
            }

            Log.Print($"{MethodBase.GetCurrentMethod()?.Name} 테스트 통과, 테스트케이스 수: {parameters.Count}");
        }

        static void Main(string[] args)
        {
            Utf8PayloadProtocol_EncodeTest();
            Utf8PayloadProtocol_DecodeTest();
            Utf8PayloadProtocol_EncodeSizeBytesTest();
            Utf8PayloadProtocol_DecodeSizeBytesTest();
            Utf8MessageTest();
        }
    }
}