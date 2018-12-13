using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot;
using Ocelot.DependencyInjection;
using System.IO;
using Ocelot.Middleware;
using Microsoft.AspNetCore.Builder;
using UcAsp.RPC.Service;

namespace UcAsp.RPC.Server.Test
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
