# Clean Code Guidelines & Best Practices

## ?? Project Architecture Overview

```
MyShopClient/
??? Models/
?   ??? Common/   ? Shared DTOs and base classes
?   ??? Products/        ? Product-related models
?   ??? Categories/      ? Category-related models
??? Services/
?   ??? Helpers/ ? Reusable utilities
?   ??? IProductService.cs
?   ??? ProductService.cs
?   ??? ... other services
??? ViewModels/
?   ??? Common/          ? BaseViewModel, ValidationHelper
?   ??? ProductListViewModel.cs
?   ??? ProductListViewModel.*.cs  ? Partial dialog states
??? Views/
    ??? ProductPage.xaml
```

---

## ?? Writing Clean Code in This Project

### 1. Creating New Models

#### Location
- **Shared types** ? `Models/Common/`
- **Domain-specific types** ? `Models/{DomainName}/`

#### Example - New Domain Model
```csharp
// Models/Suppliers/SupplierItemDto.cs
using System.Collections.Generic;

namespace MyShopClient.Models.Suppliers
{
    /// <summary>
    /// Supplier item DTO for list display
    /// </summary>
    public class SupplierItemDto
    {
        public int SupplierId { get; set; }
        public string Name { get; set; } = string.Empty;
 public string ContactEmail { get; set; } = string.Empty;
        public int ProductCount { get; set; }
    }
}
```

#### Pagination Template
```csharp
using MyShopClient.Models.Common;
using System.Collections.Generic;

namespace MyShopClient.Models.{YourDomain}
{
    /// <summary>
    /// Paginated {YourDomain} list response
    /// </summary>
    public class {YourDomain}PageDto : PaginationBase<{YourDomain}ItemDto>
    {
    }
}
```

---

### 2. Creating New Services

#### Template - GraphQL Service
```csharp
using MyShopClient.Models.Common;
using MyShopClient.Models.YourDomain;
using MyShopClient.Services.Helpers;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MyShopClient.Services
{
    /// <summary>
    /// Service for {YourDomain} GraphQL operations
    /// </summary>
    public class YourDomainService : GraphQlClientBase, IYourDomainService
    {
        // Internal payload wrapper classes
        private class GetItemsPayload
     {
            public ApiResult<YourDomainPageDto> Items { get; set; } = null!;
  }

        public YourDomainService(HttpClient httpClient, IServerConfigService serverConfig)
            : base(httpClient, serverConfig)
   {
        }

        /// <summary>
        /// Get paginated list of items
        /// </summary>
        public async Task<ApiResult<YourDomainPageDto>> GetItemsAsync(
         int page = 1,
          int pageSize = 10,
  string? search = null,
    CancellationToken cancellationToken = default)
        {
          var searchLiteral = GraphQlHelper.ToStringLiteral(search);

            var query = $@"
query GetItems {{
  items(
    pagination: {{ page: {page}, pageSize: {pageSize} }}
    search: {searchLiteral}
  ) {{
    statusCode
    success
    message
    data {{
      page
      pageSize
      totalItems
      totalPages
  items {{ 
  itemId
        name
      }}
    }}
  }}
}}";

   try
          {
  var payload = await PostGraphQlAsync<GetItemsPayload>(query, null, cancellationToken);
           return payload.Items;
 }
    catch (Exception ex)
            {
  return new ApiResult<YourDomainPageDto>
                {
     StatusCode = 500,
       Success = false,
           Message = ex.Message
         };
 }
        }
    }
}
```

#### Service Interface Pattern
```csharp
using MyShopClient.Models.Common;
using MyShopClient.Models.YourDomain;
using System.Threading;
using System.Threading.Tasks;

namespace MyShopClient.Services
{
    public interface IYourDomainService
    {
      /// <summary>
        /// Get paginated list with optional filtering
      /// </summary>
        Task<ApiResult<YourDomainPageDto>> GetItemsAsync(
            int page = 1,
 int pageSize = 10,
            string? search = null,
   CancellationToken cancellationToken = default);
  }
}
```

---

### 3. Creating New ViewModels

