# UcAsp.RPC

#服务器端配置
&lt;?xml version="1.0" encoding="utf-8"?&gt;<br />
&lt;configuration&gt;<br />
  &lt;configSections&gt;<br />
    &lt;sectionGroup name="service"&gt;<br />
      &lt;section name="server" type="System.Configuration.NameValueSectionHandler, System, Version=1.0.3300.0, Culture=neutral, PublicKeyToken=b77a5c561934e089, Custom=null" /&gt;
    &lt;/sectionGroup&gt;<br />
  &lt;/configSections&gt;<br />
  &lt;service&gt;<br />
  &lt;!--端口配置--&gt;<br />
    &lt;server&gt;<br />
      &lt;add key="port" value="9008" /&gt;<br />
    &lt;/server&gt;<br />
    &lt;!--应用注册--&gt;<br />
    &lt;assmebly&gt;<br />
      &lt;add key="Face" value="Face.dll" /&gt;<br />
      &lt;!--add key="ISCS.WMS2.BLL" value="ISCS.WMS2.BLL.dll" /&gt;--&gt;<br />
    &lt;/assmebly&gt;<br />
  &lt;/service&gt;<br />
&lt;/configuration&gt;<br />
#客户端配置<br />
&lt;?xml version="1.0" encoding="utf-8"?&gt;<br />
&lt;configuration&gt;<br />
  &lt;configSections&gt;<br />
    &lt;sectionGroup name="client"&gt;<br />
      &lt;section name="server" type="System.Configuration.NameValueSectionHandler, System, Version=1.0.3300.0, Culture=neutral, PublicKeyToken=b77a5c561934e089, Custom=null" /&gt;
    &lt;/sectionGroup&gt;<br />
  &lt;/configSections&gt;<br />
  &lt;client&gt;<br />
  &lt;!--服务器ip端口配置--&gt;<br />
    &lt;server&gt;<br />
      &lt;add key="ip" value="127.0.0.1:9008;10.10.0.66:9008" /&gt;<br />
&lt;!--连接线程数量--&gt;<br />
    &lt;/server&gt;<br />
    &lt;!--接口依赖的应用--&gt;<br />
    &lt;relation&gt;<br />
      &lt;add key="ISCS.WMS2.Model" value="ISCS.WMS2.Model.dll" /&gt;<br />
    &lt;/relation&gt;<br />
  &lt;/client&gt;<br />

&lt;/configuration&gt;




