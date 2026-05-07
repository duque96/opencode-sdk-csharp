using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;

return Cli.Run(args);

internal static class Cli
{
    public static int Run(string[] args)
    {
        if (args.Length == 3 && string.Equals(args[0], "public-api", StringComparison.Ordinal))
        {
            return WritePublicApi(args[1], args[2]);
        }

        Console.Error.WriteLine("Usage: dotnet run --project eng/Opencode.ReleaseVerifier -- public-api <assembly-path> <output-path>");
        return 1;
    }

    private static int WritePublicApi(string assemblyPath, string outputPath)
    {
        var fullAssemblyPath = Path.GetFullPath(assemblyPath);
        var fullOutputPath = Path.GetFullPath(outputPath);

        if (!File.Exists(fullAssemblyPath))
        {
            Console.Error.WriteLine($"Assembly not found: {fullAssemblyPath}");
            return 1;
        }

        var snapshot = PublicApiSnapshot.Create(fullAssemblyPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath)!);
        File.WriteAllLines(fullOutputPath, snapshot, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        Console.WriteLine($"Wrote public API snapshot to {fullOutputPath}");
        return 0;
    }
}

internal static class PublicApiSnapshot
{
    private static readonly Dictionary<Type, string> BuiltInTypeNames = new()
    {
        [typeof(bool)] = "bool",
        [typeof(byte)] = "byte",
        [typeof(char)] = "char",
        [typeof(decimal)] = "decimal",
        [typeof(double)] = "double",
        [typeof(float)] = "float",
        [typeof(int)] = "int",
        [typeof(long)] = "long",
        [typeof(object)] = "object",
        [typeof(sbyte)] = "sbyte",
        [typeof(short)] = "short",
        [typeof(string)] = "string",
        [typeof(uint)] = "uint",
        [typeof(ulong)] = "ulong",
        [typeof(ushort)] = "ushort",
        [typeof(void)] = "void",
    };

    private const BindingFlags DeclaredPublicMembers = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    public static IReadOnlyList<string> Create(string assemblyPath)
    {
        var loadContext = new AssemblyLoadContext("PublicApiSnapshot", isCollectible: true);

        try
        {
            loadContext.Resolving += (context, assemblyName) => ResolveAssembly(context, assemblyName, assemblyPath);
            var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
            var lines = new List<string>();

            foreach (var type in assembly.GetExportedTypes().OrderBy(static item => item.FullName, StringComparer.Ordinal))
            {
                lines.Add($"type {FormatTypeDeclaration(type)}");

                foreach (var member in GetMemberLines(type))
                {
                    lines.Add($"  {member}");
                }
            }

            return lines;
        }
        finally
        {
            loadContext.Unload();
        }
    }

    private static Assembly? ResolveAssembly(AssemblyLoadContext context, AssemblyName assemblyName, string assemblyPath)
    {
        var candidatePath = Path.Combine(Path.GetDirectoryName(assemblyPath)!, $"{assemblyName.Name}.dll");
        return File.Exists(candidatePath) ? context.LoadFromAssemblyPath(candidatePath) : null;
    }

    private static IEnumerable<string> GetMemberLines(Type type)
    {
        if (type.IsEnum)
        {
            foreach (var field in type.GetFields(DeclaredPublicMembers).Where(static item => item.IsLiteral).OrderBy(static item => item.Name, StringComparer.Ordinal))
            {
                yield return $"field {field.Name} = {Convert.ToInt64(field.GetRawConstantValue(), System.Globalization.CultureInfo.InvariantCulture)}";
            }

            yield break;
        }

        foreach (var constructor in type.GetConstructors(DeclaredPublicMembers).OrderBy(FormatConstructorSignature, StringComparer.Ordinal))
        {
            yield return FormatConstructorSignature(constructor);
        }

        foreach (var property in type.GetProperties(DeclaredPublicMembers).OrderBy(static item => item.Name, StringComparer.Ordinal))
        {
            yield return FormatPropertySignature(property);
        }

        foreach (var method in type.GetMethods(DeclaredPublicMembers).Where(static item => !item.IsSpecialName).OrderBy(FormatMethodSignature, StringComparer.Ordinal))
        {
            yield return FormatMethodSignature(method);
        }

        foreach (var field in type.GetFields(DeclaredPublicMembers).Where(static item => !item.IsSpecialName && !item.IsLiteral).OrderBy(static item => item.Name, StringComparer.Ordinal))
        {
            yield return $"field {FormatTypeName(field.FieldType)} {field.Name}";
        }
    }

