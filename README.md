# FastGithub
github加速神器

### 加速原理
* 修改本机的dns服务指向FastGithub自身
* 解析匹配的域名为FastGithub自身的ip
* 请求不受污染的dns服务(dnscrypt-proxy)获取域名的ip
* 使用得到的ip进行无或有SNI的https反向代理

### 程序下载
[下载最新发布版本](https://gitee.com/jiulang/fast-github)
