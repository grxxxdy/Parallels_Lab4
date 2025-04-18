namespace Server;

public class DataPayload
{
    public List<List<int>> MatrixA { get; set; } = new();
    public List<List<int>> MatrixB { get; set; } = new();
    public int K { get; set; }
}