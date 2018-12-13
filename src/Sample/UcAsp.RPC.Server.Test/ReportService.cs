using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UcAsp.WebSocket;
using UcAsp.WebSocket.Server;
using FastReport;
using FastReport.Web;


namespace UcAsp.RPC.Server.Test
{
    public class ReportService : WebSocketBehavior
    {
        protected override void OnGet(HttpRequestEventArgs ev)
        {
          
          WebReport webReport = new WebReport();
            
            webReport.Width = 1000;
            webReport.Height = 600;
            webReport.ToolbarIconsStyle = ToolbarIconsStyle.Black;
            webReport.ToolbarIconsStyle = ToolbarIconsStyle.Black;
            webReport.PrintInBrowser = true;
            webReport.PrintInPdf = true;
            webReport.ShowExports = true;
            webReport.ShowPrint = true;
            webReport.SinglePage = true;
            webReport.Report.Load(@"G:\GitDev\RPC\src\Sample\UcAsp.RPC.Server.Test\bin\Debug\wwwroot\report\temp\Report.frx");

            webReport.DesignerPath = @"G:\GitDev\RPC\src\Sample\UcAsp.RPC.Server.Test\bin\Debug\wwwroot\WebReportDesigner/index.html";
            webReport.DesignReport = true;
            webReport.DesignScriptCode = true;
            webReport.DesignerSavePath = @"G:\GitDev\RPC\src\Sample\UcAsp.RPC.Server.Test\bin\Debug\wwwroot\Theme\content\report\temp\";
            webReport.DesignerSaveCallBack = @"G:\GitDev\RPC\src\Sample\UcAsp.RPC.Server.Test\bin\Debug\wwwroot\Report\Manager\SaveDesignedReport";
            webReport.ID = "1";
            string html = webReport.GetHtml();
            

            ev.Response.WriteContent(Encoding.UTF8.GetBytes(html));
        }
    }
}
