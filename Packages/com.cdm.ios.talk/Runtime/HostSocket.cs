using System;
using iMobileDevice;
using iMobileDevice.iDevice;
using UnityEngine;

namespace Cdm.iOS.Talk
{
    public class HostSocket : IDeviceSocket
    {
        public DeviceInfo deviceInfo { get; }

        private iDeviceHandle _deviceHandle;
        private iDeviceConnectionHandle _connectionHandle;
        
        public HostSocket(DeviceInfo deviceInfo)
        {
            this.deviceInfo = deviceInfo;
        }
        
        public void Dispose()
        {
            _deviceHandle?.Dispose();
            _connectionHandle?.Dispose();
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
        /// Disconnect from the device and clean up resources.
        /// </summary>
        public void Disconnect()
        {
            var deviceApi = LibiMobileDevice.Instance.iDevice;
            deviceApi.idevice_disconnect(_connectionHandle.DangerousGetHandle());
            
            _deviceHandle?.Dispose();
            _deviceHandle = null;
            
            _connectionHandle?.Dispose();
            _connectionHandle = null;
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
        /// large enough to hold <see cref="size"/> bytes.</param>
        /// <param name="size">Buffer size or number of bytes to receive.</param>
        /// <returns></returns>
        public int Receive(byte[] buffer, int size)
        {
            var deviceApi = LibiMobileDevice.Instance.iDevice;
            
            uint receivedBytes = 0;
            deviceApi.idevice_connection_receive(_connectionHandle, buffer, (uint) size, ref receivedBytes);
            return (int) receivedBytes;
        }
    }
}