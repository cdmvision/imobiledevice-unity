using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using iMobileDevice;
using iMobileDevice.iDevice;
using iMobileDevice.Lockdown;
using UnityEngine;

namespace Cdm.iOS.Talk
{
    public class DeviceWatcher 
    {
        private const string Label = "Cdm.iOS.Talk.DeviceWatcher";
        
        private readonly HashSet<DeviceInfo> _availableDevices = new HashSet<DeviceInfo>();

        /// <summary>
        /// Gets the available devices.
        /// </summary>
        public IReadOnlyCollection<DeviceInfo> availableDevices => _availableDevices;
        
        // TODO: make member instead of static
        private static readonly ConcurrentQueue<DeviceEvent> PendingEvents = new ConcurrentQueue<DeviceEvent>();
        
        private iDeviceEventCallBack _deviceEventCallback;
        private GameObjectEventTrigger _gameObjectEventTrigger;
        
        public bool isEnabled { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="iMobileDevice.iDevice.iDeviceException"></exception>
        public void SetEnabled(bool enable)
        {
            if (enable)
            {
                Start();
            }
            else
            {
                Stop();
            }
        }
        
        private void Start()
        {
            if (isEnabled)
                return;
            
            var go = new GameObject(nameof(GameObjectEventTrigger));
            go.hideFlags = HideFlags.HideAndDontSave;
            _gameObjectEventTrigger = go.AddComponent<GameObjectEventTrigger>();
            _gameObjectEventTrigger.updateCallback = Update;
            
            _deviceEventCallback = GetDeviceEventCallback();

            try
            {
                LibiMobileDevice.Instance.iDevice.idevice_event_subscribe(_deviceEventCallback, IntPtr.Zero)
                    .ThrowOnError();
            }
            catch (Exception)
            {
                UnityEngine.Object.Destroy(_gameObjectEventTrigger.gameObject);
                _gameObjectEventTrigger = null;
                throw;
            }
            
            isEnabled = true;
        }
        
        private void Stop()
        {
            if (!isEnabled)
                return;
            
            if (_gameObjectEventTrigger != null)
            {
                UnityEngine.Object.Destroy(_gameObjectEventTrigger.gameObject);
                _gameObjectEventTrigger = null;
            }

            _deviceEventCallback = null;
            isEnabled = false;
            LibiMobileDevice.Instance.iDevice.idevice_event_unsubscribe().ThrowOnError();
        }

        private void Update()
        {
            while (PendingEvents.TryDequeue(out var deviceEvent))
            {
                var deviceInfo = availableDevices.FirstOrDefault(d => d.udid == deviceEvent.udid);
                if (deviceInfo.udid != deviceEvent.udid)
                {
                    deviceInfo = new DeviceInfo(deviceEvent.udid, "", deviceEvent.connectionType);
                    PopulateDeviceName(ref deviceInfo);
                }
                
                switch (deviceEvent.eventType)
                {
                    case iDeviceEventType.DeviceAdd:
                        _availableDevices.Add(deviceInfo);
                        OnDeviceAdded(new DeviceEventArgs(deviceInfo));
                        break;
                    case iDeviceEventType.DeviceRemove:
                        _availableDevices.Remove(deviceInfo);
                        OnDeviceRemoved(new DeviceEventArgs(deviceInfo));
                        break;
                    case iDeviceEventType.DevicePaired:
                        OnDevicePaired(new DeviceEventArgs(deviceInfo));
                        break;
                    default:
                        return;
                }
            }
        }

        private static iDeviceEventCallBack GetDeviceEventCallback()
        {
            return (ref iDeviceEvent deviceEvent, IntPtr data) =>
            {
                var udid = deviceEvent.udidString;
                var eventType =  deviceEvent.@event;
                var connectionType = deviceEvent.conn_type; // TODO:
                
                PendingEvents.Enqueue(new DeviceEvent()
                {
                    udid = udid,
                    eventType = eventType,
                    connectionType = connectionType
                });
                
                Debug.Log("OnDeviceEventCallback " + Thread.CurrentThread.ManagedThreadId);
            };
        }

        private static bool PopulateDeviceName(ref DeviceInfo deviceInfo)
        {
            iDeviceHandle deviceHandle = null;
            LockdownClientHandle lockdownClientHandle = null;

            try
            {
                var idevice = LibiMobileDevice.Instance.iDevice;
                var lockdown = LibiMobileDevice.Instance.Lockdown;

                idevice.idevice_new(out deviceHandle, deviceInfo.udid)
                    .ThrowOnError();

                lockdown.lockdownd_client_new_with_handshake(deviceHandle, out lockdownClientHandle, Label)
                    .ThrowOnError();

                lockdown.lockdownd_get_device_name(lockdownClientHandle, out var deviceName)
                    .ThrowOnError();

                deviceInfo = new DeviceInfo(deviceInfo.udid, deviceName, deviceInfo.connectionType);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning(e.Message);
                return false;
            }
            finally
            {
                deviceHandle?.Dispose();
                lockdownClientHandle?.Dispose();
            }
        }

        protected virtual void OnDeviceAdded(DeviceEventArgs e)
        {
            deviceAdded?.Invoke(e);
        }

        protected virtual void OnDeviceRemoved(DeviceEventArgs e)
        {
            deviceRemoved?.Invoke(e);
        }

        protected virtual void OnDevicePaired(DeviceEventArgs e)
        {
            devicePaired?.Invoke(e);
        }

        public event Action<DeviceEventArgs> deviceAdded;
        public event Action<DeviceEventArgs> deviceRemoved;
        public event Action<DeviceEventArgs> devicePaired;
        
        private struct DeviceEvent
        {
            public string udid;
            public iDeviceEventType eventType;
            public iDeviceConnectionType connectionType;
        }
    }

    public readonly struct DeviceEventArgs
    {
        /// <summary>
        /// Device information.
        /// </summary>
        public DeviceInfo deviceInfo { get; }

        public DeviceEventArgs(DeviceInfo deviceInfo)
        {
            this.deviceInfo = deviceInfo;
        }
    }
}