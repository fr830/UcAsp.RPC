# UcAsp.RPC

####  C# 开发的.Net框架下的RPC服务，路线:已经开发好的程序如何快速实现RPC. 

## Nuget 安装
```ps
Install-Package UcAsp.RPC 
```

## 服务器端配置
```XML
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <configSections>
    <sectionGroup name="service">
      <section name="server" type="System.Configuration.NameValueSectionHandler, System, Version=1.0.3300.0, Culture=neutral, PublicKeyToken=b77a5c561934e089, Custom=null" />
    </sectionGroup>
  </configSections>
  <service>
    <server>
      <add key="port" value="9966" />  <!--服務器端口--->
      <add key="username" value="admin" />
      <add key="password" value="admin" />
    </server>
    <assmebly>
      <add key="Face" value="Face.dll" /><!--需要被外部應用的類庫--->
    </assmebly>
  </service>
</configuration>
```
## 服務器端设置

```C#
ApplicationContext context = new ApplicationContext();
context.Start(AppDomain.CurrentDomain.BaseDirectory+ "Application.config", AppDomain.CurrentDomain.BaseDirectory);
```

## 客戶端配置
```XML
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <configSections>
    <sectionGroup name="client">
      <section name="server" type="System.Configuration.NameValueSectionHandler, System, Version=1.0.3300.0, Culture=neutral, PublicKeyToken=b77a5c561934e089, Custom=null" />
    </sectionGroup>
  </configSections>
  <client>
    <server>
      <add key="ip" value="127.0.0.1:9966" /> <!---服務器地址--->
      <add key="mode" value="tcp" />
      <add key="pool" value="10" />
      <add key="username" value="admin" />
      <add key="password" value="admin" />
    </server>
    <relation>
      <add key="ISCS.WMS2.Model" value="ISCS.WMS2.Model.dll" /><!--関聯類庫-->
    </relation>
  </client>

</configuration>
```

## 客户端设置
### 客戶端創建對象
```C#
static ApplicationContext context= new ApplicationContext();
context.Start(AppDomain.CurrentDomain.BaseDirectory + "Application.config", AppDomain.CurrentDomain.BaseDirectory);
```
### 實例一個對象
```C#
IFace.ITest clazz = context.GetProxyObject<IFace.ITest>();
int o = (int)i;
string x = clazz.R(ref o);
```
