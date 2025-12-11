using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyShopClient.Models
{
    public record LoginResult(bool IsSuccess, string? ErrorMessage);

}
