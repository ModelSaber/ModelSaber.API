del /s /q .\Bin\*
del /s /q .\Publish\*
dotnet build ModelSaber.Frontend.csproj -o ./Build -r linux-x64 -c Release
dotnet publish ModelSaber.Frontend.csproj -o ./Publish -r linux-x64 -c Release /p:PublishSingleFile=true