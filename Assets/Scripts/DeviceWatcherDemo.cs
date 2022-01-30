using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Cdm.iOS.Talk
{
    public class DeviceWatcherDemo : MonoBehaviour
    {
        private DeviceWatcher _deviceWatcher;
        
        private void Start()
        {
            var s = Stopwatch.StartNew();
            _deviceWatcher = new DeviceWatcher();
            _deviceWatcher.deviceAdded += DeviceWatcher_OnDeviceAdded;
            _deviceWatcher.deviceRemoved += DeviceWatcher_OnDeviceRemoved;
            _deviceWatcher.devicePaired += DeviceWatcher_OnDevicePaired;
            _deviceWatcher.SetEnabled(true);
            s.Stop();
            
            Debug.Log($"Took {s.ElapsedMilliseconds} ms");
        }
        
        private void OnDestroy()
        {
            if (_deviceWatcher != null)
            {
                _deviceWatcher.deviceAdded -= DeviceWatcher_OnDeviceAdded;
                _deviceWatcher.deviceRemoved -= DeviceWatcher_OnDeviceRemoved;
                _deviceWatcher.devicePaired -= DeviceWatcher_OnDevicePaired;
                _deviceWatcher.SetEnabled(false);
            }
        }

        private void DeviceWatcher_OnDeviceAdded(DeviceEventArgs e)
        {
            Debug.Log($"OnDeviceAdded: {e.deviceInfo}");
        }
        
        private void DeviceWatcher_OnDeviceRemoved(DeviceEventArgs e)
        {
            Debug.Log($"OnDeviceRemoved: {e.deviceInfo}");
        }
        
        private void DeviceWatcher_OnDevicePaired(DeviceEventArgs e)
        {
            Debug.Log($"OnDevicePaired: {e.deviceInfo}");
        }
    }
}