using System;

namespace iMobileDevice.Unity
{
    public interface IDeviceSocket : IDisposable
    {
        public void Connect(int port);
        public void Disconnect();

        public int Send(byte[] buffer, int size);
        public int Receive(byte[] buffer, int size);
    }
}