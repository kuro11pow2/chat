﻿namespace Tests
{
    public class Utf8PayloadProtocolTests
    {
        [Theory]
        [InlineData(@"0123", @"30313233")]
        [InlineData(@"abcxyz", @"61626378797a")]
        [InlineData(@"@!#$%^()[]", @"40212324255e28295b5d")]
        [InlineData(@"가나다뀕팛", @"eab080eb8298eb8ba4eb8095ed8c9b")]
        [InlineData(@"😂🤣⛴🛬🎁", @"f09f9882f09fa4a3e29bb4f09f9bacf09f8e81")]
        public void Utf8PayloadProtocol_EncodeTest(string input, string expected)
        {
            expected = expected.ToUpper();
            var actual = Convert.ToHexString(Utf8PayloadProtocol.EncodeMessage(input));
            Assert.Equal(actual, expected);
        }

        [Theory]
        [InlineData(@"30313233", @"0123")]
        [InlineData(@"61626378797a", @"abcxyz")]
        [InlineData(@"40212324255e28295b5d", @"@!#$%^()[]")]
        [InlineData(@"eab080eb8298eb8ba4eb8095ed8c9b", @"가나다뀕팛")]
        [InlineData(@"f09f9882f09fa4a3e29bb4f09f9bacf09f8e81", @"😂🤣⛴🛬🎁")]
        public void Utf8PayloadProtocol_DecodeTest(string input, string expected)
        {
            input = input.ToUpper();
            var inputHex = Convert.FromHexString(input);
            var actual = Utf8PayloadProtocol.DecodeMessage(inputHex, 0, inputHex.Length);
            Assert.Equal(actual, expected);
        }

        [Theory]
        [InlineData(4, @"0400")]
        [InlineData(6, @"0600")]
        [InlineData(10, @"0a00")]
        [InlineData(15, @"0f00")]
        [InlineData(12, @"0c00")]
        [InlineData(19, @"1300")]
        static void Utf8PayloadProtocol_EncodeSizeBytesTest(int input, string expected)
        {
            var actual = Convert.ToHexString(Utf8PayloadProtocol.EncodeSizeBytes(input)).ToLower();
            Assert.Equal(actual, expected);
        }

        [Theory]
        [InlineData(@"0400", 4)]
        [InlineData(@"0600", 6)]
        [InlineData(@"0a00", 10)]
        [InlineData(@"0f00", 15)]
        [InlineData(@"0c00", 12)]
        [InlineData(@"1300", 19)]
        static void Utf8PayloadProtocol_DecodeSizeBytesTest(string input, int expected)
        {
            var processedInput = Enumerable.Range(0, input.Length)
                                    .Where(x => x % 2 == 0)
                                    .Select(x => Convert.ToByte(input.Substring(x, 2), 16))
                                    .ToArray();
            var actual = Utf8PayloadProtocol.DecodeSizeBytes(processedInput, 0, processedInput.Length);
            Assert.Equal(actual, expected);
        }
    }
}