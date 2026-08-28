// <copyright file="FinancialStatementCrossCheckController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Text.Json;
using ERP.Modules.Reporting.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Reporting.Api;

[ApiController]
[Route("api/v1/reporting/cross-check")]
public class FinancialStatementCrossCheckController : ControllerBase
{
    private readonly ReportingDbContext _db;

    public FinancialStatementCrossCheckController(ReportingDbContext db)
    {
        _db = db;
    }

    [HttpPost("validate")]
    public async Task<IActionResult> Validate([FromBody] CrossCheckRequestDto dto)
    {
        var layout = await _db.FinancialStatementLayouts.FindAsync(dto.LayoutId);
        if (layout == null)
            return NotFound(ApiResponse<object>.Failure(new[] { "Layout not found" }));

        var rowDefs = ParseRowDefinitions(layout.RowDefinitionsJson);
        var columnDefs = ParseColumnDefinitions(layout.ColumnDefinitionsJson);

        var results = new List<CrossCheckRowResult>();
        decimal totalDebits = 0;
        decimal totalCredits = 0;

        foreach (var row in rowDefs)
        {
            var rowResult = new CrossCheckRowResult
            {
                RowNumber = row.RowNumber,
                Label = row.Label,
                RowType = row.RowType
            };

            switch (row.RowType.ToLowerInvariant())
            {
                case "data":
                    var balance = await GetAccountRangeBalance(row.AccountFrom, row.AccountTo, dto.CompanyId, dto.PeriodId);
                    rowResult.Amount = balance;
                    rowResult.Source = "GL Balance";
                    totalDebits += balance > 0 ? balance : 0;
                    totalCredits += balance < 0 ? Math.Abs(balance) : 0;
                    break;

                case "formula":
                    rowResult.Amount = ComputeFormula(row.Formula, results);
                    rowResult.Source = "Formula";
                    break;

                case "total":
                    rowResult.Amount = results.Where(r => r.RowType != "total").Sum(r => r.Amount);
                    rowResult.Source = "Sum";
                    break;

                case "subtotal":
                    rowResult.Amount = results.Where(r => r.RowNumber < row.RowNumber && r.RowType != "total").Sum(r => r.Amount);
                    rowResult.Source = "Subtotal";
                    break;
            }

            results.Add(rowResult);
        }

        var columnResults = columnDefs.Select(col => new CrossCheckColumnResult
        {
            ColumnNumber = col.ColumnNumber,
            Label = col.Label,
            ColumnType = col.ColumnType
        }).ToList();

        var isValid = true;
        var errors = new List<string>();

        var netBalance = totalDebits - totalCredits;
        if (Math.Abs(netBalance) > 0.01m)
        {
            isValid = false;
            errors.Add($"Trial balance does not net to zero: {netBalance:C}");
        }

        var emptyDataRows = results.Where(r => r.RowType == "data" && r.Amount == 0).ToList();
        if (emptyDataRows.Count > 0)
        {
            errors.Add($"{emptyDataRows.Count} data rows have zero balance - verify account ranges are correct");
        }

        return Ok(ApiResponse<object>.Success(new
        {
            LayoutId = dto.LayoutId,
            LayoutName = layout.Name,
            StatementType = layout.StatementType,
            IsValid = isValid,
            TotalDebits = totalDebits,
            TotalCredits = totalCredits,
            NetBalance = netBalance,
            RowResults = results,
            ColumnResults = columnResults,
            Errors = errors,
            CheckedOn = DateTimeOffset.UtcNow
        }));
    }

