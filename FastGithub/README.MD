# FastGithub
github加速神器

### 本机使用[推荐]
* 运行FastGithub程序，自动本机的网络适配器的dns会为127.0.0.1和安装CA证书
* 如果网络适配器的dns没有变成127.0.0.1，请手工修改网络适配器的dns
* 如果浏览器提示无效的证书，请手工安装CA证书到`将所有的证书都放入下载存储\受信任的根证书颁发机构`

### 局域网服务器使用 
* 在局域网服务器运行FastGithub程序
* 手工修改你电脑的网络适配器的dns，值为局域网服务器的ip
* 在你的电脑安装CA证书到`将所有的证书都放入下载存储\受信任的根证书颁发机构`

### 以windows服务运行
* FastGithub.exe start // 安装并启动服务
* FastGithub.exe stop  // 卸载并删除服务

### git ssl验证不通过
使用FastGithub之后，git请求时会遇到SSL certificate problem: unable to get local issuer certificate
需要关闭git的证书验证：
```
git config --global http.sslverify false
```