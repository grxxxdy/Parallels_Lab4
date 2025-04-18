using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;

namespace Server;

public class Server: IDisposable
{
    private Socket? _socket;

    public void StartTcp(int port)
    {
        string ip = GetLocalIpAddress();
        
        // Socket creation
        var tcpEndpoint = new IPEndPoint(IPAddress.Parse(ip), port);
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        
        // Socket binding
        _socket.Bind(tcpEndpoint);
        _socket.Listen(10);
        
        Console.WriteLine($"Server running on {ip}:{port}.");

        while (true)
        {
            var client = _socket.Accept();
            Console.WriteLine("Client conneted.");

            Thread clientThread = new Thread(() => HandleClient(client));
            clientThread.Start();
        }
    }

    private void HandleClient(Socket clientSocket)
    {
        try
        {
            using NetworkStream stream = new NetworkStream(clientSocket);
            long clientId = clientSocket.Handle.ToInt64();
            Task<List<List<int>>?>? res = null;

            while (true)
            {
                var (type, payload) = MessageManager.ReadMessage(stream);
                Console.WriteLine($"Received a message of type {type} from client: {payload}");

                switch (type)
                {
                    case MessageType.CONNECT:
                        MessageManager.SendMessage(stream, MessageType.CONNECT, "Connected successfully!");
                        break;
                    case MessageType.DATA:
                        var data = JsonSerializer.Deserialize<DataPayload>(payload);
                        DataManager.StoreClientData(clientId, data);
                        MessageManager.SendMessage(stream, MessageType.DATA, "Server received your data!");
                        break;
                    case MessageType.START:
                        res = Task.Run(() => DataManager.ProcessData(clientId, 6)); 
                        MessageManager.SendMessage(stream, MessageType.START, "Server started processing your data!");
                        break;
                    case MessageType.RESULT:
                        if(res == null)
                        {
                            MessageManager.SendMessage(stream, MessageType.DATA, "Data processing has not been started yet!");
                        }
                        else if (!res.IsCompleted)
                        {
                            MessageManager.SendMessage(stream, MessageType.DATA, "Data is still processing!");
                        }
                        else
                        {
                            if (res.Result == null)
                            {
                                MessageManager.SendMessage(stream, MessageType.DATA, "Data has not been processed: your data was empty!");
                            }
                            else
                            {
                                var jsonData = JsonSerializer.Serialize(res.Result);
                                MessageManager.SendMessage(stream, MessageType.RESULT, jsonData);
                            }
                        }
                        break;
                    case MessageType.DISCONNECT:
                        clientSocket.Shutdown(SocketShutdown.Both);
                        clientSocket.Close();
                        return;
                }
            }
        }
        catch (Exception ex)
        {
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
            Console.WriteLine($"Server error: {ex.Message}");
        }
    }
    
    private static string GetLocalIpAddress()
    {
        foreach (NetworkInterface netInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (netInterface.OperationalStatus != OperationalStatus.Up ||
                netInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                netInterface.NetworkInterfaceType == NetworkInterfaceType.Tunnel ||
                netInterface.Description.Contains("VPN") ||
                netInterface.Description.Contains("Virtual"))
            {
                continue;
            }

            foreach (UnicastIPAddressInformation ip in netInterface.GetIPProperties().UnicastAddresses)
            {
                if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.Address.ToString();
                }
            }
        }

        throw new Exception("No active network adapters with a valid IPv4 address found!");
    }

    public void Dispose()
    {
        _socket?.Shutdown(SocketShutdown.Both);
        _socket?.Close();
        _socket?.Dispose();
    }
}