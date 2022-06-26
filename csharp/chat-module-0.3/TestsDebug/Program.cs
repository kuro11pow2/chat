
namespace Tests
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await RoomTests.LocalRoomTest("1234123412341234", "1234123412341234");
        }
    }
}