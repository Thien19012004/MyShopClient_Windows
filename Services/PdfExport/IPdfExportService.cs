using MyShopClient.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShopClient.Services.PdfExport
{
    public interface IPdfExportService
    {
 /// <summary>
        /// Export orders to PDF file
        /// </summary>
        Task<string> ExportOrdersToPdfAsync(List<OrderDetailDto> orders, string fileName);
    }
}
