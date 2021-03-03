using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace HenryHarrow.EntityFrameworkCore
{

    public static class Extensions
    {
        public static void SyncDtoToEf(this DbContext context, object source, object target)
        {
            var objTracker = new List<object>();
            SyncDtoToEfInternal(context, source, target, objTracker);
        }

        public static void SyncDtoToEf(this DbContextWithSoftDelete context, object source, object target)
        {
            var objTracker = new List<object>();
            SyncDtoToEfInternal(context, source, target, objTracker);
        }

        private static void SyncDtoToEfObject(DbContext context, object source, object target, List<object> objTracker)
        {
            //not a collection but an object
            var typeSource = source.GetType().GetProperties();
            var typeTarget = target.GetType().GetProperties();

            if (objTracker.Contains(source) || objTracker.Contains(target))
            {
                return;
            }
            objTracker.Add(target);

            foreach (var sourceProp in typeSource)
            {
                var targetProp = typeTarget.FirstOrDefault((t) => t.Name == sourceProp.Name);

                if (targetProp != null)
                {
                    var itemType = sourceProp.DeclaringType;
                    var entityType = context.Model.GetEntityTypes(itemType).FirstOrDefault();
                    var tableDefinition = context.Model.GetRelationalModel().Tables.FirstOrDefault((t) => t.EntityTypeMappings.Any((x) => x.EntityType == entityType));
                    var isEnumerable = sourceProp.PropertyType.GenericTypeArguments.FirstOrDefault((t) => context.Model.GetEntityTypes(t)?.Count() > 0);

                    //is this property referencing parent object
                    var referencing = entityType.GetNavigations().FirstOrDefault((c) => c.Name == targetProp.Name && c.IsOnDependent);
                    if (referencing != null)
                    {
                        objTracker.Add(target);
                        continue;
                    }

                    //copying individual properties
                    if (isEnumerable == null)
                    {
                        ProcessObject(context, source, target, sourceProp, targetProp, tableDefinition);
                    }
                    else
                    {
                        //dealing with collections                      
                        var colItemType = sourceProp.PropertyType.GenericTypeArguments.First();
                        var colEntityType = context.Model.GetEntityTypes(colItemType).FirstOrDefault();
                        var colTableDefinition = context.Model.GetRelationalModel().Tables.FirstOrDefault((t) => t.EntityTypeMappings.Any((x) => x.EntityType == colEntityType));

                        if (colEntityType != null && colTableDefinition != null)
                        {
                            var primaryKey = colTableDefinition.PrimaryKey;

                            //gets the enumerator of the collection property
                            var sourceEnum = sourceProp.GetValue(source) as IEnumerable;
                            var targetEnum = targetProp.GetValue(target) as IEnumerable;

                            if (targetEnum == null)
                            {
                                targetEnum = sourceProp.PropertyType.InvokeMember(null, System.Reflection.BindingFlags.CreateInstance, null, null, null) as IEnumerable;
                            }

                            DeleteOrUpdate(context, objTracker, primaryKey, sourceEnum, targetEnum);

                            Insert(context, source, target, objTracker, sourceProp, targetProp, primaryKey);
                        }
                    }
                }
            }
        }

        private static void Insert(DbContext context, object source, object target, List<object> objTracker, System.Reflection.PropertyInfo sourceProp, System.Reflection.PropertyInfo targetProp, Microsoft.EntityFrameworkCore.Metadata.IPrimaryKeyConstraint primaryKey)
        {
            var sourceEnum = sourceProp.GetValue(source) as IEnumerable;
            var targetEnum = targetProp.GetValue(target) as IEnumerable;

            if (targetEnum == null)
            {
                targetEnum = sourceProp.PropertyType.InvokeMember(null, System.Reflection.BindingFlags.CreateInstance, null, null, null) as IEnumerable;
            }

            var toBeOnserted = new List<object>();
            foreach (var sourceItem in sourceEnum)
            {
                object found = null;
                foreach (var targetItem in targetEnum)
                {
                    foreach (var primaryColumn in primaryKey.MappedKeys)
                    {
                        var targetValue = targetItem.GetType().GetProperty(primaryColumn.Properties[0].PropertyInfo.Name).GetValue(targetItem);
                        var sourceValue = sourceItem.GetType().GetProperty(primaryColumn.Properties[0].PropertyInfo.Name).GetValue(sourceItem);

                        if ((targetValue == null && sourceValue == null) || 
                            (targetValue != null && sourceValue != null && targetValue.Equals(sourceValue)))
                        {
                            found = targetItem;
                        }
                        else
                        {
                            found = null;
                        }
                    }
                    if (found != null)
                    {
                        break;
                    }
                }

                if (found == null)
                {
                    //creating new item here
                    var newItem = targetEnum.GetType().GenericTypeArguments[0].InvokeMember(null, System.Reflection.BindingFlags.CreateInstance, null, null, null);
                    toBeOnserted.Add(newItem);
                    SyncDtoToEfInternal(context, sourceItem, newItem, objTracker);
                }
            }

            //adding them to target collection and setting the object state in Datacontext
            foreach (var item in toBeOnserted)
            {
                context.Attach(item);
                _ = targetEnum.GetType().InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, targetEnum, new object[] { item });
                if (context.Entry(item).State == EntityState.Unchanged || context.Entry(item).State == EntityState.Modified)
                {
                    context.Entry(item).State = EntityState.Added;
                }
            }
        }

        private static void DeleteOrUpdate(DbContext context, List<object> objTracker, Microsoft.EntityFrameworkCore.Metadata.IPrimaryKeyConstraint primaryKey, IEnumerable sourceEnum, IEnumerable targetEnum)
        {
            //delete and update
            List<object> deleted = new List<object>();
            foreach (var targetItem in targetEnum)
            {
                object found = null;
                foreach (var sourceItem in sourceEnum)
                {
                    foreach (var primaryColumn in primaryKey.MappedKeys)
                    {
                        var targetValue = targetItem.GetType().GetProperty(primaryColumn.Properties[0].PropertyInfo.Name).GetValue(targetItem);
                        var sourceValue = sourceItem.GetType().GetProperty(primaryColumn.Properties[0].PropertyInfo.Name).GetValue(sourceItem);

                        if ((targetValue == null && sourceValue == null) ||
                            targetValue.Equals(sourceValue))
                        {
                            found = sourceItem;
                        }
                        else
                        {
                            found = null;
                        }
                    }
                    if (found != null)
                    {
                        break;
                    }
                }

                if (found != null)
                {
                    SyncDtoToEfInternal(context, found, targetItem, objTracker);
                }
                else
                {
                    //delete
                    deleted.Add(targetItem);
                }
            }

            foreach (var obj in deleted)
            {
                if (context.Entry(obj).State == EntityState.Unchanged)
                {
                    context.Entry(obj).State = EntityState.Deleted;
                }
                targetEnum.GetType().InvokeMember("Remove", System.Reflection.BindingFlags.InvokeMethod, null, targetEnum, new object[] { obj });
            }
        }

        private static void ProcessObject(DbContext context, object source, object target, System.Reflection.PropertyInfo sourceProp, System.Reflection.PropertyInfo targetProp, Microsoft.EntityFrameworkCore.Metadata.ITable tableDefinition)
        {
            var primaryKey = tableDefinition.PrimaryKey;

            var isPrimaryKey = primaryKey.Columns.Any((p) => p.PropertyMappings.Any((m) => m.Property.Name == sourceProp.Name));

            if (!isPrimaryKey)
            {
                var valueSource = sourceProp.GetValue(source);
                var valueTarget = targetProp.GetValue(target);
                if ((valueSource == null && valueTarget != null) ||
                    (valueSource != null && valueTarget == null) ||
                    ((valueSource != null && valueTarget != null) &&
                    !valueSource.Equals(valueTarget)))
                {

                    targetProp.SetValue(target, valueSource);
                    if (context.Entry(target).State == EntityState.Unchanged)
                    {
                        context.Entry(target).State = EntityState.Modified;
                    }
                }
            }
        }

        private static void SyncDtoToEfCollection(DbContext context, object source, object target, List<object> objTracker)
        {
            //collection
            var sourceEnum = source as IEnumerable;
            var targetEnum = target as IEnumerable;

            if (sourceEnum == null || targetEnum == null)
            {
                throw new ArgumentNullException();
            }
            else
            {
                var colItemType = source.GetType().GenericTypeArguments.First();
                var colEntityType = context.Model.GetEntityTypes(colItemType).FirstOrDefault();
                var colTableDefinition = context.Model.GetRelationalModel().Tables.FirstOrDefault((t) => t.EntityTypeMappings.Any((x) => x.EntityType == colEntityType));

                if (colEntityType != null)
                {
                    if (colTableDefinition != null)
                    {
                        var primaryKey = colTableDefinition.PrimaryKey;

                        //delete and update
                        List<object> deleted = new List<object>();
                        foreach (var targetItem in targetEnum)
                        {
                            object found = null;
                            foreach (var sourceItem in sourceEnum)
                            {
                                foreach (var primaryColumn in primaryKey.MappedKeys)
                                {
                                    var targetValue = targetItem.GetType().GetProperty(primaryColumn.Properties[0].PropertyInfo.Name).GetValue(targetItem);
                                    var sourceValue = sourceItem.GetType().GetProperty(primaryColumn.Properties[0].PropertyInfo.Name).GetValue(sourceItem);

                                    if ((targetValue == null && sourceValue == null) ||
                                        (targetValue.Equals(sourceValue)))
                                    {
                                        found = sourceItem;
                                    }
                                    else
                                    {
                                        found = null;
                                    }
                                }
                                if (found != null)
                                {
                                    break;
                                }
                            }

                            if (found != null)
                            {
                                SyncDtoToEfInternal(context, found, targetItem, objTracker);
                            }
                            else
                            {
                                //delete
                                deleted.Add(targetItem);
                            }
                        }

                        foreach (var obj in deleted)
                        {
                            if (context.Entry(obj).State == EntityState.Unchanged)
                            {
                                context.Entry(obj).State = EntityState.Deleted;
                            }
                            targetEnum.GetType().InvokeMember("Remove", System.Reflection.BindingFlags.InvokeMethod, null, targetEnum, new object[] { obj });
                        }

                        //insert
                        sourceEnum = source as IEnumerable;
                        targetEnum = target as IEnumerable;

                        if (targetEnum == null)
                        {
                            targetEnum = colItemType.InvokeMember(null, System.Reflection.BindingFlags.CreateInstance, null, null, null) as IEnumerable;
                        }

                        var toBeOnserted = new List<object>();
                        foreach (var sourceItem in sourceEnum)
                        {
                            object found = null;
                            foreach (var targetItem in targetEnum)
                            {
                                foreach (var primaryColumn in primaryKey.MappedKeys)
                                {
                                    var targetValue = targetItem.GetType().GetProperty(primaryColumn.Properties[0].PropertyInfo.Name).GetValue(targetItem);
                                    var sourceValue = sourceItem.GetType().GetProperty(primaryColumn.Properties[0].PropertyInfo.Name).GetValue(sourceItem);

                                    if ((targetValue == null && sourceValue == null) ||
                                        (targetValue.Equals(sourceValue)))
                                    {
                                        found = targetItem;
                                    }
                                    else
                                    {
                                        found = null;
                                    }
                                }
                                if (found != null)
                                {
                                    break;
                                }
                            }

                            if (found == null)
                            {
                                //creating new item here
                                var newItem = targetEnum.GetType().GenericTypeArguments[0].InvokeMember(null, System.Reflection.BindingFlags.CreateInstance, null, null, null);
                                toBeOnserted.Add(newItem);
                                SyncDtoToEfInternal(context, sourceItem, newItem, objTracker);
                            }
                        }

                        //adding them to target collection and setting the object state in Datacontext
                        foreach (var item in toBeOnserted)
                        {
                            _ = targetEnum.GetType().InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, targetEnum, new object[] { item });
                            context.Attach(item);
                            if (context.Entry(item).State == EntityState.Unchanged || context.Entry(item).State == EntityState.Modified)
                            {
                                context.Entry(item).State = EntityState.Added;
                            }
                        }
                    }
                }
            }
        }

        private static void SyncDtoToEfInternal(DbContext context, object source, object target, List<object> objTracker)
        {
            var interfaces = source.GetType().FindInterfaces(
                new System.Reflection.TypeFilter((t, o) => { return o is System.Collections.IEnumerable; }), source);

            if (interfaces.Count() == 0)
            {
                SyncDtoToEfObject(context, source, target, objTracker);
            }
            else
            {
                SyncDtoToEfCollection(context, source, target, objTracker);
            }
        }

    }
}
