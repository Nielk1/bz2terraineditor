using System;
using System.Collections.Generic;
using System.Text;

namespace BZTerrainEditor.Types;

public static class TypeNameHelper
{
    private static readonly Dictionary<string, string> TypeNameOverrides = new()
    {
        { "SByte", "Int8" },
        { "Byte", "UInt8" }
        // Add more overrides here as needed
    };

    public static string GetNiceTypeName(this Type type)
    {
        // Handle arrays
        if (type.IsArray)
        {
            var rank = type.GetArrayRank();
            var baseType = type.GetElementType();
            var baseName = GetNiceTypeName(baseType!);
            var ranks = rank == 1 ? "[]" : "[" + new string(',', rank - 1) + "]";
            return baseName + ranks;
        }

        // Handle generics
        if (type.IsGenericType)
        {
            var name = type.Name;
            var tickIndex = name.IndexOf('`');
            if (tickIndex > 0)
                name = name.Substring(0, tickIndex);

            if (TypeNameOverrides.TryGetValue(name, out var overrideName))
                name = overrideName;

            var args = type.GetGenericArguments();
            var argNames = string.Join(", ", Array.ConvertAll(args, GetNiceTypeName));
            return $"{name}<{argNames}>";
        }

        // Handle overrides
        if (TypeNameOverrides.TryGetValue(type.Name, out var simpleOverride))
            return simpleOverride;

        // Default
        return type.Name;
    }
}