using Microsoft.VisualStudio.TestTools.UnitTesting;
using UcAsp.RPC;
/***************************************************
*创建人:TecD02
*创建时间:2016/7/30 14:02:42
*功能说明:<Function>
*版权所有:<Copyright>
*Frameworkversion:4.0
*CLR版本：4.0.30319.42000
***************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UcAsp.RPC;
using System.Reflection;
namespace UcAsp.RPC.Tests
{
    [TestClass()]
    public class ProxyTests
    {
       // [TestInitialize]
        public void Initial()
        {
            //ApplicationContext context = new ApplicationContext();
        }
       // [TestMethod()]
        public void GetObjectTypeTest()
        {
            //Assembly assl = Assembly.LoadFrom(@"E:\DEV\UcAsp.RPC\UcAsp.RPCTests\bin\Debug\IFace.dll");
            //Type[] types=assl.GetTypes();
            //ApplicationContext context = new ApplicationContext();

          //  IFace.ITest clazz = context.GetProxyObject<IFace.ITest>();
            //
           // clazz.Get("MM", 1);
            //List<string> m = clazz.Good("M", "MM","MMM");
           
            // IFace.IClass clazz = context.GetObject<IFace.IClass>("ClaZZ");
            //string mesage = clazz.Get("Yu", 1);

            //string s = clazz.Good("s", "m", "ss");

        }

        [TestMethod()]
        public void Distance()
        {
            int iRssi = Math.Abs(-71);
            double power = (iRssi - 59) / (10 * 2.0);
            double d = Math.Pow(10, power);
            //return d;
        }
    }
}