using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Practice
{
    public static class TaskActionDiff
    {
        /// <summary>
        /// Task.Run으로 람다 메서드가 들어오는 경우 Task 제네릭인 Func 로 추론해 처리한다.
        /// </summary>
        /// <returns></returns>
        public static Task RunTask()
        {
            return Task.Run(async () =>
            {
                Console.WriteLine("Going to sleep ...");
                await Task.Delay(1000);
                Console.WriteLine("... Woke up");
            });
        }
        /// <summary>
        /// Task.Run으로 Action이 들어오는 경우 async void 로 취급하기 때문에 action 내부의 await는 동작하지 않는다.
        /// https://blog.naver.com/techshare/222504703387
        /// </summary>
        /// <returns></returns>
        public static Task RunActionTask()
        {
            Action action = async () =>
            {
                Console.WriteLine("Going to sleep ...");
                await Task.Delay(1000);
                Console.WriteLine("... Woke up");
            };
            return Task.Run(action);
        }

    }
}
