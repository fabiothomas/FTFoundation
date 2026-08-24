#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace FTFoundation.Core
{
    // Responsible for pre-compiling Expression-tree factories and injection actions for all
    // registered service types and injection targets. Pays the reflection cost once at startup.
    internal static class ServiceCompiler
    {
        // Keyed by concrete implementation type (not interface)
        private static readonly Dictionary<Type, Func<object>> serviceFactories = new();
        private static readonly Dictionary<Type, Action<object, int, ServiceTargetData>> injectionActions = new();

        // LambdaExpression.Compile(preferInterpretation: false) JIT-compiles a delegate via
        // System.Reflection.Emit, which is unavailable under IL2CPP AOT. On an IL2CPP build we must
        // fall back to the interpreted (slower, but AOT-safe) execution path instead.
#if ENABLE_IL2CPP
        private const bool PreferInterpretation = true;
#else
        private const bool PreferInterpretation = false;
#endif

        internal static void Clear()
        {
            serviceFactories.Clear();
            injectionActions.Clear();
        }

        internal static bool TryGetFactory(Type implementationType, out Func<object> factory)
        {
            return serviceFactories.TryGetValue(implementationType, out factory);
        }

        internal static bool TryGetInjectionAction(Type type, out Action<object, int, ServiceTargetData> action)
        {
            return injectionActions.TryGetValue(type, out action);
        }

        // Factory is keyed by concrete implementation type so multiple implementations of the same
        // interface can each have their own factory.
        internal static void PrecompileFactory(Type implementationType)
        {
            if (serviceFactories.ContainsKey(implementationType)) return;
            var newExpression = Expression.New(implementationType);
            var lambda = Expression.Lambda<Func<object>>(newExpression);
            serviceFactories[implementationType] = lambda.Compile(PreferInterpretation);
        }

        // An injection action scans for injectable properties and method parameters and performs
        // the injection. Compiled once here; invoked on every Inject() call.
        internal static void PrecompileInjectionAction(Type injectionObjectType)
        {
            if (injectionActions.ContainsKey(injectionObjectType)) return;

            var configProperties = injectionObjectType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic)
              .Where(p => Attribute.IsDefined(p, typeof(ConfigAttribute)))
              .ToList();
            var injectableProperties = injectionObjectType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic)
              .Where(p => Attribute.IsDefined(p, typeof(InjectAttribute)))
              .ToList();
            var injectMethod = injectionObjectType.GetMethod("Inject", BindingFlags.Instance | BindingFlags.NonPublic);

            if (configProperties.Count != 0 || injectableProperties.Count != 0 || injectMethod != null)
            {
                injectionActions[injectionObjectType] = CreateInjectionAction(injectionObjectType, configProperties, injectableProperties, injectMethod);
            }
        }

        private static Action<object, int, ServiceTargetData> CreateInjectionAction(Type injectionObjectType, List<PropertyInfo> configProperties, List<PropertyInfo> injectableProperties, MethodInfo? injectMethod)
        {
            var objParameter = Expression.Parameter(typeof(object), "obj");
            var sceneHandleParameter = Expression.Parameter(typeof(int), "sceneHandle");
            var serviceTargetDataParameter = Expression.Parameter(typeof(ServiceTargetData), "target");

            var typedObj = Expression.Convert(objParameter, injectionObjectType);

            var expressions = new List<Expression>();

            // Config properties are applied first so values are available inside void Inject(...)
            foreach (var property in configProperties)
            {
                bool isRequired = property.GetCustomAttribute<ConfigAttribute>()!.Required;
                var call = Expression.Call(
                    typeof(ConfigLoader),
                    nameof(ConfigLoader.ApplyConfigValue),
                    null,
                    new Expression[]
                    {
                        objParameter,
                        Expression.Constant(property, typeof(System.Reflection.PropertyInfo)),
                        Expression.Constant(injectionObjectType, typeof(Type)),
                        Expression.Constant(isRequired)
                    });
                expressions.Add(call);
            }

            foreach (var property in injectableProperties)
            {
                bool isOptional = property.GetCustomAttribute<InjectAttribute>()?.Optional ?? false;

                // get the service creation expression — passes optional flag so missing services can inject null
                var serviceCall = Expression.Call(
                  typeof(ServiceResolver),
                  nameof(ServiceResolver.GetService),
                  null,
                  new Expression[]
                  {
            Expression.Constant(property.PropertyType),
            sceneHandleParameter,
            serviceTargetDataParameter,
            Expression.Constant(isOptional)
                  }
                );

                // cast to the property type
                var castedService = Expression.Convert(serviceCall, property.PropertyType);

                // create property assignment
                var propertyAccess = Expression.Property(typedObj, property);
                var assignment = Expression.Assign(propertyAccess, castedService);

                expressions.Add(assignment);
            }

            if (injectMethod != null)
            {
                var parameters = injectMethod.GetParameters();

                // create all injection parameters; method parameters are never optional
                var args = parameters.Select(p =>
                  Expression.Convert(
                    Expression.Call(
                      typeof(ServiceResolver),
                      nameof(ServiceResolver.GetService),
                      null,
                      new Expression[]
                      {
                Expression.Constant(p.ParameterType),
                sceneHandleParameter,
                serviceTargetDataParameter,
                Expression.Constant(false)
                      }
                    ),
                    p.ParameterType
                  )
                ).ToArray();

                // perform injection call
                var methodCall = Expression.Call(typedObj, injectMethod, args);
                expressions.Add(methodCall);
            }

            if (expressions.Count == 0)
            {
                // nothing to inject, return empty action
                return (_, _, _) => { };
            }

            var block = Expression.Block(expressions);
            var parameterExpressions = new ParameterExpression[]
            {
        objParameter,
        sceneHandleParameter,
        serviceTargetDataParameter
            };
            var lambda = Expression.Lambda<Action<object, int, ServiceTargetData>>(block, parameterExpressions);

            return lambda.Compile(PreferInterpretation);
        }
    }
}
