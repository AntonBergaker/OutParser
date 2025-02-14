# OutParser
OutParser is a nuget library enabling simple string parsing following a simple template pattern.

Example usage:
```csharp
using OutParsing;

string input = "I selected 69";
OutParser.Parse(input, "I selected {value}", out int value);

Console.WriteLine(value); // Prints 69
```

## Usage
### Installing
OutParser requires .NET 9.0 as it makes use of [Interceptors](https://github.com/dotnet/roslyn/blob/main/docs/features/interceptors.md). The recommended way to add OutParser to your project is to install it from nuget.

### Supported types
OutParser supports anything that implements IParseable<T> and ISpanParseable<T>, which includes many common types in .NET. This also means you can use your own types by having them implement either of the two interfaces.
