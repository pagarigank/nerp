using ERP.Modules.CashManagement.Domain.Entities;
using ERP.Modules.CashManagement.Infrastructure;
using FluentAssertions;
using Xunit;

namespace ERP.Modules.CashManagement.Tests;

public class BankStatementParserTests
{
    private readonly BankStatementParserService _parser = new BankStatementParserService();

    [Fact]
    public void DetectFormatRecognizesBai2()
    {
        var content = "01,0001,US123456789,USD,100\n03,1000,USD,000,1000.00,1000.00";

        var format = _parser.DetectFormat(content);

        format.Should().Be(BankStatementFormat.Bai2);
    }

    [Fact]
    public void DetectFormatRecognizesOfxXml()
    {
        var content = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<OFX><BANKTRANLIST></BANKTRANLIST></OFX>";

        var format = _parser.DetectFormat(content);

        format.Should().Be(BankStatementFormat.Ofx);
    }

    [Fact]
    public void DetectFormatRecognizesQbo()
    {
        var content = "!TYPE:QBO\nOFXHEADER:100\nDATA:OFXSGML";

        var format = _parser.DetectFormat(content);

        format.Should().Be(BankStatementFormat.Qbo);
    }

    [Fact]
    public void DetectFormatFallsBackToCsv()
    {
        var content = "Date,Amount,Description\n2025-01-15,100.00,Deposit";

        var format = _parser.DetectFormat(content);

        format.Should().Be(BankStatementFormat.Csv);
    }

    [Fact]
    public void ParseBai2ExtractsTransactionsAndBalances()
    {
        var content = string.Join(
            '\n',
            "01,0001,US123456789,USD,100",
            "02,DEMO,20250115,20250115,100",
            "03,1000,USD,000,1000.00,1000.00",
            "16,1000,100.00D,Z,20250115,AP PAYMENT",
            "16,1000,250.00C,Z,20250116,AR DEPOSIT",
            "49,1150.00,20250115,USD");

        var result = _parser.Parse(content, BankStatementFormat.Bai2);

        result.Format.Should().Be(BankStatementFormat.Bai2);
        result.BeginningBalance.Should().Be(1000m);
        result.EndingBalance.Should().Be(1150m);
        result.Lines.Should().HaveCount(2);

        result.Lines[0].Amount.Should().Be(-100m);
        result.Lines[0].Description.Should().Contain("AP PAYMENT");
        result.Lines[1].Amount.Should().Be(250m);
        result.Lines[1].Description.Should().Contain("AR DEPOSIT");
    }

    [Fact]
    public void ParseBai2ParsesUnsignedAmountAsGiven()
    {
        var content = string.Join(
            '\n',
            "01,0001,US123456789,USD,100",
            "16,1000,50.00,Z,20250115,INTEREST",
            "49,1050.00,20250115,USD");

        var result = _parser.Parse(content, BankStatementFormat.Bai2);

        result.Lines.Should().HaveCount(1);
        result.Lines[0].Amount.Should().Be(50m);
    }

    [Fact]
    public void ParseOfxXmlExtractsDebitsAndCredits()
    {
        var content = """
            <?xml version="1.0" encoding="UTF-8"?>
            <OFX>
              <BANKMSGSRSV1>
                <STMTTRNRS>
                  <STMTRS>
                    <LEDGERBAL><BALAMT>1150.00</BALAMT></LEDGERBAL>
                    <BANKTRANLIST>
                      <STMTTRN>
                        <TRNTYPE>DEBIT</TRNTYPE>
                        <DTPOSTED>20250115</DTPOSTED>
                        <TRNAMT>-100.00</TRNAMT>
                        <FITID>1001</FITID>
                        <NAME>PAYMENT</NAME>
                        <MEMO>PMT-0001</MEMO>
                        <CHECKNUM>1001</CHECKNUM>
                      </STMTTRN>
                      <STMTTRN>
                        <TRNTYPE>CREDIT</TRNTYPE>
                        <DTPOSTED>20250116</DTPOSTED>
                        <TRNAMT>250.00</TRNAMT>
                        <FITID>1002</FITID>
                        <NAME>DEPOSIT</NAME>
                        <MEMO>CR-0001</MEMO>
                      </STMTTRN>
                    </BANKTRANLIST>
                  </STMTRS>
                </STMTTRNRS>
              </BANKMSGSRSV1>
            </OFX>
            """;

        var result = _parser.Parse(content, BankStatementFormat.Ofx);

        result.Format.Should().Be(BankStatementFormat.Ofx);
        result.EndingBalance.Should().Be(1150m);
        result.Lines.Should().HaveCount(2);
        result.Lines[0].Amount.Should().Be(-100m);
        result.Lines[0].CheckNumber.Should().Be("1001");
        result.Lines[1].Amount.Should().Be(250m);
    }

