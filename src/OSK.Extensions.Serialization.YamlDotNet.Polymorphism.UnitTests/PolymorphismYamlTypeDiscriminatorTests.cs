using Moq;
using System.Text;
using Xunit;
using YamlDotNet.Core;
using Microsoft.Extensions.Options;
using YamlDotNet.Serialization.NamingConventions;
using Microsoft.Extensions.DependencyInjection;
using OSK.Serialization.Polymorphism.Ports;
using OSK.Serialization.Yaml.YamlDotNet;
using OSK.Serialization.Polymorphism.Discriminators;
using OSK.Serialization.Polymorphism.Models;
using OSK.Serialization.Polymorphism;
using OSK.Serialization.Abstractions.Yaml;
using OSK.Extensions.Serialization.YamlDotNet.Polymorphism.UnitTests.Helpers;

namespace OSK.Extensions.Serialization.YamlDotNet.Polymorphism.UnitTests
{
    public class PolymorphismYamlTypeDiscriminatorTests
    {
        #region Variables

        private readonly Mock<IPolymorphismContextProvider> _mockContextProvider;
        private readonly PolymorphismYamlTypeDiscriminator _typeDiscriminator;

        #endregion

        #region Constructors

        public PolymorphismYamlTypeDiscriminatorTests()
        {
            _mockContextProvider = new Mock<IPolymorphismContextProvider>();

            _typeDiscriminator = new PolymorphismYamlTypeDiscriminator(typeof(TestAbstract), _mockContextProvider.Object,
                new OptionsWrapper<YamlDotNetSerializationOptions>(YamlDotNetSerializer.DefaultOptions));
        }

        #endregion

        #region BaseType (Get)

        [Fact]
        public void BaseType_ReturnsTypePassedIntoConstructor()
        {
            // Arrange/Act
            var baseType = _typeDiscriminator.BaseType;

            // Assert
            Assert.Equal(typeof(TestAbstract), baseType);
        }

        #endregion

        #region End to End

        [Fact]
        public async void Validate_ServiceCollectionExtensions()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddYamlDotNetSerialization();
            serviceCollection.AddPolymorphismEnumDiscriminatorStrategy();
            serviceCollection.AddYamlDotNetPolymorphism(typeof(TestAbstract));

            var provider = serviceCollection.BuildServiceProvider();

            var items = new TestAbstract[]
            {
                new TestChildA()
                {
                    A = 1,
                    B = [ 1, 2, 3 ],
                    Today = DateTime.Now,
                    AbstractType = AbstractType.ChildA
                },
                new TestChildB()
                {
                    A = 1,
                    B = [ 1, 2, 3 ],
                    C = new TestChildB()
                    {
                        A = 1,
                        B = new List<int> { 1, 2, 3 },
                        AbstractType = AbstractType.ChildB
                    },
                    AbstractType = AbstractType.ChildB
                }
            };

            // Act
            var serializer = provider.GetRequiredService<IYamlSerializer>();
            var data = await serializer.SerializeAsync(items);
            _ = await serializer.DeserializeAsync(data, items.GetType());
        }

        [Fact]
        public void EndToEnd_HasDiscriminator_ReturnsExpectedObject()
        {
            // Arrange
            var mockEnumStrategy = new Mock<IPolymorphismEnumDiscriminatorStrategy>();
            mockEnumStrategy.Setup(m => m.GetConcreteType(It.IsAny<PolymorphismAttribute>(),
                It.IsAny<Type>(), It.IsAny<object>()))
                .Returns((PolymorphismAttribute attribute, Type typeToConvert, object currentValue) =>
                {
                    switch (currentValue)
                    {
                        case nameof(AbstractType.ChildA):
                            return typeof(TestChildA);
                        case nameof(AbstractType.ChildB):
                            return typeof(TestChildB);
                        default:
                            throw new InvalidCastException("Current value not an expected abstract type");
                    }
                });
                
            var context = new PolymorphismContext(
                PolymorphismAttribute.GetPolymorphismAttribute(typeof(TestAbstract)),
                typeof(TestAbstract),
                mockEnumStrategy.Object
            );

            _mockContextProvider.Setup(m => m.HasPolymorphismStrategy(It.Is<Type>(t => t == typeof(TestAbstract))))
                .Returns(true);
            _mockContextProvider.Setup(m => m.GetPolymorphismContext(It.Is<Type>(t => t == typeof(TestAbstract))))
                .Returns(context);

            var items = new TestAbstract[]
            {
                new TestChildA()
                {
                    A = 1,
                    B = [ 1, 2, 3 ],
                    Today = DateTime.Now,
                    AbstractType = AbstractType.ChildA
                },
                new TestChildB()
                {
                    A = 1,
                    B = [ 1, 2, 3 ],
                    C = new TestChildB()
                    {
                        AbstractType = AbstractType.ChildB,
                        A = 1,
                        B = new List<int> { 1, 2, 3 }
                    },
                    AbstractType = AbstractType.ChildB
                }
            };

            var yamlSerializationOptions = YamlDotNetSerializer.DefaultOptions;
            yamlSerializationOptions.SerializationNamingConvention = PascalCaseNamingConvention.Instance;
            var options = new OptionsWrapper<YamlDotNetSerializationOptions>(yamlSerializationOptions);

            using var writeStream = new MemoryStream();
            using var textWriter = new StreamWriter(writeStream, Encoding.UTF8);
            textWriter.AutoFlush = true;
            var serializer = YamlUtilsHelpers.CreateSerializer(yamlSerializationOptions);
            serializer.Serialize(textWriter, items);

            var properties = items[0].GetType().GetProperties();

            var writtedData = writeStream.ToArray();
            var dataString = Encoding.UTF8.GetString(writtedData);

            using var readStream = new MemoryStream(writtedData);
            using var streamReader = new StreamReader(readStream);
            var deserializer = YamlUtilsHelpers.CreateDeserializer(yamlSerializationOptions,
                new PolymorphismYamlTypeDiscriminator(typeof(TestAbstract),
                    _mockContextProvider.Object,
                    options));

            var parser = new Parser(streamReader);
            var result = deserializer.Deserialize(parser, items.GetType());
            Assert.NotNull(result);
        }

        #endregion
    }
}
