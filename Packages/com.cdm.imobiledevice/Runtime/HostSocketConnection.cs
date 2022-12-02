using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using iMobileDevice.iDevice;
using UnityEngine;

namespace iMobileDevice.Unity
{
    public class HostSocketConnection : ISocketConnection
    {
        public DeviceInfo deviceInfo { get; }

        private byte[] _buffer = new byte[4096];
        private iDeviceHandle _deviceHandle;
        private iDeviceConnectionHandle _connectionHandle;

        /// <summary>
        /// Timeout in milliseconds after which this function should return even if no data has been received.
        /// </summary>
        public uint receiveTimeout { get; set; } = 0;

        public HostSocketConnection(DeviceInfo deviceInfo)
        {
            this.deviceInfo = deviceInfo;
        }
        
        public void Dispose()
        {
            /* Commented because of crash.
            if (_connectionHandle != null && !_connectionHandle.IsClosed && !_connectionHandle.IsInvalid)
            {
                var deviceApi = LibiMobileDevice.Instance.iDevice;
                deviceApi.idevice_disconnect(_connectionHandle.DangerousGetHandle());    
            }*/

            _connectionHandle?.Dispose();
            _connectionHandle = null;
            
            _deviceHandle?.Dispose();
            _deviceHandle = null;
        }
        
        public void Connect(int port)
        {
            iDeviceHandle deviceHandle = null;
            iDeviceConnectionHandle connectionHandle = null;

            try
            {
                var deviceApi = LibiMobileDevice.Instance.iDevice;
                deviceApi.idevice_new(out deviceHandle, deviceInfo.udid).ThrowOnError();
                deviceApi.idevice_connect(deviceHandle, (ushort) port, out connectionHandle).ThrowOnError();
                //deviceApi.idevice_set_debug_callback(DebugCallBack);
                //deviceApi.idevice_set_debug_level(1);
                
                _deviceHandle = deviceHandle;
                _connectionHandle = connectionHandle;
            }
            catch (Exception)
            {
                deviceHandle?.Dispose();
                connectionHandle?.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Disconnects from the device.
        /// </summary>
        public void Disconnect()
        {
            Dispose();
        }

        /// <summary>
        /// Send the buffer given to the device via the given connection.
        /// </summary>
        /// <param name="buffer">Buffer with data to send.</param>
        /// <param name="size">Size of the buffer to send.</param>
        /// <returns>The number of bytes actually sent.</returns>
        public int Send(byte[] buffer, int size)
        {
            var deviceApi = LibiMobileDevice.Instance.iDevice;

            uint sentBytes = 0;
            deviceApi.idevice_connection_send(_connectionHandle, buffer, (uint) size, ref sentBytes).ThrowOnError();
            return (int) sentBytes;
        }

        /// <summary>
        /// Receive data from a device via the given connection.
        /// </summary>
        /// <param name="buffer">Buffer that will be filled with the received data. This buffer has to be
        /// large enough to hold <see cref="length"/> bytes.</param>
        /// <param name="length">Buffer size or number of bytes to receive.</param>
        /// <returns></returns>
        public int Receive(byte[] buffer, int length)
        {
            var deviceApi = LibiMobileDevice.Instance.iDevice;

            // Use actual buffer if given buffer length is small.
            if (_buffer.Length > length)
            {
                uint recvBytes = 0;

                if (receiveTimeout == 0)
                {
                    deviceApi.idevice_connection_receive(_connectionHandle, buffer, (uint) length, ref recvBytes)
                        .ThrowOnError();
                }
                else
                {
                    deviceApi.idevice_connection_receive_timeout(
                            _connectionHandle, buffer, (uint) length, ref recvBytes, receiveTimeout).ThrowOnError(); 
                }

                return (int) recvBytes;
            }

            // Read buffered.
            var recvTotal = 0;

            while (recvTotal < length)
            {
                var lengthRead = Math.Min(length - recvTotal, _buffer.Length);

                uint recv = 0;

                if (receiveTimeout == 0)
                {
                    deviceApi.idevice_connection_receive(_connectionHandle, _buffer, (uint)lengthRead, ref recv)
                        .ThrowOnError();
                }
                else
                {
                    deviceApi.idevice_connection_receive_timeout(
                            _connectionHandle, _buffer, (uint) lengthRead, ref recv, receiveTimeout).ThrowOnError(); 
                }

                if (recv == 0)
                {
                    break;
                }
                
                Array.Copy(_buffer, 0, buffer, recvTotal, recv);
                recvTotal += (int) recv;
            }

            return recvTotal;
        }

        /// <summary>
        /// Async version <see cref="Send"/>.
        /// </summary>
        public async Task<int> SendAsync(byte[] buffer, int length, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => Send(buffer, length), cancellationToken);
        }

        /// <summary>
        /// Async version <see cref="Receive"/>.
        /// </summary>
        public async Task<int> ReceiveAsync(byte[] buffer, int length, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => Receive(buffer, length), cancellationToken);
        }

        private static void DebugCallBack(IntPtr message)
        {
            var messageStr = Marshal.PtrToStringAuto(message);
            Debug.Log(messageStr);
        }
    }
}