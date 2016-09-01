/***************************************************
*创建人:TecD02
*创建时间:2016/8/2 15:27:35
*功能说明:<Function>
*版权所有:<Copyright>
*Frameworkversion:4.0
*CLR版本：4.0.30319.42000
***************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFace
{
    [Serializable]
    public class Imodel
    {
        private string Program_Id = "6ed95b67-7a65-4238-bd94-a7de0b534694";

        public string Message { get; set; }

        public int Code { get; set; }

        public string Codes(string m)
        {
            return m;
        }
    }
}
