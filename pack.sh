#! /bin/bash
cd ./FastGithub/bin/publish
 
# win-x64
zip -r FastGithub_win-x64.zip FastGithub_win-x64 -x "./FastGithub_win-x64/x86/*" -x "./FastGithub_win-x64/*.pdb"

# linux-x64
chmod 777 ./FastGithub_linux-x64/FastGithub
chmod 777 ./FastGithub_linux-x64/dnscryptproxy/dnscrypt-proxy
zip -r FastGithub_linux-x64.zip FastGithub_linux-x64 -x "./FastGithub_linux-x64/x64/*" -x "./FastGithub_linux-x64/x86/*" -x "./FastGithub_linux-x64/*.pdb"

# osx-x64
chmod 777 ./FastGithub_osx-x64/FastGithub
chmod 777 ./FastGithub_osx-x64/dnscryptproxy/dnscrypt-proxy
zip -r FastGithub_osx-x64.zip FastGithub_osx-x64 -x "./FastGithub_osx-x64/x64/*" -x "./FastGithub_osx-x64/x86/*" -x "./FastGithub_osx-x64/*.pdb"
