using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace OsuLazer.Interop;

/// <summary>
/// 把 Realm 对象转成可直接序列化的 Dictionary 列表。
/// </summary>
internal static class LazerRealmConverter
{
    /// <summary>
    /// 将 Realm 对象列表转为可序列化的 Dictionary 列表。
    /// 每个顶层对象使用独立的循环引用跟踪，避免同一条对象链内重复展开。
    /// </summary>
    public static IReadOnlyList<object> ToList(IEnumerable items, int depth, IReadOnlySet<string>? noExpandFields = null)
    {
        var result = new List<object>();
        foreach (var item in items)
        {
            if (item is null)
                continue;

            var visited = new ConditionalWeakTable<object, object>();
            result.Add(ConvertComplexValue(item.GetType(), item, depth, noExpandFields, visited));
        }

        return result;
    }

    public static Dictionary<string, object?> ToDict(object obj, int depth, IReadOnlySet<string>? noExpandFields = null,
        ConditionalWeakTable<object, object>? visited = null)
    {
        visited ??= new ConditionalWeakTable<object, object>();

        var dict = new Dictionary<string, object?>();
        foreach (var prop in GetMappedProperties(obj.GetType()))
        {
            var value = prop.GetValue(obj);
            if (value is null)
                continue;

            dict[prop.Name] = ConvertComplexValue(prop.PropertyType, value, depth, noExpandFields, visited, prop.Name);
        }

        return dict;
    }

    /// <summary>只取类型自身声明的属性：Realm 代理类型的基类成员是框架内部状态。</summary>
    private static PropertyInfo[] GetMappedProperties(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.DeclaringType == type)
            .ToArray();
    }

    private static object ConvertComplexValue(Type propType, object value, int depth,
        IReadOnlySet<string>? noExpandFields, ConditionalWeakTable<object, object> visited,
        string? propertyName = null)
    {
        if (propertyName is not null && noExpandFields?.Contains(propertyName) == true)
            depth = -1;

        var underlyingType = Nullable.GetUnderlyingType(propType) ?? propType;

        if (underlyingType.IsValueType || propType == typeof(string))
            return value;

        // 循环引用检测：同一个对象实例第二次遇到就不再展开
        if (!string.IsNullOrEmpty(propertyName))
        {
            if (visited.TryGetValue(value, out _))
                return $"[{value.GetType().Name} (already expanded)]";

            visited.Add(value, value);
        }

        if (propType.GetInterface(nameof(IDictionary)) is not null)
            return depth >= 0
                ? ConvertDict((IDictionary)value, depth - 1, noExpandFields, visited)
                : "[Dictionary]";

        if (propType.GetInterface(nameof(IEnumerable)) is not null)
            return depth >= 0
                ? ToListInternal((IEnumerable)value, depth - 1, noExpandFields, visited)
                : "[List]";

        return depth >= 0
            ? ToDict(value, depth - 1, noExpandFields, visited)
            : value.ToString() ?? "[Null]";
    }

    private static Dictionary<object, object?> ConvertDict(IDictionary dict, int depth,
        IReadOnlySet<string>? noExpandFields, ConditionalWeakTable<object, object> visited)
    {
        var result = new Dictionary<object, object?>(dict.Count);
        foreach (var key in dict.Keys)
        {
            var value = dict[key];
            result[key] = value is not null
                ? ConvertComplexValue(value.GetType(), value, depth - 1, noExpandFields, visited)
                : null;
        }

        return result;
    }

    private static List<object> ToListInternal(IEnumerable items, int depth,
        IReadOnlySet<string>? noExpandFields, ConditionalWeakTable<object, object> visited)
    {
        var result = new List<object>();
        foreach (var item in items)
        {
            if (item is null)
                continue;

            result.Add(ConvertComplexValue(item.GetType(), item, depth, noExpandFields, visited));
        }

        return result;
    }
}
