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
    public class ImageResizerHandler : HttpTaskAsyncHandler
    {
        /// <summary>
        /// You will need to configure this handler in the Web.config file of your 
        /// web and register it with IIS before being able to use it. For more information
        /// see the following link: http://go.microsoft.com/?linkid=8101007
        /// </summary>
        #region IHttpHandler Members

        public override bool IsReusable
        {
            get { return false; }
        }


        private Regex sizePattern = new Regex(@"^\s*(s|w|h)(\d{0,4})(?:\-(c|m|s))?\s*$");
        private Regex sizePattern_2 = new Regex(@"^\s*w(\d{0,4})h(\d{0,4})(?:\-(c|m|s))?\s*$");
        private Regex sizePattern_3 = new Regex(@"^\s*h(\d{0,4})w(\d{0,4})(?:\-(c|m|s))?\s*$");
        public override async Task ProcessRequestAsync(HttpContext context)
        {
            string t = "t";
            try
            {

                var paths = context.Request.Path.Split('/');
                if (paths.Length < 4)
                {
                    context.Response.StatusCode = 404;
                    return;
                }
                if (paths.Length == 4)
                {
                    if (string.IsNullOrEmpty(paths[2].Trim()) || string.IsNullOrEmpty(paths[3].Trim()))
                    {
                        context.Response.StatusCode = 404;
                        return;
                    }
                }

                var containerName = paths[1];
                var size = paths[2];
                var filename = String.Join("/", paths.Skip(3));
                var matches_p1 = sizePattern.Matches(size);
                MatchCollection matches_p2 = null;
                MatchCollection matches_p3 = null;
                if (matches_p1.Count == 0)
                {
                    matches_p2 = sizePattern_2.Matches(size);
                    if (matches_p2.Count == 0)
                    {
                        matches_p3 = sizePattern_3.Matches(size);
                    }
                }

                
                bool skipresize = false;

                if (matches_p1.Count != 0)
                {
                    if (matches_p1[0].Groups.Count >= 3)
                    {
                        var num = int.Parse(matches_p1[0].Groups[2].Value);
                        if (num != 0)
                        {
                            switch (matches_p1[0].Groups[1].Value.ToLower())
                            {
                                case "w":
                                    size = "maxwidth=" + num;
                                    break;
                                case "h":
                                    size = "maxheight=" + num;
                                    break;
                                case "s":
                                    size = "width=" + num + "&height=" + num;
                                    break;
                            }
                        }
                        else
                        {
                            skipresize = true;
                        }
                        if (matches_p1[0].Groups.Count == 4)
                        {
                            switch (matches_p1[0].Groups[3].Value.ToLower())
                            {
                                case "m":
                                    size += "&mode=max";
                                    break;
                                case "s":
                                    size += "&mode=stretch";
                                    break;
                                case "c":
                                    size += "&mode=crop";
                                    break;
                            }
                        }
                    }
                }
                else if (matches_p2.Count != 0)
                {
                    if (matches_p2[0].Groups.Count >= 3)
                    {
                        var w = int.Parse(matches_p2[0].Groups[1].Value);
                        var h = int.Parse(matches_p2[0].Groups[2].Value); 
                        size = "width=" + w + "&height=" + h;
                        if (matches_p2[0].Groups.Count == 4)
                        {
                            switch (matches_p2[0].Groups[3].Value.ToLower())
                            {
                                case "m":
                                    size += "&mode=max";
                                    break;
                                case "s":
                                    size += "&mode=stretch";
                                    break;
                                case "c":
                                    size += "&mode=crop";
                                    break;
                            }
                        }
                    }
                }
                else if (matches_p3.Count != 0)
                {
                    if (matches_p3[0].Groups.Count >= 3)
                    {
                        var w = int.Parse(matches_p3[0].Groups[2].Value);
                        var h = int.Parse(matches_p3[0].Groups[1].Value);
                        size = "width=" + w + "&height=" + h;
                        if (matches_p3[0].Groups.Count == 4)
                        {
                            switch (matches_p3[0].Groups[3].Value.ToLower())
                            {
                                case "m":
                                    size += "&mode=max";
                                    break;
                                case "s":
                                    size += "&mode=stretch";
                                    break;
                                case "c":
                                    size += "&mode=crop";
                                    break;
                            }
                        }
                    }
                }
                else
                {
                    context.Response.StatusCode = 404;
                    return;
                }

                var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["storageConnection"].ConnectionString);
                var blobStorage = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobStorage.GetContainerReference(containerName);
                var isExist = await container.ExistsAsync();
                if (isExist)
                {



                    try
                    {
                        var blobs = await container.GetBlobReferenceFromServerAsync(filename);

                        var blobExist = await blobs.ExistsAsync();
                        if (!blobExist)
                        {

                            context.Response.StatusCode = 404;
                            return;
                        }
                        else
                        {
                            if (!blobs.Properties.ContentType.StartsWith("image/"))
                            {
                                context.Response.StatusCode = 404;
                                return;
                            }
                            Stream blobStream = await blobs.OpenReadAsync();
                            context.Response.AddHeader("Content-Type", blobs.Properties.ContentType);
                            if (skipresize)
                            {
                                var dataToRead = blobStream.Length;
                                byte[] buffer = new byte[1000];
                                while (dataToRead > 0)
                                {
                                    if (context.Response.IsClientConnected)
                                    {
                                        var length = blobStream.Read(buffer, 0, 1000);
                                        context.Response.OutputStream.Write(buffer, 0, length);
                                        context.Response.OutputStream.Flush();
                                        dataToRead = dataToRead - length;
                                    }
                                    else
                                    {
                                        dataToRead = -1;
                                    }
                                }
                            }
                            else
                            {
                                var settings = new ResizeSettings(size);
                                ImageResizer.ImageBuilder.Current.Build(blobStream, context.Response.OutputStream, settings);
                            }
                            blobStream.Close();

                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        context.Response.StatusCode = 404;

                        return;
                    }

                }
                else
                {
                    context.Response.StatusCode = 404;
                    return;
                }
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 404;
                return;
            }
           

        }

        #endregion
    }
}