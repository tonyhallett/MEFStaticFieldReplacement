A registration builder ReflectionContext to be used by MEF to automatically set static fields of delegate types.

A) Create a ReflectionContext and use in an applicable ComposablePartCatalog that is part of a CompositionContainer.

```c#
public ReplacementType(string staticTypeName,List<FieldInfo> fields){}

public static RegistrationBuilderWithMethods ReflectionContext(
    IEnumerable<ReplacementType> replacementTypes,
    Func<Type,string,bool> replacementTypePredicate = null,
    Func<MethodInfo,FieldInfo,bool> methodPredicate = null,
    Func<PropertyInfo, FieldInfo, bool> propertyPredicate = null,
    string singleReplacementTypeName = null
) {

IEnumerable<ReplacementType> replacementTypes = GetFromSomewhere();
var compositionContainer = new CompositionContainer(
    new AssemblyCatalog(
        Assembly.GetEntryAssembly(),
        StaticFieldReplacement.ReflectionContext(replacementTypes)
    )
)
```

A ReplacementType represents a static type withe static fields that will be replaced by properties or methods from the catalog that are deemed a match.
There are two helpers for creating ReplacementType objects that will have find public fields of delegate type.

```c#
public static class ReplacementTypes
{
     // Exported types
     public static IEnumerable<ReplacementType> FromAssemblies(params Assembly[] assemblies) { //... }

     public static IEnumerable<ReplacementType> FromTypes(params Type[] types) { //... }
}
```

B) Ensure the names of both the types in the catalog that provide replacement methods and properties, and their members, are named accordingly.

A Type in the catalog is a match if the replacementTypePredicate returns true.

The default behaviour is to match type name ignoring Replacement or to match against a single class with the name from the singleReplacementTypeName parameter.
This defaults to 
```c#
public static string DefaultSingleReplacementTypeName = "Replacements";
```

```c#
((t, staticTypeName) =>
{
    return staticTypeName == t.Name.Replace("Replacement", "") || t.Name == singleReplacementTypeName;
});
```

If a type is a match then methods that match the delegate type or properties of the delegate type will be considered a match if the corresponding predicate returns true.

The default predicate for methods and properties is the same.

```c#
bool DefaultPredicate(MemberInfo m, FieldInfo field)
{
    if (m.Name == field.Name)
    {
        return true;
    }

    var isSingleReplacementType = m.DeclaringType.Name == singleReplacementTypeName;
    return isSingleReplacementType && GetMemberName(m) == $"{field.DeclaringType.Name}{field.Name}";
}
```


C) Use the extension method on the CompositionContainer ( you will probably not need the return value)

```c#
public static List<MatchingMember> ReplaceStaticFields(this CompositionContainer compositionContainer)
```

An example

Given below passed as a ReplacementType to StaticFieldReplacement.ReflectionContext().
```C#
public static class ReplaceMyFields{
    public static Func<bool> ReplaceMe = () => false;
}

```

Any of the below will automatically replace the ReplaceMe field

```C#
public class Replacements{
    public bool ReplaceMyFieldsReplaceMe(){ return true;}
}

public static class ReplaceMyFieldsReplacement{
    public Func<bool> ReplaceMe {get;} = () => true;
}


```


