using System.Collections.Generic;
using System.Reflection;

namespace MEFStaticFieldReplacement
{
    public class ReplacementType
    {
        private readonly List<FieldInfo> fields;

        public ReplacementType(string staticTypeName,List<FieldInfo> fields)
        {
            StaticTypeName = staticTypeName;
            this.fields = fields;
        }

        public string StaticTypeName { get; }

        public List<FieldInfo> Fields => fields;

    }
}
