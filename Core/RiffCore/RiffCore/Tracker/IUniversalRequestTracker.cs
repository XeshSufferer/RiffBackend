using System.Text;

namespace RiffCore.Tracker;

public interface IUniversalRequestTracker
{
    string CreatePendingRequest();
    bool TrySetResult(string correlationId, string jsonData);
    Task<T> WaitForResponseAsync<T>(string correlationId);
    bool TrySetResult(string correlationId, byte[] data);
    Task<T> WaitForResponseAsync<T>(string correlationId, Encoding encoding = null);

}