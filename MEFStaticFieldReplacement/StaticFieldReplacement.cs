using ExtendedRegistrationBuilder;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;

namespace MEFStaticFieldReplacement
{
    public static class StaticFieldReplacement
    {
        private static readonly List<MatchingMember<MethodInfo>> matchingMethods = new List<MatchingMember<MethodInfo>>();
        private static readonly List<MatchingMember<PropertyInfo>> matchingProperties = new List<MatchingMember<PropertyInfo>>();
        private static readonly List<MatchingMember> allMatchingMembers = new List<MatchingMember>();

        public static string DefaultSingleReplacementTypeName = "Replacements";
        
        public static RegistrationBuilderWithMethods ReflectionContext(
            IEnumerable<ReplacementType> replacementTypes,
            Func<Type,string,bool> replacementTypePredicate = null,
            Func<MethodInfo,FieldInfo,bool> methodPredicate = null,
            Func<PropertyInfo, FieldInfo, bool> propertyPredicate = null,
            string singleReplacementTypeName = null
            ) {

            singleReplacementTypeName = singleReplacementTypeName  ?? DefaultSingleReplacementTypeName;
            replacementTypePredicate = replacementTypePredicate ?? ((t, staticTypeName) =>
            {
                return staticTypeName == t.Name.Replace("Replacement", "") || t.Name == singleReplacementTypeName;
            });
            bool DefaultPredicate(MemberInfo m, FieldInfo field)
            {
                if (m.Name == field.Name)
                {
                    return true;
                }

                var isSingleReplacementType = m.DeclaringType.Name == singleReplacementTypeName;
                return isSingleReplacementType && GetMemberName(m) == $"{field.DeclaringType.Name}{field.Name}";
            }
            methodPredicate = methodPredicate ?? ((m,field) =>
            {
                return DefaultPredicate(m, field);
            });
            propertyPredicate = propertyPredicate ?? ((p, field) =>
            {
                if (p.CanRead)
                {
                    return DefaultPredicate(p, field);
                }
                return false;
                
            });
            var reflectionContext = new RegistrationBuilderWithMethods();

            Dictionary<Type, bool> typeRuns = new Dictionary<Type, bool>();
            var partBuilder = reflectionContext.ForTypesMatching(t =>
            {
                if (typeRuns.ContainsKey(t))
                {
                    return typeRuns[t];
                }

                var typeMatch = TypeMatch(t,replacementTypes, replacementTypePredicate, methodPredicate, propertyPredicate);
                typeRuns[t] = typeMatch;
                return typeMatch;
            });
            partBuilder.ExportMethods(m =>
            {
                var matchingMethod = matchingMethods.Any(mm => mm.Member == m);
                return matchingMethod;
            }, (m, exportBuilder) =>
            {
                var matchingMethod = matchingMethods.First(mm => mm.Member == m);
                exportBuilder.AsContractName(matchingMethod.ContractName);
            });
            partBuilder.ExportProperties(p =>
            {
                var matchingProperty = matchingProperties.Any(mm => mm.Member == p);
                return matchingProperty;

            }, (p, exportBuilder) =>
            {
                var matchingProperty = matchingProperties.First(mm => mm.Member == p);
                exportBuilder.AsContractName(matchingProperty.ContractName);
                exportBuilder.AsContractType(matchingProperty.DelegateType);
            });
                
            return reflectionContext;
            
        } 
        
        
        public static List<MatchingMember> ReplaceStaticFields(this CompositionContainer compositionContainer)
        {
            compositionContainer.GetExportedValueOrDefault<Dummy>();

            foreach (var matchingMember in allMatchingMembers)
            {
                var export = compositionContainer.GetExports(matchingMember.DelegateType, null, matchingMember.ContractName).FirstOrDefault().Value;
                matchingMember.Field.SetValue(null, export);
            }
            return allMatchingMembers;
        }

        private static bool TypeMatch(
            Type t,
            IEnumerable<ReplacementType> replacementTypes,
            Func<Type, string, bool> replacementTypePredicate,
            Func<MethodInfo, FieldInfo, bool> methodPredicate,
            Func<PropertyInfo, FieldInfo, bool> propertyPredicate)
        {
            var typeMatch = false;
            var matchingReplacementTypes = replacementTypes.Where(replacementType => replacementTypePredicate(t, replacementType.StaticTypeName)).ToList();
            if (matchingReplacementTypes.Count > 0)
            {
                foreach (var replacementType in matchingReplacementTypes)
                {
                    var typeMatchMethods = TypeMatchMembers(t.GetMethods, (m, rf) => methodPredicate(m, rf) && IsMethodCompatibleWithDelegate(m, rf.FieldType), replacementType, matchingMethods);
                    var typeMatchProperties = TypeMatchMembers(t.GetProperties, (p, rf) => propertyPredicate(p, rf) && p.PropertyType == rf.FieldType, replacementType, matchingProperties);
                    var replacementTypeMatches = typeMatchMethods | typeMatchProperties;
                    typeMatch = typeMatch || replacementTypeMatches;
                }

            }
            return typeMatch;
        }

        private static bool TypeMatchMembers<T>(Func<BindingFlags, T[]> memberProvider, Func<T, FieldInfo, bool> matchPredicate, ReplacementType replacementType, List<MatchingMember<T>> matchingMembers) where T : MemberInfo
        {
            var members = memberProvider(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            return members.Count(m => replacementType.Fields.Any(rf =>
            {
                var match = matchPredicate(m, rf);
                if (match)
                {
                    if(!matchingMembers.Any(mm => mm.Field == rf))
                    {
                        var matchingMember = new MatchingMember<T> { Member = m, StaticTypeName = replacementType.StaticTypeName, Field = rf, DelegateType = rf.FieldType };
                        matchingMembers.Add(matchingMember);
                        allMatchingMembers.Add(matchingMember);
                    }
                    
                }
                return match;
            })) > 0;
        }

        private static bool IsMethodCompatibleWithDelegate(MethodInfo method, Type delegateType)
        {
            MethodInfo delegateSignature = delegateType.GetMethod("Invoke");

            bool parametersEqual = delegateSignature
                .GetParameters()
                .Select(x => x.ParameterType)
                .SequenceEqual(method.GetParameters()
                    .Select(x => x.ParameterType));

            var returnTypeEqual = delegateSignature.ReturnType == method.ReturnType;

            return returnTypeEqual &&
                   parametersEqual;
        }

        private static string GetMemberName(MemberInfo member)
        {
            var memberName = member.Name;
            if (memberName.StartsWith("get_"))
            {
                memberName = memberName.Substring(4);
            }
            return memberName;
        }

    }

}
