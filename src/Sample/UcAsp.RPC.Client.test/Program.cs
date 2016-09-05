using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UcAsp.RPC;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using IFace;
using Face;
namespace UcAsp.RPC.Client.test
{
    class Program
    {
        static ApplicationContext context;

        string resut = string.Empty;
        static long d = DateTime.Now.Ticks;
        static void Main(string[] args)
        {
            

            Thread.Sleep(3000);
            context = new ApplicationContext();

            //IFace.ITest clazz = context.GetProxyObject<IFace.ITest>();
            //Console.WriteLine(".");

            //Console.WriteLine("..");
            //clazz.Get("MM", 1);
            //Console.WriteLine("...");
            //List<string> m = clazz.Good(0.ToString(), "MM", "MMM");
            //// resut = resut + "\r\n" + m[0];
            //Console.WriteLine(m[0]);
            //  IFace.ITest clazz = context.GetProxyObject<IFace.ITest>();
            d = DateTime.Now.Ticks;
            for (int i = 0; i < 5000; i++)
            {

                // new Program().Tasks(0);
               Task task = new Task(new Program().Tasks, i);
               task.Start();
                // new Program().Tasks(0);
                // Console.WriteLine(0);
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

        private void Tasks(object i)
        {

            IFace.ITest clazz =  context.GetProxyObject<IFace.ITest>();
            IFace.ITest2 clazz2 = context.GetProxyObject<IFace.ITest2>();
            //Task t1 = new Task(() =>
            //{
            Imodel im = new Imodel { Code = (int)i, Message = "厕所呢厕所呢厕所呢厕所呢厕所" };
            List<Imodel> il = new List<Imodel>();
            il.Add(im);
            string mesage = clazz.ToList(il);
            // Console.WriteLine(mesage+"/"+ i);
            //});
            //t1.Start();
            //Task t2 = new Task(() =>
            //{
            string mx = clazz.Get("MM", (int)i);
            // Console.WriteLine(mx + "/" + i);
            //});
            //t2.Start();
            //Task t3 = new Task(() =>
            //{
            int x = clazz.GetInt((int)i);

            // Console.WriteLine(x + "/" + i);
            //});
            //t3.Start();
            //Task t4 = new Task(() =>
            //{
            Tuple<int> t = clazz.GetTuple((int)i);
            int mmmm = t.Item1;
            //  Console.WriteLine(t.Item1 + "/" + i);
            //});
            //t4.Start();
            //Task t5 = new Task(() =>
            //{
            List<string> m = clazz.Good(i.ToString(), "MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM", "MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM");
            //strig m=   
            Console.Write(".");
            if ((int)i == 2999)
            {
                Console.WriteLine(DateTime.Now.Ticks-d);
            }
            Console.WriteLine(i);
            //});
            //t5.Start();



            // Thread.Sleep(1000);
            //return m[0];
        }
    }
}
