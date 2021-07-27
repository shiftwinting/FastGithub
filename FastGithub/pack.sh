#! /bin/bash
cd ./bin/publish

# linux-x64
chmod 777 ./linux-x64/dnscrypt-proxy
chmod 777 ./linux-x64/FastGithub
zip -r linux-x64.zip linux-x64

# osx-x64
chmod 777 ./osx-x64/dnscrypt-proxy
chmod 777 ./osx-x64/FastGithub
zip -r osx-x64.zip osx-x64

# windows-x64
zip -r win-x64.zip win-x64 -x "./win-x64/aspnetcorev2_inprocess.dll"