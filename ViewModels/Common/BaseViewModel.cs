using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Threading.Tasks;

namespace MyShopClient.ViewModels.Common
{
    /// <summary>
    /// Base ViewModel with common error handling and busy state management
    /// </summary>
    public abstract partial class BaseViewModel : ObservableObject
{
        [ObservableProperty]
        protected bool isBusy;

   [ObservableProperty]
     protected string? errorMessage;

      public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        /// <summary>
        /// Clear error message
        /// </summary>
 protected void ClearError() => ErrorMessage = string.Empty;

        /// <summary>
    /// Set error message
    /// </summary>
        protected void SetError(string? message)
      {
 ErrorMessage = message;
      OnPropertyChanged(nameof(HasError));
        }

        /// <summary>
  /// Execute an async operation with busy state and error handling
    /// </summary>
        protected async Task ExecuteAsync(Func<Task> operation, string? errorMessagePrefix = null)
     {
  try
      {
    IsBusy = true;
      ClearError();
    await operation();
  }
  catch (Exception ex)
    {
  SetError(errorMessagePrefix != null ? $"{errorMessagePrefix}: {ex.Message}" : ex.Message);
  }
            finally
          {
       IsBusy = false;
     }
        }

        /// <summary>
  /// Execute an async operation and return result with busy state and error handling
        /// </summary>
  protected async Task<T?> ExecuteAsync<T>(Func<Task<T>> operation, string? errorMessagePrefix = null) where T : class
        {
   try
      {
  IsBusy = true;
    ClearError();
     return await operation();
      }
  catch (Exception ex)
 {
       SetError(errorMessagePrefix != null ? $"{errorMessagePrefix}: {ex.Message}" : ex.Message);
     return null;
  }
       finally
      {
       IsBusy = false;
    }
  }
    }
}
