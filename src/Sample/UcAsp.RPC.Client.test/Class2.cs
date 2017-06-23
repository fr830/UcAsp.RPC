using System;

using System.Collections;

using System.Collections.Generic;

using System.Reflection;

using System.Threading.Tasks;

using UcAsp.RPC;

using log4net;

using System.Diagnostics;

using IFace;
namespace Face
{

    public class Test : ProxyObject, ITest
    {

        public delegate DataEventArgs GetHandler(DataEventArgs e);
        private readonly ILog _log = LogManager.GetLogger(typeof(Test));


        public String Get(String msg, Int32 c)



        {

            Stopwatch wath = new Stopwatch();
            wath.Start();
            List<object> entity = new List<object>();

            entity.Add(msg);

            entity.Add(c);

            DataEventArgs e = new DataEventArgs();
            e.Binary = this.Serializer.ToBinary(entity);
            e.CallHashCode = e.GetHashCode();
            e.T = typeof(String);
            e.ActionParam = "IFace.ITest.Get.7FF38331ABC8212084AEA31D548CC812";

            e.ActionCmd = CallActionCmd.Call.ToString();

            DataEventArgs data = new DataEventArgs();
            try
            {

                Run.CallServiceMethod(e);
                data = Run.GetResult(e);

            }
            catch (Exception ex)

            { _log.Error(ex); }

            if (data.StatusCode != StatusCode.Success)
            {

                _log.Error(data.ActionCmd + ": " + data.ActionParam + ": " + data.StatusCode + ": " + data.LastError);
                Exception ex = new Exception("Call Service Method " + data.ActionCmd + ": " + data.ActionParam + ": " + data.StatusCode + ": " + data.LastError);

                throw (ex);

            }

            wath.Stop();
            msg = data.Param[0].ToString();
            c = new JsonSerializer().ToEntity<Int32>(data.Param[1].ToString());
            _log.Info(e.ActionParam + ":" + e.CallHashCode + ":" + e.TaskId + ":" + wath.ElapsedMilliseconds);
            if (!string.IsNullOrEmpty(e.Json))
            {
                return this.Serializer.ToEntity<String>(e.Json);

            }
            else
            {
                return this.Serializer.ToEntity<String>(data.Binary);

            }
        }



        public List<String> Good(String yun, String mm, String kkk)



        {

            Stopwatch wath = new Stopwatch();
            wath.Start();
            List<object> entity = new List<object>();

            entity.Add(yun);

            entity.Add(mm);

            entity.Add(kkk);

            DataEventArgs e = new DataEventArgs();
            e.Binary = this.Serializer.ToBinary(entity);
            e.CallHashCode = e.GetHashCode();
            e.T = typeof(List<String>);
            e.ActionParam = "IFace.ITest.Good.849DB38753B6699405589F852FCAD7CA";

            e.ActionCmd = CallActionCmd.Call.ToString();

            DataEventArgs data = new DataEventArgs();
            try
            {

                Run.CallServiceMethod(e);
                data = Run.GetResult(e);

            }
            catch (Exception ex)

            { _log.Error(ex); }

            if (data.StatusCode != StatusCode.Success)
            {

                _log.Error(data.ActionCmd + ": " + data.ActionParam + ": " + data.StatusCode + ": " + data.LastError);
                Exception ex = new Exception("Call Service Method " + data.ActionCmd + ": " + data.ActionParam + ": " + data.StatusCode + ": " + data.LastError);

                throw (ex);

            }

            wath.Stop();
            yun = data.Param[0].ToString();
            mm = data.Param[1].ToString();
            kkk = data.Param[2].ToString();
            _log.Info(e.ActionParam + ":" + e.CallHashCode + ":" + e.TaskId + ":" + wath.ElapsedMilliseconds);
            if (!string.IsNullOrEmpty(e.Json))
            {
                return this.Serializer.ToEntity<List<String>>(e.Json);

            }
            else
            {
                return this.Serializer.ToEntity<List<String>>(data.Binary);

            }
        }



        public Int32 GetInt(Int32 i)



