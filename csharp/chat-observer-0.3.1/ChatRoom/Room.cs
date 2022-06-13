using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using System.IO.Pipelines;
using System.Net.Sockets;
using System.Buffers;

namespace Common
{
    using Utility;

    internal class Room : IObservable<Packet>
    {
        private readonly ConcurrentDictionary<int, IObserver<Packet>> _observers = new ConcurrentDictionary<int, IObserver<Packet>>();

        private class Unsubscriber : IDisposable
        {
            private int _cid;
            private ConcurrentDictionary<int, IObserver<Packet>> _observers;

            public Unsubscriber(int cid, ConcurrentDictionary<int, IObserver<Packet>> observers)
            {
                _cid = cid;
                _observers = observers;
            }

            public void Dispose()
            {
                if (!_observers.TryRemove(_cid, out IObserver<Packet>? tmp))
                {
                    Log.Print($"{_cid}를 Connections 에서 제외 실패", LogLevel.ERROR);
                }
            }
        }

        public IDisposable Subscribe(IObserver<Packet> observer)
        {
            int cid = _observers.Count;
            if (_observers.ContainsKey(cid))
            {
                throw new Exception($"cid 중복: {cid}");
            }
            if (!_observers.TryAdd(cid, observer))
            {
                throw new Exception($"connection 등록 실패: {cid}");
            }

            return new Unsubscriber(cid, _observers);
        }

        private async Task FillPipeAsync(Socket socket, PipeWriter writer)
        {
            const int minimumBufferSize = 512;

            while (true)
            {
                // Allocate at least 512 bytes from the PipeWriter.
                Memory<byte> memory = writer.GetMemory(minimumBufferSize);
                try
                {
                    int bytesRead = await socket.ReceiveAsync(memory, SocketFlags.None);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    // Tell the PipeWriter how much was read from the Socket.
                    writer.Advance(bytesRead);
                }
                catch (Exception ex)
                {
                    Log.Print(ex, LogLevel.ERROR);
                    break;
                }

                // Make the data available to the PipeReader.
                FlushResult result = await writer.FlushAsync();

                if (result.IsCompleted)
                {
                    break;
                }
            }

            // By completing PipeWriter, tell the PipeReader that there's no more data coming.
            await writer.CompleteAsync();
        }

        private async Task ReadPipeAsync(PipeReader reader)
        {
            while (true)
            {
                ReadResult result = await reader.ReadAsync();
                ReadOnlySequence<byte> buffer = result.Buffer;

                while (TryReadLine(ref buffer, out ReadOnlySequence<byte> line))
                {
                    // Process the line.
                    ProcessLine(line);
                }

                // Tell the PipeReader how much of the buffer has been consumed.
                reader.AdvanceTo(buffer.Start, buffer.End);

                // Stop reading if there's no more data coming.
                if (result.IsCompleted)
                {
                    break;
                }
            }

            // Mark the PipeReader as complete.
            await reader.CompleteAsync();
        }

        private bool TryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
        {
            // Look for a EOL in the buffer.
            SequencePosition? position = buffer.PositionOf((byte)'\n');

            if (position == null)
            {
                line = default;
                return false;
            }

            // Skip the line + the \n.
            line = buffer.Slice(0, position.Value);
            buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
            return true;
        }

        private void ProcessLine(ReadOnlySequence<byte> buffer)
        {
            Log.Print(buffer, LogLevel.INFO);
        }
    }
}
