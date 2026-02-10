using DynamicData;
using NodeNetwork;
using NodeNetwork.Toolkit.ValueNode;
using NodeNetwork.ViewModels;
using NodeNetwork.Views;
using ReactiveUI;
using ReactiveUI.Wpf;
using Splat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Security.Cryptography.Xml;

namespace BZTerrainEditor.ViewModels.Nodes;

public static class ExtendsValueNodeResistration
{
    [NodeRegistration(IsTypeBased = true)]
    public static void RegisterNode(GlobalNodeManager manager, Type t)
    {
        // check if T is an enumerable of comparables, or an enumerable of an enumerable of comparables, etc.
        Type elementType = t;
        bool inArray = false;
        while (elementType.IsArray)
        {
            elementType = elementType.GetElementType()!;
            inArray = true;
        }
        if (inArray && typeof(IComparable).IsAssignableFrom(elementType))
        {
            Type nodeType = typeof(ExtentsValueNode<,>).MakeGenericType(t, elementType);
            manager.Register(nodeType, $"Extents {t.Name}", $"Get Min and Max from {t.Name}.", () => (NodeViewModel)Activator.CreateInstance(nodeType));
        }
    }
}

public class ExtentsValueNode<I,O> : NodeViewModel where O : IComparable
{
    static ExtentsValueNode()
    {
        Locator.CurrentMutable.Register(() => new NodeView(), typeof(IViewFor<ExtentsValueNode<I,O>>));
    }

    private ValueNodeInputViewModel<I> _input;
    private ValueNodeOutputViewModel<O> _minOutput;
    private ValueNodeOutputViewModel<O> _maxOutput;

    public ExtentsValueNode()
    {
        Name = "Extents";

        _input = new ValueNodeInputViewModel<I>();
        _input.Name = "Collection";
        Inputs.Add(_input);

        _minOutput = new ValueNodeOutputViewModel<O>();
        _minOutput.Name = "Min";
        Outputs.Add(_minOutput);

        _maxOutput = new ValueNodeOutputViewModel<O>();
        _maxOutput.Name = "Max";
        Outputs.Add(_maxOutput);

        var minMaxObservable = _input
            .WhenAnyValue(vm => vm.Value)
            .Where(value => value != null)
            .Select(value => FindMinMax(value));

        _minOutput.Value = minMaxObservable.Select(tuple => tuple.min);
        _maxOutput.Value = minMaxObservable.Select(tuple => tuple.max);
    }

    private static (O min, O max) FindMinMax(I collection)
    {
        if (collection is not Array array || array.Length == 0)
        {
            return (default(O), default(O));
        }

        var items = Flatten(array).ToList();
        if (!items.Any())
        {
            return (default(O), default(O));
        }

        O min = items[0];
        O max = items[0];
        foreach (var item in items.Skip(1))
        {
            if (item.CompareTo(min) < 0)
                min = item;
            if (item.CompareTo(max) > 0)
                max = item;
        }
        return (min, max);
    }

    private static IEnumerable<O> Flatten(Array array)
    {
        if (array == null) yield break;
        var enumerator = array.GetEnumerator();
        while (enumerator.MoveNext())
        {
            yield return (O)enumerator.Current;
        }
    }
}