using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FTFoundation.Core.Validation
{
    internal static class ServiceStackValidator
    {
        public static void Validate(
            IReadOnlyDictionary<Type, Type> serviceCache,
            IReadOnlyDictionary<Type, List<Type>> multiServiceCache,
            BuildTargetProfile currentProfile,
            List<ProblemDetail> problems)
        {
            if (serviceCache.Count == 0)
            {
                problems.Add(new ProblemDetail(ProblemDetailType.WARNING, $"No services registered for profile '{currentProfile}'. Ensure at least one [ServiceAssembly] assembly contains [Service] decorated types."));
                return;
            }

            ValidateCircularDependencies(serviceCache, multiServiceCache, problems);

            var lines = serviceCache
                .OrderBy(kvp => kvp.Key.Name)
                .Select(kvp => $"  {kvp.Key.Name} → {kvp.Value.Name}");
            problems.Add(new ProblemDetail(ProblemDetailType.INFORMATION, $"{serviceCache.Count} service(s) active in profile '{currentProfile}':\n{string.Join("\n", lines)}"));
        }

        private static void ValidateCircularDependencies(
            IReadOnlyDictionary<Type, Type> serviceCache,
            IReadOnlyDictionary<Type, List<Type>> multiServiceCache,
            List<ProblemDetail> problems)
        {
            // Build graph: concrete type → list of concrete types it depends on
            var graph = new Dictionary<Type, List<Type>>();

            foreach (var concreteType in serviceCache.Values)
                if (!graph.ContainsKey(concreteType))
                    graph[concreteType] = GetConcreteDependencies(concreteType, serviceCache, multiServiceCache);

            // Include multi-service types that aren't single-winner entries
            foreach (var types in multiServiceCache.Values)
                foreach (var t in types)
                    if (!graph.ContainsKey(t))
                        graph[t] = GetConcreteDependencies(t, serviceCache, multiServiceCache);

            var visited = new HashSet<Type>();
            var inStack = new HashSet<Type>();
            var stack = new List<Type>();

            foreach (var type in graph.Keys)
                if (!visited.Contains(type))
                    DetectCycle(type, graph, visited, inStack, stack, problems);
        }

        private static List<Type> GetConcreteDependencies(
            Type concreteType,
            IReadOnlyDictionary<Type, Type> serviceCache,
            IReadOnlyDictionary<Type, List<Type>> multiServiceCache)
        {
            var deps = new List<Type>();

            var properties = concreteType
                .GetProperties(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(p => Attribute.IsDefined(p, typeof(InjectAttribute)));

            foreach (var prop in properties)
                ResolveInterfaceToConcrete(prop.PropertyType, serviceCache, multiServiceCache, deps);

            var injectMethod = concreteType.GetMethod("Inject", BindingFlags.Instance | BindingFlags.NonPublic);
            if (injectMethod != null)
                foreach (var param in injectMethod.GetParameters())
                    ResolveInterfaceToConcrete(param.ParameterType, serviceCache, multiServiceCache, deps);

            return deps;
        }

        private static void ResolveInterfaceToConcrete(
            Type interfaceType,
            IReadOnlyDictionary<Type, Type> serviceCache,
            IReadOnlyDictionary<Type, List<Type>> multiServiceCache,
            List<Type> deps)
        {
            if (interfaceType == typeof(IServiceTargetData)) return;

            if (interfaceType.IsGenericType)
            {
                var def = interfaceType.GetGenericTypeDefinition();
                if (def == typeof(IReadOnlyList<>) || def == typeof(IEnumerable<>) || def == typeof(List<>))
                {
                    var elementType = interfaceType.GetGenericArguments()[0];
                    if (multiServiceCache.TryGetValue(elementType, out var types))
                        deps.AddRange(types);
                    return;
                }
            }

            if (serviceCache.TryGetValue(interfaceType, out var concrete))
                deps.Add(concrete);
        }

        private static void DetectCycle(
            Type current,
            Dictionary<Type, List<Type>> graph,
            HashSet<Type> visited,
            HashSet<Type> inStack,
            List<Type> stack,
            List<ProblemDetail> problems)
        {
            visited.Add(current);
            inStack.Add(current);
            stack.Add(current);

            if (graph.TryGetValue(current, out var deps))
            {
                foreach (var dep in deps)
                {
                    if (!visited.Contains(dep))
                    {
                        DetectCycle(dep, graph, visited, inStack, stack, problems);
                    }
                    else if (inStack.Contains(dep))
                    {
                        int startIdx = stack.IndexOf(dep);
                        var chain = stack.Skip(startIdx).Select(t => t.Name).Concat(new[] { dep.Name });
                        problems.Add(new ProblemDetail(ProblemDetailType.ERROR, $"Circular dependency detected: {string.Join(" → ", chain)}"));
                    }
                }
            }

            stack.RemoveAt(stack.Count - 1);
            inStack.Remove(current);
        }
    }
}
