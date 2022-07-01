using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

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

    public class RoomQ
    {
        private readonly int Port;
        private readonly TcpListener listener;

        private readonly Dictionary<string, IClient> Users;
        private readonly ConcurrentQueue<Action> ActionQueue;

        public RoomStatus Status;

        private readonly CancellationTokenSource RoomStopTokenSource;


        public RoomQ(int port)
        {
            Port = port;
            listener = new TcpListener(IPAddress.Any, Port);

            Users = new Dictionary<string, IClient>();
            ActionQueue = new ConcurrentQueue<Action>();

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

        private async Task Process()
        {
            while (RoomStopTokenSource.IsCancellationRequested == false)
            {
                while (ActionQueue.IsEmpty == false)
                {
                    ActionQueue.TryDequeue(out Action? action);
                    if (null == action)
                    { 
                        Log.Print($"action을 dequeue하지 못 함");
                        continue;
                    }

                    int maxRunningTime = 2000;
                    CancellationTokenSource timerTokenSource = TimerTokenSource.GetTimer(maxRunningTime);

                    try
                    {
                        CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(RoomStopTokenSource.Token, timerTokenSource.Token);
                        await Task.Run(action).WaitAsync(cts.Token);
                    }
                    catch (OperationCanceledException ex)
                    {
                        Log.Print($"작업 시간 초과\n{ex}", LogLevel.WARN);
                    }
                }
                await Task.Delay(100, RoomStopTokenSource.Token);
            }
        }


        private void Register(IClient user)
        {
            ActionQueue.Enqueue(() =>
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
        }

        private void Activate(IClient user)
        {
            ActionQueue.Enqueue(async () =>
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

                    Broadcast(user, message);
                }

                // 반드시 연결 해제된 유저를 동기적으로 제거 해야 함
                Kick(user.Cid);
                Status.AddCurrentUserCount(-1);
            });
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
            ActionQueue.Enqueue(async () =>
            {
                List<Task> tasks = new();
                long sendCount = Users.Count;

                foreach (var pair in Users)
                {
                    IClient dst = pair.Value;
                    tasks.Add(dst.Send(message, RoomStopTokenSource.Token));
                }

                await Task.WhenAll(tasks);
                Status.AddSendMessageCount(sendCount);
                Status.AddSendByteSize(sendCount * message.GetFullBytesLength());
            });
        }

        public void Kick(string cid)
        {
            Users.Remove(cid, out IClient? tmpUser);

            try
            {
                tmpUser?.Disconnect();
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

            foreach (var pair in Users)
            {
                pair.Value?.Disconnect();
            }
            listener.Stop();
        }

        public string Info()
        {
            return $"\nQueue-{nameof(ActionQueue.Count)}: {ActionQueue.Count}\n{nameof(Status)}: {Status}";
        }

        public async Task RunMonitor()
        {
            while (RoomStopTokenSource.IsCancellationRequested == false)
            {
                Log.Print(Info(), LogLevel.OFF, "server monitor");
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
