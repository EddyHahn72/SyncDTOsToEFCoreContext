using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HenryHarrow.EntityFrameworkCore
{
    public partial class DbContextWithSoftDelete : DbContext
    {
        public DbContextWithSoftDelete() : base()
        {

        }

        public DbContextWithSoftDelete(DbContextOptions options) : base(options)
        {

        }

        public override int SaveChanges()
        {
            ApplySoftDelete();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            ApplySoftDelete();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void ApplySoftDelete()
        {
            ChangeTracker.DetectChanges();

            var markedAsDeleted = ChangeTracker.Entries().Where(x => x.State == EntityState.Deleted);

            foreach (var item in markedAsDeleted)
            {
                var itemType = item.Entity.GetType();
                var attr = itemType.GetCustomAttributes(false).FirstOrDefault((t) => t is SoftDeleteAttribute) as SoftDeleteAttribute;
                if (attr != null)
                {
                    //mark it undchanged so the generated update does not include all columns
                    item.State = EntityState.Unchanged;
                    //here need to set the LCV
                    if (attr.ValueType == SoftDeleteValue.PresetValue)
                    {
                        var prop = itemType.GetProperty(attr.SoftDeletePropertyName);

                        if (prop == null)
                        {
                            throw new ArgumentException("Invalid property name specified for SoftDeleteAttribute");
                        }

                        itemType.InvokeMember(prop.Name, System.Reflection.BindingFlags.SetProperty, null, item.Entity, new object[] { attr.Value });
                    }
                    else
                    {
                        var prop = itemType.GetProperty(attr.SoftDeletePropertyName);
                        var entityType = Model.GetEntityTypes(itemType).FirstOrDefault();
                        var tableDefinition = Model.GetRelationalModel().Tables.FirstOrDefault((t) => t.EntityTypeMappings.Any((x) => x.EntityType == entityType));

                        var primaryKey = tableDefinition.PrimaryKey;

                        if (primaryKey.Columns.Count != 1)
                        {
                            throw new ArgumentException("SoftDeleteAttribute supports single column primary key");
                        }

                        var pkValue = item.Entity.GetType().GetProperty(primaryKey.MappedKeys.First().Properties[0].PropertyInfo.Name).GetValue(item.Entity);

                        itemType.InvokeMember(prop.Name, System.Reflection.BindingFlags.SetProperty, null, item.Entity, new object[] { pkValue });
                    }
                    item.State = EntityState.Modified;
                }
            }
        }
    }
}
