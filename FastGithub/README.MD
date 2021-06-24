# FastGithub
github定制版的dns服务，解析github最优的ip

### 本机使用
* 运行FastGithub程序，本机的网络适配器的dns会自动变成127.0.0.1
* 如果网络适配器的dns没有变成127.0.0.1，请手工修改网络适配器的dns

### 局域网服务器使用
* 在局域网服务器运行FastGithub程序
* 手工修改你电脑的网络适配器的dns，值为局域网服务器的ip

### 以windows服务运行
以管理员身份运行cmd，键入如下命令，其中D:\Softs为软件实际目录
* D:\Softs\FastGithub.exe start // 安装并启动服务
* D:\Softs\FastGithub.exe stop  // 卸载并删除服务