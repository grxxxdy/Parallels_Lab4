using System.Text.Json;

namespace Client;

class Program
{
    static void Main(string[] args)
    {
        int threadAmount = 6;
        
        // Connect the client
        Client client = new Client();
        Console.WriteLine("\nTrying to connect to the server.");
        client.Connect("192.168.68.111", 5000);
        
        // Set config
        Console.WriteLine("Sending a request to update the config to the server.");
        client.UpdateConfig(threadAmount);
        
        // Create matrices
        DataPayload data = new DataPayload();
        int rows = 3, columns = 4, k = 10;
            
        for (int i = 0; i < rows; i++)
        {
            data.MatrixA.Add(new List<int>(new int[columns]));
            data.MatrixB.Add(new List<int>(new int[columns]));
        }

        data.K = k;
        
        FillWithRandsParallel(data.MatrixA, rows, columns, threadAmount);
        FillWithRandsParallel(data.MatrixB, rows, columns, threadAmount);
        
        Console.WriteLine("Created matrices:\nMatrix A:");
        PrintMatrix(data.MatrixA, rows, columns);
        Console.WriteLine("Matrix B:");
        PrintMatrix(data.MatrixB, rows, columns);
        
        // Pass data to server
        string jsonData = JsonSerializer.Serialize(data);
        Console.WriteLine("Sending data to the server.");
        client.SendData(jsonData);

        // Start calculation
        Console.WriteLine("Sending a request of processing data to the server.");
        client.RequestDataProcessing();

        // Get result
        while (true)
        {
            Console.WriteLine("Sending a result request to the server.");

            if (client.GetResult())
            {
                break;
            }
            
            Thread.Sleep(1000);
        }
        
        // Disconnect
        client.Disconnect();

        Console.ReadKey();
    }
    
    private static void FillWithRandsParallel(List<List<int>> matrix, int rows, int columns, int threadsAmount)
    {
        ThreadLocal<Random> rand = new ThreadLocal<Random>(() => new Random(Guid.NewGuid().GetHashCode()));
    
        // Divide the rows
        int rowsPerThread = rows / threadsAmount;
        Thread[] threads = new Thread[threadsAmount];
    
        // Start threads
        for (int t = 0; t < threadsAmount; t++)
        {
            int start = rowsPerThread * t;
            int finish = (t == threadsAmount - 1) ? rows : start + rowsPerThread;
        
            threads[t] = new Thread(() =>
            {
                var rnd = rand.Value;
                for (int i = start; i < finish; i++)
                {
                    for (int j = 0; j < columns; j++)
                    {
                        matrix[i][j] = rnd.Next(1, 11);
                    }
                }
            });

            threads[t].Start();
        }
    
        // Wait for threads
        foreach (var thread in threads)
        {
            thread.Join();
        }
    }

    public static void PrintMatrix(List<List<int>> matrix, int rows, int columns)
    {
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                Console.Write(matrix[i][j] + "\t");
            }
            
            Console.WriteLine();
        }
        
        Console.WriteLine();

    }
}