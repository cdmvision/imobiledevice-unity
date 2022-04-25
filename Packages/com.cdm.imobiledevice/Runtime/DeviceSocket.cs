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
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _serverSocket.Bind(new IPEndPoint(IPAddress.Loopback, port));
            _serverSocket.Listen(1);
            
            _socket = _serverSocket.Accept();
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
            _socket?.Close();
            _serverSocket?.Close();
        }
    }
}