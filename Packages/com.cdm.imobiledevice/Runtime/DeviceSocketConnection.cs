using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace iMobileDevice.Unity
{
    public class DeviceSocketConnection : ISocketConnection
    {
        private Socket _serverSocket;
        
        public Socket socket { get; private set; }
        
        public void Connect(Socket acceptSocket)
        {
            if (socket != null && socket.Connected)
                throw new InvalidOperationException("Disconnect current socket before using a new socket.");

            socket?.Dispose();
            socket = acceptSocket;
        }
        
        public void Connect(int port)
        {
            if (socket == null || !socket.Connected)
            {
                _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _serverSocket.Bind(new IPEndPoint(IPAddress.Loopback, port));
                _serverSocket.Listen(1);
                
                socket = _serverSocket.Accept();
            }
        }

        public void Disconnect()
        {
            if (socket != null && socket.Connected)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Disconnect(false);
            }

            if (_serverSocket != null && _serverSocket.Connected)
            {
                _serverSocket.Shutdown(SocketShutdown.Both);
                _serverSocket.Disconnect(false);
            }
        }

        public int Send(byte[] buffer, int length)
        {
            return SendInternal(buffer, length);
        }

        public int Receive(byte[] buffer, int length)
        {
            return ReceiveInternal(buffer, length);
        }

        public async Task<int> SendAsync(byte[] buffer, int length, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => SendInternal(buffer, length, cancellationToken), cancellationToken);
        }

        public async Task<int> ReceiveAsync(byte[] buffer, int length, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => ReceiveInternal(buffer, length, cancellationToken), cancellationToken);
        }

        private int SendInternal(byte[] buffer, int length, CancellationToken cancellationToken = default)
        {
            if (socket == null)
                throw new InvalidOperationException("Socket is not connected.");
            
            var totalSentBytes = 0;

            while (totalSentBytes < length)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw new TaskCanceledException();

                var sentBytes = socket.Send(buffer, totalSentBytes, length - totalSentBytes, SocketFlags.None);
                if (sentBytes == 0)
                    break;

                totalSentBytes += sentBytes;
            }

            return totalSentBytes;
        }

        private int ReceiveInternal(byte[] buffer, int length, CancellationToken cancellationToken = default)
        {
            if (socket == null)
                throw new InvalidOperationException("Socket is not connected.");
            
            var totalReceivedBytes = 0;

            while (totalReceivedBytes < length)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw new TaskCanceledException();

                var receivedBytes = 
                    socket.Receive(buffer, totalReceivedBytes, length - totalReceivedBytes, SocketFlags.None);
                if (receivedBytes == 0)
                    break;

                totalReceivedBytes += receivedBytes;
            }

            return totalReceivedBytes;
        }

        public void Dispose()
        {
            socket?.Close();
            socket = null;
            
            _serverSocket?.Close();
            _serverSocket = null;
        }
    }
}