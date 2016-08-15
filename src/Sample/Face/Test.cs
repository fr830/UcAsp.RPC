/***************************************************
*创建人:TecD02
*创建时间:2016/8/5 13:52:27
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
    public class Test : ITest
    {

        public string Get(string msg, int c)
        {
            return msg + c.ToString();
        }
        public List<string> Good(string yun, string mm, string kkk)
        {
            List<string> list = new List<string>();
            list.Add(yun);
            list.Add(mm);
            list.Add(kkk);
            return list;
        }

        public int GetInt(int i)
        {
            return i;
        }
        public Tuple<int> GetTuple(int i)
        {
            Tuple<int> t = new Tuple<int>(i + 100000);
            return t;
        }

        public float GetFloat(float i)
        {
            return i;
        }
        public List<Imodel> GetModel(int i)
        {
            List<Imodel> list = new List<Imodel>();
            Imodel model = new Imodel { Code = i, Message = "测试" };
            list.Add(model);
            return list;
        }
    }
}
