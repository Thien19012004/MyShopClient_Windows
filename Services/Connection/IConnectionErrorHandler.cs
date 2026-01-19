using System;

namespace MyShopClient.Services.Connection
{
  /// <summary>
    /// Interface for handling connection errors globally
    /// </summary>
    public interface IConnectionErrorHandler
    {
        /// <summary>
  /// Event fired when a connection error occurs (e.g., server unreachable)
        /// </summary>
 event EventHandler<ConnectionErrorEventArgs>? ConnectionErrorOccurred;

      /// <summary>
        /// Notify that a connection error has occurred
      /// </summary>
        void NotifyConnectionError(Exception exception, string? message = null);
    }

    public class ConnectionErrorEventArgs : EventArgs
    {
        public Exception Exception { get; }
        public string Message { get; }

        public ConnectionErrorEventArgs(Exception exception, string? message = null)
        {
       Exception = exception;
          Message = message ?? GetDefaultMessage(exception);
        }

        private static string GetDefaultMessage(Exception ex)
      {
     return ex switch
      {
   System.Net.Sockets.SocketException => "Cannot connect to server. Please check your network connection and server configuration.",
         System.Net.Http.HttpRequestException => "Network error occurred. Please try again.",
            TimeoutException => "Request timed out. Please try again.",
              _ => "A connection error occurred."
      };
        }
    }
}
