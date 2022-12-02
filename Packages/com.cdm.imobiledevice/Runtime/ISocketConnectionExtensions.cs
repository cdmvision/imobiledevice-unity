using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace iMobileDevice.Unity
{
    public static class ISocketConnectionExtensions
    {
        public static async Task ConnectAsync(this ISocketConnection socket, int port)
        {
            await Task.Run(() => socket.Connect(port));
        }
        
        public static async Task DisconnectAsync(this ISocketConnection socket)
        {
            await Task.Run(socket.Disconnect);
        }
        
        public static async Task<bool> SendInt32Async(this ISocketConnection socket, int value, 
            CancellationToken cancellationToken = default)
        {
            return await socket.SendAsync(value, BitConverter.GetBytes, cancellationToken);
        }
        
        public static async Task<int?> ReceiveInt32Async(this ISocketConnection socket, 
            CancellationToken cancellationToken = default)
        {
            return await socket.ReceiveAsync(BitConverter.ToInt32, cancellationToken);
        }

        public static async Task<bool> SendFloatAsync(this ISocketConnection socket, float value,
            CancellationToken cancellationToken = default)
        {
            return await socket.SendAsync(value, BitConverter.GetBytes, cancellationToken);
        }
        
        public static async Task<float?> ReceiveFloatAsync(this ISocketConnection socket,
            CancellationToken cancellationToken = default)
        {
            return await socket.ReceiveAsync(BitConverter.ToSingle, cancellationToken);
        }
        
        public static async Task<bool> SendDoubleAsync(this ISocketConnection socket, double value,
            CancellationToken cancellationToken = default)
        {
            return await socket.SendAsync(value, BitConverter.GetBytes, cancellationToken);
        }
        
        public static async Task<double?> ReceiveDoubleAsync(this ISocketConnection socket,
            CancellationToken cancellationToken = default)
        {
            return await socket.ReceiveAsync(BitConverter.ToDouble, cancellationToken);
        }
        
        public static async Task<bool> SendBufferAsync(this ISocketConnection socket, byte[] buffer, 
            CancellationToken cancellationToken = default)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            
            
            if (await socket.SendInt32Async(buffer.Length, cancellationToken) &&
                await socket.SendAsync(buffer, buffer.Length, cancellationToken) == buffer.Length)
            {
                return true;
            }

            return false;
        }

        public static async Task<byte[]> ReceiveBufferAsync(this ISocketConnection socket,
            CancellationToken cancellationToken = default)
        {
            var length = await socket.ReceiveInt32Async(cancellationToken);
            if (length.HasValue)
            {
                var bytes = new byte[length.Value];
                if (await socket.ReceiveAsync(bytes, bytes.Length, cancellationToken) == bytes.Length)
                {
                    return bytes;
                }
            }

            return null;
        }
        
        public static async Task<bool> SendStringAsync(this ISocketConnection socket, string content,
            CancellationToken cancellationToken = default)
        {
            return await SendStringAsync(socket, content, Encoding.UTF8, cancellationToken);
        }

        public static async Task<bool> SendStringAsync(this ISocketConnection socket, string content, Encoding encoding,
            CancellationToken cancellationToken = default)
        {
            var bytes = encoding.GetBytes(content);
            return await socket.SendBufferAsync(bytes, cancellationToken);
        }

        public static async Task<string> ReceiveStringAsync(this ISocketConnection socket,
            CancellationToken cancellationToken = default)
        {
            return await ReceiveStringAsync(socket, Encoding.UTF8, cancellationToken);
        }

        public static async Task<string> ReceiveStringAsync(this ISocketConnection socket, Encoding encoding,
            CancellationToken cancellationToken = default)
        {
            var bytes = await socket.ReceiveBufferAsync(cancellationToken);
            if (bytes != null)
            {
                return encoding.GetString(bytes);
            }
            
            return string.Empty;
        }

        private static async Task<bool> SendAsync<T>(this ISocketConnection socket, T value, 
            ConvertToBytesDelegate<T> convert, CancellationToken cancellationToken) where T : struct
        {
            var bytes = convert(value);
            return await socket.SendAsync(bytes, bytes.Length, cancellationToken) == bytes.Length;

        }
        
        private static async Task<T?> ReceiveAsync<T>(this ISocketConnection socket, 
            ConvertFromBytesDelegate<T> convert, CancellationToken cancellationToken) 
            where T : struct
        {
            var bytes = new byte[Marshal.SizeOf<T>()];
            
            var size = await socket.ReceiveAsync(bytes, bytes.Length, cancellationToken);
            if (size == bytes.Length)
            {
                return convert(bytes);
            }
            
            return null;
        }
        
        private delegate byte[] ConvertToBytesDelegate<T>(T value);
        private delegate T ConvertFromBytesDelegate<out T>(ReadOnlySpan<byte> bytes);
    }
}