#### Inherit from BaseViewModel
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyShopClient.Services;
using MyShopClient.ViewModels.Common;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MyShopClient.ViewModels
{
    /// <summary>
    /// ViewModel for {YourDomain} management
    /// </summary>
    public partial class YourDomainViewModel : BaseViewModel
    {
        private readonly IYourDomainService _service;

      [ObservableProperty] 
        private ObservableCollection<YourDomainItemDto> items = new();

        [ObservableProperty] 
        private int currentPage = 1;

        [ObservableProperty] 
        private int totalPages = 1;

        public YourDomainViewModel(IYourDomainService service)
      {
            _service = service;
        _ = LoadItemsAsync();
    }

        [RelayCommand]
        private async Task LoadItemsAsync()
      {
       await ExecuteAsync(async () =>
{
       var result = await _service.GetItemsAsync(CurrentPage);
    
                if (!result.Success)
      {
       SetError(result.Message ?? "Failed to load items");
   return;
         }

     Items.Clear();
    foreach (var item in result.Data?.Items ?? new())
           {
         Items.Add(item);
      }

        CurrentPage = result.Data?.Page ?? 1;
TotalPages = result.Data?.TotalPages ?? 1;
     }, "Error loading items");
        }
    }
}
```

---

### 4. Adding Validation Rules

#### Extend ValidationHelper
```csharp
public static class ValidationHelper
{
    // Add new validation method
    
    /// <summary>
    /// Validate supplier email
    /// </summary>
  public static bool ValidateEmail(string? email, out string? error)
    {
        if (!IsRequired(email, out error))
     return false;

        if (!email!.Contains("@"))
  {
            error = "Please enter a valid email address.";
 return false;
     }

     error = null;
        return true;
    }
}
```

---

### 5. Dialog State Pattern

#### Template for New Dialog
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using MyShopClient.ViewModels.Common;

namespace MyShopClient.ViewModels
{
  /// <summary>
    /// Dialog state for {YourDomain} management
    /// </summary>
    public partial class YourDomainDialogState : ObservableObject
    {
        [ObservableProperty] 
        private string? dialogError;

        public bool HasError => !string.IsNullOrWhiteSpace(DialogError);

   [ObservableProperty] 
  private string? nameText;

  /// <summary>
        /// Validate all fields
        /// </summary>
        public bool Validate(out string? error)
        {
  error = null;

         if (!ValidationHelper.IsRequired(NameText, out var nameError))
    {
        error = nameError;
           return false;
            }

            return true;
        }

        /// <summary>
        /// Reset to empty state
        /// </summary>
   public void Reset()
        {
         DialogError = string.Empty;
         NameText = string.Empty;
        }
    }
}
```

---

## ?? Common Mistakes to Avoid

### ? Don't Do This

```csharp
// Bad: Duplicate validation logic
if (string.IsNullOrWhiteSpace(name))
{
    error = "Name is required";
    return false;
}
if (string.IsNullOrWhiteSpace(sku))
{
    error = "SKU is required";
 return false;
}

// Better: Use helper
if (!ValidationHelper.IsRequired(name, out error)) return false;
if (!ValidationHelper.IsRequired(sku, out error)) return false;
```

### ? Don't Do This

```csharp
// Bad: Duplicate GraphQL escaping
var name = value
  .Replace("\\", "\\\\")
    .Replace("\"", "\\\"");

// Better: Use helper
var literal = GraphQlHelper.ToStringLiteral(value);
```

### ? Don't Do This

```csharp
// Bad: Repeated try-catch in every method
try
{
    IsBusy = true;
    // ... operation
}
catch (Exception ex)
{
    ErrorMessage = ex.Message;
}
finally
{
    IsBusy = false;
}

// Better: Use BaseViewModel helper
await ExecuteAsync(async () => 
{
    // ... operation
});
```

### ? Don't Do This

```csharp
// Bad: Long ViewModel files
public partial class YourViewModel : ObservableObject
{
    // 1000+ lines of code...
    // Dialog logic mixed with list logic
}

// Better: Use partial classes
public partial class YourViewModel : BaseViewModel { }
public partial class YourViewModel { /* dialog state */ }
```

---

## ? Best Practices

### 1. Use XML Documentation
```csharp
/// <summary>
/// Validates product price
/// </summary>
/// <param name="priceText">Price as string</param>
/// <param name="error">Out error message</param>
/// <returns>True if valid</returns>
public static bool ValidatePrice(string? priceText, out string? error)
```

### 2. Meaningful Names
```csharp
// Good
public async Task<ApiResult<ProductPageDto>> GetProductsAsync(
    ProductQueryOptions options)

// Bad
public async Task<ApiResult<ProductPageDto>> GetAsync(dynamic opts)
```

