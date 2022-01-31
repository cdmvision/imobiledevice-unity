using System;
using System.Net;
using System.Net.Sockets;

namespace Cdm.iOS.Talk
{
    public class DeviceSocket : IDeviceSocket
    {
        private Socket _mainSocket;
        private Socket _socket;
        
        public void Dispose()
        {
            _mainSocket?.Dispose();
            _socket?.Dispose();
        }

        public void Connect(int port)
        {
            var mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            mainSocket.Bind(new IPEndPoint(IPAddress.Loopback, port));
            mainSocket.Listen(1);
            
            var socket = mainSocket.Accept();

            _mainSocket = mainSocket;
            _socket = socket;
        }

        public void Disconnect()
        {
            _mainSocket?.Disconnect(false);
            _socket?.Disconnect(false);
        }

        public int Send(byte[] buffer, int size)
        {
            if (_socket == null)
                throw new InvalidOperationException("There is no connection.");
            
            return _socket.Send(buffer, 0, size, SocketFlags.None);
        }

        public int Receive(byte[] buffer, int size)
        {            
            if (_socket == null)
                throw new InvalidOperationException("There is no connection.");
            
            return _socket.Receive(buffer, 0, size, SocketFlags.None);
        }
    }
}