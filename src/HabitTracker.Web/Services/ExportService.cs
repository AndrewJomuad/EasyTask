using System.Text;
using ClosedXML.Excel;
using HabitTracker.Web.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HabitTracker.Web.Services;

public interface IExportService
{
    byte[] BuildPdf(ReportsViewModel report, string userName);
    byte[] BuildExcel(ReportsViewModel report, string userName);
    byte[] BuildCsv(ReportsViewModel report);
}

public class ExportService : IExportService
{
    static ExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // ------------------------------------------------------------- PDF
    public byte[] BuildPdf(ReportsViewModel report, string userName)
    {
        var a = report.Analytics;

        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(t => t.FontSize(9).FontFamily(Fonts.SegoeUI));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Habit Tracking Management System").FontSize(18).Bold().FontColor("#1D4ED8");
                            c.Item().Text("Progress Report").FontSize(12).SemiBold().FontColor("#334155");
                        });
                        row.ConstantItem(160).AlignRight().Column(c =>
                        {
                            c.Item().Text($"Generated {DateTime.Now:dd MMM yyyy HH:mm}").FontSize(8).FontColor("#64748B");
                            c.Item().Text($"User: {userName}").FontSize(8).FontColor("#64748B");
                        });
                    });
                    col.Item().PaddingTop(6).LineHorizontal(1).LineColor("#CBD5E1");
                    col.Item().PaddingTop(6).Text($"Period: {report.From:dd MMM yyyy} — {report.To:dd MMM yyyy}")
                        .FontSize(9).FontColor("#475569");
                });

                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Spacing(12);

                    // Summary cards
                    col.Item().Row(row =>
                    {
                        SummaryBox(row, "Scheduled", a.TotalScheduled.ToString(), "#1D4ED8");
                        SummaryBox(row, "Completed", a.TotalCompleted.ToString(), "#16A34A");
                        SummaryBox(row, "Skipped", a.TotalSkipped.ToString(), "#F59E0B");
                        SummaryBox(row, "Missed", a.TotalMissed.ToString(), "#DC2626");
                        SummaryBox(row, "Completion", $"{a.CompletionRate:0.#}%", "#0EA5E9");
                    });

                    col.Item().Text("Habit performance").FontSize(12).Bold().FontColor("#0F172A");

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn(2);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1.2f);
                        });

                        table.Header(h =>
                        {
                            foreach (var title in new[] { "Habit", "Category", "Sched.", "Done", "Skip", "Miss", "Rate" })
                                h.Cell().Background("#1D4ED8").Padding(4)
                                    .Text(title).FontColor("#FFFFFF").FontSize(8).Bold();
                        });

                        var index = 0;
                        foreach (var row in a.HabitComparison)
                        {
                            var bg = index++ % 2 == 0 ? "#F8FAFC" : "#FFFFFF";
                            table.Cell().Background(bg).Padding(4).Text(row.Name).FontSize(8);
                            table.Cell().Background(bg).Padding(4).Text(row.CategoryName).FontSize(8);
                            table.Cell().Background(bg).Padding(4).AlignCenter().Text(row.Scheduled.ToString()).FontSize(8);
                            table.Cell().Background(bg).Padding(4).AlignCenter().Text(row.Completed.ToString()).FontSize(8);
                            table.Cell().Background(bg).Padding(4).AlignCenter().Text(row.Skipped.ToString()).FontSize(8);
                            table.Cell().Background(bg).Padding(4).AlignCenter().Text(row.Missed.ToString()).FontSize(8);
                            table.Cell().Background(bg).Padding(4).AlignCenter().Text($"{row.Rate:0.#}%").FontSize(8);
                        }
                    });

                    col.Item().Text("Category breakdown").FontSize(12).Bold().FontColor("#0F172A");

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1.2f);
                        });

                        table.Header(h =>
                        {
                            foreach (var title in new[] { "Category", "Scheduled", "Completed", "Rate" })
                                h.Cell().Background("#0EA5E9").Padding(4)
                                    .Text(title).FontColor("#FFFFFF").FontSize(8).Bold();
                        });

                        foreach (var row in a.CategoryBreakdown)
                        {
                            table.Cell().Padding(4).Text(row.Name).FontSize(8);
                            table.Cell().Padding(4).AlignCenter().Text(row.Scheduled.ToString()).FontSize(8);
                            table.Cell().Padding(4).AlignCenter().Text(row.Completed.ToString()).FontSize(8);
                            table.Cell().Padding(4).AlignCenter().Text($"{row.Rate:0.#}%").FontSize(8);
                        }
                    });

                    col.Item().Text("Recent records").FontSize(12).Bold().FontColor("#0F172A");

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(3);
                            c.RelativeColumn(1.4f);
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(2.4f);
                        });

                        table.Header(h =>
                        {
                            foreach (var title in new[] { "Date", "Habit", "Category", "Status", "Notes" })
                                h.Cell().Background("#334155").Padding(4)
                                    .Text(title).FontColor("#FFFFFF").FontSize(8).Bold();
                        });

                        foreach (var log in report.Logs.Take(60))
                        {
                            table.Cell().Padding(4).Text(log.LogDate.ToString("dd MMM yyyy")).FontSize(8);
                            table.Cell().Padding(4).Text(log.Habit?.Name ?? "-").FontSize(8);
                            table.Cell().Padding(4).Text(log.Habit?.Category?.Name ?? "-").FontSize(8);
                            table.Cell().Padding(4).Text(log.Status.ToString()).FontSize(8);
                            table.Cell().Padding(4).Text(log.Notes ?? "").FontSize(8);
                        }
                    });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Habit Tracking Management System — page ").FontSize(8).FontColor("#94A3B8");
                    t.CurrentPageNumber().FontSize(8).FontColor("#94A3B8");
                    t.Span(" of ").FontSize(8).FontColor("#94A3B8");
                    t.TotalPages().FontSize(8).FontColor("#94A3B8");
                });
            });
        }).GeneratePdf();
    }

    private static void SummaryBox(RowDescriptor row, string label, string value, string color)
    {
        row.RelativeItem().PaddingRight(6).Border(1).BorderColor("#E2E8F0").Padding(8).Column(c =>
        {
            c.Item().Text(value).FontSize(16).Bold().FontColor(color);
            c.Item().Text(label).FontSize(8).FontColor("#64748B");
        });
    }

    // ------------------------------------------------------------- Excel
    public byte[] BuildExcel(ReportsViewModel report, string userName)
    {
        var a = report.Analytics;

        using var workbook = new XLWorkbook();

        // ---- Summary sheet
        var summary = workbook.Worksheets.Add("Summary");
        summary.Cell(1, 1).Value = "Habit Tracking Management System — Progress Report";
        summary.Cell(1, 1).Style.Font.Bold = true;
        summary.Cell(1, 1).Style.Font.FontSize = 16;
        summary.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#1D4ED8");

        summary.Cell(2, 1).Value = "User";
        summary.Cell(2, 2).Value = userName;
        summary.Cell(3, 1).Value = "Period";
        summary.Cell(3, 2).Value = $"{report.From:dd MMM yyyy} — {report.To:dd MMM yyyy}";
        summary.Cell(4, 1).Value = "Generated";
        summary.Cell(4, 2).Value = DateTime.Now.ToString("dd MMM yyyy HH:mm");

        var metrics = new (string Label, object Value)[]
        {
            ("Scheduled opportunities", a.TotalScheduled),
            ("Completed", a.TotalCompleted),
            ("Skipped", a.TotalSkipped),
            ("Missed", a.TotalMissed),
            ("Completion rate (%)", a.CompletionRate),
            ("Perfect days", a.PerfectDays),
            ("Active days", a.ActiveDays),
            ("Best streak (days)", a.BestStreak),
            ("Current streak (days)", a.CurrentStreak)
        };

        var r = 6;
        summary.Cell(r, 1).Value = "Metric";
        summary.Cell(r, 2).Value = "Value";
        summary.Range(r, 1, r, 2).Style.Font.Bold = true;
        summary.Range(r, 1, r, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#1D4ED8");
        summary.Range(r, 1, r, 2).Style.Font.FontColor = XLColor.White;

        foreach (var (label, value) in metrics)
        {
            r++;
            summary.Cell(r, 1).Value = label;
            summary.Cell(r, 2).Value = XLCellValue.FromObject(value);
        }
        summary.Columns().AdjustToContents();

        // ---- Habit performance
        var habits = workbook.Worksheets.Add("Habit Performance");
        var headers = new[] { "Habit", "Category", "Scheduled", "Completed", "Skipped", "Missed", "Rate %", "Current streak", "Longest streak" };
        for (var i = 0; i < headers.Length; i++)
        {
            habits.Cell(1, i + 1).Value = headers[i];
            habits.Cell(1, i + 1).Style.Font.Bold = true;
            habits.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1D4ED8");
            habits.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
        }

        var row = 1;
        foreach (var h in a.HabitComparison)
        {
            row++;
            habits.Cell(row, 1).Value = h.Name;
            habits.Cell(row, 2).Value = h.CategoryName;
            habits.Cell(row, 3).Value = h.Scheduled;
            habits.Cell(row, 4).Value = h.Completed;
            habits.Cell(row, 5).Value = h.Skipped;
            habits.Cell(row, 6).Value = h.Missed;
            habits.Cell(row, 7).Value = h.Rate;
            habits.Cell(row, 8).Value = h.CurrentStreak;
            habits.Cell(row, 9).Value = h.LongestStreak;
        }
        habits.Columns().AdjustToContents();
        habits.SheetView.FreezeRows(1);

        // ---- Daily series
        var daily = workbook.Worksheets.Add("Daily Progress");
        daily.Cell(1, 1).Value = "Date";
        daily.Cell(1, 2).Value = "Scheduled";
        daily.Cell(1, 3).Value = "Completed";
        daily.Cell(1, 4).Value = "Rate %";
        daily.Range(1, 1, 1, 4).Style.Font.Bold = true;
        daily.Range(1, 1, 1, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#0EA5E9");
        daily.Range(1, 1, 1, 4).Style.Font.FontColor = XLColor.White;

        row = 1;
        foreach (var d in a.DailySeries)
        {
            row++;
            daily.Cell(row, 1).Value = d.Date.ToString("yyyy-MM-dd");
            daily.Cell(row, 2).Value = d.Scheduled;
            daily.Cell(row, 3).Value = d.Completed;
            daily.Cell(row, 4).Value = d.Rate;
        }
        daily.Columns().AdjustToContents();

        // ---- Records
        var records = workbook.Worksheets.Add("Records");
        var recordHeaders = new[] { "Date", "Habit", "Category", "Status", "Notes", "Recorded at" };
        for (var i = 0; i < recordHeaders.Length; i++)
        {
            records.Cell(1, i + 1).Value = recordHeaders[i];
            records.Cell(1, i + 1).Style.Font.Bold = true;
            records.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#334155");
            records.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
        }

        row = 1;
        foreach (var log in report.Logs)
        {
            row++;
            records.Cell(row, 1).Value = log.LogDate.ToString("yyyy-MM-dd");
            records.Cell(row, 2).Value = log.Habit?.Name ?? "-";
            records.Cell(row, 3).Value = log.Habit?.Category?.Name ?? "-";
            records.Cell(row, 4).Value = log.Status.ToString();
            records.Cell(row, 5).Value = log.Notes ?? "";
            records.Cell(row, 6).Value = log.CompletedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        }
        records.Columns().AdjustToContents();
        records.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Raw log rows as RFC 4180 CSV, for further analysis in a spreadsheet or script.
    /// Values are quoted and internal quotes doubled so notes containing commas,
    /// quotes or newlines cannot break the row structure.
    /// </summary>
    public byte[] BuildCsv(ReportsViewModel report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Date,Habit,Category,Status,Notes,CompletedAt");

        foreach (var log in report.Logs)
        {
            sb.Append(Csv(log.LogDate.ToString("yyyy-MM-dd"))).Append(',');
            sb.Append(Csv(log.Habit?.Name ?? "-")).Append(',');
            sb.Append(Csv(log.Habit?.Category?.Name ?? "-")).Append(',');
            sb.Append(Csv(log.Status.ToString())).Append(',');
            sb.Append(Csv(log.Notes ?? string.Empty)).Append(',');
            sb.Append(Csv(log.CompletedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm")));
            sb.AppendLine();
        }

        // UTF-8 BOM so Excel opens non-ASCII notes with the correct encoding.
        var preamble = Encoding.UTF8.GetPreamble();
        var body = Encoding.UTF8.GetBytes(sb.ToString());
        var result = new byte[preamble.Length + body.Length];
        Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
        Buffer.BlockCopy(body, 0, result, preamble.Length, body.Length);
        return result;
    }

    private static string Csv(string value)
    {
        if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0) return value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
