﻿namespace Tests
{
    class FakeConnectionContext : IConnectionContext
    {
        private ReadOnlyMemory<byte> _buffer;

        public string ConnectionId { get; }
        public bool IsConnected { get; set; }

        public FakeConnectionContext(string connectionId = "", bool isConnected = true)
        {
            ConnectionId = connectionId;
            IsConnected = isConnected;
        }

        public void Close()
        {
        }

        public Task<int> ReadAsync(byte[] buffer, int offset, int count)
        {
            return Task.Run(() =>
            {
                Buffer.BlockCopy(_buffer.ToArray(), 0, buffer, offset, count);
                _buffer = _buffer.Slice(count);
                return count;
            });
        }

        public ValueTask WriteAsync(ReadOnlyMemory<byte> buffer)
        {
            _buffer = buffer;
            return ValueTask.CompletedTask;
        }
    }

    public class UserTests
    {
        [Theory]
        [InlineData(@"0123", @"0123")]
        [InlineData(@"abcxyz", @"abcxyz")]
        [InlineData(@"@!#$%^()[]", @"@!#$%^()[]")]
        [InlineData(@"가나다뀕팛", @"가나다뀕팛")]
        [InlineData(@"😂🤣⛴🛬🎁", @"😂🤣⛴🛬🎁")]
        [InlineData(@"1111000011110000111100001111000011110000111100001111000011110000111100001111000011110000111100001111000011110000111100001111000011110000111100001111000011110000111100001111000011110000111100001111000011110000111100001111000011110000111100001111000011110000999", @"1111000011110000111100001111000011110000111100001111000011110000111100001111000011110000111100001111000011110000111100001111000011110000111100001111000011110000111100001111000011110000111100001111000011110000111100001111000011110000111100001111000011110000999")]
        [InlineData(@"1234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234", @"1234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234")]
        static async Task ChatUserTest(string input, string expected)
        {
            Config Config = new Config();
            FakeConnectionContext connection = new FakeConnectionContext();
            User user = new User(Config.ServerAddress, Config.Port, connectionContext: connection);

            IMessage message = new Utf8Message();
            message.SetMessage(input);

            await user.Send(message);
            var output = await user.Receive();
            var output_message = output.GetMessage();

            Assert.Equal(expected, output_message);
        }
    }
}