        {

            Stopwatch wath = new Stopwatch();
            wath.Start();
            List<object> entity = new List<object>();

            entity.Add(i);

            DataEventArgs e = new DataEventArgs();
            e.Binary = this.Serializer.ToBinary(entity);
            e.CallHashCode = e.GetHashCode();
            e.T = typeof(Int32);
            e.ActionParam = "IFace.ITest.GetInt.BBD34525B0D9E245872EEF6469C93F4A";

            e.ActionCmd = CallActionCmd.Call.ToString();

            DataEventArgs data = new DataEventArgs();
            try
            {

                Run.CallServiceMethod(e);
                data = Run.GetResult(e);

            }
            catch (Exception ex)

            { _log.Error(ex); }

            if (data.StatusCode != StatusCode.Success)
            {

                _log.Error(data.ActionCmd + ": " + data.ActionParam + ": " + data.StatusCode + ": " + data.LastError);
                Exception ex = new Exception("Call Service Method " + data.ActionCmd + ": " + data.ActionParam + ": " + data.StatusCode + ": " + data.LastError);

                throw (ex);

            }

            wath.Stop();
            i = new JsonSerializer().ToEntity<Int32>(data.Param[0].ToString());
            _log.Info(e.ActionParam + ":" + e.CallHashCode + ":" + e.TaskId + ":" + wath.ElapsedMilliseconds);
            if (!string.IsNullOrEmpty(e.Json))
            {
                return this.Serializer.ToEntity<Int32>(e.Json);

            }
            else
            {
                return this.Serializer.ToEntity<Int32>(data.Binary);

            }
        }



        public Tuple<Int32> GetTuple(Int32 i)



        {

            Stopwatch wath = new Stopwatch();
            wath.Start();
            List<object> entity = new List<object>();

            entity.Add(i);

            DataEventArgs e = new DataEventArgs();
            e.Binary = this.Serializer.ToBinary(entity);
            e.CallHashCode = e.GetHashCode();
            e.T = typeof(Tuple<Int32>);
            e.ActionParam = "IFace.ITest.GetTuple.0F11127A91A6E5D6E5DDFD812EB659B4";

            e.ActionCmd = CallActionCmd.Call.ToString();

            DataEventArgs data = new DataEventArgs();
            try
            {

                Run.CallServiceMethod(e);
                data = Run.GetResult(e);

            }
            catch (Exception ex)

            { _log.Error(ex); }

            if (data.StatusCode != StatusCode.Success)
            {

                _log.Error(data.ActionCmd + ": " + data.ActionParam + ": " + data.StatusCode + ": " + data.LastError);
                Exception ex = new Exception("Call Service Method " + data.ActionCmd + ": " + data.ActionParam + ": " + data.StatusCode + ": " + data.LastError);

                throw (ex);

            }

            wath.Stop();
            i = new JsonSerializer().ToEntity<Int32>(data.Param[0].ToString());
            _log.Info(e.ActionParam + ":" + e.CallHashCode + ":" + e.TaskId + ":" + wath.ElapsedMilliseconds);
            if (!string.IsNullOrEmpty(e.Json))
            {
                return this.Serializer.ToEntity<Tuple<Int32>>(e.Json);

            }
            else
            {
                return this.Serializer.ToEntity<Tuple<Int32>>(data.Binary);

            }
        }



        public Single GetFloat(Single i)



        {

            Stopwatch wath = new Stopwatch();
            wath.Start();
            List<object> entity = new List<object>();

            entity.Add(i);

            DataEventArgs e = new DataEventArgs();
            e.Binary = this.Serializer.ToBinary(entity);
            e.CallHashCode = e.GetHashCode();
            e.T = typeof(Single);
            e.ActionParam = "IFace.ITest.GetFloat.4647866A735C524213927FACBCC86AA6";

            e.ActionCmd = CallActionCmd.Call.ToString();

            DataEventArgs data = new DataEventArgs();
            try
            {

                Run.CallServiceMethod(e);
                data = Run.GetResult(e);

            }
            catch (Exception ex)

            { _log.Error(ex); }

            if (data.StatusCode != StatusCode.Success)
            {

                _log.Error(data.ActionCmd + ": " + data.ActionParam + ": " + data.StatusCode + ": " + data.LastError);
                Exception ex = new Exception("Call Service Method " + data.ActionCmd + ": " + data.ActionParam + ": " + data.StatusCode + ": " + data.LastError);

                throw (ex);

            }

            wath.Stop();
            i = new JsonSerializer().ToEntity<Single>(data.Param[0].ToString());
            _log.Info(e.ActionParam + ":" + e.CallHashCode + ":" + e.TaskId + ":" + wath.ElapsedMilliseconds);
            if (!string.IsNullOrEmpty(e.Json))
            {
                return this.Serializer.ToEntity<Single>(e.Json);

            }
            else
            {
                return this.Serializer.ToEntity<Single>(data.Binary);

            }
        }



