
namespace Tests
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await RoomQTests.LocalRoomQTest("1234123412341234", "1234123412341234");
        }
    }
}