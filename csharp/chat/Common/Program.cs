using System;
using System.Diagnostics;

namespace Common
{
    using Utility;

    internal class Program
    {
        static void Utf8EncoderTest()
        {
            Log.Print($"Utf8EncoderTest 테스트 시작");

            List<(string, string)> parameters = new List<(string, string)>
            {
                (@"0123", @"30313233"),
                (@"abcxyz", @"61626378797a"),
                (@"@!#$%^()[]", @"40212324255e28295b5d"),
                (@"가나다뀕팛", @"eab080eb8298eb8ba4eb8095ed8c9b"),
                (@"凰猫天䬌", @"e587b0e78cabe5a4a9e4ac8c"),
                (@"😂🤣⛴🛬🎁", @"f09f9882f09fa4a3e29bb4f09f9bacf09f8e81"),
            };

            Utf8PayloadEncoder encoder = new Utf8PayloadEncoder();

            for (int i = 0; i < parameters.Count; i++)
            {
                parameters[i] = (parameters[i].Item1, parameters[i].Item2.ToUpper());
            }

            // Encode
            foreach (var parameter in parameters)
            {
                var input = parameter.Item1;
                var output = Convert.ToHexString(encoder.Encode(input));
                var expected = parameter.Item2;

                Debug.Assert(output == expected, $"테스트 실패, input: {input}, output: {output}, expected: {expected}");
            }

            // Decode
            foreach (var parameter in parameters)
            {
                var input = parameter.Item2;
                var inputHex = Convert.FromHexString(input);
                var output = encoder.Decode(inputHex, 0, inputHex.Length);
                var expected = parameter.Item1;

                Debug.Assert(output == expected, $"테스트 실패, input: {input}, output: {output}, expected: {expected}");
            }

            Log.Print($"Utf8EncoderTest 테스트 통과, 테스트케이스 수: {parameters.Count}");
        }

        static void Utf8MessageTest()
        {
            Log.Print($"Utf8MessageTest 테스트 시작");

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

            Log.Print($"Utf8MessageTest 테스트 통과, 테스트케이스 수: {parameters.Count}");
        }

        static void Main(string[] args)
        {
            Utf8EncoderTest();
            Utf8MessageTest();
        }
    }
}