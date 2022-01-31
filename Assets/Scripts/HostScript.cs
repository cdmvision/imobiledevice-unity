using System.Diagnostics;
using TMPro;
using UnityEngine;
using Cdm.iOS.Talk;

public class HostScript : MonoBehaviour
{
    public TMP_Text deviceInfoText;
    public Texture2D textureToSend;

#if UNITY_EDITOR || UNITY_STANDALONE
    private DeviceWatcher _deviceWatcher;

    private void Start()
    {
        _deviceWatcher = new DeviceWatcher();
        _deviceWatcher.deviceAdded += DeviceWatcher_OnDeviceAdded;
        _deviceWatcher.deviceRemoved += DeviceWatcher_OnDeviceRemoved;
        _deviceWatcher.devicePaired += DeviceWatcher_OnDevicePaired;
        _deviceWatcher.SetEnabled(true);
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
        deviceInfoText.text = $"{deviceInfoText.name} [{e.deviceInfo.udid}] [{e.deviceInfo.connectionType}]";

        using (var deviceSocket = new HostSocket(e.deviceInfo))
        {
            deviceSocket.Connect(DeviceScript.Port);

            // Send texture width, height and format.
            deviceSocket.Send(textureToSend.width);
            deviceSocket.Send(textureToSend.height);
            deviceSocket.Send((int)textureToSend.format);

            // Send data length then the data itself.
            var data = textureToSend.GetRawTextureData();
            deviceSocket.Send(data.Length);
            deviceSocket.Send(data, data.Length);
        }
    }

    private void DeviceWatcher_OnDeviceRemoved(DeviceEventArgs e)
    {
        deviceInfoText.text = "Waiting for connection...";
    }

    private void DeviceWatcher_OnDevicePaired(DeviceEventArgs e)
    {
        deviceInfoText.text = $"{deviceInfoText.name} [{e.deviceInfo.udid}] [{e.deviceInfo.connectionType}] [Paired]";
    }
#endif
}