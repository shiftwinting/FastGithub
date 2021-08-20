cd ./FastGithub
set output=./bin/publish
if exist "%output%" rd /S /Q "%output%"
dotnet publish -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true -f net6.0 --self-contained -r win-x64 -o "%output%/FastGithub_win10-x64"
dotnet publish -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true -f net5.0 --self-contained -r win-x64 -o "%output%/FastGithub_win7-x64"
dotnet publish -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true -f net6.0 --self-contained -r linux-x64 -o "%output%/FastGithub_linux-x64"
dotnet publish -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true -f net6.0 --self-contained -r osx-x64 -o "%output%/FastGithub_osx-x64"