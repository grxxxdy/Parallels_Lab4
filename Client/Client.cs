using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace Client;

public class Client : IDisposable
{
    private Socket? _socket;
    
    public void Connect(string serverIp, int port)
    {
        // Connect socket
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _socket.Connect(new IPEndPoint(IPAddress.Parse(serverIp), port));
        
        // Send message
        using NetworkStream stream = new NetworkStream(_socket);
        
        MessageManager.SendMessage(stream, MessageType.CONNECT, "Hello from client");
        
        // Read response
        var (type, payload) = MessageManager.ReadMessage(stream);
        Console.WriteLine($"Server response of type \"{type}\": {payload}\n");
    }

    public void UpdateConfig(int threadAmount)
    {
        if (_socket == null || !_socket.IsBound)
            return;
        
        // Send message
        using NetworkStream stream = new NetworkStream(_socket);
        
        MessageManager.SendMessage(stream, MessageType.CONFIG, threadAmount.ToString());
        
        // Read response
        var (type, payload) = MessageManager.ReadMessage(stream);
        Console.WriteLine($"Server response of type \"{type}\": {payload}\n");
    }

    public void SendData(string payloadToSend)
    {
        if (_socket == null || !_socket.IsBound)
            return;
        
        // Send message
        using NetworkStream stream = new NetworkStream(_socket);
        
        MessageManager.SendMessage(stream, MessageType.DATA, payloadToSend);
        
        // Read response
        var (type, payload) = MessageManager.ReadMessage(stream);
        Console.WriteLine($"Server response of type \"{type}\": {payload}\n");
    }

    public void RequestDataProcessing()
    {
        if (_socket == null || !_socket.IsBound)
            return;
        
        // Send message
        using NetworkStream stream = new NetworkStream(_socket);
        
        MessageManager.SendMessage(stream, MessageType.START, "");
        
        // Read response
        var (type, payload) = MessageManager.ReadMessage(stream);
        Console.WriteLine($"Server response of type \"{type}\": {payload}\n");
    }

    public bool GetResult()
    {
        if (_socket == null || !_socket.IsBound)
            return false;
        
        // Send message
        using NetworkStream stream = new NetworkStream(_socket);
        
        MessageManager.SendMessage(stream, MessageType.RESULT, "");
        
        // Read response
        var (type, payload) = MessageManager.ReadMessage(stream);
        Console.WriteLine($"Server response of type \"{type}\": {payload}\n");
        
        //Fancy output
        if (type == MessageType.RESULT)
        {
            var res = JsonSerializer.Deserialize<List<List<int>>>(payload);
            
            if(res != null)
            {
                Console.WriteLine("Calculated result:");
                Program.PrintMatrix(res, res.Count, res[0].Count);
            }
            
            return true;
        }

        return false;
    }

    public void Disconnect()
    {
        if (_socket == null || !_socket.IsBound)
            return;
        
        // Send message
        using NetworkStream stream = new NetworkStream(_socket);
        
        MessageManager.SendMessage(stream, MessageType.DISCONNECT, "");
    }

    public void Dispose()
    {
        _socket?.Shutdown(SocketShutdown.Both);
        _socket?.Close();
        _socket?.Dispose();
    }
}