    [Fact]
    public void ParseQboSgmlExtractsTransactions()
    {
        var content = string.Join(
            '\n',
            "!TYPE:QBO",
            "OFXHEADER:100",
            "DATA:OFXSGML",
            "VERSION:102",
            string.Empty,
            "<OFX>",
            "<BANKMSGSRSV1>",
            "<STMTTRNRS>",
            "<STMTRS>",
            "<BANKTRANLIST>",
            "<STMTTRN>",
            "<TRNTYPE>CREDIT</TRNTYPE>",
            "<DTPOSTED>20250116</DTPOSTED>",
            "<TRNAMT>250.00</TRNAMT>",
            "<FITID>2001</FITID>",
            "<NAME>DEPOSIT</NAME>",
            "</STMTTRN>",
            "</BANKTRANLIST>",
            "</STMTRS>",
            "</STMTTRNRS>",
            "</BANKMSGSRSV1>",
            "</OFX>");

        var result = _parser.Parse(content, BankStatementFormat.Qbo);

        result.Format.Should().Be(BankStatementFormat.Qbo);
        result.Lines.Should().HaveCount(1);
        result.Lines[0].Amount.Should().Be(250m);
    }

    [Fact]
    public void ParseCsvWithAmountColumnWorks()
    {
        var content = string.Join(
            '\n',
            "Date,Amount,Description,Reference,Balance",
            "2025-01-15,-100.00,Vendor payment,PMT-0001,1000.00",
            "2025-01-16,250.00,Customer payment,CR-0001,1250.00");

        var result = _parser.Parse(content, BankStatementFormat.Csv);

        result.Format.Should().Be(BankStatementFormat.Csv);
        result.Lines.Should().HaveCount(2);
        result.Lines[0].Amount.Should().Be(-100m);
        result.Lines[0].ReferenceNumber.Should().Be("PMT-0001");
        result.Lines[1].Amount.Should().Be(250m);
        result.EndingBalance.Should().Be(1250m);
    }

    [Fact]
    public void ParseCsvWithDebitAndCreditColumnsWorks()
    {
        var content = string.Join(
            '\n',
            "Date,Description,Debit,Credit",
            "2025-01-15,Vendor payment,100.00,",
            "2025-01-16,Customer deposit,,250.00");

        var result = _parser.Parse(content, BankStatementFormat.Csv);

        result.Lines.Should().HaveCount(2);
        result.Lines[0].Amount.Should().Be(-100m);
        result.Lines[1].Amount.Should().Be(250m);
    }

    [Fact]
    public void ParseEmptyContentThrows()
    {
        var act = () => _parser.Parse("   ", BankStatementFormat.Csv);

        act.Should().Throw<ArgumentException>().WithMessage("*empty*");
    }

    [Fact]
    public void ParseCsvAddsWarningWhenNoAmountColumnFound()
    {
        var content = "Date,Description,Reference\n2025-01-15,Note,REF-001";

        var result = _parser.Parse(content, BankStatementFormat.Csv);

        result.Warnings.Should().Contain(w => w.Contains("amount column", StringComparison.OrdinalIgnoreCase));
    }
}
