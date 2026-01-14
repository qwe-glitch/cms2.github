using ComplaintManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ClosedXML.Excel;

namespace ComplaintManagementSystem.Services;

/// <summary>
/// Implementation of reporting and analytics operations
/// </summary>
public class ReportingService : IReportingService
{
    private readonly DB _context;

    public ReportingService(DB context)
    {
        _context = context;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<object> GetSlaComplianceReportAsync(DateTime? startDate = null, DateTime? endDate = null, int? categoryId = null, int? departmentId = null)
    {
        // Default to last 30 days if not specified
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        // Query complaints within date range
        var query = _context.Complaints
            .Include(c => c.Category)
            .Include(c => c.Department)
            .Include(c => c.Staff)
            .Where(c => c.SubmittedAt >= start && c.SubmittedAt <= end);

        if (categoryId.HasValue)
            query = query.Where(c => c.CategoryId == categoryId.Value);

        if (departmentId.HasValue)
            query = query.Where(c => c.DepartmentId == departmentId.Value);

        var complaints = await query.ToListAsync();

        // Calculate SLA compliance
        var totalComplaints = complaints.Count;
        var complaintsWithSla = complaints.Where(c => c.SlaDueDate.HasValue).ToList();
        var resolvedComplaints = complaints.Where(c => c.Status == "Resolved" || c.Status == "Closed").ToList();
        
        var slaMetComplaints = resolvedComplaints.Where(c => 
            c.SlaDueDate.HasValue && 
            c.ResolvedAt.HasValue && 
            c.ResolvedAt <= c.SlaDueDate
        ).Count();

        var slaMissedComplaints = resolvedComplaints.Where(c => 
            c.SlaDueDate.HasValue && 
            c.ResolvedAt.HasValue && 
            c.ResolvedAt > c.SlaDueDate
        ).Count();

        var overdueComplaints = complaints.Where(c => 
            c.Status != "Resolved" && 
            c.Status != "Closed" && 
            c.SlaDueDate.HasValue && 
            DateTime.UtcNow > c.SlaDueDate
        ).Count();

        var complianceRate = (slaMetComplaints + slaMissedComplaints) > 0 
            ? Math.Round((double)slaMetComplaints / (slaMetComplaints + slaMissedComplaints) * 100, 1) 
            : 0;

        // SLA by Category
        var slaByCategory = complaints
            .Where(c => c.SlaDueDate.HasValue)
            .GroupBy(c => new { c.CategoryId, c.Category.CategoryName })
            .Select(g => new
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.CategoryName,
                Total = g.Count(),
                Met = g.Count(c => (c.Status == "Resolved" || c.Status == "Closed") && c.ResolvedAt.HasValue && c.ResolvedAt <= c.SlaDueDate),
                Missed = g.Count(c => (c.Status == "Resolved" || c.Status == "Closed") && c.ResolvedAt.HasValue && c.ResolvedAt > c.SlaDueDate),
                Overdue = g.Count(c => c.Status != "Resolved" && c.Status != "Closed" && DateTime.UtcNow > c.SlaDueDate),
                ComplianceRate = (g.Count(c => (c.Status == "Resolved" || c.Status == "Closed") && c.ResolvedAt.HasValue && c.ResolvedAt <= c.SlaDueDate) + 
                                 g.Count(c => (c.Status == "Resolved" || c.Status == "Closed") && c.ResolvedAt.HasValue && c.ResolvedAt > c.SlaDueDate)) > 0
                    ? Math.Round((double)g.Count(c => (c.Status == "Resolved" || c.Status == "Closed") && c.ResolvedAt.HasValue && c.ResolvedAt <= c.SlaDueDate) / 
                                (g.Count(c => (c.Status == "Resolved" || c.Status == "Closed") && c.ResolvedAt.HasValue && c.ResolvedAt <= c.SlaDueDate) + 
                                 g.Count(c => (c.Status == "Resolved" || c.Status == "Closed") && c.ResolvedAt.HasValue && c.ResolvedAt > c.SlaDueDate)) * 100, 1)
                    : 0
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        // SLA by Department
        var slaByDepartment = complaints
            .Where(c => c.SlaDueDate.HasValue && c.DepartmentId.HasValue)
            .GroupBy(c => new { c.DepartmentId, c.Department!.DepartmentName })
            .Select(g => new
            {
                DepartmentId = g.Key.DepartmentId,
                DepartmentName = g.Key.DepartmentName,
                Total = g.Count(),
                Met = g.Count(c => (c.Status == "Resolved" || c.Status == "Closed") && c.ResolvedAt.HasValue && c.ResolvedAt <= c.SlaDueDate),
                Missed = g.Count(c => (c.Status == "Resolved" || c.Status == "Closed") && c.ResolvedAt.HasValue && c.ResolvedAt > c.SlaDueDate),
                Overdue = g.Count(c => c.Status != "Resolved" && c.Status != "Closed" && DateTime.UtcNow > c.SlaDueDate),
                ComplianceRate = (g.Count(c => (c.Status == "Resolved" || c.Status == "Closed") && c.ResolvedAt.HasValue && c.ResolvedAt <= c.SlaDueDate) + 
                                 g.Count(c => (c.Status == "Resolved" || c.Status == "Closed") && c.ResolvedAt.HasValue && c.ResolvedAt > c.SlaDueDate)) > 0
                    ? Math.Round((double)g.Count(c => (c.Status == "Resolved" || c.Status == "Closed") && c.ResolvedAt.HasValue && c.ResolvedAt <= c.SlaDueDate) / 
                                (g.Count(c => (c.Status == "Resolved" || c.Status == "Closed") && c.ResolvedAt.HasValue && c.ResolvedAt <= c.SlaDueDate) + 
                                 g.Count(c => (c.Status == "Resolved" || c.Status == "Closed") && c.ResolvedAt.HasValue && c.ResolvedAt > c.SlaDueDate)) * 100, 1)
                    : 0
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        // Average resolution time
        var avgResolutionHours = resolvedComplaints
            .Where(c => c.ResolvedAt.HasValue)
            .Select(c => (c.ResolvedAt!.Value - c.SubmittedAt).TotalHours)
            .DefaultIfEmpty(0)
            .Average();

        return new
        {
            StartDate = start,
            EndDate = end,
            TotalComplaints = totalComplaints,
            ComplaintsWithSla = complaintsWithSla.Count,
            SlaMetCount = slaMetComplaints,
            SlaMissedCount = slaMissedComplaints,
            OverdueCount = overdueComplaints,
            ComplianceRate = complianceRate,
            AvgResolutionHours = Math.Round(avgResolutionHours, 1),
            SlaByCategory = slaByCategory,
            SlaByDepartment = slaByDepartment
        };
    }

    public async Task<object> GetTeamWorkloadMetricsAsync(DateTime? startDate = null, DateTime? endDate = null, int? departmentId = null)
    {
        // Default to last 30 days if not specified
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        // Query complaints within date range
        var query = _context.Complaints
            .Include(c => c.Staff)
            .ThenInclude(s => s!.Department)
            .Where(c => c.SubmittedAt >= start && c.SubmittedAt <= end && c.StaffId.HasValue);

        if (departmentId.HasValue)
            query = query.Where(c => c.Staff!.DepartmentId == departmentId.Value);

        var complaints = await query.ToListAsync();

        // Staff workload metrics
        var staffMetrics = complaints
            .GroupBy(c => new { c.StaffId, c.Staff!.Name, c.Staff.Department.DepartmentName })
            .Select(g => new
            {
                StaffId = g.Key.StaffId,
                StaffName = g.Key.Name,
                DepartmentName = g.Key.DepartmentName,
                TotalAssigned = g.Count(),
                Pending = g.Count(c => c.Status == "Pending"),
                InProgress = g.Count(c => c.Status == "In Progress"),
                Resolved = g.Count(c => c.Status == "Resolved" || c.Status == "Closed"),
                AvgResolutionHours = g.Where(c => c.Status == "Resolved" && c.ResolvedAt.HasValue)
                    .Select(c => (c.ResolvedAt!.Value - c.SubmittedAt).TotalHours)
                    .DefaultIfEmpty(0)
                    .Average(),
                CurrentWorkload = g.Count(c => c.Status != "Resolved" && c.Status != "Closed")
            })
            .OrderByDescending(x => x.TotalAssigned)
            .ToList();

        // Department aggregations
        var departmentMetrics = complaints
            .GroupBy(c => new { c.Staff!.DepartmentId, c.Staff.Department.DepartmentName })
            .Select(g => new
            {
                DepartmentId = g.Key.DepartmentId,
                DepartmentName = g.Key.DepartmentName,
                TotalAssigned = g.Count(),
                StaffCount = g.Select(c => c.StaffId).Distinct().Count(),
                AvgWorkloadPerStaff = Math.Round((double)g.Count() / g.Select(c => c.StaffId).Distinct().Count(), 1),
                Resolved = g.Count(c => c.Status == "Resolved" || c.Status == "Closed"),
                ResolutionRate = Math.Round((double)g.Count(c => c.Status == "Resolved" || c.Status == "Closed") / g.Count() * 100, 1)
            })
            .OrderByDescending(x => x.TotalAssigned)
            .ToList();

        return new
        {
            StartDate = start,
            EndDate = end,
            TotalAssignedComplaints = complaints.Count,
            StaffMetrics = staffMetrics,
            DepartmentMetrics = departmentMetrics
        };
    }

    public async Task<object> GetGeographicHotspotsAsync(DateTime? startDate = null, DateTime? endDate = null, int? categoryId = null)
    {
        // Default to last 30 days if not specified
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        // Query complaints with location data
        var query = _context.Complaints
            .Include(c => c.Category)
            .Where(c => c.SubmittedAt >= start && c.SubmittedAt <= end && c.Latitude.HasValue && c.Longitude.HasValue);

        if (categoryId.HasValue)
            query = query.Where(c => c.CategoryId == categoryId.Value);

        var complaints = await query.ToListAsync();

        // Group by approximate location (round to 3 decimal places ~ 100m accuracy)
        var hotspots = complaints
            .GroupBy(c => new
            {
                Lat = Math.Round(c.Latitude!.Value, 3),
                Lng = Math.Round(c.Longitude!.Value, 3)
            })
            .Select(g => new
            {
                Latitude = g.Key.Lat,
                Longitude = g.Key.Lng,
                Count = g.Count(),
                Categories = g.GroupBy(c => c.Category.CategoryName)
                    .Select(cg => new { Category = cg.Key, Count = cg.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList(),
                Complaints = g.Select(c => new
                {
                    c.ComplaintId,
                    c.Title,
                    c.Status,
                    Category = c.Category.CategoryName,
                    c.SubmittedAt
                }).ToList()
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        // All individual complaints for map markers
        var markers = complaints.Select(c => new
        {
            c.ComplaintId,
            c.Title,
            c.Status,
            Category = c.Category.CategoryName,
            c.Latitude,
            c.Longitude,
            c.SubmittedAt,
            c.Location
        }).ToList();

        return new
        {
            StartDate = start,
            EndDate = end,
            TotalWithLocation = complaints.Count,
            Hotspots = hotspots,
            Markers = markers
        };
    }

    public async Task<byte[]> ExportComplaintsToExcelAsync(DateTime? startDate = null, DateTime? endDate = null, string? status = null, int? categoryId = null, int? departmentId = null)
    {
        var query = _context.Complaints
            .Include(c => c.Category)
            .Include(c => c.Department)
            .Include(c => c.Staff)
            .Include(c => c.Citizen)
            .AsQueryable();

        if (startDate.HasValue)
            query = query.Where(c => c.SubmittedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(c => c.SubmittedAt <= endDate.Value);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(c => c.Status == status);

        if (categoryId.HasValue)
            query = query.Where(c => c.CategoryId == categoryId.Value);

        if (departmentId.HasValue)
            query = query.Where(c => c.DepartmentId == departmentId.Value);

        var complaints = await query.OrderByDescending(c => c.SubmittedAt).ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Complaints");

        // Header
        var headers = new[] 
        { 
            "Complaint ID", "Title", "Category", "Department", "Status", "Priority", 
            "Submitted At", "Resolved At", "Citizen", "Location", "Latitude", "Longitude", 
            "SLA Due Date", "Resolution Hours" 
        };

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Data
        int row = 2;
        foreach (var c in complaints)
        {
            var resolutionHours = c.ResolvedAt.HasValue ? (c.ResolvedAt.Value - c.SubmittedAt).TotalHours : 0;
            
            worksheet.Cell(row, 1).Value = c.ComplaintId;
            worksheet.Cell(row, 2).Value = c.Title;
            worksheet.Cell(row, 3).Value = c.Category.CategoryName;
            worksheet.Cell(row, 4).Value = c.Department?.DepartmentName ?? "Unassigned";
            worksheet.Cell(row, 5).Value = c.Status;
            worksheet.Cell(row, 6).Value = c.Priority;
            worksheet.Cell(row, 7).Value = c.SubmittedAt;
            worksheet.Cell(row, 8).Value = c.ResolvedAt;
            worksheet.Cell(row, 9).Value = c.IsAnonymous ? "Anonymous" : c.Citizen.Name;
            worksheet.Cell(row, 10).Value = c.Location;
            worksheet.Cell(row, 11).Value = c.Latitude;
            worksheet.Cell(row, 12).Value = c.Longitude;
            worksheet.Cell(row, 13).Value = c.SlaDueDate;
            worksheet.Cell(row, 14).Value = c.ResolvedAt.HasValue ? Math.Round(resolutionHours, 2) : "";

            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }



    public async Task<byte[]> ExportReportToPDFAsync(string reportType, DateTime? startDate = null, DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        // Pre-fetch data
        object? data = null;
        if (reportType.ToLower() == "sla")
        {
            data = await GetSlaComplianceReportAsync(startDate, endDate);
        }
        else if (reportType.ToLower() == "workload")
        {
            data = await GetTeamWorkloadMetricsAsync(startDate, endDate);
        }
        else if (reportType.ToLower() == "complaints")
        {
            data = await GetComplaintReportAsync(startDate, endDate);
        }

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header()
                    .Text($"Complaint Management System - {reportType.ToUpper()} Report")
                    .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(x =>
                    {
                        x.Spacing(20);

                        x.Item().Text($"Period: {start:yyyy-MM-dd} to {end:yyyy-MM-dd}");
                        x.Item().Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");

                        if (reportType.ToLower() == "sla" && data != null)
                        {
                            dynamic slaData = data;
                            x.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Metric");
                                    header.Cell().Element(CellStyle).Text("Value");

                                    static IContainer CellStyle(IContainer container)
                                    {
                                        return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                                    }
                                });

                                table.Cell().Element(CellStyle).Text("Total Complaints");
                                table.Cell().Element(CellStyle).Text($"{slaData.TotalComplaints}");

                                table.Cell().Element(CellStyle).Text("Complaints with SLA");
                                table.Cell().Element(CellStyle).Text($"{slaData.ComplaintsWithSla}");

                                table.Cell().Element(CellStyle).Text("SLA Met");
                                table.Cell().Element(CellStyle).Text($"{slaData.SlaMetCount}");

                                table.Cell().Element(CellStyle).Text("SLA Missed");
                                table.Cell().Element(CellStyle).Text($"{slaData.SlaMissedCount}");

                                table.Cell().Element(CellStyle).Text("Currently Overdue");
                                table.Cell().Element(CellStyle).Text($"{slaData.OverdueCount}");

                                table.Cell().Element(CellStyle).Text("Compliance Rate");
                                table.Cell().Element(CellStyle).Text($"{slaData.ComplianceRate}%");

                                table.Cell().Element(CellStyle).Text("Avg Resolution Time");
                                table.Cell().Element(CellStyle).Text($"{slaData.AvgResolutionHours} hours");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                                }
                            });
                        }
                        else if (reportType.ToLower() == "workload" && data != null)
                        {
                            dynamic workloadData = data;
                            x.Item().Text($"Total Assigned Complaints: {workloadData.TotalAssignedComplaints}");
                            
                            // Add more workload details here if needed
                        }
                        else if (reportType.ToLower() == "complaints" && data != null)
                        {
                            dynamic complaintData = data;
                            var complaints = (IEnumerable<dynamic>)complaintData.Complaints;

                            x.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(40);
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("ID");
                                    header.Cell().Element(CellStyle).Text("Title");
                                    header.Cell().Element(CellStyle).Text("Category");
                                    header.Cell().Element(CellStyle).Text("Status");
                                    header.Cell().Element(CellStyle).Text("Submitted");
                                    header.Cell().Element(CellStyle).Text("Citizen");

                                    static IContainer CellStyle(IContainer container)
                                    {
                                        return container.DefaultTextStyle(x => x.SemiBold().FontSize(10)).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                                    }
                                });

                                foreach (var item in complaints)
                                {
                                    table.Cell().Element(CellStyle).Text(((object)item.ComplaintId).ToString());
                                    table.Cell().Element(CellStyle).Text((string)item.Title);
                                    table.Cell().Element(CellStyle).Text((string)item.Category);
                                    table.Cell().Element(CellStyle).Text((string)item.Status);
                                    table.Cell().Element(CellStyle).Text(((DateTime)item.SubmittedAt).ToString("yyyy-MM-dd"));
                                    table.Cell().Element(CellStyle).Text((string)item.CitizenName);
                                }

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.DefaultTextStyle(x => x.FontSize(9)).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                                }
                            });
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
            });
        });

