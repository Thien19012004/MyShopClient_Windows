using System;
using System.Diagnostics;

namespace MyShopClient.Services.Connection
{
    /// <summary>
    /// Handles connection errors globally and notifies subscribers
    /// </summary>
    public class ConnectionErrorHandler : IConnectionErrorHandler
{
     public event EventHandler<ConnectionErrorEventArgs>? ConnectionErrorOccurred;

        public void NotifyConnectionError(Exception exception, string? message = null)
        {
  Debug.WriteLine($"[ConnectionErrorHandler] Connection error: {exception.GetType().Name} - {exception.Message}");
    ConnectionErrorOccurred?.Invoke(this, new ConnectionErrorEventArgs(exception, message));
        }
    }
}
