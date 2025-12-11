# Quick Reference Guide - Clean Code Project

## ?? When You Need To...

### Add a New Model/DTO
1. Create file in `Models/{Domain}/{Name}.cs`
2. Use namespace: `MyShopClient.Models.{Domain}`
3. Add XML summary comments
4. For paginated results: inherit from `PaginationBase<T>`

```csharp
// Example location: Models/Orders/OrderItemDto.cs
namespace MyShopClient.Models.Orders
{
    public class OrderItemDto
    {
        public int OrderId { get; set; }
   public string OrderNumber { get; set; } = string.Empty;
    }
}
```

---

### Create a New Service
1. Create `Services/YourDomainService.cs`
2. Create `Services/IYourDomainService.cs` interface
3. Inherit from `GraphQlClientBase`
4. Use helper methods for GraphQL operations

```csharp
public class OrderService : GraphQlClientBase, IOrderService
{
    public OrderService(HttpClient http, IServerConfigService config) 
        : base(http, config) { }

    public async Task<ApiResult<OrderPageDto>> GetOrdersAsync(...)
    {
   var query = $@"query Get Orders {{ ... }}";
        try 
   {
          var payload = await PostGraphQlAsync<GetOrdersPayload>(query, ...);
    return payload.Orders;
        }
     catch (Exception ex) { ... }
    }
}
```

---

### Create a New ViewModel
1. Create `ViewModels/YourNameViewModel.cs`
2. Inherit from `BaseViewModel`
3. Use `[ObservableProperty]` for properties
4. Use `[RelayCommand]` for commands

```csharp
public partial class OrderViewModel : BaseViewModel
{
    private readonly IOrderService _service;
    
    [ObservableProperty] private ObservableCollection<OrderItemDto> orders = new();
  
    public OrderViewModel(IOrderService service)
    {
        _service = service;
  _ = LoadOrdersAsync();
    }
    
    [RelayCommand]
    private async Task LoadOrdersAsync()
    {
  await ExecuteAsync(async () =>
        {
      var result = await _service.GetOrdersAsync();
            if (result.Success)
              // Update Orders
        });
    }
}
```

---

### Validate User Input
Use `ValidationHelper` for consistency:

```csharp
// Single validation
if (!ValidationHelper.ValidateProductName(name, out var error))
{
    ErrorMessage = error;
    return;
}

// Multiple validations
string? firstError = null;
if (!ValidationHelper.IsRequired(field1, out var e1)) firstError = e1;
else if (!ValidationHelper.ValidatePrice(field2, out var e2)) firstError = e2;
else if (!ValidationHelper.ValidateCategorySelection(field3, out var e3)) firstError = e3;

if (firstError != null)
{
    ErrorMessage = firstError;
    return;
}
```

---

### Handle GraphQL Strings
Use helpers to avoid manual escaping:

```csharp
// ? Don't do this manually
var name = value.Replace("\\", "\\\\").Replace("\"", "\\\"");

// ? Use helper
var nameLiteral = GraphQlHelper.ToStringLiteral(value);
var priceLiteral = GraphQlHelper.ToNullableIntLiteral(maxPrice);
var ascLiteral = GraphQlHelper.ToBoolLiteral(isAscending);

// In query
var query = $@"
    query GetItems {{
      items(search: {nameLiteral}, maxPrice: {priceLiteral}, asc: {ascLiteral})
    }}
";
```

---

### Handle Async Operations with Error Management
Use `BaseViewModel.ExecuteAsync`:

```csharp
// Simple operation (no return value)
await ExecuteAsync(async () =>
{
    var result = await _service.DoSomethingAsync();
    if (!result.Success)
 SetError(result.Message);
}, "Operation failed");

// Operation with return value
var data = await ExecuteAsync(async () =>
{
    var result = await _service.GetDataAsync();
    return result.Success ? result.Data : null;
}, "Failed to fetch data");

// Error is automatically set in catch block
// IsBusy is automatically managed
```

---

### Create a Dialog/Modal
1. Create partial class: `YourViewModel.DialogState.cs`
2. Encapsulate dialog properties
3. Include validation logic
4. Include conversion to Input DTOs

```csharp
public partial class YourViewModel : ObservableObject
{
    [ObservableProperty] private string? name;
    [ObservableProperty] private string? error;
    public bool HasError => !string.IsNullOrWhiteSpace(Error);
    
    public bool Validate(out string? error)
    {
        if (!ValidationHelper.IsRequired(Name, out error))
    return false;
        return true;
 }
    
    public YourCreateInput ToInput()
    {
  return new() { Name = Name! };
    }
}
```

