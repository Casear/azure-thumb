using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Web;
using System.Web.Routing;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Configuration;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using ImageResizer;
using System.Linq;
using System.Text.RegularExpressions;

namespace Thumb
{
    public class KeepAliveHandler : HttpTaskAsyncHandler
    {
        /// <summary>
        /// You will need to configure this handler in the Web.config file of your 
        /// web and register it with IIS before being able to use it. For more information
        /// see the following link: http://go.microsoft.com/?linkid=8101007
        /// </summary>
        #region IHttpHandler Members

        public override bool IsReusable
        {
            get { return true; }
        }


       
        public override async Task ProcessRequestAsync(HttpContext context)
        {
           

          
            context.Response.AddHeader("Content-Type", "application/json");
            context.Response.Write("{alive:true}");
            return;
            
        }

        #endregion
    }
}