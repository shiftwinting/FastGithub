set output=./publish
if exist "%output%" rd /S /Q "%output%"
 
dotnet publish -c Release /p:PublishSingleFile=true /p:PublishTrimmed=false --self-contained -r linux-x64 -o "%output%/fastgithub_linux-x64" ./FastGithub/FastGithub.csproj 