### 3. Single Responsibility
```csharp
// Each class has ONE reason to change
GraphQlHelper   // Changes: GraphQL format changes
ProductService         // Changes: Product API changes
ValidationHelper     // Changes: Validation rules change
BaseViewModel          // Changes: Common ViewModel patterns
```

### 4. Async/Await Properly
```csharp
// Good: Named Task method
[RelayCommand]
private async Task LoadItemsAsync()
{
    await ExecuteAsync(async () => { ... });
}

// Bad: Fire and forget
_ = LoadItemsAsync();  // Only acceptable for fire-and-forget with proper handling
```

### 5. Null Safety
```csharp
// Good: Explicit null checks
if (category == null)
{
    error = "Please select a category";
 return false;
}

// Use nullable reference types
public string? OptionalField { get; set; }
public string RequiredField { get; set; } = string.Empty;
```

---

## ?? Code Review Checklist

When adding new features:

- [ ] Models in correct folder structure
- [ ] Services inherit from GraphQlClientBase (or appropriate base)
- [ ] ViewModels inherit from BaseViewModel
- [ ] Validation uses ValidationHelper
- [ ] GraphQL strings use GraphQlHelper
- [ ] XML documentation added
- [ ] No duplicate code (check for reusability)
- [ ] Error messages are user-friendly
- [ ] No async void methods
- [ ] Dialog logic in partial class
- [ ] Tests written for validation logic
- [ ] No magic strings or hardcoded values

---

## ?? Testing Examples

### Test Validation Helper
```csharp
[TestClass]
public class ValidationHelperTests
{
    [TestMethod]
 public void ValidateProductName_WithValidName_ReturnsTrue()
    {
  var result = ValidationHelper.ValidateProductName("Test Product", out var error);
        Assert.IsTrue(result);
        Assert.IsNull(error);
    }

    [TestMethod]
  public void ValidateProductName_WithEmpty_ReturnsFalse()
  {
        var result = ValidationHelper.ValidateProductName("", out var error);
    Assert.IsFalse(result);
        Assert.IsNotNull(error);
    }
}
```

### Test GraphQL Helper
```csharp
[TestClass]
public class GraphQlHelperTests
{
    [TestMethod]
    public void ToStringLiteral_WithSpecialChars_EscapesCorrectly()
    {
        var result = GraphQlHelper.ToStringLiteral("Test \"Quote\"");
        Assert.AreEqual("\"Test \\\"Quote\\\"\"", result);
    }
}
```

---

## ?? Performance Tips

1. **Caching** - Cache category list in service
2. **Pagination** - Always use pagination for large lists
3. **Lazy Loading** - Load details on demand
4. **Debouncing** - Debounce search input
5. **Virtual Lists** - Use virtualized lists for large datasets

---

## ?? Documentation Standards

### Every Service Method
```csharp
/// <summary>
/// Brief description of what the method does
/// </summary>
/// <param name="parameter">What the parameter is for</param>
/// <param name="cancellationToken">Cancellation token</param>
/// <returns>Description of return value</returns>
/// <exception cref="ArgumentNullException">When parameters are null</exception>
public async Task<ApiResult<T>> DoSomethingAsync(
    string parameter, 
    CancellationToken cancellationToken = default)
```

### Every ViewModel Class
```csharp
/// <summary>
/// Handles [feature name] UI state and business logic
/// </summary>
/// <remarks>
/// This ViewModel manages:
/// - Loading items from service
/// - Dialog state management
/// - Validation of user input
/// </remarks>
public partial class YourViewModel : BaseViewModel
```

---

## ?? Maintenance Guidelines

### Adding Feature Checklist
- [ ] Create models in appropriate folder
- [ ] Create service interface
- [ ] Implement service with GraphQlClientBase
- [ ] Create ViewModel inheriting from BaseViewModel
- [ ] Add validation to ValidationHelper if needed
- [ ] Create/update UI (View)
- [ ] Add XML documentation
- [ ] Test validation and basic flow
- [ ] Code review with team

### Refactoring Checklist
- [ ] All tests still pass
- [ ] No breaking changes to public APIs
- [ ] Documentation updated
- [ ] Code review completed
- [ ] Verified with functional testing

---

## ?? Additional Resources

- **MVVM Toolkit**: https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/
- **Clean Code**: Robert C. Martin's "Clean Code" book
- **SOLID Principles**: https://en.wikipedia.org/wiki/SOLID

---

**Last Updated**: [Date]
**Version**: 1.0
**Maintained By**: [Your Name/Team]
