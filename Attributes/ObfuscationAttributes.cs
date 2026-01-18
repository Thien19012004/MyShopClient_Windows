using System;

namespace MyShopClient.Attributes
{
    /// <summary>
    /// ?ánh d?u class/method/property này s? KHÔNG b? rename khi obfuscate.
    /// S? d?ng cho các thành ph?n:
    /// - ???c g?i qua Reflection
  /// - ???c bind trong XAML
    /// - ???c serialize/deserialize JSON
    /// - ???c ??ng ký v?i DI container
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Class | 
      AttributeTargets.Method | 
        AttributeTargets.Property | 
        AttributeTargets.Field | 
AttributeTargets.Event |
        AttributeTargets.Interface |
  AttributeTargets.Struct |
        AttributeTargets.Enum,
        AllowMultiple = false,
        Inherited = false)]
    public sealed class ObfuscationExcludeAttribute : Attribute
    {
        /// <summary>
        /// Lý do exclude kh?i obfuscation (?? documentation)
        /// </summary>
        public string? Reason { get; set; }

        public ObfuscationExcludeAttribute() { }

        public ObfuscationExcludeAttribute(string reason)
        {
      Reason = reason;
        }
    }

    /// <summary>
 /// ?ánh d?u assembly này có s? d?ng obfuscation
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class ObfuscateAssemblyAttribute : Attribute
  {
        public bool AssemblyIsPrivate { get; }
        public bool StripAfterObfuscation { get; set; } = true;

        public ObfuscateAssemblyAttribute(bool assemblyIsPrivate)
        {
            AssemblyIsPrivate = assemblyIsPrivate;
        }
 }
}
