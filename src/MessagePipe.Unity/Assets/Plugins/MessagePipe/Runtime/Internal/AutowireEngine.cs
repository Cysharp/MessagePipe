#if !UNITY_2018_3_OR_NEWER

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MessagePipe.Internal
{
    internal static class AutowireEngine
    {
        public static IEnumerable<Type> CollectFromCurrentDomain()
        {
            var wellKnownIgnoreAssemblies = new[]
            {
                "netstandard",
                "System.*",
                "Microsoft.Win32.*",
                "Microsoft.Extensions.*",
                "Microsoft.AspNetCore",
                "Microsoft.AspNetCore.*",
                "Grpc.*",
                "MessagePack",
                "MessagePack.*",
                "MagicOnion.Server",
                "MagicOnion.Server.*",
                "MagicOnion.Client",
                "MagicOnion.Client.*",
                "MagicOnion.Abstractions",
                "MagicOnion.Shared",
            };

            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(x => x.GetName().Name != "MessagePipe" && x.GetName().Name != "MessagePipe.Redis")
                .Where(x =>
                {
                    return !wellKnownIgnoreAssemblies.Any(y =>
                    {
                        if (y.EndsWith("*"))
                        {
                            return x.GetName().Name!.StartsWith(y.Substring(0, y.Length - 1));
                        }
                        else
                        {
                            return x.GetName().Name == y;
                        }
                    });
                });

            return CollectFromAssemblies(assemblies);
        }

        public static IEnumerable<Type> CollectFromAssemblies(IEnumerable<Assembly> searchAssemblies)
        {
            var types = searchAssemblies
                .Where(x => x.GetName().Name != "MessagePipe" && x.GetName().Name != "MessagePipe.Redis")
                .SelectMany(x =>
                {
                    try
                    {
                        return x.GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        return ex.Types.Where(t => t != null);
                    }
                })
                .Where(x => x != null);

            return types!;
        }

        public static void RegisterFromTypes(IServiceCollection services, MessagePipeOptions options, IEnumerable<Type> targetTypes)
        {
            foreach (var objectType in targetTypes)
            {
                if (objectType.IsInterface || objectType.IsAbstract) continue;

                foreach (var interfaceType in objectType.GetInterfaces())
                {
                    if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IRequestHandlerCore<,>))
                    {
                        services.Add(interfaceType, objectType, options.InstanceLifetime);
                        goto NEXT_TYPE;
                    }

                    if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IAsyncRequestHandlerCore<,>))
                    {
                        services.Add(interfaceType, objectType, options.InstanceLifetime);
                        goto NEXT_TYPE;
                    }
                }

                foreach (var baseType in objectType.GetBaseTypes())
                {
                    if (baseType == typeof(MessageHandlerFilter))
                    {
                        services.AddTransient(objectType);
                        goto NEXT_TYPE;
                    }

                    if (baseType == typeof(AsyncMessageHandlerFilter))
                    {
                        services.AddTransient(objectType);
                        goto NEXT_TYPE;
                    }

                    if (baseType == typeof(RequestHandlerFilter))
                    {
                        services.AddTransient(objectType);
                        goto NEXT_TYPE;
                    }

                    if (baseType == typeof(AsyncRequestHandlerFilter))
                    {
                        services.AddTransient(objectType);
                        goto NEXT_TYPE;
                    }
                }

            NEXT_TYPE:
                continue;
            }
        }

        internal static IEnumerable<Type> GetBaseTypes(this Type t)
        {
            if (t == null) yield break;
            t = t.BaseType;
            while (t != null)
            {
                yield return t;
                t = t.BaseType;
            }
        }
    }
}

#endif