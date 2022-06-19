namespace Tests
{
    public class Utf8MessageTests
    {
        [Theory]
        [InlineData(@"0123", @"0123")]
        [InlineData(@"abcxyz", @"abcxyz")]
        [InlineData(@"@!#$%^()[]", @"@!#$%^()[]")]
        [InlineData(@"가나다뀕팛", @"가나다뀕팛")]
        [InlineData(@"😂🤣⛴🛬🎁", @"😂🤣⛴🛬🎁")]
        static void Utf8MessageTest(string input, string expected)
        {
            Utf8Message message = new Utf8Message();
            message.SetMessage(input);
            var actual = message.ToString();
            Assert.Equal(actual, expected);
        }
    }
}