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
namespace UcAsp.RPC
{
    public static class Proxy
    {
        private static object obj = new object();
        private static Dictionary<string, object> _nameSpaceList = new Dictionary<string, object>();
        public static object GetObjectType<T>(string nameSpace, string nameClass)
        {

            lock (obj)
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
                paras.ReferencedAssemblies.Add(assembly.ManifestModule.Name);
                paras.GenerateExecutable = false;
                paras.GenerateInMemory = true;




                CompilerResults cr = complier.CompileAssemblyFromSource(paras, GetCodeString(type, type.Namespace, nameSpace, nameClass));
                if (cr.Errors.HasErrors)
                {
                    Console.WriteLine("编译错误：");
                    foreach (CompilerError err in cr.Errors)
                    {
                        Console.WriteLine(err.ErrorText);
                    }
                    return default(T);
                }
                else
                {
                    Assembly objAssembly = cr.CompiledAssembly;
                    object objHelloWorld = objAssembly.CreateInstance(nameSpace + "." + nameClass);
                    _nameSpaceList.Add(nameSpace + nameClass, objHelloWorld);
                    return objHelloWorld;
                }
            }

        }

        private static string GetCodeString(Type type, string assmbly, string nameSpace, string nameClass)
        {


            string interFaceName = type.Name;
            Assembly assembly = type.Assembly;
            MemberInfo[] m = type.GetMethods();
            PropertyInfo[] p = type.GetProperties();

            StringBuilder sb = new StringBuilder();
            sb.Append("using System;\r\n");
            sb.Append("using System.Collections;\r\n");
            sb.Append("using System.Collections.Generic;\r\n");
            sb.Append("using System.Reflection;\r\n");
            sb.Append("using UcAsp.RPC;\r\n");
            sb.Append("using " + assmbly + ";\r\n");
            sb.Append("namespace " + nameSpace + "");

            sb.Append(Environment.NewLine);
            sb.Append("{");
            sb.Append(Environment.NewLine);
            sb.Append("    public class " + nameClass + ":ProxyObject," + interFaceName);
            sb.Append(Environment.NewLine);
            sb.Append("    {");
            foreach (MethodInfo mi in m)
            {

                MethodBase method = type.GetMethod(mi.Name);
                ParameterInfo[] para = method.GetParameters();

                sb.Append(Environment.NewLine);
                sb.Append("        public " + GetTypeName(mi.ReturnType) + " " + method.Name + "(");
                for (int x = 0; x < para.Length; x++)
                {
                    sb.Append("" + GetTypeName(para[x].ParameterType) + " " + para[x].Name + "");
                    if (x != para.Length - 1)
                    {
                        sb.Append(",");
                    }
                }
                sb.Append("     )");
                sb.Append(Environment.NewLine);
                sb.Append("        {\r\n");




                sb.AppendLine("            List<object> entity = new List<object>();");
                foreach (var pa in para)
                {
                    sb.AppendLine(string.Format("            entity.Add({0});", pa.Name));
                }

                sb.AppendLine("            DataEventArgs e = new DataEventArgs();");
                sb.AppendLine("            e.Binary = this.Serializer.ToBinary(entity);");

                string action = mi.DeclaringType.FullName + "." + mi.Name + "." + GetMethodMd5Code(mi);
                sb.AppendLine(string.Format("            e.ActionParam = \"{0}\";", action));
                sb.AppendLine("            e.ActionCmd = CallActionCmd.Call.ToString();");

                sb.AppendLine("            DataEventArgs data = this.Client.CallServiceMethod(e);");
                sb.AppendLine("            if (data.ActionCmd == CallActionCmd.Timeout.ToString() || data.ActionCmd == CallActionCmd.Error.ToString()) { ");
                sb.AppendLine("                Exception ex = new Exception(\"Call Service Method \" + data.ActionCmd + \": \" + data.ActionParam);");
                sb.AppendLine("                throw (ex);");
                sb.AppendLine("            }");

                if (IsVoid(mi.ReturnType) == false)
                {
                    sb.AppendLine(string.Format("            return this.Serializer.ToEntity<{0}>(data.Binary);", GetTypeName(mi.ReturnType)));

                }

                sb.Append("        }");

            }
            sb.Append(Environment.NewLine);
            sb.Append("    }");
            sb.Append(Environment.NewLine);
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
