# FastGithub
github加速神器

### 运行方式
#### windows本机
* 双击运行FastGithub.exe程序
* 程序自动设置网络适配器的主DNS为127.0.0.1
* 程序自动安装CA证书到`将所有的证书都放入下载存储\受信任的根证书颁发机构` 
* FastGithub.exe start // 安装并启动服务
* FastGithub.exe stop  // 卸载并删除服务

#### linux本机
* 执行./FastGithub运行程序
* 手工将你的电脑的主DNS设置为127.0.0.1
* 手工在你的电脑安装CA证书到系统根证书存储区

#### 局域网服务器
* 在局域网服务器运行FastGithub程序
* 手工将你的电脑的主DNS设置为局域网服务器的ip
* 手工在你的电脑安装CA证书到系统根证书存储区


### 证书验证
#### git ssl验证不通过
使用FastGithub之后，git请求时会遇到SSL certificate problem: unable to get local issuer certificate
需要关闭git的证书验证：
```
git config --global http.sslverify false
```

### 配置文件
#### 主配置文件
appsettings.json为主配置文件，里面有dns配置和域名的配置示例。

#### 子配置文件
appsettings.*.json为子配置文件，默认有github和stackoverflow的配置，新增子配置文件后需要重启FastGithub才生效。