using DataSyncEngine.Contracts;
using DataSyncEngine.Core.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace DataSyncEngine.Infrastructure.Data;

public class SqlServerBulkRepository : IBulkRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SqlServerBulkRepository> _logger;

    private const string CreateTempTableSql = @"
        CREATE TABLE ##TempProducts (
            ExternalId nvarchar(100) NOT NULL PRIMARY KEY,
            Name nvarchar(500) NOT NULL,
            CategoryCode nvarchar(50) NOT NULL,
            Price decimal(18,4) NOT NULL,
            StockQuantity int NOT NULL,
            IsActive bit NOT NULL,
            LastModifiedUtc datetime2 NOT NULL
        );";

    private const string MergeSql = @"
        MERGE Products WITH (HOLDLOCK) AS target
        USING ##TempProducts AS source
        ON target.ExternalId = source.ExternalId
        WHEN MATCHED AND (
            target.Name <> source.Name OR
            target.CategoryCode <> source.CategoryCode OR
            target.Price <> source.Price OR
            target.StockQuantity <> source.StockQuantity OR
            target.IsActive <> source.IsActive
        ) THEN UPDATE SET
            target.Name = source.Name,
            target.CategoryCode = source.CategoryCode,
            target.Price = source.Price,
            target.StockQuantity = source.StockQuantity,
            target.IsActive = source.IsActive,
            target.IsDeleted = 0,
            target.LastSyncedAtUtc = SYSUTCDATETIME(),
            target.UpdatedAtUtc = SYSUTCDATETIME()
        WHEN NOT MATCHED BY TARGET THEN INSERT (
            ExternalId, Name, CategoryCode, Price, StockQuantity,
            IsActive, IsDeleted, LastSyncedAtUtc, CreatedAtUtc, UpdatedAtUtc
        ) VALUES (
            source.ExternalId, source.Name, source.CategoryCode, source.Price,
            source.StockQuantity, source.IsActive, 0,
            SYSUTCDATETIME(), SYSUTCDATETIME(), SYSUTCDATETIME()
        )
        OUTPUT $action;";

    private const string SoftDeleteSql = @"
        UPDATE p
        SET p.IsDeleted = 1,
            p.UpdatedAtUtc = SYSUTCDATETIME()
        FROM Products p
        WHERE p.IsDeleted = 0
          AND p.ExternalId NOT IN (SELECT t.ExternalId FROM ##TempProducts t);";

    private const string CleanupTempTableSql = @"
        IF OBJECT_ID('tempdb..##TempProducts') IS NOT NULL
            DROP TABLE ##TempProducts;";

    public SqlServerBulkRepository(
        string connectionString,
        ILogger<SqlServerBulkRepository> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<int> StageProductsAsync(
        IReadOnlyList<ExternalProductDto> products,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var dropCommand = new SqlCommand(CleanupTempTableSql, connection);
        await dropCommand.ExecuteNonQueryAsync(cancellationToken);

        await using var createCommand = new SqlCommand(CreateTempTableSql, connection);
        await createCommand.ExecuteNonQueryAsync(cancellationToken);

        var dataTable = BuildDataTable(products);

        using var bulkCopy = new SqlBulkCopy(connection)
        {
            DestinationTableName = "##TempProducts",
            BatchSize = 1000
        };

        bulkCopy.ColumnMappings.Add(nameof(ExternalProductDto.ExternalId), "ExternalId");
        bulkCopy.ColumnMappings.Add(nameof(ExternalProductDto.Name), "Name");
        bulkCopy.ColumnMappings.Add(nameof(ExternalProductDto.CategoryCode), "CategoryCode");
        bulkCopy.ColumnMappings.Add(nameof(ExternalProductDto.Price), "Price");
        bulkCopy.ColumnMappings.Add(nameof(ExternalProductDto.StockQuantity), "StockQuantity");
        bulkCopy.ColumnMappings.Add(nameof(ExternalProductDto.IsActive), "IsActive");
        bulkCopy.ColumnMappings.Add(nameof(ExternalProductDto.LastModifiedUtc), "LastModifiedUtc");

        await bulkCopy.WriteToServerAsync(dataTable, cancellationToken);

        _logger.LogInformation("Staged {Count} records into ##TempProducts", dataTable.Rows.Count);

        return dataTable.Rows.Count;
    }

    public async Task<(int Inserted, int Updated)> MergeProductsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(MergeSql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var inserted = 0;
        var updated = 0;

        while (await reader.ReadAsync(cancellationToken))
        {
            var action = reader.GetString(0);
            if (action == "INSERT")
                inserted++;
            else if (action == "UPDATE")
                updated++;
        }

        _logger.LogInformation(
            "MERGE complete: {Inserted} inserted, {Updated} updated",
            inserted, updated);

        return (inserted, updated);
    }

    public async Task<int> SoftDeleteMissingAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(SoftDeleteSql, connection);
        var deletedCount = await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("Soft-deleted {Count} records no longer in source", deletedCount);

        return deletedCount;
    }

    public async Task CleanupTempTableAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(CleanupTempTableSql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("Temp table cleanup complete");
    }

    private static DataTable BuildDataTable(IReadOnlyList<ExternalProductDto> products)
    {
        var table = new DataTable();

        table.Columns.Add(nameof(ExternalProductDto.ExternalId), typeof(string));
        table.Columns.Add(nameof(ExternalProductDto.Name), typeof(string));
        table.Columns.Add(nameof(ExternalProductDto.CategoryCode), typeof(string));
        table.Columns.Add(nameof(ExternalProductDto.Price), typeof(decimal));
        table.Columns.Add(nameof(ExternalProductDto.StockQuantity), typeof(int));
        table.Columns.Add(nameof(ExternalProductDto.IsActive), typeof(bool));
        table.Columns.Add(nameof(ExternalProductDto.LastModifiedUtc), typeof(DateTime));

        foreach (var product in products)
        {
            table.Rows.Add(
                product.ExternalId,
                product.Name,
                product.CategoryCode,
                product.Price,
                product.StockQuantity,
                product.IsActive,
                product.LastModifiedUtc);
        }

        return table;
    }
}
