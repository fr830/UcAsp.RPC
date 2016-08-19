/***************************************************
*创建人:TecD02
*创建时间:2016/8/15 14:05:45
*功能说明:<Function>
*版权所有:<Copyright>
*Frameworkversion:4.0
*CLR版本：4.0.30319.42000
***************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFace;
namespace Face
{
    public class Test2:ITest2
    {
        private string Program_Id = "c55f679a-8596-4a93-9aa3-591c33e90c0c";
        public int GetMore(int code)
        {
            throw new Exception("错误");
            return code;
        }
    }
}
