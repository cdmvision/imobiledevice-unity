using System;
using System.Net;
using System.Net.Sockets;

namespace iMobileDevice.Unity
{
    public class DeviceSocket : IDeviceSocket
    {
        private Socket _serverSocket;
        private Socket _socket;

        public void Connect(int port)
        {
            var serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Loopback, port));
            serverSocket.Listen(1);
            
            var socket = serverSocket.Accept();

            _serverSocket = serverSocket;
            _socket = socket;
        }

        public void Disconnect()
        {
            _serverSocket?.Disconnect(false);
            _socket?.Disconnect(false);
        }

        public int Send(byte[] buffer, int length)
        {
            if (_socket == null)
                throw new InvalidOperationException("Socket is not connected.");
            
            var sentTotal = 0;
            
            while (sentTotal < length) 
            {
                var recv = _socket.Send(buffer, sentTotal, length - sentTotal, SocketFlags.None);
                if (recv == 0)
                {
                    break;
                }
                sentTotal += recv;
            }
            
            return sentTotal;
        }

        public int Receive(byte[] buffer, int length)
        {            
            if (_socket == null)
                throw new InvalidOperationException("Socket is not connected.");
            
            var recvTotal = 0;
            
            while (recvTotal < length) 
            {
                var recv = _socket.Receive(buffer, recvTotal, length - recvTotal, SocketFlags.None);
                if (recv == 0)
                {
                    break;
                }
                recvTotal += recv;
            }
            
            return recvTotal;
        }
        
        public void Dispose()
        {
            _socket?.Shutdown(SocketShutdown.Both);
            _socket?.Close();
            _socket?.Dispose();
            
            _serverSocket?.Shutdown(SocketShutdown.Both);
            _serverSocket?.Close();
            _serverSocket?.Dispose();
        }
    }
}