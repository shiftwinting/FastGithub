cd ./FastGithub
set output=./bin/publish
if exist "%output%" rd /S /Q "%output%"
dotnet publish -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true -f net6.0 -r win-x64 -o "%output%/win10-x64"
dotnet publish -c Release /p:PublishSingleFile=true /p:PublishTrimmed=false -f net5.0 -r win-x64 -o "%output%/win7-x64"
dotnet publish -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true -f net6.0 -r linux-x64 -o "%output%/linux-x64"
dotnet publish -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true -f net6.0 -r osx-x64 -o "%output%/osx-x64"