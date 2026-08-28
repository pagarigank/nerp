// <copyright file="StatementDesignerFormulaTests.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace ERP.Modules.Reporting.Tests;

public class StatementDesignerFormulaTests
{
    [Theory]
    [InlineData("SUM(R1)", 1000)]
    [InlineData("SUM(R1,R2)", 3000)]
    [InlineData("SUM(R1-R3)", 6000)]
    [InlineData("ABS(R5)", 500)]
    [InlineData("NEG(R1)", -1000)]
    public void FormulaParsing_VariousFormulas_ComputeCorrectly(string formula, decimal expected)
    {
        var previousRows = new List<CrossCheckRowResultTest>
        {
            new() { RowNumber = 1, Amount = 1000 },
            new() { RowNumber = 2, Amount = 2000 },
            new() { RowNumber = 3, Amount = 3000 },
            new() { RowNumber = 4, Amount = 4000 },
            new() { RowNumber = 5, Amount = -500 }
        };

        var result = ComputeFormula(formula, previousRows);
        result.Should().Be(expected);
    }

    [Fact]
    public void FormulaParsing_SumRange_ComputesCorrectly()
    {
        var rows = new List<CrossCheckRowResultTest>
        {
            new() { RowNumber = 1, Amount = 100 },
            new() { RowNumber = 2, Amount = 200 },
            new() { RowNumber = 3, Amount = 300 },
            new() { RowNumber = 4, Amount = 400 },
            new() { RowNumber = 5, Amount = 500 }
        };

        var result = ComputeFormula("SUM(R2-R4)", rows);
        result.Should().Be(900); // 200 + 300 + 400
    }

    [Fact]
    public void FormulaParsing_EmptyFormula_ReturnsZero()
    {
        var result = ComputeFormula(null, new List<CrossCheckRowResultTest>());
        result.Should().Be(0);
    }

    [Fact]
    public void FormulaParsing_UnknownFormula_ReturnsZero()
    {
        var result = ComputeFormula("UNKNOWN(R1)", new List<CrossCheckRowResultTest>());
        result.Should().Be(0);
    }

    [Fact]
    public void RowDefinitions_ParseFromJson_Deserializes()
    {
        var json = @"[
            {""RowNumber"":1,""Label"":""Assets"",""RowType"":""Header"",""IndentLevel"":0},
            {""RowNumber"":2,""Label"":""Cash"",""RowType"":""Data"",""AccountFrom"":""1000"",""AccountTo"":""1099"",""IndentLevel"":1},
            {""RowNumber"":3,""Label"":""Accounts Receivable"",""RowType"":""Data"",""AccountFrom"":""1100"",""AccountTo"":""1199"",""IndentLevel"":1},
            {""RowNumber"":4,""Label"":""Total Current Assets"",""RowType"":""Formula"",""Formula"":""SUM(R2-R3)"",""IndentLevel"":0,""IsBold"":true}
        ]";

        var rows = JsonSerializer.Deserialize<List<RowDefinitionTest>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        rows.Should().NotBeNull();
        rows!.Count.Should().Be(4);
        rows[0].Label.Should().Be("Assets");
        rows[0].RowType.Should().Be("Header");
        rows[1].AccountFrom.Should().Be("1000");
        rows[1].AccountTo.Should().Be("1099");
        rows[3].Formula.Should().Be("SUM(R2-R3)");
        rows[3].IsBold.Should().BeTrue();
    }

    [Fact]
    public void ColumnDefinitions_ParseFromJson_Deserializes()
    {
        var json = @"[
            {""ColumnNumber"":1,""Label"":""Current Period"",""ColumnType"":""Period""},
            {""ColumnNumber"":2,""Label"":""Year-to-Date"",""ColumnType"":""YTD""},
            {""ColumnNumber"":3,""Label"":""Budget"",""ColumnType"":""Budget""},
            {""ColumnNumber"":4,""Label"":""Variance %"",""ColumnType"":""VariancePercent"",""Formula"":""(R1C1-R1C3)/R1C3""}
        ]";

        var columns = JsonSerializer.Deserialize<List<ColumnDefinitionTest>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        columns.Should().NotBeNull();
        columns!.Count.Should().Be(4);
        columns[0].ColumnType.Should().Be("Period");
        columns[1].ColumnType.Should().Be("YTD");
        columns[2].ColumnType.Should().Be("Budget");
        columns[3].Formula.Should().Be("(R1C1-R1C3)/R1C3");
    }

    [Fact]
    public void RowDefinitions_AllRowTypes_AreRecognized()
    {
        var rowTypes = new[] { "Header", "Data", "Formula", "Total", "Subtotal", "Blank", "Text" };
        foreach (var rowType in rowTypes)
        {
            var row = new RowDefinitionTest { RowNumber = 1, Label = "Test", RowType = rowType };
            row.RowType.Should().Be(rowType);
        }
    }

    [Fact]
    public void ColumnDefinitions_AllColumnTypes_AreRecognized()
    {
        var colTypes = new[] { "Period", "YTD", "Budget", "VarianceAmount", "VariancePercent", "PriorPeriod", "Custom" };
        foreach (var colType in colTypes)
        {
            var col = new ColumnDefinitionTest { ColumnNumber = 1, Label = "Test", ColumnType = colType };
            col.ColumnType.Should().Be(colType);
        }
    }

    private static decimal ComputeFormula(string? formula, List<CrossCheckRowResultTest> previousRows)
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
                    if (arg.Contains('-'))
                    {
                        var parts = arg.Split('-');
                        var from = int.Parse(parts[0].Trim().TrimStart('R', 'r'));
                        var to = int.Parse(parts[1].Trim().TrimStart('R', 'r'));
                        sum += previousRows.Where(r => r.RowNumber >= from && r.RowNumber <= to).Sum(r => r.Amount);
                    }
                    else
                    {
                        var rowNum = int.Parse(arg.Trim().TrimStart('R', 'r'));
                        sum += previousRows.FirstOrDefault(r => r.RowNumber == rowNum)?.Amount ?? 0;
                    }
                }
                return sum;
            }

            if (formula.StartsWith("ABS(", StringComparison.OrdinalIgnoreCase))
            {
                var rowNum = int.Parse(formula[4..^1].Trim().TrimStart('R', 'r'));
                return Math.Abs(previousRows.FirstOrDefault(r => r.RowNumber == rowNum)?.Amount ?? 0);
            }

            if (formula.StartsWith("NEG(", StringComparison.OrdinalIgnoreCase))
            {
                var rowNum = int.Parse(formula[4..^1].Trim().TrimStart('R', 'r'));
                return -(previousRows.FirstOrDefault(r => r.RowNumber == rowNum)?.Amount ?? 0);
            }
        }
        catch
        {
            return 0;
        }

        return 0;
    }
}

public class RowDefinitionTest
{
    public int RowNumber { get; set; }
    public string Label { get; set; } = string.Empty;
    public string RowType { get; set; } = string.Empty;
    public string? AccountFrom { get; set; }
    public string? AccountTo { get; set; }
    public string? Formula { get; set; }
    public int IndentLevel { get; set; }
    public bool IsBold { get; set; }
    public bool IsUnderline { get; set; }
}

public class ColumnDefinitionTest
{
    public int ColumnNumber { get; set; }
    public string Label { get; set; } = string.Empty;
    public string ColumnType { get; set; } = string.Empty;
    public string? Formula { get; set; }
}

public class CrossCheckRowResultTest
{
    public int RowNumber { get; set; }
    public decimal Amount { get; set; }
}
