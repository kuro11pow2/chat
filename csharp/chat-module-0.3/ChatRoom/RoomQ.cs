using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

using System.Reflection;

namespace Chat
{
    using Common;
    using Common.Interface;
    using Common.Utility;

    public class RoomStatus
    {
        private long _sendMessageCount, _receivedMessageCount;
        private long _sendByteSize, _receivedByteSize;
        private long _currentUserCount;

        public long SendMessageCount { get { return _sendMessageCount; } }
        public long ReceivedMessageCount { get { return _receivedMessageCount; } }
        public long SendByteSize { get { return _sendByteSize; } }
        public long ReceivedByteSize { get { return _receivedByteSize; } }
        public long CurrentUserCount { get { return _currentUserCount; } }

        internal void AddSendMessageCount(long n)
        {
            Interlocked.Add(ref _sendMessageCount, n);
        }
        internal void AddReceivedMessageCount(long n)
        {
            Interlocked.Add(ref _receivedMessageCount, n);
        }
        internal void AddSendByteSize(long n)
        {
            Interlocked.Add(ref _sendByteSize, n);
        }
        internal void AddReceivedByteSize(long n)
        {
            Interlocked.Add(ref _receivedByteSize, n);
        }
        internal void AddCurrentUserCount(long n)
        {
            Interlocked.Add(ref _currentUserCount, n);
        }

        public override string ToString()
        {
            return $"{nameof(CurrentUserCount)}: {CurrentUserCount}\n{nameof(SendMessageCount)}: {SendMessageCount}\n{nameof(ReceivedMessageCount)}: {ReceivedMessageCount}\n{nameof(SendByteSize)}: {SendByteSize}\n{nameof(ReceivedByteSize)}: {ReceivedByteSize}";
        }
    }

    public class Work
    {
        public string Name { get; }
        public Func<Task> Task { get; }
        public Work(string name, Func<Task> task)
        {
            Name = name;
            Task = task;
        }
    }

    public class RoomQ
    {
        private readonly int Port;
        private readonly TcpListener listener;

        private readonly ConcurrentDictionary<string, IClient> Users;
        private readonly BlockingCollection<Work> WorkQueue;

        public RoomStatus Status;

        private readonly CancellationTokenSource RoomStopTokenSource;


        public RoomQ(int port)
        {
            Port = port;
            listener = new TcpListener(IPAddress.Any, Port);

            Users = new ConcurrentDictionary<string, IClient>();
            WorkQueue = new BlockingCollection<Work>();

            Status = new RoomStatus();

            RoomStopTokenSource = new CancellationTokenSource(); ;
        }

        public async Task Run()
        {
            listener.Start();
            _ = Accept();
            await Process();
        }

        private async Task Accept()
        {
            while (RoomStopTokenSource.IsCancellationRequested == false)
            {
                IClient user = await AcceptUser();
                Register(user);
            }
        }

        private async Task<IClient> AcceptUser()
        {
            TcpClient client = await listener.AcceptTcpClientAsync(RoomStopTokenSource.Token);
            User user = new(new ConnectionContext(client));
            Log.Print($"유저 연결됨\n{user.Info} ", LogLevel.INFO);
            return user;
        }

        /// <summary>
        /// https://docs.microsoft.com/en-us/dotnet/standard/collections/thread-safe/blockingcollection-overview
        /// https://chomdoo.tistory.com/19
        /// </summary>
        /// <returns></returns>
        private async Task Process()
        {
            //const int takeDelay = 100;
            const int maxRunningTime = 1000;

            await Task.Run(async () =>
            {
                while (RoomStopTokenSource.IsCancellationRequested == false || !WorkQueue.IsCompleted)
                {
                    Work? work = null;

                    try
                    {
                        work = WorkQueue.Take(RoomStopTokenSource.Token);
                        //if (WorkQueue.TryTake(out work, takeDelay, RoomStopTokenSource.Token) == false)
                        //    continue;
                    }
                    catch (InvalidOperationException)
                    {
                        Log.Print($"ActionQueue에서 Take 할 수 없는 상태", LogLevel.ERROR);
                    }
                    catch (OperationCanceledException)
                    {
                        Log.Print($"WorkQueue 동작 취소됨", LogLevel.ERROR);
                    }

                    if (work != null)
                    {
                        CancellationTokenSource timerTokenSource = TimerTokenSource.GetTimer(maxRunningTime);

                        try
                        {
                            CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(RoomStopTokenSource.Token, timerTokenSource.Token);
                            await Task.Run(work.Task).WaitAsync(cts.Token);
                        }
                        catch (OperationCanceledException ex)
                        {
                            Log.Print($"작업 시간 초과 : {work.Name}\n{ex}", LogLevel.WARN);
                        }
                    }
                }
            });
        }