---

### Update XAML Namespaces
After moving models to organized folders:

```xaml
<!-- Old -->
xmlns:models="using:MyShopClient.Models"

<!-- New - specific to domain -->
xmlns:models="using:MyShopClient.Models.Products"
xmlns:catmodels="using:MyShopClient.Models.Categories"
xmlns:orders="using:MyShopClient.Models.Orders"

<!-- Usage -->
<DataTemplate x:DataType="models:ProductItemDto">
<DataTemplate x:DataType="catmodels:CategoryItemDto">
<DataTemplate x:DataType="orders:OrderItemDto">
```

---

## ?? Finding Things

### Where are models?
```
Models/Common/       ? ApiResult, PaginationBase
Models/Products/ ? ProductItemDto, ProductCreateInput, etc.
Models/Categories/   ? CategoryItemDto, CategoryCreateInput, etc.
Models/Orders/       ? (Future) OrderItemDto, etc.
```

### Where are services?
```
Services/IProductService.cs     ? Interface
Services/ProductService.cs      ? Implementation (inherits GraphQlClientBase)
Services/Helpers/               ? Shared utilities
```

### Where are ViewModels?
```
ViewModels/Common/BaseViewModel.cs      ? Base class
ViewModels/Common/ValidationHelper.cs        ? Validation logic
ViewModels/ProductListViewModel.cs           ? Main logic
ViewModels/ProductListViewModel.AddDialogState.cs ? Dialog state
```

---

## ?? Folder Structure

```
MyShopClient/
??? Models/
?   ??? Common/
? ?   ??? ApiResult.cs
?   ?   ??? PaginationBase.cs
?   ??? Products/
?   ?   ??? ProductItemDto.cs
?   ?   ??? ProductPageResult.cs
?   ?   ??? ProductCreateUpdateInput.cs
? ?   ??? ProductQueryOptions.cs
?   ??? Categories/
?       ??? CategoryModels.cs
??? Services/
?   ??? Helpers/
?   ?   ??? GraphQlHelper.cs
?   ?   ??? GraphQlClientBase.cs
?   ?   ??? ProductSortHelper.cs
?   ??? IProductService.cs
?   ??? ProductService.cs
?   ??? ICategoryService.cs
?   ??? CategoryService.cs
?   ??? (other services)
??? ViewModels/
?   ??? Common/
?   ?   ??? BaseViewModel.cs
?   ?   ??? ValidationHelper.cs
?   ??? ProductListViewModel.cs
?   ??? ProductListViewModel.AddDialogState.cs
?   ??? ProductListViewModel.EditDialogState.cs
?   ??? ProductListViewModel.CategoryDialogState.cs
??? Views/
    ??? ProductPage.xaml(cs)
```

---

## ??? Common Tasks - Copy & Paste Ready

### Template: Add Validation Rule
```csharp
// In ValidationHelper.cs
/// <summary>
/// Validate [field name]
/// </summary>
public static bool Validate[FieldName]([Type] value, out string? error)
{
    if (/* validation fails */)
    {
        error = "[Error message]";
        return false;
    }
    
    error = null;
    return true;
}

// Usage in ViewModel
if (!ValidationHelper.Validate[FieldName](value, out var error))
{
    DialogError = error;
    return;
}
```

### Template: Add API Method to Service
```csharp
/// <summary>
/// [What this does]
/// </summary>
public async Task<ApiResult<T>> [MethodName]Async(
    [Parameters],
    CancellationToken cancellationToken = default)
{
    const string query = @"
query [QueryName]([Variables]) {
  [operation] {
    statusCode
    success
    message
    data { [fields] }
  }
}";

    var variables = new { [variables] };
    
    try
    {
        var payload = await PostGraphQlAsync<[PayloadType]>(query, variables, cancellationToken);
   return payload.[Field];
    }
    catch (Exception ex)
    {
        return new ApiResult<T>
        {
  StatusCode = 500,
    Success = false,
  Message = ex.Message
        };
    }
}
```

---

## ?? What NOT To Do

