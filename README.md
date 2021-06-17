# FastGithub
github定制版的dns服务，解析访问github最快的ip

### 加速原理
* 使用github公开的ip范围，扫描所有可用的ip；
* 轮询检测并统计可用ip的访问成功率与访问耗时；
* 拦截dns，访问github时，服务端模拟TTL，返回最优ip；

### 使用说明
在局域网服务器(没有就使用本机)运行本程序，将网络连接的dns设置为程序运行的机器的ip。

> 若无法下载相关资源，请转到[https://gitee.com/jiulang/fast-github](https://gitee.com/jiulang/fast-github)
