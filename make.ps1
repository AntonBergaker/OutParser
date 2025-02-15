dotnet build --configuration Release
cd .\OutParser.Generator
nuget pack .\OutParser.nuspec -Prop Configuration=Release