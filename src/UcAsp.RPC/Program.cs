using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using ISCS.WCS.WebSocket;
namespace ISCS.WCS.RPC
{
    class Program
    {
        static void Main(string[] args)
        {
            
            ApplicationContext context = new ApplicationContext();
            context.Start(AppDomain.CurrentDomain.BaseDirectory + "Application.config", AppDomain.CurrentDomain.BaseDirectory);
            Console.ReadKey();
        }
    }
}
