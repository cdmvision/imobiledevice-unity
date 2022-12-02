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
        private Socket _socket;
        
        public Socket socket => _socket;

        private int _receiveTimeout = 0;
        private int _sendTimeout = 0;

        /// <summary>
        /// Gets or sets a value that specifies the amount of time after which a synchronous <see cref="Receive"/>
        /// call will time out.
        /// </summary>
        public int receiveTimeout
        {
            get => _receiveTimeout;
            set
            {
                _receiveTimeout = value;
                
                if (socket != null)
                {
                    socket.ReceiveTimeout = _receiveTimeout;
                }
            }
        }
        
        /// <summary>
        /// Gets or sets a value that specifies the amount of time after which a synchronous <see cref="Send"/>
        /// call will time out.
        /// </summary>
        public int sendTimeout
        {
            get => _sendTimeout;
            set
            {
                _sendTimeout = value;

                if (socket != null)
                {
                    socket.SendTimeout = _sendTimeout;
                }
            }
        }
        
        public void Connect(int port)
        {
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _serverSocket.Bind(new IPEndPoint(IPAddress.Loopback, port));
            _serverSocket.Listen(1);

            _socket = _serverSocket.Accept();
            _socket.ReceiveTimeout = receiveTimeout;
            _socket.SendTimeout = sendTimeout;
        }

        public void Disconnect()
        {
            if (_socket != null && _socket.Connected)
            {
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Disconnect(false);
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
            if (_socket == null)
                throw new InvalidOperationException("Socket is not connected.");
            
            var totalSentBytes = 0;

            while (totalSentBytes < length)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw new TaskCanceledException();

                var sentBytes = _socket.Send(buffer, totalSentBytes, length - totalSentBytes, SocketFlags.None);
                if (sentBytes == 0)
                    break;

                totalSentBytes += sentBytes;
            }

            return totalSentBytes;
        }

        private int ReceiveInternal(byte[] buffer, int length, CancellationToken cancellationToken = default)
        {
            if (_socket == null)
                throw new InvalidOperationException("Socket is not connected.");
            
            var totalReceivedBytes = 0;

            while (totalReceivedBytes < length)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw new TaskCanceledException();

                var receivedBytes = 
                    _socket.Receive(buffer, totalReceivedBytes, length - totalReceivedBytes, SocketFlags.None);
                if (receivedBytes == 0)
                    break;

                totalReceivedBytes += receivedBytes;
            }

            return totalReceivedBytes;
        }

        public void Dispose()
        {
            _socket?.Close();
            _serverSocket?.Close();
        }
    }
}