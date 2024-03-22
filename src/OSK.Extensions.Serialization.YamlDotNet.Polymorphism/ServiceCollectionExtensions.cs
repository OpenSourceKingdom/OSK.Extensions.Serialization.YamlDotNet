using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OSK.Serialization.Polymorphism;
using OSK.Serialization.Polymorphism.Ports;
using OSK.Serialization.Yaml.YamlDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization.BufferedDeserialization.TypeDiscriminators;

namespace OSK.Extensions.Serialization.YamlDotNet.Polymorphism
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddYamlDotNetPolymorphism(this IServiceCollection services,
            Type assemblyMarkerType)
        {
            var polymorphismAttributeType = typeof(PolymorphismAttribute);
            AddPolymorphismTypes(services,
                assemblyMarkerType.Assembly
                        .GetTypes()
                        .Where(type => PolymorphismAttribute.GetPolymorphismAttribute(type) != null));

            return services;
        }

        #region Helpers

        private static void AddPolymorphismTypes(IServiceCollection services, IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                services.AddTransient<ITypeDiscriminator>(provider =>
                {
                    var serializationOptions = provider.GetRequiredService<IOptions<YamlDotNetSerializationOptions>>();
                    var contextProvider = provider.GetRequiredService<IPolymorphismContextProvider>();
                    return new PolymorphismYamlTypeDiscriminator(type, contextProvider, serializationOptions);
                });
            }
        }

        #endregion
    }
}
