# FastGithub
github加速神器

### 加速原理
* 修改本机的dns服务指向FastGithub自身
* 解析匹配的域名为FastGithub自身的ip
* 请求不受污染的dns服务(dnscrypt-proxy)获取域名的ip
* 使用得到的ip进行无或有SNI的https反向代理

### 加速站点
#### github
* github.com
* githubstatus.com
* *.github.com
* *.github.io
* *.githubapp.com
* *.githubassets.com
* *.githubusercontent.com
* \*github\*.s3.amazonaws.com

#### stackoverflow
* ajax.googleapis.com -> gapis.geekzu.org/ajax
* fonts.googleapis.com -> fonts.geekzu.org
* themes.googleusercontent.com -> gapis.geekzu.org/g-themes
* fonts.gstatic.com -> gapis.geekzu.org/g-fonts
* secure.gravatar.com -> sdn.geekzu.org
* *.gravatar.com -> fdn.geekzu.org
* i.stack.imgur.com => 404
* lh*.googleusercontent.com => 404
* www.google.com => 404

### 程序下载
[下载最新发布版本](https://github.com/xljiulang/FastGithub/releases) 

### 安全性说明
FastGithub生成自签名CA证书，要求安装到本机设备。FastGithub为每台不同设备生成的CA不同的证书，保存在CACert文件夹下，请不要将证书私钥泄露给他人，以免造成损失。

### 合法性说明
《国际联网暂行规定》第六条规定：“计算机信息网络直接进行国际联网，必须使用邮电部国家公用电信网提供的国际出入口信道。任何单位和个人不得自行建立或者使用其他信道进行国际联网。”
FastGithub本地代理使用的都是“公用电信网提供的国际出入口信道”，从国外的Github服务器到国内用户电脑的流量，使用的是正常流量通道，其间未对流量进行任何额外加密（仅有网页原有的TLS加密，用户和代理人并不掌握该TLS密钥，区别于VPN的流量加密），而FastGithub获取到网页数据之后发生的整个代理过程完全在国内，不再适用国际互联网相关之规定。
