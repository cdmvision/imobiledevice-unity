using iMobileDevice.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeviceScript : MonoBehaviour
{
    public Texture2D textureToSend;
    public RawImage image;
    public TMP_Text deviceInfoText;

    private void OnEnable()
    {
        if (Application.platform != RuntimePlatform.IPhonePlayer)
        {
            enabled = false;
        }
    }

    private async void Start()
    {
        deviceInfoText.text = "Waiting for connection...";

        Debug.Log($"Waiting for incoming connection on port {SocketTextureUtility.Port}...");
        using var socket = new DeviceSocket();
        await socket.ConnectAsync(SocketTextureUtility.Port);
        Debug.Log($"Connected to host!");

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
        Debug.Log($"DONE!");

        socket.Disconnect();
        socket.Dispose();
    }
}