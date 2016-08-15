using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UcAsp.RPC;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using IFace;
namespace UcAsp.RPC.Client.test
{
    class Program
    {
        static ApplicationContext context = new ApplicationContext();

        string resut = string.Empty;
        static void Main(string[] args)
        {


            Thread.Sleep(3000);
            //IFace.ITest clazz = context.GetProxyObject<IFace.ITest>();
            //Console.WriteLine(".");

            //Console.WriteLine("..");
            //clazz.Get("MM", 1);
            //Console.WriteLine("...");
            //List<string> m = clazz.Good(0.ToString(), "MM", "MMM");
            //// resut = resut + "\r\n" + m[0];
            //Console.WriteLine(m[0]);
          //  IFace.ITest clazz = context.GetProxyObject<IFace.ITest>();
            for (int i = 0; i < 10000; i++)
            {
                
                //  new Program().Tasks(0);
                 Task task = new Task(new Program().Tasks, i);
                 task.Start();
                //Task tas = new Task(() =>
                //{
                //    IFace.ITest clazz = new Program().context.GetProxyObject<IFace.ITest>();

                //});
                //tas.Start();

                // Console.WriteLine(task.Result);
                // Thread thread = new Thread(new ParameterizedThreadStart(new Program().Tasks));
                // thread.Start(i);
                //Thread.Sleep(1000);
            }
            Console.ReadKey();
        }
        IFace.ITest clazz = context.GetProxyObject<IFace.ITest>();
        private void Tasks(object i)
        {
            
            string mx = clazz.Get("MM", (int)i);
            int x = clazz.GetInt((int)i);
            Tuple<int> t = clazz.GetTuple((int)i);
            List<string> m = clazz.Good(i.ToString(), "MM", "MMM");

            List<Imodel> model = clazz.GetModel((int)i);

            Console.WriteLine(x);
            Console.WriteLine(t.Item1);
            Console.WriteLine(mx);
            Console.WriteLine(m[0]);
            Console.WriteLine(model[0].Code);

            // Thread.Sleep(1000);
            //return m[0];
        }
    }
}
