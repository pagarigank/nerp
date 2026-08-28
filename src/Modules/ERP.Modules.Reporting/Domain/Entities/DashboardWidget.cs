// <copyright file="DashboardWidget.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Reporting.Domain.Entities;

public class DashboardWidget : AuditableAggregateRoot
{
    protected DashboardWidget() { }

    public DashboardWidget(
        Guid companyId,
        string dashboardId,
        string name,
        string widgetType,
        string? dataSourceType,
        string? dataSourceConfigJson,
        string? displayConfigJson,
        int positionX,
        int positionY,
        int width,
        int height) : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        DashboardId = dashboardId ?? throw new ArgumentNullException(nameof(dashboardId));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        WidgetType = widgetType ?? throw new ArgumentNullException(nameof(widgetType));
        DataSourceType = dataSourceType;
        DataSourceConfigJson = dataSourceConfigJson;
        DisplayConfigJson = displayConfigJson;
        PositionX = positionX;
        PositionY = positionY;
        Width = width;
        Height = height;
        RefreshIntervalSeconds = 300;
        IsActive = true;
    }

    public Guid CompanyId { get; private set; }
    public string DashboardId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string WidgetType { get; private set; } = string.Empty; // StatCard, PieChart, BarChart, LineChart, Table, Gauge
    public string? DataSourceType { get; private set; } // ReportDefinition, QuickQuery, Api, Static
    public string? DataSourceConfigJson { get; private set; }
    public string? DisplayConfigJson { get; private set; }
    public int PositionX { get; private set; }
    public int PositionY { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public int RefreshIntervalSeconds { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(string name, string widgetType, string? dataSourceType,
        string? dataSourceConfigJson, string? displayConfigJson,
        int positionX, int positionY, int width, int height)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        WidgetType = widgetType ?? throw new ArgumentNullException(nameof(widgetType));
        DataSourceType = dataSourceType;
        DataSourceConfigJson = dataSourceConfigJson;
        DisplayConfigJson = displayConfigJson;
        PositionX = positionX;
        PositionY = positionY;
        Width = width;
        Height = height;
    }

    public void SetRefreshInterval(int seconds) => RefreshIntervalSeconds = seconds;
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
