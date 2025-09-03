using System;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RiffCore.Tracker;

public class UniversalRequestTracker : IUniversalRequestTracker
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<object>> _pendingRequests = new();
    private readonly JsonSerializerOptions _jsonOptions; 

    public UniversalRequestTracker()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = null
        };
    }

    public string CreatePendingRequest()
    {
        var correlationId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        
        if (!_pendingRequests.TryAdd(correlationId, tcs))
        {
            throw new InvalidOperationException($"Request with ID {correlationId} already exists");
        }
        
        _ = Task.Delay(TimeSpan.FromSeconds(30))
              .ContinueWith(_ => 
              {
                  if (_pendingRequests.TryRemove(correlationId, out var timeoutTcs))
                  {
                      timeoutTcs.TrySetException(new TimeoutException("Response timeout exceeded"));
                  }
              });
              
        return correlationId;
    }

    public bool TrySetResult(string correlationId, object result)
    {
        if (result == null)
        {
            return TrySetException(correlationId, new ArgumentNullException(nameof(result)));
        }

        if (_pendingRequests.TryRemove(correlationId, out var tcs))
        {
            return tcs.TrySetResult(result);
        }
        return false;
    }

    public bool TrySetException(string correlationId, Exception exception)
    {
        if (_pendingRequests.TryRemove(correlationId, out var tcs))
        {
            return tcs.TrySetException(exception);
        }
        return false;
    }


    public async Task<T> WaitForResponseAsync<T>(string correlationId)
    {
        if (!_pendingRequests.TryGetValue(correlationId, out var tcs))
        {
            throw new KeyNotFoundException($"Request with ID {correlationId} not found");
        }

        try
        {
            var result = await tcs.Task;
            
            if (result is T typedResult)
            {
                return typedResult;
            }

            if (result is string jsonString)
            {
                return JsonSerializer.Deserialize<T>(jsonString, _jsonOptions) 
                    ?? throw new InvalidOperationException("Deserialization returned null");
            }
            
            if (result is byte[] byteData)
            {
                var json = Encoding.UTF8.GetString(byteData);
                return JsonSerializer.Deserialize<T>(json, _jsonOptions) 
                    ?? throw new InvalidOperationException("Deserialization returned null");
            }
            
            throw new InvalidCastException($"Cannot convert {result.GetType().Name} to {typeof(T).Name}");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to deserialize response to type {typeof(T).Name}", ex);
        }
    }
    
    public bool TrySetJsonResult(string correlationId, string jsonData)
    {
        if (string.IsNullOrEmpty(jsonData))
        {
            return TrySetException(correlationId, new ArgumentException("JSON data cannot be null or empty"));
        }
        return TrySetResult(correlationId, jsonData);
    }

    public bool TrySetByteResult(string correlationId, byte[] data, Encoding encoding = null)
    {
        encoding ??= Encoding.UTF8;
        try
        {
            var jsonString = encoding.GetString(data);
            return TrySetJsonResult(correlationId, jsonString);
        }
        catch (DecoderFallbackException ex)
        {
            return TrySetException(correlationId, new InvalidOperationException("Failed to decode byte array", ex));
        }
    }
}