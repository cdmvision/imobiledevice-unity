using System;
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

#if UNITY_IOS || UNITY_EDITOR
    private Texture2D _texture;
    
    private int _textureWidth;
    private int _textureHeight;
    private int _textureFormat;
    private byte[] _textureData;
    private bool _isTextureReceived = false;
    
    private void Start()
    {
        deviceInfoText.text = "Waiting for connection...";

        Task.Run(AcceptSocket);
    }

    private void Update()
    {
        if (_isTextureReceived)
        {
            _texture = new Texture2D(_textureWidth, _textureHeight, (TextureFormat)_textureFormat, false);
            _texture.LoadRawTextureData(_textureData);
            _texture.Apply();

            image.gameObject.SetActive(true);
            image.texture = _texture;
            _isTextureReceived = false;
        }
    }

    private void OnDestroy()
    {
        if (_texture != null)
            DestroyImmediate(_texture);
    }

    private void AcceptSocket()
    {
        IDeviceSocket deviceSocket = null;
        
        try
        {
            Debug.Log($"Waiting for incoming connection...");
            deviceSocket = new DeviceSocket();
            deviceSocket.Connect(Port);
            Debug.Log($"Connected to host!");

            if (deviceSocket.Receive(out int width) &&
                deviceSocket.Receive(out int height) &&
                deviceSocket.Receive(out int format) &&
                deviceSocket.Receive(out int length))
            {
                Debug.Log($"Received texture info: {width}x{height} {(TextureFormat) format} with {length} bytes");
            
                _textureData = new byte[length];
                if (deviceSocket.Receive(_textureData, _textureData.Length) == _textureData.Length)
                {
                    Debug.Log($"Received texture data: {length} bytes");
                    
                    _textureWidth = width;
                    _textureHeight = height;
                    _textureFormat = format;
                    _isTextureReceived = true;
                    return;
                }
            }
        
            Debug.LogError($"Texture could not be received!");
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            throw;
        }
        finally
        {
            deviceSocket?.Disconnect();
            deviceSocket?.Dispose();
        }
    }
#endif
}