    private static string FormatTypeDeclaration(Type type)
    {
        var modifiers = new List<string>();

        if (type.IsAbstract && type.IsSealed)
        {
            modifiers.Add("static");
        }
        else
        {
            if (type.IsAbstract && !type.IsInterface)
            {
                modifiers.Add("abstract");
            }

            if (type.IsSealed && !type.IsValueType && !IsDelegate(type))
            {
                modifiers.Add("sealed");
            }
        }

        modifiers.Add(GetTypeKind(type));
        modifiers.Add(FormatTypeName(type));
        return string.Join(' ', modifiers);
    }

    private static string GetTypeKind(Type type)
    {
        if (type.IsInterface)
        {
            return "interface";
        }

        if (type.IsEnum)
        {
            return "enum";
        }

        if (IsDelegate(type))
        {
            return "delegate";
        }

        if (type.IsValueType)
        {
            return "struct";
        }

        return "class";
    }

    private static bool IsDelegate(Type type)
    {
        return typeof(MulticastDelegate).IsAssignableFrom(type.BaseType);
    }

    private static string FormatConstructorSignature(ConstructorInfo constructor)
    {
        var typeName = constructor.DeclaringType?.Name.Split('`')[0] ?? ".ctor";
        return $"ctor {typeName}({FormatParameters(constructor.GetParameters())})";
    }

    private static string FormatPropertySignature(PropertyInfo property)
    {
        var accessors = new List<string>();

        if (property.GetMethod is not null && property.GetMethod.IsPublic)
        {
            accessors.Add("get;");
        }

        if (property.SetMethod is not null && property.SetMethod.IsPublic)
        {
            accessors.Add(IsInitOnly(property.SetMethod) ? "init;" : "set;");
        }

        return $"property {FormatTypeName(property.PropertyType)} {property.Name} {{ {string.Join(' ', accessors)} }}";
    }

    private static bool IsInitOnly(MethodInfo setMethod)
    {
        return setMethod.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit));
    }

    private static string FormatMethodSignature(MethodInfo method)
    {
        var genericSuffix = method.IsGenericMethodDefinition
            ? $"<{string.Join(", ", method.GetGenericArguments().Select(static item => item.Name))}>"
            : string.Empty;

        return $"method {FormatTypeName(method.ReturnType)} {method.Name}{genericSuffix}({FormatParameters(method.GetParameters())})";
    }

    private static string FormatParameters(IReadOnlyList<ParameterInfo> parameters)
    {
        return string.Join(", ", parameters.Select(FormatParameter));
    }

    private static string FormatParameter(ParameterInfo parameter)
    {
        var modifier = parameter.IsOut
            ? "out "
            : parameter.ParameterType.IsByRef
                ? parameter.IsIn ? "in " : "ref "
                : string.Empty;

        var parameterType = parameter.ParameterType.IsByRef
            ? parameter.ParameterType.GetElementType()!
            : parameter.ParameterType;

        return $"{modifier}{FormatTypeName(parameterType)} {parameter.Name}";
    }

    private static string FormatTypeName(Type type)
    {
        if (BuiltInTypeNames.TryGetValue(type, out var builtInTypeName))
        {
            return builtInTypeName;
        }

        if (type.IsGenericParameter)
        {
            return type.Name;
        }

        if (type.IsArray)
        {
            return $"{FormatTypeName(type.GetElementType()!)}[]";
        }

        var nullableUnderlyingType = Nullable.GetUnderlyingType(type);
        if (nullableUnderlyingType is not null)
        {
            return $"{FormatTypeName(nullableUnderlyingType)}?";
        }

        if (type.IsGenericType)
        {
            var genericTypeDefinition = type.IsGenericTypeDefinition ? type : type.GetGenericTypeDefinition();
            var genericTypeName = (genericTypeDefinition.FullName ?? genericTypeDefinition.Name).Split('`')[0].Replace('+', '.');
            var arguments = type.GetGenericArguments().Select(FormatTypeName);
            return $"{genericTypeName}<{string.Join(", ", arguments)}>";
        }

        return (type.FullName ?? type.Name).Replace('+', '.');
    }
}