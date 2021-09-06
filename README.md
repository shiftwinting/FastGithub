# FastGithub
github加速神器，解决github打不开、用户头像无法加载、releases无法上传下载、git-clone、git-pull、git-push失败等问题。

### 程序下载
如果不能下载[releases](https://github.com/dotnetcore/FastGithub)里发布的程序，可以到Q群`307306673`里面的群文件下载。

### 加速原理
* 修改本机的dns服务指向FastGithub自身
* 解析匹配的域名为FastGithub自身的ip
* 请求安全dns服务(dnscrypt-proxy)获取域名的ip
* 选择最优的ip进行ssh代理或https反向代理 

### 协议列表
| 协议  | 资源标识              | 端口要求   | 用途                         |
| ----- | --------------------- | ---------- | ---------------------------- |
| DNS   | `udp://0.0.0.0:53`    | 要求可用   | 解析配置的域名指向FastGithub |
| DoH   | `https://0.0.0.0:443` | 要求可用   | 解析配置的域名指向FastGithub |
| HTTPS | `https://0.0.0.0:443` | 要求可用   | 反向代理https请求            |
| HTTP  | `http://0.0.0.0:80`   | 不要求可用 | 反向代理http请求             |
| SSH   | `ssh://0.0.0.0:22`    | 不要求可用 | 代理ssh请求到github          |

### 部署方式
#### windows本机
* 双击运行FastGithub.exe程序
* `FastGithub.exe start` // 以windows服务安装并启动
* `FastGithub.exe stop` // 以windows服务卸载并删除
#### linux本机
* 执行`sudo ./FastGithub`
* 手工添加127.0.0.1做为/etc/resolv.conf的第一条记录
* 手工安装CACert/FastGithub.cer到受信任的根证书颁发机构

#### macOS本机
* 双击运行FastGithub程序
* 手工添加127.0.0.1做为连接网络的DNS的第一条记录
* 手工安装CACert/FastGithub.cer并设置信任

#### 局域网服务器
* 在局域网服务器运行FastGithub程序
* 手工将你电脑的主DNS设置为局域网服务器的ip
* 手工在你电脑安装FastGithub.cer到受信任的根证书颁发机构
  
### 证书验证
#### git
git操作提示`SSL certificate problem`

需要关闭git的证书验证：`git config --global http.sslverify false`

#### firefox
firefox提示`连接有潜在的安全问题`

设置->隐私与安全->证书->查看证书->证书颁发机构，导入FastGithub.cer，勾选信任由此证书颁发机构来标识网站

### 应用冲突
#### hosts文件
需要从hosts文件移除github相关域名的配置

#### 代理(proxy)
关闭代理，或将浏览器和系统配置为不代理github相关域名

#### 浏览器安全DNS
关闭浏览器的安全DNS功能或将安全DNS设置为https://127.0.0.1

### 安全性说明
FastGithub为每台不同的主机生成自颁发CA证书，保存在CACert文件夹下。客户端设备需要安装和无条件信任自颁发的CA证书，请不要将证书私钥泄露给他人，以免造成损失。

### 合法性说明
《国际联网暂行规定》第六条规定：“计算机信息网络直接进行国际联网，必须使用邮电部国家公用电信网提供的国际出入口信道。任何单位和个人不得自行建立或者使用其他信道进行国际联网。”
FastGithub本地代理使用的都是“公用电信网提供的国际出入口信道”，从国外Github服务器到国内用户电脑上FastGithub程序的流量，使用的是正常流量通道，其间未对流量进行任何额外加密（仅有网页原有的TLS加密，区别于VPN的流量加密），而FastGithub获取到网页数据之后发生的整个代理过程完全在国内，不再适用国际互联网相关之规定。
