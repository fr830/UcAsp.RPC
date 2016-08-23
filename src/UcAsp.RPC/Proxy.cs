/***************************************************
*创建人:TecD02
*创建时间:2016/7/30 13:18:31
*功能说明:<Function>
*版权所有:<Copyright>
*Frameworkversion:4.0
*CLR版本：4.0.30319.42000
***************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using log4net;
namespace UcAsp.RPC
{
    public static class Proxy
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Proxy));
        public static Dictionary<string, string> RelationDll { set; get; }
        private static object _obj = new object();
        private static Dictionary<string, object> _nameSpaceList = new Dictionary<string, object>();
        public static object GetObjectType<T>(string nameSpace, string nameClass)
        {

            lock (_obj)
            {
                Type type = typeof(T);


                if (_nameSpaceList.ContainsKey(nameSpace + nameClass))
                {
                    return (T)_nameSpaceList[nameSpace + nameClass];
                }

                Assembly assembly = type.Assembly;
                MemberInfo[] m = type.GetMethods();
                PropertyInfo[] p = type.GetProperties();
                ICodeCompiler complier = (new CSharpCodeProvider().CreateCompiler());

                CompilerParameters paras = new CompilerParameters();
                paras.ReferencedAssemblies.Add("System.dll");
                paras.ReferencedAssemblies.Add("UcAsp.RPC.dll");
                paras.ReferencedAssemblies.Add("log4net.dll");
                if (RelationDll != null)
                {
                    foreach (var vdll in RelationDll)
                    {
                        paras.ReferencedAssemblies.Add(vdll.Value);
                    }
                }
                paras.ReferencedAssemblies.Add(assembly.ManifestModule.Name);
                paras.GenerateExecutable = false;
                paras.GenerateInMemory = true;
                CompilerResults cr = complier.CompileAssemblyFromSource(paras, GetCodeString(type, type.Namespace, nameSpace, nameClass));
                if (cr.Errors.HasErrors)
                {
                    foreach (CompilerError err in cr.Errors)
                    {
                        _log.Error(err);
                        Console.WriteLine(err.ErrorText);
                    }
                    return default(T);
                }
                else
                {
                    Assembly objAssembly = cr.CompiledAssembly;
                    _log.Info("创建对象" + string.Format("{0}.{1}", nameSpace, nameClass));
                    object obj = objAssembly.CreateInstance(string.Format("{0}.{1}", nameSpace, nameClass));
                    _nameSpaceList.Add(nameSpace + nameClass, obj);
                    return obj;
                }
            }

        }

        private static string GetCodeString(Type type, string assmbly, string nameSpace, string nameClass)
        {


            string interFaceName = type.Name;
            Assembly assembly = type.Assembly;
            MemberInfo[] m = type.GetMethods();
            PropertyInfo[] pi = type.GetProperties();

            StringBuilder sb = new StringBuilder();
            sb.Append("using System;\r\n");
            sb.Append("using System.Collections;\r\n");
            sb.Append("using System.Collections.Generic;\r\n");
            sb.Append("using System.Reflection;\r\n");
            sb.Append("using UcAsp.RPC;\r\n");
            sb.Append("using log4net;\r\n");
            if (RelationDll != null)
            {
                foreach (var rassmbly in RelationDll)
                {
                    sb.AppendLine(string.Format("using {0};\r\n", rassmbly.Key));
                }
            }
            sb.AppendFormat("using {0};\r\n", assmbly);
            sb.AppendFormat("namespace {0}\r\n", nameSpace);
            sb.Append("{\r\n");

            sb.AppendFormat("    public class {0}:ProxyObject,{1}\r\n", nameClass, interFaceName);
            sb.Append("    {\r\n");

            sb.AppendFormat("private readonly ILog _log = LogManager.GetLogger(typeof({0}));\r\n", nameClass);

            foreach (PropertyInfo p in pi)
            {
                sb.AppendFormat(" public {0} {1}{{get;set;}}\r\n", GetTypeName(p.PropertyType), p.Name);
            }

            foreach (MethodInfo method in m)
            {

                //MethodBase method = mi;// type.GetMethod(mi);
                ParameterInfo[] para = method.GetParameters();

                sb.Append(Environment.NewLine);
                sb.AppendFormat("        public {0} {1}(", GetTypeName(method.ReturnType), method.Name);
                for (int x = 0; x < para.Length; x++)
                {
                    sb.AppendFormat("{0} {1}", GetTypeName(para[x].ParameterType), para[x].Name);
                    if (x != para.Length - 1)
                    {
                        sb.Append(",");
                    }
                }
                sb.Append("     )\r\n");
                sb.Append(Environment.NewLine);
                sb.Append("        {\r\n");




                sb.AppendLine("            List<object> entity = new List<object>();\r\n");
                foreach (var pa in para)
                {
                    sb.AppendLine(string.Format("            entity.Add({0});\r\n", pa.Name));
                }

                sb.AppendLine("            DataEventArgs e = new DataEventArgs();");
                sb.AppendLine("            e.Binary = this.Serializer.ToBinary(entity);");

                string action = string.Format("{0}.{1}.{2}", method.DeclaringType.FullName, method.Name, GetMethodMd5Code(method));
                sb.AppendLine(string.Format("            e.ActionParam = \"{0}\";\r\n", action));
                sb.AppendLine("            e.ActionCmd = CallActionCmd.Call.ToString();\r\n");
                sb.Append("       DataEventArgs data=new DataEventArgs();");
                sb.Append("        try{\r\n");
                sb.AppendLine("             data = this.Client.CallServiceMethod(e);\r\n");
                sb.Append("       }catch (Exception ex)\r\n");
                sb.Append("       { _log.Error(ex);}\r\n");
                sb.AppendLine("            if (data.ActionCmd == CallActionCmd.Timeout.ToString() || data.ActionCmd == CallActionCmd.Error.ToString()) {\r\n ");
                sb.AppendLine("           _log.Error(data.ActionCmd+\"/\"+data.ActionCmd );");
                sb.AppendLine("                Exception ex = new Exception(\"Call Service Method \" + data.ActionCmd + \": \" + data.ActionParam);\r\n");
                sb.AppendLine("                throw (ex);\r\n");
                sb.AppendLine("            }\r\n");

                if (IsVoid(method.ReturnType) == false)
                {
                    sb.AppendLine(string.Format("            return this.Serializer.ToEntity<{0}>(data.Binary);\r\n", GetTypeName(method.ReturnType)));

                }

                sb.Append("        }\r\n");

            }
            sb.Append("    }\r\n");
            sb.Append("}");
            string result = sb.ToString();
            return result;
        }
        /// <summary>
        /// 获取类型的字符串表达示
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public static string GetTypeName(Type type)
        {
            if (type == typeof(void))
            {
                return "void";
            }
            else if (type.IsGenericType)
            {
                return string.Format(Regex.Replace(type.Name, @"`\d+", "<{0}>"), GetGenericTypeArgs(type));
            }
            else
            {
                return type.Name;
            }
        }

        /// <summary>
        /// 获取泛型参数
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns></returns>
        private static string GetGenericTypeArgs(Type type)
        {
            Type[] typeArguments = type.GetGenericArguments();
            string args = string.Empty;
            foreach (var t in typeArguments)
            {
                args = args + GetTypeName(t) + ",";
            }
            return args.TrimEnd(',');
        }

        /// <summary>
        /// 是否是无返回值类型
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public static bool IsVoid(Type type)
        {
            return typeof(void).Equals(type);
        }

        public static String GetMethodMd5Code(MethodInfo method)
        {
            string text = GetTypeName(method.ReturnType) + method.Name;

            ParameterInfo[] parameters = method.GetParameters();

            // 获取所有参数的类型
            for (int i = 0; i < parameters.Length; i++)
            {
                text = text + GetTypeName(parameters[i].ParameterType);
            }

            Console.WriteLine(text);
            byte[] result = Encoding.UTF8.GetBytes(text);
            //Debug.WriteLine(text);
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] output = md5.ComputeHash(result);
            Console.WriteLine(BitConverter.ToString(output).Replace("-", ""));
            return BitConverter.ToString(output).Replace("-", "");
        }


    }
}
