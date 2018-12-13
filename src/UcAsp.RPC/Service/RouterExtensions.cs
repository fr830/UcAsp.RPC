using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Builder;
namespace UcAsp.RPC.Service
{
    public static class RouterExtensions
    {
        public static IApplicationBuilder UseUcAspRouter(this IApplicationBuilder app)
        {
            return UseUcAspRouter(app, null);
        }
        public static IApplicationBuilder UseUcAspRouter(this IApplicationBuilder app, Action<IRouterBuilder> action)
        {
            var router = new EndpointRouteBuilder(app);
            action(router);
            return app.UseMiddleware<RouterMiddleware>(router);
        }
        private class EndpointRouteBuilder : IRouterBuilder
        {
            public EndpointRouteBuilder(IApplicationBuilder app)
            {

            }

        }

    }

}
