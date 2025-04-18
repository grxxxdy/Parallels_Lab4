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
        
        Console.WriteLine($"\nServer running on {ip}:{port}.");

        while (true)
        {
            var client = _socket.Accept();
            Console.WriteLine("\nClient connected.");

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
                Console.WriteLine($"\nReceived a message of type {type} from client {clientId}: {payload}");

                MessageType typeToSend = MessageType.UNKNOWN;
                string payloadToSend = "";
                
                switch (type)
                {
                    case MessageType.CONNECT:
                        typeToSend = MessageType.CONNECT;
                        payloadToSend = "Connected successfully!";
                        break;
                    case MessageType.CONFIG:
                        typeToSend = MessageType.CONFIG;
                        
                        if (int.TryParse(payload, out int threadAmount))
                        {
                            DataManager.UpdateThreadAmount(threadAmount);
                            payloadToSend = $"Thread config updated successfully. Thread amount is set to {threadAmount}.";
                        }
                        else
                        {
                            payloadToSend = "Invalid number of threads.";
                        }
                        break;
                    case MessageType.DATA:
                        var data = JsonSerializer.Deserialize<DataPayload>(payload);
                        DataManager.StoreClientData(clientId, data);
                        
                        typeToSend = MessageType.DATA;
                        payloadToSend = "Server received your data.";
                        break;
                    case MessageType.START:
                        res = Task.Run(() => DataManager.ProcessData(clientId)); 
                        
                        typeToSend = MessageType.START;
                        payloadToSend = "Server started processing your data.";
                        break;
                    case MessageType.RESULT:
                        typeToSend = MessageType.DATA;
                        
                        if(res == null)
                        {
                            payloadToSend = "Data processing has not been started yet.";
                        }
                        else if (!res.IsCompleted)
                        {
                            payloadToSend = "Data is still processing.";
                        }
                        else
                        {
                            if (res.Result == null)
                            {
                                payloadToSend = "Data has not been processed: your data was empty.";
                            }
                            else
                            {
                                var jsonData = JsonSerializer.Serialize(res.Result);
                                
                                typeToSend = MessageType.RESULT;
                                payloadToSend = jsonData;
                            }
                        }
                        break;
                    case MessageType.DISCONNECT:
                        DataManager.RemoveClientData(clientId);
                        clientSocket.Shutdown(SocketShutdown.Both);
                        clientSocket.Close();
                        return;
                }
                
                MessageManager.SendMessage(stream, typeToSend, payloadToSend);
                Console.WriteLine($"Sent a message of type {typeToSend} to client {clientId}: \"{payloadToSend}\"");
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