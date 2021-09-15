cd ./FastGithub
set output=./bin/publish
if exist "%output%" rd /S /Q "%output%"
dotnet publish -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true --self-contained -r win-x64 -o "%output%/FastGithub_win-x64"
dotnet publish -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true --self-contained -r linux-x64 -o "%output%/FastGithub_linux-x64"
dotnet publish -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true --self-contained -r osx-x64 -o "%output%/FastGithub_osx-x64"