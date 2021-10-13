# MacOSX运行FastGithub配置教程

### 1 解决 "XXX" cannot be opened because the developer cannot be verified
打开终端进入FastGithub执行文件所在路径执行命令：
`sudo xattr -d com.apple.quarantine *.*`

### 2 安装证书
打开FastGithub后，目录内会生成cacert目录，双击打开fastgithub.cer，系统弹出Keychain Access窗口，列表中双击FastGitHub，弹出证书详情窗口，展开Trust并选择Always Trust。

<img src="https://github.com/dotnetcore/FastGithub/blob/master/Resources/MacOSXConfig/KeychainAccess.png?raw=true"/>

<img src="https://github.com/dotnetcore/FastGithub/blob/master/Resources/MacOSXConfig/trust.png?raw=true"/>

### 3 配置代理
#### 3.1 自动代理
打开mac设置，网络，点击高级，选择代理，勾选网自动代理配置，填写FastGithub窗口提示的地址

<img src="https://github.com/dotnetcore/FastGithub/blob/master/Resources/MacOSXConfig/autoproxy.png?raw=true"/>

<img src="https://github.com/dotnetcore/FastGithub/blob/master/Resources/MacOSXConfig/cmdwin.png?raw=true"/>

#### 3.2 手动代理
打开mac设置，网络，点击高级，选择代理，勾选网页代理(HTTP)及安全网页代理(HTTPS),填写FastGithub窗口提示的地址

<img src="https://github.com/dotnetcore/FastGithub/blob/master/Resources/MacOSXConfig/proxy.png?raw=true"/>

<img src="https://github.com/dotnetcore/FastGithub/blob/master/Resources/MacOSXConfig/cmdwin.png?raw=true"/>
