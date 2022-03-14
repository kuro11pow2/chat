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
                sum += 2;

            watch.Stop();
            Console.WriteLine($"{watch.ElapsedMilliseconds,4}ms, {sum}");
        }
        public static void ThreadAdd(ulong totalSumCount, ulong threadCount)
        {
            ulong sumPerThread = totalSumCount / threadCount;
            ulong remain = totalSumCount % threadCount;

            if (remain > 0)
                threadCount++;

            ulong[] results = new ulong[threadCount];
            Thread[] threads = new Thread[threadCount];

            for (ulong i = 0; i < threadCount; i++)
            {
                ulong n = i;

                if (remain > 0 && i == threadCount - 1)
                {
                    threads[n] = new Thread(() =>
                    {
                        for (ulong j = 0; j < remain; j++)
                            results[n] += 2;
                    });
                }
                else
                {
                    threads[n] = new Thread(() =>
                    {
                        for (ulong j = 0; j < sumPerThread; j++)
                            results[n] += 2;
                    });
                }
            }


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

            Console.WriteLine($"{watch.ElapsedMilliseconds,4}ms, {threads.Length}, {sumPerThread}, {remain} = {sum}");
        }
        public static void TaskAdd(ulong totalSumCount, ulong threadCount)
        {
            ulong sumPerThread = totalSumCount / threadCount;
            List<Task<ulong>> tasks = new List<Task<ulong>>();

            for (ulong i = 0; i < threadCount; i++)
            {
                tasks.Add(new Task<ulong>(() =>
                {
                    ulong sum = 0;
                    for (ulong j = 0; j < sumPerThread; j++)
                        sum += 2;
                    return sum;
                }));
            }

            ulong remain = totalSumCount % threadCount;
            if (remain > 0)
                tasks.Add(new Task<ulong>(() => {
                    ulong sum = 0;
                    for (ulong j = 0; j < remain; j++)
                        sum += 2;
                    return sum;
                }));


            Stopwatch watch = new Stopwatch();
            watch.Start();

            foreach (var task in tasks)
                task.Start();

            ulong sum = 0;
            foreach (var task in tasks)
                sum += task.Result;

            watch.Stop();

            Console.WriteLine($"{watch.ElapsedMilliseconds,4}ms, {tasks.Count}, {sumPerThread}, {remain} = {sum}");
        }

        public static void TestAll()
        {
            ulong count = 100 * 1000 * 1000;
            ulong maxThreadCount = 1;

            Console.WriteLine("싱글 스레드 더하기");
            SingleAdd(count);
            Console.WriteLine("멀티 스레드 더하기");
            for (ulong i = 0; i < maxThreadCount; i++)
                ThreadAdd(count, i + 1);
            Console.WriteLine("멀티 테스크 더하기");
            for (ulong i = 0; i < maxThreadCount; i++)
                TaskAdd(count, i + 1);
        }

        public static void ThreadSleep()
        {
            List<Thread> threads = new List<Thread>();

            for (int i = 0; i < 10; i++)
            {
                //int timeout = 1000 * 1000 * 10 * (i + 1);
                //Thread t = new Thread(() => { for (int j = 0; j < timeout; j++) { j += 2; j -= 2; } });

                int timeout = 100 * (i + 1);
                Thread t = new Thread(() => { Thread.Sleep(timeout); });
                threads.Add(t);
            }

            Stopwatch sw = new Stopwatch();

            sw.Start();
            for (int i = 0; i < threads.Count; i++)
            {
                threads[i].Start();
            }

            for (int i = 0; i < threads.Count; i++)
            {
                threads[threads.Count - i - 1].Join();
            }
            sw.Stop();
        }

        public static void Main()
        {
            ulong count = 100 * 1000 * 1000;
            ulong maxThreadCount = 8;
            TaskAdd(count, maxThreadCount);
        }
    }
}
