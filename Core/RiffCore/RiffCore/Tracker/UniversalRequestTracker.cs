using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace RiffCore.Tracker;

public class UniversalRequestTracker : IUniversalRequestTracker
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<object>> _pendingRequests = new();
    private readonly JsonSerializerOptions _jsonOptions; 

    public UniversalRequestTracker()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true 
        };
    }

    public string CreatePendingRequest()
    {
        var correlationId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingRequests[correlationId] = tcs;
              
        return correlationId;
    }
    
    public bool TrySetResult(string correlationId, string jsonData)
    {
        if (_pendingRequests.TryRemove(correlationId, out var tcs))
        {
            return tcs.TrySetResult(jsonData);
        }
        return false;
    }
    
    public async Task<T> WaitForResponseAsync<T>(string correlationId)
    {
        if (_pendingRequests.TryGetValue(correlationId, out var tcs))
        {
            var jsonResult = await tcs.Task as string;
            
            try
            {
                var result = JsonSerializer.Deserialize<T>(jsonResult, _jsonOptions);
                if (result == null)
                {
                    throw new InvalidOperationException("Deserialization returned null");
                }
                return result;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to deserialize response to type {typeof(T).Name}", ex);
            }
        }
        throw new KeyNotFoundException($"Request with ID {correlationId} not found");
    }
    
    public bool TrySetResult(string correlationId, byte[] data)
    {
        if (_pendingRequests.TryRemove(correlationId, out var tcs))
        {
            return tcs.TrySetResult(data);
        }
        return false;
    }

    public async Task<T> WaitForResponseAsync<T>(string correlationId, Encoding encoding = null)
    {
        if (_pendingRequests.TryGetValue(correlationId, out var tcs))
        {
            var byteResult = await tcs.Task as byte[];
            encoding ??= Encoding.UTF8;
            
            try
            {
                var jsonString = encoding.GetString(byteResult);
                var result = JsonSerializer.Deserialize<T>(jsonString, _jsonOptions);
                if (result == null)
                {
                    throw new InvalidOperationException("Deserialization returned null");
                }
                return result;
            }
            catch (Exception ex) when (ex is JsonException || ex is DecoderFallbackException)
            {
                throw new InvalidOperationException($"Failed to deserialize response to type {typeof(T).Name}", ex);
            }
        }
        throw new KeyNotFoundException($"Request with ID {correlationId} not found");
    }
}