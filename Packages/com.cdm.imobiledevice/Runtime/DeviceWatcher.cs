using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using iMobileDevice.iDevice;
using iMobileDevice.Lockdown;
using UnityEngine;

namespace iMobileDevice.Unity
{
    public class DeviceWatcher 
    {
        private const string HandshakeLabel = "iMobileDevice.Unity";
        
        private readonly HashSet<DeviceInfo> _availableDevices = new HashSet<DeviceInfo>();
        private readonly ConcurrentQueue<DeviceEvent> _pendingEvents = new ConcurrentQueue<DeviceEvent>();
        
        /// <summary>
        /// Gets the available devices.
        /// </summary>
        public IReadOnlyCollection<DeviceInfo> availableDevices => _availableDevices;
        
        private iDeviceEventCallBack _deviceEventCallback;
        private GameObjectEventTrigger _gameObjectEventTrigger;
        private GCHandle _instanceHandle;
        
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
            
            _instanceHandle = GCHandle.Alloc(this);
            _deviceEventCallback = GetDeviceEventCallback();

            try
            {
                LibiMobileDevice.Instance.iDevice.idevice_event_subscribe(
                        _deviceEventCallback, GCHandle.ToIntPtr(_instanceHandle)).ThrowOnError();
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

            _instanceHandle.Free();
            _deviceEventCallback = null;
            isEnabled = false;
            
            if (_gameObjectEventTrigger != null)
            {
                UnityEngine.Object.Destroy(_gameObjectEventTrigger.gameObject);
                _gameObjectEventTrigger = null;
            }

            LibiMobileDevice.Instance.iDevice.idevice_event_unsubscribe().ThrowOnError();
        }

        private void Update()
        {
            while (_pendingEvents.TryDequeue(out var deviceEvent))
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

        private void OnDeviceEvent(iDeviceEvent iDeviceEvent)
        {
            var udid = iDeviceEvent.udidString;
            var eventType =  iDeviceEvent.@event;
            var connectionType = iDeviceEvent.conn_type;
                
            _pendingEvents.Enqueue(new DeviceEvent()
            {
                udid = udid,
                eventType = eventType,
                connectionType = connectionType
            });
        }

        private static iDeviceEventCallBack GetDeviceEventCallback()
        {
            return (ref iDeviceEvent deviceEvent, IntPtr data) =>
            {
                var instanceHandle = GCHandle.FromIntPtr(data);
                var deviceWatcher = instanceHandle.Target as DeviceWatcher;
                deviceWatcher?.OnDeviceEvent(deviceEvent);
            };
        }

        private static bool PopulateDeviceName(ref DeviceInfo deviceInfo)
        {
            iDeviceHandle deviceHandle = null;
            LockdownClientHandle lockdownClientHandle = null;

            try
            {
                var deviceApi = LibiMobileDevice.Instance.iDevice;
                var lockdownApi = LibiMobileDevice.Instance.Lockdown;

                deviceApi.idevice_new(out deviceHandle, deviceInfo.udid)
                    .ThrowOnError();

                lockdownApi.lockdownd_client_new_with_handshake(deviceHandle, out lockdownClientHandle, HandshakeLabel)
                    .ThrowOnError();

                lockdownApi.lockdownd_get_device_name(lockdownClientHandle, out var deviceName)
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