using System;
namespace HenryHarrow.EntityFrameworkCore
{

    public enum SoftDeleteValue
    {
        ValueOfPrimaryKey,
        PresetValue
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class SoftDeleteAttribute : Attribute
    {
        private readonly string softDeletePropertyName;
        private readonly SoftDeleteValue valueType;
        private readonly object value;

        public SoftDeleteAttribute(string softDeletePropertyName, SoftDeleteValue valueType = SoftDeleteValue.ValueOfPrimaryKey, object value = null)
        {
            this.softDeletePropertyName = softDeletePropertyName;
            this.valueType = valueType;
            this.value = value;
        }

        public string SoftDeletePropertyName { get { return softDeletePropertyName; } }
        public SoftDeleteValue ValueType { get { return valueType; } }
        public object Value { get { return value; } }
    }
}
