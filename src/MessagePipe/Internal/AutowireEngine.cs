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
                "MessagePipe",
                "MessagePipe.*",
            };

            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
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
                });

            return types;
        }

        public static void RegisterFromTypes(IServiceCollection services, MessagePipeOptions options, IEnumerable<Type> targetTypes)
        {
            foreach (var objectType in targetTypes)
            {
                var interfaces = objectType.GetInterfaces();

                foreach (var interfaceType in interfaces)
                {
                    if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IRequestHandlerCore<,>))
                    {
                        services.Add(interfaceType, objectType, options.InstanceScope);
                        goto NEXT_TYPE;
                    }

                    if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IAsyncRequestHandlerCore<,>))
                    {
                        services.Add(interfaceType, objectType, options.InstanceScope);
                        goto NEXT_TYPE;
                    }

                    if (interfaceType == typeof(MessageHandlerFilter))
                    {
                        services.TryAddSingleton(objectType);
                        goto NEXT_TYPE;
                    }

                    if (interfaceType == typeof(AsyncMessageHandlerFilter))
                    {
                        services.TryAddSingleton(objectType);
                        goto NEXT_TYPE;
                    }

                    if (interfaceType == typeof(RequestHandlerFilter))
                    {
                        services.TryAddSingleton(objectType);
                        goto NEXT_TYPE;
                    }

                    if (interfaceType == typeof(AsyncRequestHandlerFilter))
                    {
                        services.TryAddSingleton(objectType);
                        goto NEXT_TYPE;
                    }
                }

            NEXT_TYPE:
                continue;
            }
        }
    }
}
