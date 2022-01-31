using System;
using System.Diagnostics;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Cdm.iOS.Talk;
using Debug = UnityEngine.Debug;

public class HostScript : MonoBehaviour
{
    public TMP_Text deviceInfoText;
    public Texture2D textureToSend;

#if UNITY_EDITOR || UNITY_STANDALONE
    private DeviceWatcher _deviceWatcher;
    private int _textureWidth;
    private int _textureHeight;
    private int _textureFormat;
    private byte[] _textureData;

    private void Start()
    {
        _deviceWatcher = new DeviceWatcher();
        _deviceWatcher.deviceAdded += DeviceWatcher_OnDeviceAdded;
        _deviceWatcher.deviceRemoved += DeviceWatcher_OnDeviceRemoved;
        _deviceWatcher.devicePaired += DeviceWatcher_OnDevicePaired;
        _deviceWatcher.SetEnabled(true);
        Debug.Log("Device watcher running...");
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
        Debug.Log($"Device added: {deviceInfoText.name} [{e.deviceInfo.udid}] [{e.deviceInfo.connectionType}]");
        
        deviceInfoText.text = $"{deviceInfoText.name} [{e.deviceInfo.udid}] [{e.deviceInfo.connectionType}]";

        _textureWidth = textureToSend.width;
        _textureHeight = textureToSend.height;
        _textureFormat = (int)textureToSend.format;
        _textureData = textureToSend.GetRawTextureData();
        
        Task.Run(() => SendTexture(e.deviceInfo));
    }

    private void SendTexture(DeviceInfo deviceInfo)
    {
        try
        {
            using var deviceSocket = new HostSocket(deviceInfo);
            deviceSocket.Connect(DeviceScript.Port);
            Debug.Log($"Device connected with port {DeviceScript.Port}");
        
            // Send texture width, height and format.
            if (deviceSocket.Send(_textureWidth) &&
                deviceSocket.Send(_textureHeight) &&
                deviceSocket.Send(_textureFormat))
            {
                // Send data length then the data itself.
                if (deviceSocket.Send(_textureData.Length) &&
                    deviceSocket.Send(_textureData, _textureData.Length) == _textureData.Length)
                {
                    Debug.Log("Texture has been sent!");
                    return;
                }
            }
        
            Debug.LogError("Texture could not be sent!");
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    private void DeviceWatcher_OnDeviceRemoved(DeviceEventArgs e)
    {
        Debug.Log($"Device removed: {deviceInfoText.name} [{e.deviceInfo.udid}] [{e.deviceInfo.connectionType}]");
        
        deviceInfoText.text = "Waiting for connection...";
    }

    private void DeviceWatcher_OnDevicePaired(DeviceEventArgs e)
    {
        Debug.Log($"Device paired: {deviceInfoText.name} [{e.deviceInfo.udid}] [{e.deviceInfo.connectionType}]");
        deviceInfoText.text = $"{deviceInfoText.name} [{e.deviceInfo.udid}] [{e.deviceInfo.connectionType}] [Paired]";
    }
#endif
}