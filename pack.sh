#! /bin/bash
cd ./FastGithub/bin/publish
 
# win-x64
zip -r fastgithub_win-x64.zip fastgithub_win-x64 -x "./fastgithub_win-x64/x86/*" -x "./fastgithub_win-x64/*.pdb"

# linux-x64
chmod 777 ./fastgithub_linux-x64/fastgithub
chmod 777 ./fastgithub_linux-x64/dnscrypt-proxy/dnscrypt-proxy
zip -r fastgithub_linux-x64.zip fastgithub_linux-x64 -x "./fastgithub_linux-x64/x64/*" -x "./fastgithub_linux-x64/x86/*" -x "./fastgithub_linux-x64/*.pdb"

# osx-x64
chmod 777 ./fastgithub_osx-x64/fastgithub
chmod 777 ./fastgithub_osx-x64/dnscrypt-proxy/dnscrypt-proxy
zip -r fastgithub_osx-x64.zip fastgithub_osx-x64 -x "./fastgithub_osx-x64/x64/*" -x "./fastgithub_osx-x64/x86/*" -x "./fastgithub_osx-x64/*.pdb"
