// <copyright file="BankStatementParserService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using ERP.Modules.CashManagement.Domain.Entities;

namespace ERP.Modules.CashManagement.Infrastructure;

public interface IBankStatementParserService
{
    BankStatementFormat DetectFormat(string content);

    ParsedStatementResult Parse(string content, BankStatementFormat? expectedFormat = null);
}

public record ParsedStatementLine(
    DateTimeOffset TransactionDate,
    decimal Amount,
    string Description,
    string? ReferenceNumber,
    string? CheckNumber,
    decimal? Balance);

public record ParsedStatementResult(
    BankStatementFormat Format,
    decimal? BeginningBalance,
    decimal? EndingBalance,
    DateTimeOffset? AsOfDate,
    IReadOnlyList<ParsedStatementLine> Lines,
    IReadOnlyList<string> Warnings);

public class BankStatementParserService : IBankStatementParserService
{
    private static readonly Regex Bai2RecordRegex = new Regex(@"^(?<record>\d{2}),(?<content>.*)$", RegexOptions.Compiled | RegexOptions.Multiline);

    public BankStatementFormat DetectFormat(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Statement content is empty.", nameof(content));

        var trimmed = content.TrimStart();

        if (trimmed.StartsWith("OFXHEADER", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("!TYPE:QBO", StringComparison.OrdinalIgnoreCase))
        {
            return BankStatementFormat.Qbo;
        }

        if (trimmed.StartsWith('<')
            && (trimmed.Contains("<OFX", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("<STMTTRN", StringComparison.OrdinalIgnoreCase)))
        {
            return BankStatementFormat.Ofx;
        }

        if (Bai2RecordRegex.IsMatch(trimmed) && trimmed.StartsWith("01,", StringComparison.Ordinal))
        {
            return BankStatementFormat.Bai2;
        }

        return BankStatementFormat.Csv;
    }

    public ParsedStatementResult Parse(string content, BankStatementFormat? expectedFormat = null)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Statement content is empty.", nameof(content));

        var format = expectedFormat ?? DetectFormat(content);

        return format switch
        {
            BankStatementFormat.Bai2 => ParseBai2(content),
            BankStatementFormat.Ofx => ParseOfx(content, BankStatementFormat.Ofx),
            BankStatementFormat.Qbo => ParseOfx(content, BankStatementFormat.Qbo),
            _ => ParseCsv(content),
        };
    }

    internal ParsedStatementResult ParseBai2(string content)
    {
        var warnings = new List<string>();
        var lines = new List<ParsedStatementLine>();
        decimal? beginningBalance = null;
        decimal? endingBalance = null;
        DateTimeOffset? asOfDate = null;

        foreach (var raw in content.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var match = Bai2RecordRegex.Match(raw);
            if (!match.Success)
            {
                warnings.Add($"Skipped unrecognized BAI2 line: {raw}");
                continue;
            }

            var recordType = match.Groups["record"].Value;
            var fields = match.Groups["content"].Value.Split(',', StringSplitOptions.TrimEntries);

            switch (recordType)
            {
                case "02":
                    if (fields.Length >= 3 && TryParseBai2Date(fields[2], out var groupDate))
                    {
                        asOfDate = groupDate;
                    }

                    break;
                case "03":
                    if (fields.Length >= 5 && decimal.TryParse(fields[4], NumberStyles.Any, CultureInfo.InvariantCulture, out var opening))
                    {
                        beginningBalance = opening;
                    }

                    break;
                case "16":
                    if (fields.Length >= 2)
                    {
                        var amount = ParseSignedAmount(fields[1], warnings, raw);
                        var transactionDate = fields.Length >= 4 && TryParseBai2Date(fields[3], out var valueDate)
                            ? valueDate
                            : asOfDate ?? DateTimeOffset.UtcNow;
                        var description = fields.Length >= 5 ? fields[4] : string.Empty;

                        lines.Add(new ParsedStatementLine(
                            transactionDate,
                            amount,
                            description,
                            null,
                            null,
                            null));
                    }

                    break;
                case "49":
                    if (fields.Length >= 1 && decimal.TryParse(fields[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var closing))
                    {
                        endingBalance = closing;
                    }

                    break;
            }
        }

        ValidateLines(lines, warnings);
        return new ParsedStatementResult(BankStatementFormat.Bai2, beginningBalance, endingBalance, asOfDate, lines, warnings);
    }

    internal ParsedStatementResult ParseOfx(string content, BankStatementFormat format)
    {
        var warnings = new List<string>();
        var lines = new List<ParsedStatementLine>();
        decimal? beginningBalance = null;
        decimal? endingBalance = null;
        DateTimeOffset? asOfDate = null;

        // BAI2-style conversion not needed; OFX is tag-based. Handle SGML (QBO) and XML (OFX 2.x).
        XDocument doc;
        try
        {
            doc = ParseOfxXml(content);
        }
        catch (XmlException)
        {
            var sgml = ConvertSgmlToXml(content);
            doc = XDocument.Parse(sgml);
        }

        var stmtTrnElements = doc.Descendants().Where(e => e.Name.LocalName == "STMTTRN").ToList();
        if (stmtTrnElements.Count == 0)
        {
            warnings.Add("No STMTTRN elements found in OFX document.");
        }

        foreach (var trn in stmtTrnElements)
        {
            var type = trn.ElementLocal("TRNTYPE")?.Value ?? string.Empty;
            var amountRaw = trn.ElementLocal("TRNAMT")?.Value ?? "0";
            var datePosted = trn.ElementLocal("DTPOSTED")?.Value;
            var fitId = trn.ElementLocal("FITID")?.Value;
            var name = trn.ElementLocal("NAME")?.Value;
            var memo = trn.ElementLocal("MEMO")?.Value;
            var checkNum = trn.ElementLocal("CHECKNUM")?.Value;

            if (!decimal.TryParse(amountRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
            {
                warnings.Add($"Unrecognized TRNAMT '{amountRaw}' for FITID {fitId}.");
                continue;
            }

            // OFX deposits are positive credits. Invert so that credits are positive for our model.
            var normalizedAmount = NormalizeOfxAmount(type, amount);

            var transactionDate = ParseOfxDate(datePosted) ?? asOfDate ?? DateTimeOffset.UtcNow;
            var description = string.IsNullOrWhiteSpace(name) ? (memo ?? string.Empty) : $"{name} {memo}".Trim();

            lines.Add(new ParsedStatementLine(
                transactionDate,
                normalizedAmount,
                description,
                fitId,
                checkNum,
                null));
        }

        var ledgers = doc.Descendants().Where(e => e.Name.LocalName == "LEDGERBAL").ToList();
        if (ledgers.Count > 0)
        {
            var balElem = ledgers[0].Elements().FirstOrDefault(e => e.Name.LocalName == "BALAMT");
            if (balElem != null && decimal.TryParse(balElem.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var ledgerBalance))
            {
                endingBalance = ledgerBalance;
            }
        }

        var availBal = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "AVAILBAL");
        if (availBal != null)
        {
            var balElem = availBal.Elements().FirstOrDefault(e => e.Name.LocalName == "BALAMT");
            if (balElem != null && decimal.TryParse(balElem.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var availAmount))
            {
                asOfDate ??= DateTimeOffset.UtcNow;
            }
        }

        ValidateLines(lines, warnings);
        return new ParsedStatementResult(format, beginningBalance, endingBalance, asOfDate, lines, warnings);
    }

    internal ParsedStatementResult ParseCsv(string content)
    {
        var warnings = new List<string>();
        var lines = new List<ParsedStatementLine>();
        decimal? endingBalance = null;
        DateTimeOffset? asOfDate = null;

        var rows = CsvSplit(content);
        if (rows.Count == 0)
            return new ParsedStatementResult(BankStatementFormat.Csv, null, null, null, lines, warnings);

        // Header row detection: look for known column names in the first row.
        var header = rows[0].Select(h => h.Trim().ToUpperInvariant()).ToList();
        var hasHeader = header.Any(h =>
            h.Contains("DATE", StringComparison.Ordinal)
            || h.Contains("AMOUNT", StringComparison.Ordinal)
            || h.Contains("DESCRIPTION", StringComparison.Ordinal)
            || h.Contains("MEMO", StringComparison.Ordinal)
            || h.Contains("CHECK", StringComparison.Ordinal)
            || h.Contains("REFERENCE", StringComparison.Ordinal)
            || h.Contains("BALANCE", StringComparison.Ordinal));

        var startIndex = hasHeader ? 1 : 0;
        var dateIdx = FindColumn(header, hasHeader, "date", "posting date", "transaction date", "value date");
        var amountIdx = FindColumn(header, hasHeader, "amount", "transaction amount");
        var descriptionIdx = FindColumn(header, hasHeader, "description", "memo", "narrative", "payee");
        var referenceIdx = FindColumn(header, hasHeader, "reference", "reference number", "ref", "fitid", "id");
        var checkIdx = FindColumn(header, hasHeader, "check", "check number", "cheque", "chk#");
        var balanceIdx = FindColumn(header, hasHeader, "balance", "running balance");
        var debitIdx = FindColumn(header, hasHeader, "debit", "withdrawal", "payment");
        var creditIdx = FindColumn(header, hasHeader, "credit", "deposit");

        if (amountIdx < 0)
            amountIdx = FindColumn(header, hasHeader, "transaction amount", "amount", "sum");
        if (debitIdx < 0 && creditIdx < 0 && amountIdx < 0)
        {
            warnings.Add("No amount column detected in CSV. Expected 'Amount', 'Debit', or 'Credit'.");
        }

        for (var i = startIndex; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row.All(string.IsNullOrWhiteSpace))
                continue;

            decimal amount;
            if (amountIdx >= 0 && TryParseDecimal(row[amountIdx], out var parsed))
            {
                amount = parsed;
            }
            else if (debitIdx >= 0 && TryParseDecimal(row[debitIdx], out var debit))
            {
                amount = -Math.Abs(debit);
            }
            else if (creditIdx >= 0 && TryParseDecimal(row[creditIdx], out var credit))
            {
                amount = Math.Abs(credit);
            }
            else
            {
                warnings.Add($"Skipped CSV row {i + 1}: no parseable amount.");
                continue;
            }

            var date = dateIdx >= 0 && TryParseCsvDate(row[dateIdx], out var parsedDate)
                ? parsedDate
                : asOfDate ?? DateTimeOffset.UtcNow;
            asOfDate ??= date;

            var description = descriptionIdx >= 0 ? row[descriptionIdx] : string.Empty;
            var reference = referenceIdx >= 0 ? row[referenceIdx] : null;
            var check = checkIdx >= 0 ? row[checkIdx] : null;
            decimal? balance = balanceIdx >= 0 && TryParseDecimal(row[balanceIdx], out var parsedBalance)
                ? parsedBalance
                : null;

            if (balance.HasValue && (!endingBalance.HasValue || date >= asOfDate))
            {
                endingBalance = balance;
            }

            lines.Add(new ParsedStatementLine(date, amount, description, reference, check, balance));
        }

        ValidateLines(lines, warnings);
        return new ParsedStatementResult(BankStatementFormat.Csv, null, endingBalance, asOfDate, lines, warnings);
    }

    private static void ValidateLines(IReadOnlyList<ParsedStatementLine> lines, List<string> warnings)
    {
        if (lines.Count == 0)
        {
            warnings.Add("Statement contains no transaction lines.");
        }

        var fitIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line.ReferenceNumber)
                && !fitIds.Add(line.ReferenceNumber!))
            {
                warnings.Add($"Duplicate reference number in statement: {line.ReferenceNumber}");
            }
        }
    }

    private static XDocument ParseOfxXml(string content)
    {
        var start = content.IndexOf("<OFX", StringComparison.OrdinalIgnoreCase);
        if (start < 0)
            start = content.IndexOf('<', StringComparison.Ordinal);

        var xml = content.Substring(start);

        // OFX 1.x SGML may contain unclosed tags; if it fails as XML we fall back to SGML conversion.
        return XDocument.Parse(xml, LoadOptions.None);
    }

    private static string ConvertSgmlToXml(string content)
    {
        var start = content.IndexOf("<OFX", StringComparison.OrdinalIgnoreCase);
        var xml = start >= 0 ? content.Substring(start) : content;

        // SGML attribute shorthand like <OFX> and unquoted attributes are rare in bank files,
        // but many QBO files omit closing tags for the outer <OFX>. Ensure a well-formed document.
        xml = Regex.Replace(xml, @"(?<=[\r\n])\s*</?(?![A-Za-z/])", string.Empty);

        return $"<OFX>{xml.Trim()}</OFX>";
    }

    private static decimal ParseSignedAmount(string value, List<string> warnings, string raw)
    {
        // BAI2 uses a trailing sign convention: e.g. "1234.56" or "1234.56D" (debit) / "1234.56C" (credit).
        var trimmed = value.Trim();
        if (decimal.TryParse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
            return amount;

        if (trimmed.EndsWith('D') || trimmed.EndsWith('d'))
        {
            var numeric = trimmed[..^1];
            return decimal.TryParse(numeric, NumberStyles.Any, CultureInfo.InvariantCulture, out var debit)
                ? -debit
                : 0;
        }

        if (trimmed.EndsWith('C') || trimmed.EndsWith('c'))
        {
            var numeric = trimmed[..^1];
            return decimal.TryParse(numeric, NumberStyles.Any, CultureInfo.InvariantCulture, out var credit)
                ? credit
                : 0;
        }

        warnings.Add($"Could not parse BAI2 amount '{value}' on line: {raw}");
        return 0;
    }

    private static decimal NormalizeOfxAmount(string type, decimal amount)
    {
        // In OFX, credits are positive. Some banks export debits as negative, others positive.
        // Our model: deposit/credit = positive, withdrawal/debit = negative.
        var debitTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "DEBIT", "CHECK", "PAYMENT", "FEE", "ATM", "XFER", "POS" };
        if (debitTypes.Contains(type) && amount > 0)
            return -amount;
        return amount;
    }

    private static DateTimeOffset? ParseOfxDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        // OFX dates: YYYYMMDD[HHMMSS[.XXX][gmt offset]]
        var cleaned = raw.Trim();
        if (cleaned.Length >= 8
            && int.TryParse(cleaned.AsSpan(0, 4), out var year)
            && int.TryParse(cleaned.AsSpan(4, 2), out var month)
            && int.TryParse(cleaned.AsSpan(6, 2), out var day))
        {
            return new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero);
        }

        return null;
    }

    private static bool TryParseBai2Date(string value, out DateTimeOffset result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value) || value.Length < 8)
            return false;

        if (!int.TryParse(value.AsSpan(0, 4), out var year)
            || !int.TryParse(value.AsSpan(4, 2), out var month)
            || !int.TryParse(value.AsSpan(6, 2), out var day))
        {
            return false;
        }

        if (month < 1 || month > 12 || day < 1 || day > 31)
            return false;

        result = new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero);
        return true;
    }

    private static bool TryParseCsvDate(string value, out DateTimeOffset result)
    {
        return DateTimeOffset.TryParse(
            value,
            CultureInfo.CurrentCulture,
            DateTimeStyles.AssumeUniversal,
            out result);
    }

    private static bool TryParseDecimal(string value, out decimal result)
    {
        var cleaned = value.Trim();
        if (cleaned.StartsWith('"'))
        {
            cleaned = cleaned.Trim('"');
        }

        return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out result)
            || decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.CurrentCulture, out result);
    }

    private static int FindColumn(IReadOnlyList<string> header, bool hasHeader, params string[] candidates)
    {
        if (!hasHeader)
            return -1;

        for (var i = 0; i < header.Count; i++)
        {
            if (candidates.Any(c => string.Equals(header[i], c, StringComparison.OrdinalIgnoreCase)))
                return i;
        }

        return -1;
    }

    private static List<IReadOnlyList<string>> CsvSplit(string content)
    {
        var rows = new List<IReadOnlyList<string>>();
        var current = new List<string>();
        var field = new System.Text.StringBuilder();
        var inQuotes = false;

        foreach (var line in content.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            current.Clear();
            field.Clear();
            inQuotes = false;

            var text = line.TrimEnd('\r');
            var i = 0;
            while (i < text.Length)
            {
                var c = text[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i += 2;
                        continue;
                    }

                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    current.Add(field.ToString().Trim());
                    field.Clear();
                }
                else
                {
                    field.Append(c);
                }

                i++;
            }

            current.Add(field.ToString().Trim());
            rows.Add(current.ToList());
        }

        return rows;
    }
}

internal static class XElementExtensions
{
    public static XElement? ElementLocal(this XElement element, string localName)
    {
        return element.Elements().FirstOrDefault(e => e.Name.LocalName == localName);
    }
}
