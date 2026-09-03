// <copyright file="ICertificateService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.EInvoice.Application;

public interface ICertificateService
{
    Task<List<CertificateDto>> GetAllByCompanyIdAsync(Guid companyId);
    Task<CertificateDto?> GetByIdAsync(Guid id);
    Task<CertificateDto> UploadAsync(Guid companyId, byte[] fileContent, string fileName, string name, bool isActive);
    Task<CertificateDto> ActivateAsync(Guid id);
    Task<bool> RevokeAsync(Guid id, string reason);
}

public class CertificateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public DateTime NotBefore { get; set; }
    public DateTime NotAfter { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}