using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
namespace UcAsp.RPC.Service
{
    public class RouterMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IRouterBuilder _router;
        public RouterMiddleware(RequestDelegate next, IRouterBuilder router)
        {
            _next = next;
            _router = router;
        }
        public async Task Invoke(HttpContext context)
        {
            Console.WriteLine(context.Request.Path);
            string requestpath = context.Request.Path;
            foreach (KeyValuePair<string, MapRoute> pair in MapRouteBuilderExtensions.Routers)
            {
                Regex regex = new Regex(pair.Key,RegexOptions.IgnoreCase);

                if (regex.IsMatch(requestpath))
                {
                    context.Response.Headers.Add("Server", "ISCS.WCS.Net/1.0");
                    context.Response.Headers.Add("Author", "Rixiang.Yu");
                    pair.Value.Defaults.Executer(context);
                    await _next(context);
                    return;
                }

            }

            await _next(context);
            return;


        }
    }
}
