﻿namespace Tests
{
    public class RoomTests
    {
        [Theory]
        [InlineData(@"0123", @"0123")]
        [InlineData(@"abcxyz", @"abcxyz")]
        [InlineData(@"@!#$%^()[]", @"@!#$%^()[]")]
        [InlineData(@"가나다뀕팛", @"가나다뀕팛")]
        [InlineData(@"😂🤣⛴🛬🎁", @"😂🤣⛴🛬🎁")]
        [InlineData(@"1111000011110000111100001111000011110000111100001111000011110000111100001111000011110000111100001111000011110000111100001111000011110000111100001111000011110000111100001111000011110000111100001111000011110000111100001111000011110000111100001111000011110000999", @"1111000011110000111100001111000011110000111100001111000011110000111100001111000011110000111100001111000011110000111100001111000011110000111100001111000011110000111100001111000011110000111100001111000011110000111100001111000011110000111100001111000011110000999")]
        [InlineData(@"1234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234", @"1234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234123412341234")]
        static async Task LocalRoomTest(string input, string expected)
        {
            int port = 1234;
            int clientCount = 3;

            Room room = new Room("room0", port);
            _ = room.Run();
            _ = room.RunMonitor();

            List<Client> clients = new List<Client>();
            for (int i = 0; i < clientCount; i++)
            {
                clients.Add(new Client("127.0.0.1", port, i));
            }

            foreach (var client in clients)
            {
                await client.Connect();
            }

            IMessage message = new Utf8Message();
            message.SetMessage(input);

            for (int i = 0; i < clients.Count; i++)
            {
                await clients[i].Send(message);

                foreach (var client in clients)
                {
                    var output = await client.Receive();
                    var output_message = output.GetMessage();
                    Assert.Equal(expected, output_message);
                }
            }

            foreach (var client in clients)
            {
                client.Disconnect();
            }
        }
    }
}