        return document.GeneratePdf();
    }

    public async Task<object> GetComplaintReportAsync(DateTime? startDate = null, DateTime? endDate = null, string? status = null, int? categoryId = null, int? departmentId = null)
    {
        var query = _context.Complaints
            .Include(c => c.Category)
            .Include(c => c.Department)
            .Include(c => c.Staff)
            .Include(c => c.Citizen)
            .AsQueryable();

        if (startDate.HasValue)
            query = query.Where(c => c.SubmittedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(c => c.SubmittedAt <= endDate.Value);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(c => c.Status == status);

        if (categoryId.HasValue)
            query = query.Where(c => c.CategoryId == categoryId.Value);

        if (departmentId.HasValue)
            query = query.Where(c => c.DepartmentId == departmentId.Value);

        var complaints = await query.OrderByDescending(c => c.SubmittedAt).ToListAsync();

        return new
        {
            StartDate = startDate,
            EndDate = endDate,
            Status = status,
            CategoryId = categoryId,
            DepartmentId = departmentId,
            Complaints = complaints.Select(c => new
            {
                c.ComplaintId,
                c.Title,
                Category = c.Category.CategoryName,
                Department = c.Department?.DepartmentName ?? "Unassigned",
                c.Status,
                c.Priority,
                c.SubmittedAt,
                CitizenName = c.IsAnonymous ? "Anonymous" : c.Citizen.Name
            }).ToList()
        };
    }

    public async Task<object> GetAnalyticsDashboardDataAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        // Default to last 30 days if not specified
        var end = endDate ?? DateTime.UtcNow;
        var start = startDate ?? end.AddDays(-30);

        // Calculate previous period for trends
        var duration = end - start;
        var previousStart = start - duration;
        var previousEnd = start;

        // Fetch current period data
        var currentComplaints = await _context.Complaints
            .Include(c => c.Category)
            .Include(c => c.Feedbacks)
            .Where(c => c.SubmittedAt >= start && c.SubmittedAt <= end)
            .ToListAsync();

        // Fetch previous period data for trends
        var previousComplaints = await _context.Complaints
            .Where(c => c.SubmittedAt >= previousStart && c.SubmittedAt < previousEnd)
            .ToListAsync();

        // 1. KPI Cards Calculations
        
        // Total Complaints
        var totalComplaints = currentComplaints.Count;
        var prevTotalComplaints = previousComplaints.Count;
        var totalComplaintsTrend = CalculateTrend(totalComplaints, prevTotalComplaints);

        // Resolution Rate
        var resolvedCount = currentComplaints.Count(c => c.Status == "Resolved" || c.Status == "Closed");
        var resolutionRate = totalComplaints > 0 ? Math.Round((double)resolvedCount / totalComplaints * 100, 1) : 0;
        
        var prevResolvedCount = previousComplaints.Count(c => c.Status == "Resolved" || c.Status == "Closed");
        var prevResolutionRate = prevTotalComplaints > 0 ? (double)prevResolvedCount / prevTotalComplaints * 100 : 0;
        var resolutionRateTrend = CalculateTrend(resolutionRate, prevResolutionRate);

        // Avg Response Time (Hours)
        var responseTimes = currentComplaints
            .Where(c => c.FirstResponseTime.HasValue)
            .Select(c => (c.FirstResponseTime!.Value - c.SubmittedAt).TotalHours)
            .ToList();
        var avgResponseTime = responseTimes.Any() ? Math.Round(responseTimes.Average(), 1) : 0;

        var prevResponseTimes = previousComplaints
            .Where(c => c.FirstResponseTime.HasValue)
            .Select(c => (c.FirstResponseTime!.Value - c.SubmittedAt).TotalHours)
            .ToList();
        var prevAvgResponseTime = prevResponseTimes.Any() ? prevResponseTimes.Average() : 0;
        var avgResponseTimeTrend = CalculateTrend(avgResponseTime, prevAvgResponseTime);

        // Avg Resolution Time (Days)
        var resolutionTimes = currentComplaints
            .Where(c => (c.Status == "Resolved" || c.Status == "Closed") && c.ResolvedAt.HasValue)
            .Select(c => (c.ResolvedAt!.Value - c.SubmittedAt).TotalDays)
            .ToList();
        var avgResolutionTime = resolutionTimes.Any() ? Math.Round(resolutionTimes.Average(), 1) : 0;

        var prevResolutionTimes = previousComplaints
            .Where(c => (c.Status == "Resolved" || c.Status == "Closed") && c.ResolvedAt.HasValue)
            .Select(c => (c.ResolvedAt!.Value - c.SubmittedAt).TotalDays)
            .ToList();
        var prevAvgResolutionTime = prevResolutionTimes.Any() ? prevResolutionTimes.Average() : 0;
        var avgResolutionTimeTrend = CalculateTrend(avgResolutionTime, prevAvgResolutionTime);

        // 2. Monthly Complaint Trends (Area Chart)
        var trendData = currentComplaints
            .GroupBy(c => c.SubmittedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                Date = g.Key.ToString("MMM dd"),
                Total = g.Count(),
                Pending = g.Count(c => c.Status == "Pending" || c.Status == "In Progress"),
                Resolved = g.Count(c => c.Status == "Resolved" || c.Status == "Closed")
            })
            .ToList();

        // 3. Average Resolution Time by Category (Horizontal Bar Chart)
        var categoryResolutionData = currentComplaints
            .Where(c => (c.Status == "Resolved" || c.Status == "Closed") && c.ResolvedAt.HasValue)
            .GroupBy(c => c.Category.CategoryName)
            .Select(g => new
            {
                Category = g.Key,
                AvgDays = Math.Round(g.Average(c => (c.ResolvedAt!.Value - c.SubmittedAt).TotalDays), 1)
            })
            .OrderByDescending(x => x.AvgDays)
            .Take(6) // Top 6 categories
            .ToList();

        // 4. Weekly Performance Metrics (Line Chart)
        var weeklyMetrics = currentComplaints
            .GroupBy(c => System.Globalization.ISOWeek.GetWeekOfYear(c.SubmittedAt))
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                Week = $"Week {g.Key}",
                AvgResolutionDays = Math.Round(g.Where(c => (c.Status == "Resolved" || c.Status == "Closed") && c.ResolvedAt.HasValue)
                    .Select(c => (c.ResolvedAt!.Value - c.SubmittedAt).TotalDays)
                    .DefaultIfEmpty(0)
                    .Average(), 1),
                AvgResponseHours = Math.Round(g.Where(c => c.FirstResponseTime.HasValue)
                    .Select(c => (c.FirstResponseTime!.Value - c.SubmittedAt).TotalHours)
                    .DefaultIfEmpty(0)
                    .Average(), 1),
                AvgSatisfaction = Math.Round(g.SelectMany(c => c.Feedbacks)
                    .Where(f => f.Rating.HasValue)
                    .Select(f => (double)f.Rating!.Value)
                    .DefaultIfEmpty(0)
                    .Average(), 1)
            })
            .ToList();

        return new
        {
            StartDate = start,
            EndDate = end,
            KPIs = new
            {
                TotalComplaints = new { Value = totalComplaints, Trend = totalComplaintsTrend },
                ResolutionRate = new { Value = resolutionRate, Trend = resolutionRateTrend },
                AvgResponseTime = new { Value = avgResponseTime, Trend = avgResponseTimeTrend },
                AvgResolutionTime = new { Value = avgResolutionTime, Trend = avgResolutionTimeTrend }
            },
            Charts = new
            {
                MonthlyTrends = trendData,
                CategoryResolution = categoryResolutionData,
                WeeklyPerformance = weeklyMetrics
            }
        };
    }

    private double CalculateTrend(double current, double previous)
    {
        if (previous == 0) return current > 0 ? 100 : 0;
        return Math.Round((current - previous) / previous * 100, 1);
    }
}
