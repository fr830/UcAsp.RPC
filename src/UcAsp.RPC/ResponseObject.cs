/***************************************************
*创建人:TecD02
*创建时间:2016/7/30 18:47:54
*功能说明:<Function>
*版权所有:<Copyright>
*Frameworkversion:4.0
*CLR版本：4.0.30319.42000
***************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UcAsp.RPC
{
    [Serializable]
    public class ResponseObject
    {
        private string Program_Id = "2faef033-02ea-4379-a3c4-9f568ad84fad";
        public bool Success { get; set; }

        public string ErrorMessage { get; set; }

        public Object Data { get; set; }
    }
}
