// <copyright file="TaxExemptionCertificateTests.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System;
using ERP.Modules.OrderManagement.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace ERP.Modules.OrderManagement.Tests;

public class TaxExemptionCertificateTests
{
    private static TaxExemptionCertificate Cert(DateTime from, DateTime to, bool active = true)
    {
        var c = new TaxExemptionCertificate(
            Guid.NewGuid(), "EX-1", Guid.NewGuid(), "CA", from, to, null, null);
        if (!active)
            c.Revoke();
        return c;
    }

    [Fact]
    public void IsValidOn_ReturnsTrue_WhenDateInsideWindow_AndActive()
    {
        var c = Cert(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
        c.IsValidOn(new DateTime(2026, 6, 15)).Should().BeTrue();
    }

    [Fact]
    public void IsValidOn_ReturnsFalse_BeforeWindow()
    {
        var c = Cert(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
        c.IsValidOn(new DateTime(2025, 12, 31)).Should().BeFalse();
    }

    [Fact]
    public void IsValidOn_ReturnsFalse_AfterWindow()
    {
        var c = Cert(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
        c.IsValidOn(new DateTime(2027, 1, 1)).Should().BeFalse();
    }

    [Fact]
    public void IsValidOn_ReturnsFalse_WhenRevoked()
    {
        var c = Cert(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), active: false);
        c.IsValidOn(new DateTime(2026, 6, 15)).Should().BeFalse();
        c.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Revoke_SetsInactive()
    {
        var c = Cert(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
        c.Revoke();
        c.IsActive.Should().BeFalse();
    }
}
