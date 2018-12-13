using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace UcAsp.RPC.Service
{
    public interface IBehavior
    {
        void Executer(HttpContext context);
    }
}
