# FastGithub
github加速神器

### 加速原理
* 多种渠道获取github的ip(github公开的ip、各dns服务器提供的ip、ipaddress.com反查的ip)
* 轮询完整扫描github的所有ip，记录可访问的ip；
* 轮询扫描历史扫描出的可访问ip，统计ip的访问成功率与访问耗时；
* 提供dns服务，返回github最优ip或反向代理服务ip；
* 提供github反向代理，解决浏览器出现连接被重置的问题；

### 程序下载
[下载最新发布版本](https://gitee.com/jiulang/fast-github)
