// <copyright file="InventoryTestCollection.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Xunit;

namespace ERP.IntegrationTests.Inventory;

/// <summary>
/// Serializes the Inventory integration tests. Each test calls CleanDatabaseAsync
/// which issues DELETEs against the shared inv.* tables; running these classes in
/// parallel (xUnit's default) causes them to truncate each other's seed data and
/// produce intermittent failures. A single shared collection forces sequential
/// execution within the Inventory test set while leaving other modules parallel.
/// </summary>
[CollectionDefinition("Inventory Integration", DisableParallelization = true)]
public class InventoryTestCollection
{
}
