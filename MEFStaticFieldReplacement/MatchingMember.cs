using System;
using System.Reflection;

namespace MEFStaticFieldReplacement
{
    public abstract class MatchingMember
    {
        public string StaticTypeName { get; internal set; }
        public FieldInfo Field { get; internal set; }

        public Type DelegateType { get; internal set; }
        public abstract string ContractName { get;  }
        public abstract MemberInfo GetMember();
    }

    internal class MatchingMember<T> : MatchingMember where T: MemberInfo
    {
        public T Member { get; internal set; }
        public override string ContractName => $"{StaticTypeName}.{Field.Name}";

        public override MemberInfo GetMember()
        {
            return Member;
        }
    }
}
