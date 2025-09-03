using System.Text;

namespace RiffCore.Tracker;

public interface IUniversalRequestTracker
{
    string CreatePendingRequest();
    bool TrySetResult(string correlationId, object result);
    bool TrySetException(string correlationId, Exception exception);
    Task<T> WaitForResponseAsync<T>(string correlationId);
    bool TrySetJsonResult(string correlationId, string jsonData);
    bool TrySetByteResult(string correlationId, byte[] data, Encoding encoding = null);

}