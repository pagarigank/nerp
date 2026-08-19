// <copyright file="SegmentValidationService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Text.Json;
using ERP.Modules.Platform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Platform.Infrastructure;

public interface ISegmentValidationService
{
    Task<bool> ValidateCombinationAsync(Guid companyId, Dictionary<string, string> segmentValues, CancellationToken cancellationToken = default);
}

public class SegmentValidationService : ISegmentValidationService
{
    private readonly PlatformDbContext _context;

    public SegmentValidationService(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public static string BuildCombinationKey(Dictionary<string, string> segmentValues)
    {
        var sorted = segmentValues.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase);
        return string.Join(":", sorted.Select(kv => $"{kv.Key}={kv.Value}"));
    }

    public async Task<bool> ValidateCombinationAsync(
        Guid companyId,
        Dictionary<string, string> segmentValues,
        CancellationToken cancellationToken = default)
    {
        var requiredSegmentTypes = await _context.SegmentTypes
            .Where(x => x.CompanyId == companyId && x.IsActive && x.IsRequired)
            .ToListAsync(cancellationToken);

        foreach (var required in requiredSegmentTypes)
        {
            if (segmentValues == null || !segmentValues.ContainsKey(required.Code))
                return false;

            var value = segmentValues[required.Code];
            var exists = await _context.SegmentValues
                .AnyAsync(x => x.SegmentTypeId == required.Id && x.Value == value && x.IsActive, cancellationToken);

            if (!exists)
                return false;
        }

        if (segmentValues == null || segmentValues.Count == 0)
            return true;

        var combinationKey = BuildCombinationKey(segmentValues);
        var validCombination = await _context.ValidatedCombinations
            .AnyAsync(x =>
                x.CompanyId == companyId
                && x.CombinationKey == combinationKey
                && x.IsActive,
                cancellationToken);

        return validCombination;
    }
}
