﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UcAsp.RPC;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using IFace;
using System.Collections;
namespace UcAsp.RPC.Client.test
{
    class Program
    {
        static ApplicationContext context;

        string resut = string.Empty;
        static long d = DateTime.Now.Ticks;
        static IFace.ITest clazz;
        static IFace.ITest2 clazz2;
        private object count = 0;
        static void Main(string[] args)
        {
            context = new ApplicationContext();
            context.Start(AppDomain.CurrentDomain.BaseDirectory + "Application.config", AppDomain.CurrentDomain.BaseDirectory);

            Thread.Sleep(3000);
            ArrayList data = new ArrayList();
            data.Add(true);
            data.Add("sssssss");
            bool o = (Boolean)data[0];

            string code = (String)data[1];

            //IFace.ITest clazz = context.GetProxyObject<IFace.ITest>();
            //Console.WriteLine(".");

            //Console.WriteLine("..");
            //clazz.Get("MM", 1);
            //Console.WriteLine("...");
            //List<string> m = clazz.Good(0.ToString(), "MM", "MMM");
            //// resut = resut + "\r\n" + m[0];
            //Console.WriteLine(m[0]);
            //  IFace.ITest clazz = context.GetProxyObject<IFace.ITest>();
            //   d = DateTime.Now.Ticks;
            // Thread.Sleep(3000);

            // bll = context.GetProxyObject<ISwExportBLL>();
            // Thread.Sleep(3000);


            // new Program().Tasks(i);

            // if (i % 3 == 0)
            // {
            //     Thread.Sleep(100);
            // }
            // new Program().Tasks(0);
            // Console.WriteLine(0);

            // ThreadPool.SetMaxThreads(1, 5);
            for (int i = 0; i < 20000; i++)
            {
                // Task tas = new Task(() =>
                //{
                //   ThreadPool.QueueUserWorkItem(new WaitCallback(s =>
                // {
                //new Program().Tasks(i);
                //}));
                //  Task.Run(() =>
                //{
                new Program().Tasks(i);
                //});
                //Task t3 = new Task(() =>
                //{
                //    clazz = context.GetProxyObject<IFace.ITest>();
                //    int x = clazz.GetInt((int)i);

                //    Console.WriteLine(x + "/" + i);
                //});
                //t3.Start();

                // Thread t = new Thread(new ParameterizedThreadStart(new Program().Tasks));
                // t.Start(i);
                // Thread.Sleep(100);

                // Console.WriteLine(i);

                // new Program().Tasks(i);

                //  });
                // tas.Start();
            }
            // Console.WriteLine(task.Result);
            // Thread thread = new Thread(new ParameterizedThreadStart(new Program().Tasks));
            // thread.Start(i);
            //Thread.Sleep(1000);



            Console.ReadKey();
        }
        // ApplicationContext context = new ApplicationContext()
        private void Tasks(object i)
        {


            // UcAsp.RPC.IClient client = ApplicationContext._clients[0];




            IFace.ITest2 clazz2 = context.GetProxyObject<IFace.ITest2>();
            //  int imx = clazz2.GetMore(123);
            // Console.WriteLine("omx" + clazz2);
            //Task t1 = new Task(() =>
            //{
            //    clazz = context.GetProxyObject<IFace.ITest>();
            //    Imodel im = new Imodel { Code = (int)i, Message = "厕所呢厕所呢厕所呢厕所呢厕所" };
            //    List<Imodel> il = new List<Imodel>();
            //    il.Add(im);
            //    string mesage = clazz.ToList(il);
            //    Console.WriteLine(mesage + "/" + i);
            //});
            //t1.Start();
            ThreadPool.SetMaxThreads(6, 24);
            Task t2 = new Task(() =>
           {
                for (int m = 0; m < 10; m++)
               {
                   clazz = context.GetProxyObject<IFace.ITest>();
                   string mx = clazz.Get("m", (int)i);
                   Console.WriteLine("MM/" + i);
               }
        });
            t2.Start();


            Task t5 = new Task(() =>
           {
                // lock (count)
                // {
                //List<SwExport> list = bll.Query();
                // Console.WriteLine(list.Count);
                try
            {

                clazz = context.GetProxyObject<IFace.ITest>();

                List<string> m = clazz.Good(i.ToString(), "MMMMM", "m");
                Console.WriteLine("Gods:" + m[0]);



                //int mmx = 9;
                //int xx = 1;
                //bool mm = true;
                //clazz.X(out mmx, out xx, ref mm);
                //Console.WriteLine(xx);



                //int o = (int)i;
                //string x = clazz.R(ref o);
                //Console.WriteLine(x + "." + o);



                //Tuple<int> t = clazz.GetTuple(1000);
                //Console.WriteLine("tuple:" + t.Item1);

                //List<Nvr> n = clazz.GetModel(222);
                //Console.WriteLine("nvr:" + n.Count);





            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        });
              t5.Start();



            // Thread.Sleep(1000);
            //return m[0];
        }

        private void Clazz_OnChange(object sender, Event e)
        {
            Console.WriteLine(e.C);
        }
    }
}
