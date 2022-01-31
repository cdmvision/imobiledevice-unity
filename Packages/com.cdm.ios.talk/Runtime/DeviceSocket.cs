using System;
using System.Net;
using System.Net.Sockets;

namespace Cdm.iOS.Talk
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

        public int Send(byte[] buffer, int size)
        {
            if (_socket == null)
                throw new InvalidOperationException("Socket is not connected.");
            
            return _socket.Send(buffer, 0, size, SocketFlags.None);
        }

        public int Receive(byte[] buffer, int size)
        {            
            if (_socket == null)
                throw new InvalidOperationException("Socket is not connected.");
            
            return _socket.Receive(buffer, 0, size, SocketFlags.None);
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