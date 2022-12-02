using System;
using System.Threading;
using System.Threading.Tasks;

namespace iMobileDevice.Unity
{
    public interface ISocketConnection : IDisposable
    {
        public void Connect(int port);
        public void Disconnect();

        public int Send(byte[] buffer, int size);
        public int Receive(byte[] buffer, int size);
        
        public Task<int> SendAsync(byte[] buffer, int length, CancellationToken cancellationToken = default);
        public Task<int> ReceiveAsync(byte[] buffer, int length, CancellationToken cancellationToken = default);
    }
}