        public List<Nvr> GetModel(Int32 i)



        {

            Stopwatch wath = new Stopwatch();
            wath.Start();
            List<object> entity = new List<object>();

            entity.Add(i);

            DataEventArgs e = new DataEventArgs();
            e.Binary = this.Serializer.ToBinary(entity);
            e.CallHashCode = e.GetHashCode();
            e.T = typeof(List<Nvr>);
            e.ActionParam = "IFace.ITest.GetModel.0A6FED17D0D74A963D6D9FEF9505E529";

            e.ActionCmd = CallActionCmd.Call.ToString();

            DataEventArgs data = new DataEventArgs();
            try
            {

                Run.CallServiceMethod(e);
                data = Run.GetResult(e);

            }
            catch (Exception ex)

            { _log.Error(ex); }

            if (data.StatusCode != StatusCode.Success)
            {

                _log.Error(data.ActionCmd + ": " + data.ActionParam + ": " + data.StatusCode + ": " + data.LastError);
                Exception ex = new Exception("Call Service Method " + data.ActionCmd + ": " + data.ActionParam + ": " + data.StatusCode + ": " + data.LastError);

                throw (ex);

            }

            wath.Stop();
            i = new JsonSerializer().ToEntity<Int32>(data.Param[0].ToString());
            _log.Info(e.ActionParam + ":" + e.CallHashCode + ":" + e.TaskId + ":" + wath.ElapsedMilliseconds);
            if (!string.IsNullOrEmpty(e.Json))
            {
                return this.Serializer.ToEntity<List<Nvr>>(e.Json);

            }
            else
            {
                return this.Serializer.ToEntity<List<Nvr>>(data.Binary);

            }
        }



        public String ToList(List<Imodel> i)



        {

            Stopwatch wath = new Stopwatch();
            wath.Start();
            List<object> entity = new List<object>();

            entity.Add(i);

            DataEventArgs e = new DataEventArgs();
            e.Binary = this.Serializer.ToBinary(entity);
            e.CallHashCode = e.GetHashCode();
            e.T = typeof(String);
            e.ActionParam = "IFace.ITest.ToList.D1B7E7014B3CD69D0411A60DA71532F3";

            e.ActionCmd = CallActionCmd.Call.ToString();

            DataEventArgs data = new DataEventArgs();
            try
            {

                Run.CallServiceMethod(e);
                data = Run.GetResult(e);

            }
            catch (Exception ex)

            { _log.Error(ex); }

            if (data.StatusCode != StatusCode.Success)
            {

                _log.Error(data.ActionCmd + ": " + data.ActionParam + ": " + data.StatusCode + ": " + data.LastError);
                Exception ex = new Exception("Call Service Method " + data.ActionCmd + ": " + data.ActionParam + ": " + data.StatusCode + ": " + data.LastError);

                throw (ex);

            }

            wath.Stop();
            i = new JsonSerializer().ToEntity<List<Imodel>>(data.Param[0].ToString());
            _log.Info(e.ActionParam + ":" + e.CallHashCode + ":" + e.TaskId + ":" + wath.ElapsedMilliseconds);
            if (!string.IsNullOrEmpty(e.Json))
            {
                return this.Serializer.ToEntity<String>(e.Json);

            }
            else
            {
                return this.Serializer.ToEntity<String>(data.Binary);

            }
        }



        public String R(ref Int32 o)



