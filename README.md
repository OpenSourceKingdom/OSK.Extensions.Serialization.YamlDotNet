# OSK.Extensions.Serialization.YamlDotNet
This library provides a set of extensions for the `YamlDotNet` package and the related `IYamlSerializer` that uses it. Using dependency injection,
consumers can use `AddYamlDotNetPolymorphism(Type)` to provide a marker type for this library to use when it performs the setup necessary for the 
OSK Yaml Serializer to handle polymorphism strategies. By using this library, consumers can support abstraction deserialization with the OSK Yaml serializer.

# Contributions and Issues
Any and all contributions are appreciated! Please be sure to follow the branch naming convention OSK-{issue number}-{deliminated}-{branch}-{name} as current workflows rely on it for automatic issue closure. Please submit issues for discussion and tracking using the github issue tracker.