    private async Task<decimal> GetAccountRangeBalance(string? accountFrom, string? accountTo, Guid companyId, Guid? periodId)
    {
        if (string.IsNullOrEmpty(accountFrom) && string.IsNullOrEmpty(accountTo))
            return 0;

        try
        {
            var sql = @"SELECT ISNULL(SUM(Amount), 0) FROM rpt.GlAccountBalances
                       WHERE CompanyId = @companyId
                       AND AccountNumber >= @accountFrom AND AccountNumber <= @accountTo";

            if (periodId.HasValue)
                sql += " AND PeriodId = @periodId";

            var connection = _db.Database.GetDbConnection();
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = sql;

            var pCompanyId = command.CreateParameter();
            pCompanyId.ParameterName = "@companyId";
            pCompanyId.Value = companyId;
            command.Parameters.Add(pCompanyId);

            var pFrom = command.CreateParameter();
            pFrom.ParameterName = "@accountFrom";
            pFrom.Value = accountFrom ?? string.Empty;
            command.Parameters.Add(pFrom);

            var pTo = command.CreateParameter();
            pTo.ParameterName = "@accountTo";
            pTo.Value = accountTo ?? "ZZZZZZZZ";
            command.Parameters.Add(pTo);

            if (periodId.HasValue)
            {
                var pPeriod = command.CreateParameter();
                pPeriod.ParameterName = "@periodId";
                pPeriod.Value = periodId.Value;
                command.Parameters.Add(pPeriod);
            }

            var result = await command.ExecuteScalarAsync();
            return Convert.ToDecimal(result ?? 0, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch
        {
            return 0;
        }
    }

    private static decimal ComputeFormula(string? formula, List<CrossCheckRowResult> previousRows)
    {
        if (string.IsNullOrEmpty(formula))
            return 0;

        try
        {
            if (formula.StartsWith("SUM(", StringComparison.OrdinalIgnoreCase))
            {
                var args = formula[4..^1].Split(',');
                decimal sum = 0;
                foreach (var arg in args)
                {
                    if (arg.Contains('-', System.StringComparison.Ordinal))
                    {
                        var parts = arg.Split('-');
                        var from = int.Parse(parts[0].Trim().TrimStart('R', 'r'), System.Globalization.CultureInfo.InvariantCulture);
                        var to = int.Parse(parts[1].Trim().TrimStart('R', 'r'), System.Globalization.CultureInfo.InvariantCulture);
                        sum += previousRows.Where(r => r.RowNumber >= from && r.RowNumber <= to).Sum(r => r.Amount);
                    }
                    else
                    {
                        var rowNum = int.Parse(arg.Trim().TrimStart('R', 'r'), System.Globalization.CultureInfo.InvariantCulture);
                        sum += previousRows.FirstOrDefault(r => r.RowNumber == rowNum)?.Amount ?? 0;
                    }
                }
                return sum;
            }

            if (formula.StartsWith("ABS(", StringComparison.OrdinalIgnoreCase))
            {
                var rowNum = int.Parse(formula[4..^1].Trim().TrimStart('R', 'r'), System.Globalization.CultureInfo.InvariantCulture);
                return Math.Abs(previousRows.FirstOrDefault(r => r.RowNumber == rowNum)?.Amount ?? 0);
            }

            if (formula.StartsWith("NEG(", StringComparison.OrdinalIgnoreCase))
            {
                var rowNum = int.Parse(formula[4..^1].Trim().TrimStart('R', 'r'), System.Globalization.CultureInfo.InvariantCulture);
                return -(previousRows.FirstOrDefault(r => r.RowNumber == rowNum)?.Amount ?? 0);
            }
        }
        catch
        {
            return 0;
        }

        return 0;
    }

    private static List<RowDefinition> ParseRowDefinitions(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return new List<RowDefinition>();

        try
        {
            return JsonSerializer.Deserialize<List<RowDefinition>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }
        catch
        {
            return new List<RowDefinition>();
        }
    }

    private static List<ColumnDefinition> ParseColumnDefinitions(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return new List<ColumnDefinition>();

        try
        {
            return JsonSerializer.Deserialize<List<ColumnDefinition>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }
        catch
        {
            return new List<ColumnDefinition>();
        }
    }
}

public class CrossCheckRequestDto
{
    public Guid LayoutId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? PeriodId { get; set; }
}

public class CrossCheckRowResult
{
    public int RowNumber { get; set; }
    public string Label { get; set; } = string.Empty;
    public string RowType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Source { get; set; } = string.Empty;
}

public class CrossCheckColumnResult
{
    public int ColumnNumber { get; set; }
    public string Label { get; set; } = string.Empty;
    public string ColumnType { get; set; } = string.Empty;
}

public class RowDefinition
{
    public int RowNumber { get; set; }
    public string Label { get; set; } = string.Empty;
    public string RowType { get; set; } = string.Empty;
    public string? AccountFrom { get; set; }
    public string? AccountTo { get; set; }
    public string? Formula { get; set; }
}

public class ColumnDefinition
{
    public int ColumnNumber { get; set; }
    public string Label { get; set; } = string.Empty;
    public string ColumnType { get; set; } = string.Empty;
}
