using System;
using System.Threading;
using System.Threading.Tasks;
using iMobileDevice.iDevice;
using TMPro;
using UnityEngine;
using iMobileDevice.Unity;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class HostScript : MonoBehaviour
{
    public float connectionWaitTime = 1f;
    public Texture2D textureToSend;
    public RawImage image;
    public TMP_Text deviceInfoText;
    
    private DeviceWatcher _deviceWatcher;
    private string _deviceId;

    private CancellationTokenSource _cancellationTokenSource;
    
    private void OnEnable()
    {
        if (Application.platform != RuntimePlatform.WindowsEditor &&
            Application.platform != RuntimePlatform.WindowsPlayer &&
            Application.platform != RuntimePlatform.LinuxEditor &&
            Application.platform != RuntimePlatform.LinuxPlayer &&
            Application.platform != RuntimePlatform.OSXEditor &&
            Application.platform != RuntimePlatform.OSXPlayer)
        {
            enabled = false;
        }
        
        _cancellationTokenSource = new CancellationTokenSource();
    }

    private void OnDisable()
    {
        _cancellationTokenSource.Cancel();
    }

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

    private async void DeviceWatcher_OnDeviceAdded(DeviceEventArgs e)
    {
        Debug.Log($"Device added: {deviceInfoText.name} [{e.deviceInfo.udid}] [{e.deviceInfo.connectionType}]");

        if (!string.IsNullOrEmpty(_deviceId))
            return;

        if (e.deviceInfo.connectionType != iDeviceConnectionType.Usbmuxd)
            return;
        
        deviceInfoText.text = $"{deviceInfoText.name} [{e.deviceInfo.udid}] [{e.deviceInfo.connectionType}]";

        Debug.Log($"Trying to connect to the device on port {SocketTextureUtility.Port}...");
        IDeviceSocket socket = null;

        var isConnected = false;
        while (!isConnected && !_cancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                socket = new HostSocket(e.deviceInfo);
                await socket.ConnectAsync(SocketTextureUtility.Port);
                isConnected = true;
            }
            catch (iDeviceException ex)
            {
                Debug.Log($"Connection failed due to {ex.ErrorCode}. Trying to connect after {connectionWaitTime} secs...");
                
                isConnected = false;
                socket?.Dispose();
                socket = null;
            }

            await Task.Delay((int) (connectionWaitTime * 1000), _cancellationTokenSource.Token);
        }

        if (socket == null)
            return;
        
        Debug.Log($"Connection has been established!");

        _deviceId = e.deviceInfo.udid;
        
        var success = await SocketTextureUtility.SendAsync(socket, textureToSend);
        if (success)
        {
            Debug.Log("Texture has been sent!");
        }
        else
        {
            Debug.LogWarning("Texture could not be sent!");
        }
        
        Debug.Log($"Waiting for ACK...");
        var ack = await socket.ReceiveInt32Async();
        Debug.Log($"Received  ACK: {(ack.HasValue ? "YES" : "NO")}");
        
        var texture = await SocketTextureUtility.ReceiveAsync(socket);
        if (texture != null)
        {
            Debug.Log("Texture has been received!");
            
            image.gameObject.SetActive(true);
            image.texture = texture;
        }
        else
        {
            Debug.Log("Texture could not be received.");   
        }
        
        Debug.Log($"Sending ACK...");
        await socket.SendInt32Async(1);
        Debug.Log($"DONE!");
        
        socket.Disconnect();
        socket.Dispose();

        _deviceId = string.Empty;
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
}