using MyShopClient.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyShopClient.Services.PdfExport
{
    public class PdfExportService : IPdfExportService
    {
        public PdfExportService()
        {
// Set license for QuestPDF (Community license is free for non-commercial use)
       QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<string> ExportOrdersToPdfAsync(List<OrderDetailDto> orders, string fileName)
        {
          return await Task.Run(() =>
            {
                // Get Downloads folder path
 var downloadsPath = Path.Combine(
       Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads");

          // Ensure unique filename
            var fullPath = Path.Combine(downloadsPath, fileName);
    if (File.Exists(fullPath))
        {
        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
         var ext = Path.GetExtension(fileName);
  var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
       fullPath = Path.Combine(downloadsPath, $"{nameWithoutExt}_{timestamp}{ext}");
      }

         // Create PDF document
              Document.Create(container =>
       {
    container.Page(page =>
          {
        page.Size(PageSizes.A4);
     page.Margin(30);
     page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(10));

    page.Header()
    .AlignCenter()
      .Text("ORDER INVOICE")
           .FontSize(20)
   .Bold()
    .FontColor(Colors.Blue.Medium);

           page.Content()
           .Column(column =>
    {
     foreach (var order in orders)
  {
       // Order header
        column.Item().PaddingVertical(10).Row(row =>
 {
        row.RelativeItem().Column(col =>
     {
    col.Item().Text($"Order ID: #{order.OrderId}").Bold().FontSize(14);
       col.Item().Text($"Customer: {order.CustomerName}");
         col.Item().Text($"Sale: {order.SaleName}");
           });

          row.RelativeItem().Column(col =>
  {
     col.Item().AlignRight().Text($"Status: {order.Status}").Bold();
          col.Item().AlignRight().Text($"Date: {order.CreatedAt:yyyy-MM-dd HH:mm}");
       });
       });

     // Order items table
      column.Item().Table(table =>
                  {
    // Define columns
 table.ColumnsDefinition(columns =>
        {
 columns.ConstantColumn(40);  // No.
   columns.RelativeColumn(3);   // Product
          columns.ConstantColumn(80);  // Quantity
     columns.ConstantColumn(100); // Price
          columns.ConstantColumn(100); // Total
          });

 // Header
              table.Header(header =>
               {
    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("No.").Bold();
            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Product").Bold();
           header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignCenter().Text("Quantity").Bold();
        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Price").Bold();
           header.Cell().Background(Colors.Grey.Lighten2).Padding(5).AlignRight().Text("Total").Bold();
    });

        // Items
    int itemNo = 1;
foreach (var item in order.Items)
   {
      var bgColor = itemNo % 2 == 0 ? Colors.Grey.Lighten4 : Colors.White;

        table.Cell().Background(bgColor).Padding(5).Text(itemNo.ToString());
   table.Cell().Background(bgColor).Padding(5).Text(item.ProductName);
       table.Cell().Background(bgColor).Padding(5).AlignCenter().Text(item.Quantity.ToString());
         table.Cell().Background(bgColor).Padding(5).AlignRight().Text($"{item.UnitPrice:N0} VND");
       table.Cell().Background(bgColor).Padding(5).AlignRight().Text($"{item.Quantity * item.UnitPrice:N0} VND");

   itemNo++;
}
    });

        // Total
column.Item().PaddingTop(10).AlignRight().Text($"Total Amount: {order.TotalPrice:N0} VND")
     .FontSize(14).Bold().FontColor(Colors.Blue.Medium);

        // Separator between orders
         if (order != orders.Last())
       {
      column.Item().PaddingVertical(15).LineHorizontal(1).LineColor(Colors.Grey.Medium);
      }
                }
       });

  page.Footer()
      .AlignCenter()
     .Text(text =>
 {
           text.Span("Page ");
                 text.CurrentPageNumber();
      text.Span(" / ");
       text.TotalPages();
           text.Span($" - Generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
   });
        });
                })
              .GeneratePdf(fullPath);

                return fullPath;
            });
        }
    }
}
