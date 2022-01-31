using System;
using System.Runtime.InteropServices;

namespace Cdm.iOS.Talk
{
    public static class DeviceSocketExtensions
    {
        public static bool Send(this IDeviceSocket socket, int value)
        {
            return socket.Send(value, BitConverter.GetBytes);
        }

        public static bool Send(this IDeviceSocket socket, float value)
        {
            return socket.Send(value, BitConverter.GetBytes);
        }
        
        public static bool Send(this IDeviceSocket socket, double value)
        {
            return socket.Send(value, BitConverter.GetBytes);
        }
        
        public static bool Receive(this IDeviceSocket socket, out int value)
        {
            return socket.Receive(out value, BitConverter.ToInt32);
        }
        
        public static bool Receive(this IDeviceSocket socket, out float value)
        {
            return socket.Receive(out value, BitConverter.ToSingle);
        }
        
        public static bool Receive(this IDeviceSocket socket, out double value)
        {
            return socket.Receive(out value, BitConverter.ToDouble);
        }

        private static bool Send<T>(this IDeviceSocket socket, T value , ConvertToBytesDelegate<T> convert) 
            where T : struct
        {
            var bytes = convert(value);
            return socket.Send(bytes, bytes.Length) == bytes.Length;
        }

        private static bool Receive<T>(this IDeviceSocket socket, out T value, ConvertFromBytesDelegate<T> convert) 
            where T : struct
        {
            var bytes = new byte[Marshal.SizeOf<T>()];
            
            var size = socket.Receive(bytes, bytes.Length);
            if (size == bytes.Length)
            {
                value = convert(bytes);
                return true;
            }

            value = default;
            return false;
        }

        private delegate byte[] ConvertToBytesDelegate<T>(T value);
        private delegate T ConvertFromBytesDelegate<out T>(ReadOnlySpan<byte> bytes);
    }
}