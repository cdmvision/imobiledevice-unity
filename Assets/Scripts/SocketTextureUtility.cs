using System.Threading.Tasks;
using iMobileDevice.Unity;
using UnityEngine;

public static class SocketTextureUtility
{
    public const int Port = 7799;
    
    public static async Task<bool> SendAsync(IDeviceSocket socket, Texture2D texture)
    {
        Debug.Log($"Sending texture info: {texture.width}x{texture.height} {texture.format}");

        if (await socket.SendInt32Async(texture.width) &&
            await socket.SendInt32Async(texture.height) &&
            await socket.SendInt32Async((int) texture.format))
        {
            var textureData = texture.GetRawTextureData();
            Debug.Log($"Sending texture data with {textureData.Length} bytes...");

            if (await socket.SendInt32Async(textureData.Length) &&
                await socket.SendAsync(textureData, textureData.Length) == textureData.Length)
            {
                return true;
            }
        }
        
        return false;
    }

    public static async Task<Texture2D> ReceiveAsync(IDeviceSocket socket)
    {
        Debug.Log($"Receiving texture info...");

        var width = await socket.ReceiveInt32Async();
        var height = await socket.ReceiveInt32Async();
        var format = await socket.ReceiveInt32Async();
        var length = await socket.ReceiveInt32Async();

        if (width.HasValue && height.HasValue && format.HasValue && length.HasValue)
        {
            Debug.Log($"Received texture info: {width}x{height} {(TextureFormat) format} with {length} bytes");
            
            var textureData = new byte[length.Value];
            if (await socket.ReceiveAsync(textureData, textureData.Length) == textureData.Length)
            {
                Debug.Log($"Received texture data: {length} bytes");
                    
                var texture = new Texture2D(width.Value, height.Value, (TextureFormat) format.Value, false);
                texture.LoadRawTextureData(textureData);
                texture.Apply();
                
                return texture;
            }
        }

        return null;
    }
}