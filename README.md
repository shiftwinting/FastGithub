# FastGithub
github定制版的dns服务，能解析最快访问github的ip


### 原理
* 使用github公开的ip范围，扫描所有可用的ip；
* 轮休检测与记录扫描到的ip的访问耗时；
* 拦截dns，访问github时，返回最快的ip；
