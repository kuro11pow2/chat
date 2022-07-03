using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Practice
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await TaskActionDiff.RunTask();
            await Task.Delay(1000);
            await TaskActionDiff.RunActionTask();
        }
    }
}
