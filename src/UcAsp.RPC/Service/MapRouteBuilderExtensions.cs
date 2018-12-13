using System;
using System.Collections.Generic;
using System.Text;

namespace UcAsp.RPC.Service
{
    public static class MapRouteBuilderExtensions
    {

        internal static Dictionary<string, MapRoute> Routers = new Dictionary<string, MapRoute>();
        public static IRouterBuilder MapRoute(this IRouterBuilder routeBuilder, string path, IBehavior behavior)
        {
            return MapRoute(routeBuilder, path, behavior, null);
        }
        public static IRouterBuilder MapRoute(this IRouterBuilder routeBuilder, string path, IBehavior behavior, dynamic rule)
        {
            MapRoute route = new MapRoute(path, path, behavior, rule);
            if (!Routers.ContainsKey(path))
            {
                Routers.Add(path, route);
            }
            return routeBuilder;
        }
    }
}
