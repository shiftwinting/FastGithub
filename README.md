# FastGithub
github加速神器，解决github打不开、用户头像无法加载、releases无法上传下载、git-clone、git-pull、git-push失败等问题。

### 1 程序下载
如果不能下载[releases](https://github.com/dotnetcore/FastGithub)里发布的程序，可以到Q群`307306673`里面的群文件下载。

### 2 部署方式
#### 2.1 windows本机
* 双击运行fastgithub.exe程序
* `fastgithub.exe start` // 以windows服务安装并启动
* `fastgithub.exe stop` // 以windows服务卸载并删除

#### 2.2 linux本机
* 执行`sudo ./fastgithub`
* 手工安装cacert/fastgithub.cer到受信任的根证书颁发机构
* 手工设置系统代理为http://127.0.0.1:38457或自动代理为http://127.0.0.1:38457/proxy.pac

#### 2.3 macOS本机
* 双击运行fastgithub程序
* 手工安装cacert/fastgithub.cer并设置信任
* 手工设置系统代理为http://127.0.0.1:38457或自动代理为http://127.0.0.1:38457/proxy.pac

  
### 3 证书验证
#### 3.1 git
git操作提示`SSL certificate problem`</br>

需要关闭git的证书验证：`git config --global http.sslverify false`

#### 3.2 firefox
firefox提示`连接有潜在的安全问题`</br>

设置->隐私与安全->证书->查看证书->证书颁发机构，导入fastgithub.cer，勾选信任由此证书颁发机构来标识网站
  

### 4 安全性说明
FastGithub为每台不同的主机生成自颁发CA证书，保存在cacert文件夹下。客户端设备需要安装和无条件信任自颁发的CA证书，请不要将证书私钥泄露给他人，以免造成损失。

### 5 合法性说明
《国际联网暂行规定》第六条规定：“计算机信息网络直接进行国际联网，必须使用邮电部国家公用电信网提供的国际出入口信道。任何单位和个人不得自行建立或者使用其他信道进行国际联网。”
FastGithub本地代理使用的都是“公用电信网提供的国际出入口信道”，从国外Github服务器到国内用户电脑上FastGithub程序的流量，使用的是正常流量通道，其间未对流量进行任何额外加密（仅有网页原有的TLS加密，区别于VPN的流量加密），而FastGithub获取到网页数据之后发生的整个代理过程完全在国内，不再适用国际互联网相关之规定。
