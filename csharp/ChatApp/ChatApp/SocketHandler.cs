using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Reflection;


namespace ChatApp
{
    class SocketHandler
    {
        Socket _socket;
        
        public SocketHandler() : this(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
        {
        }
        public SocketHandler(Socket socket)
        {
            _socket = socket;
        }

        public void StartBindAndListen(int port, int backlog)
        {
            Log.Print($"start", ChatLogLevel.VERBOSE, nameof(StartBindAndListen));

            _socket.Bind(new IPEndPoint(IPAddress.Any, port));
            _socket.Listen(backlog);

            Log.Print($"end: port {port}, backlog {backlog}", ChatLogLevel.VERBOSE, nameof(StartBindAndListen));
        }
        public async Task ConnectAsync(string listenerAddress, int port)
        {
            Log.Print($"start", ChatLogLevel.VERBOSE, nameof(ConnectAsync));

            await _socket.ConnectAsync(listenerAddress, port);

            Log.Print($"end, listenerAddress {listenerAddress}, port {port}", ChatLogLevel.VERBOSE, nameof(ConnectAsync));
        }
        public async Task<Socket> AcceptAsync()
        {
            Log.Print($"start", ChatLogLevel.VERBOSE, nameof(AcceptAsync));

            Socket socket = await _socket.AcceptAsync();

            Log.Print($"end", ChatLogLevel.VERBOSE, nameof(AcceptAsync));
            return socket;
        }
        public async Task SendAsync(string message)
        {
            Log.Print($"start", ChatLogLevel.VERBOSE, nameof(SendAsync));

            byte[] messageByte = Encoding.UTF8.GetBytes(message);
            int messageByteSize = messageByte.Length;

            byte[] packetSizeBuffer = BitConverter.GetBytes(messageByteSize);
            int packetSizeLen = packetSizeBuffer.Length;

            byte[] sendData = new byte[messageByteSize + packetSizeLen];
            Array.Copy(packetSizeBuffer, sendData, packetSizeLen);
            Array.Copy(messageByte, 0, sendData, packetSizeLen, messageByteSize);

            int totalSendByte = 0;
            do
            {
                totalSendByte += await _socket.SendAsync(sendData[totalSendByte..], SocketFlags.None);
            } while (totalSendByte < sendData.Length);

            Log.Print($"end, message {message}, totalSendByte {totalSendByte}", ChatLogLevel.VERBOSE, nameof(SendAsync));
        }
        public async Task<string> ReceiveAsync()
        {
            Log.Print($"start", ChatLogLevel.VERBOSE, nameof(ReceiveAsync));

            byte[] packetSizeBuffer = new byte[4];
            int packetSizeLen = packetSizeBuffer.Length;

            int receiveByte = 0;
            do
            {
                receiveByte += await _socket.ReceiveAsync(packetSizeBuffer, SocketFlags.None);
            } while (receiveByte < packetSizeLen);

            int packetSize = BitConverter.ToInt32(packetSizeBuffer);
            byte[] receiveBuffer = new byte[packetSize];

            int totalReceiveByte = receiveByte;
            receiveByte = 0;
            do
            {
                var memory = new Memory<byte>(receiveBuffer, receiveByte, packetSize - receiveByte);
                receiveByte += await _socket.ReceiveAsync(memory, SocketFlags.None);
            } while (receiveByte < packetSize);
            totalReceiveByte += receiveByte;
            string message = Encoding.UTF8.GetString(receiveBuffer);

            Log.Print($"end, message {message}, totalReceiveByte {totalReceiveByte}", ChatLogLevel.VERBOSE, nameof(ReceiveAsync));
            return message;
        }
        public void Close() 
        {
            Log.Print($"start", ChatLogLevel.VERBOSE, nameof(Close));

            _socket.Shutdown(SocketShutdown.Both);
            _socket.Disconnect(false);
            _socket.Close();

            Log.Print($"end", ChatLogLevel.VERBOSE, nameof(Close));
        }
    }
}