        {

            Stopwatch wath = new Stopwatch();
            wath.Start();
            List<object> entity = new List<object>();

            entity.Add(o);

            DataEventArgs e = new DataEventArgs();
            e.Binary = this.Serializer.ToBinary(entity);
            e.CallHashCode = e.GetHashCode();
            e.T = typeof(String);
            e.ActionParam = "IFace.ITest.R.B628D9C0818BDBB9F013AADB9CD46102";

            e.ActionCmd = CallActionCmd.Call.ToString();

            DataEventArgs data = new DataEventArgs();
            try
            {

                Run.CallServiceMethod(e);
                data = Run.GetResult(e);

            }
            catch (Exception ex)

            { _log.Error(ex); }

            if (data.StatusCode != StatusCode.Success)
            {

                _log.Error(data.ActionCmd + ": " + data.ActionParam + ": " + data.StatusCode + ": " + data.LastError);
                Exception ex = new Exception("Call Service Method " + data.ActionCmd + ": " + data.ActionParam + ": " + data.StatusCode + ": " + data.LastError);

                throw (ex);

            }

            wath.Stop();
            o = new JsonSerializer().ToEntity<Int32>(data.Param[0].ToString());
            _log.Info(e.ActionParam + ":" + e.CallHashCode + ":" + e.TaskId + ":" + wath.ElapsedMilliseconds);
            if (!string.IsNullOrEmpty(e.Json))
            {
                return this.Serializer.ToEntity<String>(e.Json);

            }
            else
            {
                return this.Serializer.ToEntity<String>(data.Binary);

            }
        }



        public String M(ref Boolean o, String code)



        {

            Stopwatch wath = new Stopwatch();
            wath.Start();
            List<object> entity = new List<object>();

            entity.Add(o);

            entity.Add(code);

            DataEventArgs e = new DataEventArgs();
            e.Binary = this.Serializer.ToBinary(entity);
            e.CallHashCode = e.GetHashCode();
            e.T = typeof(String);
            e.ActionParam = "IFace.ITest.M.BF60C06BDCB45E5478100137C887AC36";

            e.ActionCmd = CallActionCmd.Call.ToString();

            DataEventArgs data = new DataEventArgs();
            try
            {

                Run.CallServiceMethod(e);
                data = Run.GetResult(e);

            }
            catch (Exception ex)

            { _log.Error(ex); }

            if (data.StatusCode != StatusCode.Success)
            {

                _log.Error(data.ActionCmd + ": " + data.ActionParam + ": " + data.StatusCode + ": " + data.LastError);
                Exception ex = new Exception("Call Service Method " + data.ActionCmd + ": " + data.ActionParam + ": " + data.StatusCode + ": " + data.LastError);

                throw (ex);

            }

            wath.Stop();
            o = new JsonSerializer().ToEntity<Boolean>(data.Param[0].ToString());
            code = data.Param[1].ToString();
            _log.Info(e.ActionParam + ":" + e.CallHashCode + ":" + e.TaskId + ":" + wath.ElapsedMilliseconds);
            if (!string.IsNullOrEmpty(e.Json))
            {
                return this.Serializer.ToEntity<String>(e.Json);

            }
            else
            {
                return this.Serializer.ToEntity<String>(data.Binary);

            }
        }



        public String X(out Int32 m, out Int32 x, ref Boolean o)



        {

            Stopwatch wath = new Stopwatch();
            wath.Start();
            List<object> entity = new List<object>();
           
            entity.Add(m);

            entity.Add(x);

            entity.Add(o);

            DataEventArgs e = new DataEventArgs();
            e.Binary = this.Serializer.ToBinary(entity);
            e.CallHashCode = e.GetHashCode();
            e.T = typeof(String);
            e.ActionParam = "IFace.ITest.X.D1958591891A2A6F01DF442B5CFBD851";

            e.ActionCmd = CallActionCmd.Call.ToString();

            DataEventArgs data = new DataEventArgs();
            try
            {

                Run.CallServiceMethod(e);
                data = Run.GetResult(e);

            }
            catch (Exception ex)

            { _log.Error(ex); }

            if (data.StatusCode != StatusCode.Success)
            {

                _log.Error(data.ActionCmd + ": " + data.ActionParam + ": " + data.StatusCode + ": " + data.LastError);
                Exception ex = new Exception("Call Service Method " + data.ActionCmd + ": " + data.ActionParam + ": " + data.StatusCode + ": " + data.LastError);

                throw (ex);

            }

            wath.Stop();
            m = new JsonSerializer().ToEntity<Int32>(data.Param[0].ToString());
            x = new JsonSerializer().ToEntity<Int32>(data.Param[1].ToString());
            o = new JsonSerializer().ToEntity<Boolean>(data.Param[2].ToString());
            _log.Info(e.ActionParam + ":" + e.CallHashCode + ":" + e.TaskId + ":" + wath.ElapsedMilliseconds);
            if (!string.IsNullOrEmpty(e.Json))
            {
                return this.Serializer.ToEntity<String>(e.Json);

            }
            else
            {
                return this.Serializer.ToEntity<String>(data.Binary);

            }
        }

    }
}