| Don't | Do Instead |
|-------|-----------|
| Mix dialog logic in main ViewModel | Create dialog state in partial class |
| Duplicate validation code | Use ValidationHelper |
| Manual GraphQL string escaping | Use GraphQlHelper |
| try-catch in every method | Use BaseViewModel.ExecuteAsync |
| Flat Models folder | Organize by domain (Products, Categories, etc.) |
| Service-specific helpers | Put in Services/Helpers/ for reuse |
| Long method names | Use clear, concise names with good documentation |
| Magic strings | Use constants and enums |

---

## ?? File Dependencies

### ProductListViewModel depends on:
```
??? Services/IProductService.cs
??? Services/ProductService.cs
??? Services/ICategoryService.cs
??? Services/CategoryService.cs
??? ViewModels/Common/BaseViewModel.cs
??? ViewModels/Common/ValidationHelper.cs
??? Models/Products/*
??? Models/Categories/*
??? Models/Common/*
```

### ProductService depends on:
```
??? Services/Helpers/GraphQlClientBase.cs
??? Services/Helpers/GraphQlHelper.cs
??? Services/Helpers/ProductSortHelper.cs
??? Models/Products/*
??? Models/Common/ApiResult.cs
```

---

## ?? Key Classes Reference

### BaseViewModel
```csharp
public abstract partial class BaseViewModel : ObservableObject
{
    protected bool IsBusy { get; set; }
    protected string? ErrorMessage { get; set; }
    public bool HasError { get; }
    
    protected void ClearError()
    protected void SetError(string? message)
    protected async Task ExecuteAsync(Func<Task> operation, string? errorPrefix = null)
    protected async Task<T?> ExecuteAsync<T>(Func<Task<T>> operation, string? errorPrefix = null)
}
```

### ValidationHelper
```csharp
public static class ValidationHelper
{
    // Returns: bool Validate(string? value, out string? error)
    static methods:
    - IsRequired(...)
    - ValidateProductName(...)
 - ValidateProductSku(...)
    - ValidatePrice(..., string fieldName)
    - ValidateStockQuantity(...)
    - ValidateCategorySelection<T>(...)
}
```

### GraphQlHelper
```csharp
public static class GraphQlHelper
{
    // Parsing
    static T ExtractData<T>(string jsonContent)
    
    // String utilities
    static string ToStringLiteral(string? value)
    static string ToNullableIntLiteral(int? value)
    static string ToBoolLiteral(bool value)
}
```

### GraphQlClientBase
```csharp
public abstract class GraphQlClientBase
{
    protected async Task<TPayload> PostGraphQlAsync<TPayload>(
        string query,
   object? variables = null,
      CancellationToken cancellationToken = default)
}
```

---

## ?? Pro Tips

1. **Use IntelliSense** - Type `ValidationHelper.` to see all validation methods
2. **XML Docs** - Hover over methods to see documentation
3. **Rename Refactoring** - Use Visual Studio's rename feature when changing names
4. **Find All References** - Right-click ? "Find All References" to see usage
5. **Go To Definition** - F12 to jump to class/method definitions
6. **Code Snippets** - Create custom snippets for templates

---

## ?? Debugging Tips

### Check UI Binding Issues
```csharp
// In ViewModel
[ObservableProperty] private string? myValue = "test";

// In XAML
<TextBlock Text="{Binding MyValue}" />

// Make sure property name matches (MyValue not myValue)
```

### GraphQL Query Errors
```csharp
// Copy generated query to GraphQL testing tool
var query = BuildGetProductsQuery(options);
System.Diagnostics.Debug.WriteLine(query);  // View output window
```

### Service Not Registered
```csharp
// In App.xaml.cs, check ServiceCollection
services.AddSingleton<IYourService, YourService>();

// If missing, add it!
```

---

## ?? Quick Help

**Q: Where do I put new validation rules?**
A: `ViewModels/Common/ValidationHelper.cs`

**Q: How do I add a new API endpoint?**
A: Create method in Service, inherit from `GraphQlClientBase`, use `PostGraphQlAsync<T>`

**Q: The build fails with namespace errors.**
A: Check that Models are in correct folder structure and XAML namespaces are updated

**Q: How do I avoid duplicating validation logic?**
A: Always use `ValidationHelper` methods

**Q: My ViewModel is too long, what should I do?**
A: Extract dialog logic to partial classes (see `ProductListViewModel.*.cs` examples)

---

**Version**: 1.0
**Last Updated**: 2024
**Difficulty**: ?? (Intermediate)

