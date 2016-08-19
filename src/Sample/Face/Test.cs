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
using System.Threading;
using IFace;
namespace Face
{
    public class Test : ITest
    {
        public string ToList(List<Imodel> i)
        {
          //  Thread.Sleep(1000);

            return i[0].Code.ToString() + i[0].Message + i[0].Codes(i[0].Message);
        }
        public string Get(string msg, int c)
        {
           // Thread.Sleep(2000);
            return msg + c.ToString();
        }
        /// <summary>
        /// 获取过得
        /// </summary>
        /// <param name="yun">文字</param>
        /// <param name="mm">字符</param>
        /// <param name="kkk">表格</param>
        /// <returns>数据列</returns>
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
           // Thread.Sleep(1500);
            return i;
        }
        public Tuple<int> GetTuple(int i)
        {
            //Thread.Sleep(3000);
            Tuple<int> t = new Tuple<int>(i + 100000);
            return t;
        }

        public float GetFloat(float i)
        {
           // Thread.Sleep(1200);
            return i;
        }
        public List<Imodel> GetModel(int i)
        {
           // Thread.Sleep(2300);
            List<Imodel> list = new List<Imodel>();
            Imodel model = new Imodel { Code = i, Message = "测试" };
            list.Add(model);
            return list;
        }
    }
}