        private void Register(IClient user)
        {
            var funcTask = new Func<Task>(async () =>
            {
                await Task.Run(() =>
                {
                    string cid = user.Cid;

                    bool res = Users.TryAdd(cid, user);
                    if (false == res)
                    {
                        string exs = $"CID 중복\n{user.Info}";
                        Log.Print(exs, LogLevel.ERROR);
                        throw new InvalidDataException(exs);
                    }

                    Status.AddCurrentUserCount(1);
                    Activate(user);
                });
            }); 

            WorkQueue.Add(new Work($"{nameof(Register)}", funcTask));
        }

        private void Activate(IClient user)
        {
            Task.Run(async () =>
            {
                while (RoomStopTokenSource.IsCancellationRequested == false && user.IsReady)
                {
                    Log.Print($"\n{user.Info}", LogLevel.DEBUG);

                    IMessage message;

                    try
                    {
                        message = await user.Receive();
                    }
                    catch (IOException ex)
                    {
                        Log.Print($"receive에서 연결 종료 감지 (정상 종료)\n{user.Info}\n{ex}", LogLevel.ERROR);
                        break;
                    }

                    Status.AddReceivedMessageCount(1);
                    Status.AddReceivedByteSize(message.GetFullBytesLength());

                    ProcessUserRequest(user, message);
                }

                // 반드시 연결 해제된 유저를 동기적으로 제거 해야 함
                Kick(user.Cid);
                Status.AddCurrentUserCount(-1);
            });
        }

        private void ProcessUserRequest(IClient user, IMessage message)
        {
            Broadcast(user, message);
        }

        public void Broadcast(string cid, IMessage message)
        {
            Users.TryGetValue(cid, out IClient? user);
            if (user == null)
                throw new Exception($"존재하지 않는 cid : {cid}");
            Broadcast(user, message);
        }

        public void Broadcast(IClient src, IMessage message)
        {
            var funcTask = new Func<Task>(async () =>
            {
                List<Task> tasks = new();
                long sendCount = Users.Count;

                foreach (var pair in Users)
                {
                    IClient dst = pair.Value;
                    string info = dst.Info;
                    try
                    {
                        tasks.Add(dst.Send(message, RoomStopTokenSource.Token));
                    }
                    catch (Exception ex)
                    {
                        Log.Print($"Broadcast 시작중 연결 종료 감지\n{info}\n{ex}", LogLevel.ERROR);
                    }
                }

                try
                {
                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    Log.Print($"Broadcast 진행중 연결 종료 감지\n{ex}", LogLevel.ERROR);
                }
                Status.AddSendMessageCount(sendCount);
                Status.AddSendByteSize(sendCount * message.GetFullBytesLength());
            });

            WorkQueue.Add(new Work($"{nameof(Broadcast)}", funcTask));
        }

        public void Kick(string cid)
        {
            Users.Remove(cid, out IClient? tmpUser);

            if (tmpUser == null)
            {
                Log.Print($"{cid} 유저가 존재하지 않음", LogLevel.ERROR);
                return;
            }

            try
            {
                tmpUser.Disconnect();
                Log.Print($"{cid} connection 리소스 해제 완료", LogLevel.INFO);
            }
            catch (Exception ex)
            {
                Log.Print($"{cid} 리소스 해제 실패\n{ex}", LogLevel.ERROR);
            }

            Log.Print($"{cid} 유저 제거 완료", LogLevel.INFO);
        }

        public void Close()
        {
            Log.Print($"방 종료 시작", LogLevel.INFO);

            RoomStopTokenSource.Cancel();
            WorkQueue.CompleteAdding();
            listener.Stop();

            foreach (var pair in Users)
            {
                pair.Value?.Disconnect();
            }
        }

        public string Info()
        {
            return $"{nameof(WorkQueue)} length: {WorkQueue.Count}\n{nameof(Status)}: {Status}";
        }

        public async Task RunMonitor()
        {
            while (RoomStopTokenSource.IsCancellationRequested == false)
            {
                Log.Print(Info(), LogLevel.RETURN, "server monitor");
                await Task.Delay(5000);
            }
        }

        public async Task UserCommand()
        {
            await Task.Run(() =>
            {
                int checkDelay = 1000;

                while (RoomStopTokenSource.IsCancellationRequested == false)
                {
                    ProcessUserInput.Run(this, checkDelay);
                }
            });
        }
    }
}
