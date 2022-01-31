using System.Threading.Tasks;
using Cdm.iOS.Talk;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeviceScript : MonoBehaviour
{
    public const int Port = 7799;

    public RawImage image;
    public TMP_Text deviceInfoText;

#if UNITY_IOS //|| UNITY_EDITOR
    private Texture2D _texture;
    private IDeviceSocket _deviceSocket;
    
    private void Start()
    {
        deviceInfoText.text = "Waiting for connection...";

        Task.Run(AcceptSocket);
    }
    
    private void OnDestroy()
    {
        _deviceSocket?.Disconnect();
        _deviceSocket?.Dispose();
    }

    private void AcceptSocket()
    {
        Debug.Log($"Waiting for incoming connection...");
        _deviceSocket = new DeviceSocket();
        _deviceSocket.Connect(Port);
        deviceInfoText.text = "Connected!";
        Debug.Log($"Connected to host!");

        if (_deviceSocket.Receive(out int width) &&
            _deviceSocket.Receive(out int height) &&
            _deviceSocket.Receive(out int format) &&
            _deviceSocket.Receive(out int length))
        {
            Debug.Log($"Received texture info: {width}x{height} {(TextureFormat) format}");
            
            var textureData = new byte[length];
            if (_deviceSocket.Receive(textureData, textureData.Length) == textureData.Length)
            {
                Debug.Log($"Received texture data: {length} bytes");
                
                if (_texture != null)
                {
                    DestroyImmediate(_texture);
                    _texture = null;
                }

                _texture = new Texture2D(width, height, (TextureFormat)format, false);
                _texture.LoadRawTextureData(textureData);
                _texture.Apply();

                image.gameObject.SetActive(true);
                image.texture = _texture;
                return;
            }
        }
        
        Debug.LogError($"Texture could not be received!");

        if (_texture == null)
        {
            deviceInfoText.text = "Failed to receive the texture!";
        }
    }
#endif
}