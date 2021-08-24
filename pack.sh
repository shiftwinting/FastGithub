#! /bin/bash
cd ./FastGithub/bin/publish

# linux-x64
chmod 777 ./FastGithub_linux-x64/FastGithub
chmod 777 ./FastGithub_linux-x64/dnscryptproxy/dnscrypt-proxy
zip -r FastGithub_linux-x64.zip FastGithub_linux-x64

# osx-x64
chmod 777 ./FastGithub_osx-x64/FastGithub
chmod 777 ./FastGithub_osx-x64/dnscryptproxy/dnscrypt-proxy
zip -r FastGithub_osx-x64.zip FastGithub_osx-x64

# win10-x64
zip -r FastGithub_win10-x64.zip FastGithub_win10-x64 -x "./FastGithub_win10-x64/aspnetcorev2_inprocess.dll"

# win7-x64
zip -r FastGithub_win7-x64.zip FastGithub_win7-x64