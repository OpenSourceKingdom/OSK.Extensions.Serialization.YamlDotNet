using Microsoft.Extensions.Options;
using OSK.Serialization.Polymorphism.Discriminators;
using OSK.Serialization.Polymorphism.Models;
using OSK.Serialization.Polymorphism.Ports;
using OSK.Serialization.Yaml.YamlDotNet;
using System;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization.BufferedDeserialization.TypeDiscriminators;

namespace OSK.Extensions.Serialization.YamlDotNet.Polymorphism
{
    public class PolymorphismYamlTypeDiscriminator : ITypeDiscriminator
    {
        #region Variables

        private readonly Type _typeToConvert;
        private readonly IPolymorphismContextProvider _contextProvider;
        private readonly IOptions<YamlDotNetSerializationOptions> _options;

        #endregion

        #region Constructors

        public PolymorphismYamlTypeDiscriminator(
            Type polymorphismType,
            IPolymorphismContextProvider polymorphismContextProvider,
            IOptions<YamlDotNetSerializationOptions> serializationOptions)
        {
            _typeToConvert = polymorphismType ?? throw new ArgumentNullException(nameof(polymorphismType));
            _contextProvider = polymorphismContextProvider ?? throw new ArgumentNullException(nameof(polymorphismContextProvider));
            _options = serializationOptions ?? throw new ArgumentNullException(nameof(serializationOptions));
        }

        #endregion

        #region ITypeDiscriminator

        public Type BaseType => _typeToConvert;

        public bool TryDiscriminate(IParser buffer, out Type suggestedType)
        {
            var polymorphismContext = _contextProvider.GetPolymorphismContext(_typeToConvert);

            if (!buffer.TryFindMappingEntry(
                scalar =>
                    string.Equals(_options.Value.DeserializationNamingConvention.Apply(scalar.Value),
                        polymorphismContext.PolymorphismPropertyName,
                        StringComparison.InvariantCultureIgnoreCase),
                out var key, out var parsingEvent))
            {
                throw new InvalidOperationException($"Object of type {_typeToConvert.FullName} was expected to have a discriminator {polymorphismContext.PolymorphismPropertyName} but it wasn't found.");
            }

            var polymorphismPropertyValue = buffer.Consume<Scalar>();
            var convertedYamlPropertyValue = ApplyNamingConventionToPolymorhpismStrategy(polymorphismContext, polymorphismPropertyValue.Value);
            suggestedType = polymorphismContext.GetConcreteType(convertedYamlPropertyValue);

            return suggestedType != null;
        }

        #endregion

        #region Helpers

        private string ApplyNamingConventionToPolymorhpismStrategy(PolymorphismContext context, string polymorphicPropertyValue)
        {
            if (context.IsStrategyOfType<IPolymorphismEnumDiscriminatorStrategy>())
            {
                return _options.Value.EnumNamingConvention.Apply(polymorphicPropertyValue);
            }

            return _options.Value.DeserializationNamingConvention.Apply(polymorphicPropertyValue);
        }

        #endregion
    }
}
