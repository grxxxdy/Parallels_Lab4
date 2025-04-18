namespace Server;

public class DataManager
{
    private static readonly Dictionary<long, DataPayload> _clientData = new();
    private static int _threadAmount = 1;
    private static readonly object _threadAmountLock = new();
    
    public static void StoreClientData(long clientId, DataPayload data)
    {
        lock (_clientData)
        {
            _clientData[clientId] = data;
        }
    }

    public static void UpdateThreadAmount(int newAmnt)
    {
        lock(_threadAmountLock)
        {
            _threadAmount = newAmnt;
        }
    }

    public static DataPayload? GetClientData(long clientId)
    {
        lock (_clientData)
        {
            return _clientData.TryGetValue(clientId, out var data) ? data : null;
        }
    }

    public static void RemoveClientData(long clientId)
    {
        lock (_clientData)
        {
            _clientData.Remove(clientId);
        }
    }

    public static List<List<int>>? ProcessData(long clientId)
    {
        List<List<int>> matrixA, matrixB;
        int k;
            
        lock (_clientData)
        {
            if (_clientData[clientId] == null)
                return null;

            // Get data
            matrixA = _clientData[clientId].MatrixA.Select(row => new List<int>(row)).ToList(); 
            matrixB = _clientData[clientId].MatrixB.Select(row => new List<int>(row)).ToList();
            k = _clientData[clientId].K;
        }

        // Divide rows
        int rows = matrixA.Count, columns = matrixA[0].Count;
        int rowsPerThread = rows / _threadAmount;
        Thread[] threads = new Thread[_threadAmount];
        
        // Res matrix
        List<List<int>> matrixResult = new List<List<int>>();
        for (int i = 0; i < rows; i++)
        {
            matrixResult.Add(new List<int>(new int[columns]));
        }
        
        // Start threads
        for (int t = 0; t < _threadAmount; t++)
        {
            int start = rowsPerThread * t;
            int finish = (t == _threadAmount - 1) ? rows : start + rowsPerThread;
        
            threads[t] = new Thread(() =>
            {
                for (int i = start; i < finish; i++)
                {
                    for (int j = 0; j < columns; j++)
                    {
                        matrixResult[i][j] = matrixA[i][j] - k * matrixB[i][j];
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

        return matrixResult;
    }
}