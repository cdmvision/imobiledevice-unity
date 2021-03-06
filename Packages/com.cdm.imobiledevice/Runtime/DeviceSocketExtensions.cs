using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace iMobileDevice.Unity
{
    public static class DeviceSocketExtensions
    {
        public static async Task ConnectAsync(this IDeviceSocket socket, int port)
        {
            await Task.Run(() => socket.Connect(port));
        }
        
        public static async Task DisconnectAsync(this IDeviceSocket socket)
        {
            await Task.Run(socket.Disconnect);
        }
        
        public static async Task<int> SendAsync(this IDeviceSocket socket, byte[] buffer, int size)
        {
            return await Task.Run(() => socket.Send(buffer, size));
        }
        
        public static async Task<bool> SendInt32Async(this IDeviceSocket socket, int value)
        {
            return await Task.Run(() => socket.SendInt32(value));
        }
        
        public static async Task<bool> SendFloatAsync(this IDeviceSocket socket, float value)
        {
            return await Task.Run(() => socket.SendFloat(value));
        }
        
        public static async Task<bool> SendDoubleAsync(this IDeviceSocket socket, double value)
        {
            return await Task.Run(() => socket.SendDouble(value));
        }
        
        public static async Task<int> ReceiveAsync(this IDeviceSocket socket, byte[] buffer, int size)
        {
            return await Task.Run(() => socket.Receive(buffer, size));
        }
        
        public static async Task<int?> ReceiveInt32Async(this IDeviceSocket socket)
        {
            return await socket.ReceiveAsync(BitConverter.ToInt32);
        }
        
        public static async Task<float?> ReceiveFloatAsync(this IDeviceSocket socket)
        {
            return await socket.ReceiveAsync(BitConverter.ToSingle);
        }
        
        public static async Task<double?> ReceiveDoubleAsync(this IDeviceSocket socket)
        {
            return await socket.ReceiveAsync(BitConverter.ToDouble);
        }
        
        public static bool SendInt32(this IDeviceSocket socket, int value)
        {
            return socket.Send(value, BitConverter.GetBytes);
        }

        public static bool SendFloat(this IDeviceSocket socket, float value)
        {
            return socket.Send(value, BitConverter.GetBytes);
        }
        
        public static bool SendDouble(this IDeviceSocket socket, double value)
        {
            return socket.Send(value, BitConverter.GetBytes);
        }
        
        public static int? ReceiveInt32(this IDeviceSocket socket)
        {
            return socket.Receive(BitConverter.ToInt32);
        }
        
        public static float? ReceiveFloat(this IDeviceSocket socket)
        {
            return socket.Receive(BitConverter.ToSingle);
        }
        
        public static double? ReceiveDouble(this IDeviceSocket socket)
        {
            return socket.Receive(BitConverter.ToDouble);
        }
        
        public static async Task<bool> SendBufferAsync(this IDeviceSocket socket, byte[] buffer, int length = -1)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (buffer.Length < length)
                throw new ArgumentOutOfRangeException(nameof(length));
            
            if (length < 0)
            {
                length = buffer.Length;
            }
            
            if (await socket.SendInt32Async(length) &&
                await socket.SendAsync(buffer, length) == length)
            {
                return true;
            }

            return false;
        }

        public static async Task<byte[]> ReceiveBufferAsync(this IDeviceSocket socket)
        {
            var length = await socket.ReceiveInt32Async();
            if (length.HasValue)
            {
                var bytes = new byte[length.Value];
                if (await socket.ReceiveAsync(bytes, bytes.Length) == bytes.Length)
                {
                    return bytes;
                }
            }

            return null;
        }

        public static async Task<bool> SendStringAsync(this IDeviceSocket socket, string content)
        {
            return await SendStringAsync(socket, content, Encoding.UTF8);
        }

        public static async Task<bool> SendStringAsync(this IDeviceSocket socket, string content, Encoding encoding)
        {
            var bytes = encoding.GetBytes(content);
            return await socket.SendBufferAsync(bytes);
        }

        public static async Task<string> ReceiveStringAsync(this IDeviceSocket socket)
        {
            return await ReceiveStringAsync(socket, Encoding.UTF8);
        }

        public static async Task<string> ReceiveStringAsync(this IDeviceSocket socket, Encoding encoding)
        {
            var bytes = await socket.ReceiveBufferAsync();
            if (bytes != null)
            {
                return encoding.GetString(bytes);
            }
            
            return string.Empty;
        }

        private static bool Send<T>(this IDeviceSocket socket, T value , ConvertToBytesDelegate<T> convert) 
            where T : struct
        {
            var bytes = convert(value);
            return socket.Send(bytes, bytes.Length) == bytes.Length;
        }

        private static T? Receive<T>(this IDeviceSocket socket, ConvertFromBytesDelegate<T> convert) 
            where T : struct
        {
            var bytes = new byte[Marshal.SizeOf<T>()];
            
            var size = socket.Receive(bytes, bytes.Length);
            if (size == bytes.Length)
            {
                return convert(bytes);
            }
            
            return null;
        }
        
        private static async Task<T?> ReceiveAsync<T>(this IDeviceSocket socket, ConvertFromBytesDelegate<T> convert) 
            where T : struct
        {
            var bytes = new byte[Marshal.SizeOf<T>()];
            
            var size = await socket.ReceiveAsync(bytes, bytes.Length);
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