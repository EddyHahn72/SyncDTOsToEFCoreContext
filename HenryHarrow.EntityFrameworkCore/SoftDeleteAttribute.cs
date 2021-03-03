using System;
namespace HenryHarrow.EntityFrameworkCore
{

    public enum SoftDeleteValueEnum
    {
        ValueOfPrimaryKey,
        PresetValue
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class SoftDeleteAttribute : Attribute
    {
        private string softDeletePropertyName;
        private SoftDeleteValueEnum valueType;
        private object value;

        public SoftDeleteAttribute(string softDeletePropertyName, SoftDeleteValueEnum valueType = SoftDeleteValueEnum.ValueOfPrimaryKey, object value = null)
        {
            this.softDeletePropertyName = softDeletePropertyName;
            this.valueType = valueType;
            this.value = value;
        }

        public string SoftDeletePropertyName { get { return softDeletePropertyName; } }
        public SoftDeleteValueEnum ValueType { get { return valueType; } }
        public object Value { get { return value; } }
    }
}
