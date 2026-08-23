// <copyright file="AuditSensitiveValueRedactor.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.Platform.Infrastructure;

/// <summary>
/// Applies case-insensitive property-name redaction rules to sensitive values
/// (SSNs, encrypted columns, bank account/routing numbers) captured by the
/// shared audit trail before they are serialized into audit entries.
/// </summary>
public static class AuditSensitiveValueRedactor
{
    private const string EncryptedPlaceholder = "<encrypted>";

    public static object? Redact(string propertyName, object? value)
    {
        if (value is not string text)
        {
            return value;
        }

        if (propertyName.Contains("Ssn", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("SocialSecurity", StringComparison.OrdinalIgnoreCase)
            || propertyName.EndsWith("Encrypted", StringComparison.OrdinalIgnoreCase))
        {
            return EncryptedPlaceholder;
        }

        if (propertyName.Contains("AccountNumber", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("BankAccount", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("RoutingNumber", StringComparison.OrdinalIgnoreCase))
        {
            return Mask(text);
        }

        return value;
    }

    private static string Mask(string value)
    {
        if (value.Length <= 4)
        {
            return new string('*', value.Length);
        }

        return new string('*', value.Length - 4) + value[^4..];
    }
}
