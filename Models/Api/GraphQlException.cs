using System;

namespace MyShopClient.Models
{
    public class GraphQlException : Exception
    {
        public GraphQlError[]? Errors { get; }

        public GraphQlException(string message, GraphQlError[]? errors = null)
            : base(message)
        {
            Errors = errors;
        }
    }
}
