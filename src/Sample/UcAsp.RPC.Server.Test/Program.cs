using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UcAsp.RPC.Server.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            ApplicationContext context = new ApplicationContext();
            context.Start(AppDomain.CurrentDomain.BaseDirectory+ "Application.config", AppDomain.CurrentDomain.BaseDirectory);
            Console.ReadKey();
        }
    }
}
