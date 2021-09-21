using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MEFStaticFieldReplacement
{
    public static class ReplacementTypes
    {
        public static IEnumerable<ReplacementType> FromAssemblies(params Assembly[] assemblies)
        {
            return assemblies.SelectMany(a => FromTypes(a.ExportedTypes.ToArray()));
        }

        public static IEnumerable<ReplacementType> FromTypes(params Type[] types)
        {
            var staticTypes = types.Where(t => t.IsAbstract && t.IsSealed);
            foreach (var staticType in staticTypes)
            {
                var fields = staticType.GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.FieldType.IsSubclassOf(typeof(Delegate))).ToList();
                if (fields.Count > 0)
                {
                    yield return new ReplacementType(staticType.Name, fields);
                }
            }
        }
    }
}
