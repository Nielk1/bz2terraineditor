using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BZTerrainEditor.ViewModels;

[AttributeUsage(AttributeTargets.Method)]
public class NodeRegistrationAttribute : Attribute
{
    public bool IsTypeBased { get; set; } = false;
}

public static class NodeRegistrationSystem
{
    public static void RegisterAll(GlobalNodeManager manager)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var methods = assembly.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(m => m.GetCustomAttribute<NodeRegistrationAttribute>() != null)
            .ToList();

        // First, register non-type-based nodes
        foreach (var method in methods.Where(m => !m.GetCustomAttribute<NodeRegistrationAttribute>().IsTypeBased))
        {
            method.Invoke(null, new object[] { manager });
        }

        // Collect IComparable types from inputs and outputs of registered nodes, this is not fully reliable for highly dynamic nodes but they probably got their types from others anyway
        var comparableTypes = new HashSet<Type>();
        foreach (var kvp in manager.GlobalNodeTypes)
        {
            var nodeType = kvp.Key;
            var outputTypes = nodeType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType.IsGenericType && (p.PropertyType.GetGenericTypeDefinition() == typeof(ValueNodeOutputViewModel<>) || p.PropertyType.GetGenericTypeDefinition() == typeof(ValueNodeInputViewModel<>)))
                .Select(p => p.PropertyType.GetGenericArguments()[0])
                //.Where(t => typeof(IComparable).IsAssignableFrom(t))
                ;
            foreach (var t in outputTypes)
            {
                comparableTypes.Add(t);
            }
        }

        // Then, register type-based nodes for each collected IComparable type
        foreach (var type in comparableTypes)
        {
            foreach (var method in methods.Where(m => m.GetCustomAttribute<NodeRegistrationAttribute>().IsTypeBased))
            {
                method.Invoke(null, new object[] { manager, type });
            }
        }
    }
}