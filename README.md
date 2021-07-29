# FastGithub
github加速神器，解决github打不开、用户头像无法加载、releases无法上传下载、git-clone、git-pull、git-push失败等问题。

### 加速原理
* 修改本机的dns服务指向FastGithub自身
* 解析匹配的域名为FastGithub自身的ip
* 请求不受污染的dns服务(dnscrypt-proxy)获取域名的ip
* 使用得到的ip进行无或有SNI的https反向代理 

### 程序下载
如果不能下载[releases](https://github.com/dotnetcore/FastGithub)里发布的程序，可以到Q群`307306673`里面的群文件下载。

### 使用说明
运行FastGithub，然后浏览器访问 http://127.0.0.1 或 https://127.0.0.1 或其它ip进入Dashboard

### 安全性说明
FastGithub为每台不同的主机生成自颁发CA证书，保存在CACert文件夹下。客户端设备需要安装和无条件信任自颁发的CA证书，请不要将证书私钥泄露给他人，以免造成损失。

### 合法性说明
《国际联网暂行规定》第六条规定：“计算机信息网络直接进行国际联网，必须使用邮电部国家公用电信网提供的国际出入口信道。任何单位和个人不得自行建立或者使用其他信道进行国际联网。”
FastGithub本地代理使用的都是“公用电信网提供的国际出入口信道”，从国外Github服务器到国内用户电脑上FastGithub程序的流量，使用的是正常流量通道，其间未对流量进行任何额外加密（仅有网页原有的TLS加密，区别于VPN的流量加密），而FastGithub获取到网页数据之后发生的整个代理过程完全在国内，不再适用国际互联网相关之规定。
