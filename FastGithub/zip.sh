#! /bin/bash  # employ bash shell
cd ./bin/publish

# linux
chmod 777 ./linux-x64/dnscrypt-proxy
chmod 777 ./linux-x64/FastGithub
zip -r linux-x64.zip linux-x64

# windows
zip -r win-x64.zip win-x64 -x "./win-x64/aspnetcorev2_inprocess.dll"