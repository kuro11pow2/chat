using System;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

using System.Diagnostics;

namespace ConcurrencyTest
{
    class Program
    {

        // ThreadProc은 스레드의 시작과 함께 호출된다.
        // 10회 반복문을 돌며 콘솔에 출력하고, 매번 자신의 나머지 time slice를 yield한다. 그리고 종료한다.
        public static void ThreadProc()
        {
            for (int i = 0; i < 100; i++)
            {
                Console.WriteLine("ThreadProc: {0}", i);
                // 나머지 time slice를 yield
                Thread.Sleep(0);
            }
        }

        public static void SimpleThreadExample()
        {
            Console.WriteLine("Main thread: 두 번째 스레드 시작");
            // 스레드 클래스의 생성자는 스레드에서 실행할 메소드를 나타내는 ThreadStart delegate가 필요하다.
            Thread t = new Thread(new ThreadStart(ThreadProc));

            // ThreadProc을 시작한다. 
            // 단일 프로세서에서는 새 스레드는 main 스레드가 preempted 되거나 yield될 때까지 프로세서 시간을 얻지 못한다.
            // (Thread.Sleep을 만나면 여분의 time slice가 있을 때 다른 스레드가 프로세서 시간을 얻음.)
            t.Start();

            for (int i = 0; i < 100; i++)
            {
                Console.WriteLine("Main thread: {0}", i);
                Thread.Sleep(0);
            }

            Console.WriteLine("Main thread: Join()을 호출하여 ThreadProc 끝날 때까지 대기한다.");
            t.Join();
            Console.WriteLine("Main thread: ThreadProc.Join 가 반환되었음.");
        }

        public static void SingleAdd(ulong totalSumCount)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            ulong sum = 0;
            for (ulong i = 0; i < totalSumCount; i++)
                sum++;

            watch.Stop();
            //Console.WriteLine($"{watch.ElapsedMilliseconds,4}ms, {sum}");
        }
        public static void AddExample(ulong totalSumCount, ulong threadCount)
        {

            ulong sumPerThread = totalSumCount / threadCount;

            ulong[] results = new ulong[threadCount+1];
            Thread[] threads = new Thread[threadCount+1];

            for (ulong i = 0; i < threadCount; i++)
            {
                ulong n = i;
                threads[n] = new Thread(() =>
                {
                    for (ulong j = 0; j < sumPerThread; j++)
                        results[n]++;
                });
            }

            ulong remain = totalSumCount % threadCount;
            threads[threadCount] = new Thread(() =>
            {
                for (ulong j = 0; j < remain; j++)
                    results[threadCount]++;
            });

            Stopwatch watch = new Stopwatch();
            watch.Start();

            foreach (var thread in threads)
                thread.Start();

            foreach (var thread in threads)
                thread.Join();

            ulong sum = 0;
            foreach (var result in results)
                sum += result;

            watch.Stop();

            //Console.WriteLine($"{watch.ElapsedMilliseconds, 4}ms, {threadCount}*{sumPerThread} + {remain} = {sum}");
        }

        public static void Main()
        {
            ulong count = 10 * 1000 * 1000;
            SingleAdd(count);
            for (ulong i = 1; i < 16; i++)
                AddExample(count, i);
        }
    }
}
