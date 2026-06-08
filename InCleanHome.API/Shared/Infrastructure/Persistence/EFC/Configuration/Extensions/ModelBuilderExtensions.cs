using Microsoft.EntityFrameworkCore;

namespace InCleanHome.API.Shared.Infrastructure.Persistence.EFC.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void UseSnakeCaseNamingConvention(this ModelBuilder builder)
    {
        foreach (var entity in builder.Model.GetEntityTypes())
        {
            var tableName = entity.GetTableName();
            if (!string.IsNullOrEmpty(tableName)) entity.SetTableName(tableName.ToPlural().ToSnakeCase());

            foreach (var property in entity.GetProperties())
                property.SetColumnName(property.GetColumnName().ToSnakeCase());

            foreach (var key in entity.GetKeys())
            {
                var keyName = key.GetName();
                if (!string.IsNullOrEmpty(keyName)) key.SetName(keyName.ToSnakeCase());
            }

            foreach (var foreignKey in entity.GetForeignKeys())
            {
                var fkName = foreignKey.GetConstraintName();
                if (!string.IsNullOrEmpty(fkName)) foreignKey.SetConstraintName(fkName.ToSnakeCase());
            }

            foreach (var index in entity.GetIndexes())
            {
                var idxName = index.GetDatabaseName();
                if (!string.IsNullOrEmpty(idxName)) index.SetDatabaseName(idxName.ToSnakeCase());
            }
        }
    }
}
