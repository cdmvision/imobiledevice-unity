using System.Threading.Tasks;
using iMobileDevice.Unity;
using UnityEngine;

public static class SocketTextureUtility
{
    public const int Port = 7799;
    
    public static async Task<bool> SendAsync(ISocketConnection socket, Texture2D texture)
    {
        Debug.Log($"Sending texture info: {texture.width}x{texture.height} {texture.format}");

        if (await socket.SendInt32Async(texture.width) &&
            await socket.SendInt32Async(texture.height) &&
            await socket.SendInt32Async((int) texture.format))
        {
            var textureData = texture.GetRawTextureData();
            Debug.Log($"Sending texture data with {textureData.Length} bytes...");

            if (await socket.SendBufferAsync(textureData))
            {
                return true;
            }
        }
        
        return false;
    }

    public static async Task<Texture2D> ReceiveAsync(ISocketConnection socket)
    {
        Debug.Log($"Receiving texture info...");

        var width = await socket.ReceiveInt32Async();
        var height = await socket.ReceiveInt32Async();
        var format = await socket.ReceiveInt32Async();

        if (width.HasValue && height.HasValue && format.HasValue)
        {
            Debug.Log($"Received texture info: {width}x{height} {(TextureFormat) format}");

            var textureData = await socket.ReceiveBufferAsync();
            if (textureData != null)
            {
                Debug.Log($"Received texture data: {textureData.Length} bytes");
                    
                var texture = new Texture2D(width.Value, height.Value, (TextureFormat) format.Value, false);
                texture.LoadRawTextureData(textureData);
                texture.Apply();
                
                return texture;
            }
        }

        return null;
    }
}