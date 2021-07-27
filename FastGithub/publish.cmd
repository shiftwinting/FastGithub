set output=./bin/publish
if exist "%output%" rd /S /Q "%output%"
dotnet publish -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true -r win-x64 -o "%output%/win-x64"
dotnet publish -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true -r linux-x64 -o "%output%/linux-x64"
dotnet publish -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true -r osx-x64 -o "%output%/osx-x64"