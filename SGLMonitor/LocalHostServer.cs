using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using MDPGen.Core.Services;

namespace SGLMonitor
{
    /// <summary>
    /// This is a LocalHost simple HTTP server which serves up our
    /// content. It's based on the built-in HttpListener class which works with http.sys.
    /// </summary>
    public class LocalHostServer : IDisposable
    {
        private HttpListener httpListener;
        private volatile bool running;
        private readonly string baseDir;

        /// <summary>
        /// The default file.
        /// </summary>
        public string DefaultFile { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="baseDir">Directory to serve files from</param>
        public LocalHostServer(string baseDir)
        {
            this.DefaultFile = "default.html";
            this.baseDir = baseDir;
            running = false;
        }

        /// <summary>
        /// Starts the HttpListener
        /// </summary>
        /// <param name="port">Port to listen on, assumes all files</param>
        /// <returns>Task</returns>
        public Task Run(int port)
        {
            string url = $"http://*:{port}/";

            httpListener = new HttpListener();
            httpListener.Prefixes.Add(url);

            try
            {
                httpListener.Start();
            }
            catch (HttpListenerException ex)
            {
                TraceLog.Write(TraceType.Error, $"Failed to start HttpListener: {ex.ErrorCode} - {ex.Message}");

                AddAddress(url, Environment.UserDomainName, Environment.UserName);
                // Have to recreate it; for some reason the above error disposes the listener.
                httpListener = new HttpListener();
                httpListener.Prefixes.Add(url);
                try
                {
                    httpListener.Start();
                }
                catch (HttpListenerException innerException)
                {
                    return Task.FromException<HttpListenerException>(innerException);
                }
            }

            running = true;

            return Task.Run(() =>
            {
                while (running)
                {
                    HttpListenerContext ctx = httpListener.GetContext();
                    Task.Run(async () => {
                        try
                        {
                            await ProcessRequest(ctx);
                        }
                        catch (Exception)
                        {
                           ctx.Response.Abort();
                        }
                    });
                }
            });
        }

        /// <summary>
        /// Stops the listener
        /// </summary>
        public void Stop()
        {
            if (running)
            {
                httpListener?.Stop();
                running = false;
            }
        }

        /// <summary>
        /// Processes a single request from the web server
        /// </summary>
        /// <param name="ctx">Request Context</param>
        /// <returns>Task</returns>
        private async Task ProcessRequest(HttpListenerContext ctx)
        {
            if (ctx.Request.HttpMethod == "GET")
            {
                string url = ctx.Request.Url.AbsolutePath;
                if (url == "/") // root?
                    url = DefaultFile;

                string filename = Path.Combine(baseDir, GetFilename(url));
                if (filename.EndsWith(@"\"))
                    filename += DefaultFile;

                filename = WebUtility.UrlDecode(filename);

                if (string.IsNullOrEmpty(Path.GetExtension(filename)))
                    filename = Path.ChangeExtension(filename, ".html");

                if (!File.Exists(filename))
                {
                    TraceLog.Write(TraceType.Warning, $"GET {url} : 404 NOT FOUND");
                    ctx.Response.StatusCode = (int) HttpStatusCode.NotFound;
                    ctx.Response.StatusDescription = HttpStatusCode.NotFound.ToString();
                }
                else
                {
                    TraceLog.Write(TraceType.Normal, $"GET {url}");

                    ctx.Response.StatusCode = (int) HttpStatusCode.OK;
                    string contentType = IdentifyContent(filename);
                    if (!string.IsNullOrEmpty(contentType))
                        ctx.Response.ContentType = contentType;
                    ctx.Response.StatusDescription = HttpStatusCode.OK.ToString();
                    ctx.Response.AddHeader("Date", DateTime.UtcNow.ToString("r"));
                    ctx.Response.AddHeader("X-UA-Compatible", "IE=edge");

                    try
                    {
                        byte[] b = File.ReadAllBytes(filename);
                        ctx.Response.ContentLength64 = b.Length;
                        await ctx.Response.OutputStream.WriteAsync(b, 0, b.Length);
                    }
                    finally
                    {
                        ctx.Response.OutputStream.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Method to register our localhost address as a listening server address.
        /// This calls out to NETSH to open the port.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="domain"></param>
        /// <param name="user"></param>
        public static void AddAddress(string address, string domain, string user)
        {
            var psi = new ProcessStartInfo("netsh", $"http add urlacl url={address} user={domain}\\{user}")
            {
                Verb = "runas",
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = true
            };

            Process.Start(psi)?.WaitForExit();
        }

        /// <summary>
        /// Retrieves a local file for a given URL request
        /// </summary>
        /// <param name="requestPath">URL</param>
        /// <returns>Filename path</returns>
        private static string GetFilename(string requestPath)
        {
            return requestPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Identify a content type based on the extension
        /// </summary>
        /// <param name="filename">Filename of the file to serve</param>
        /// <returns>HTML content type</returns>
        private static string IdentifyContent(string filename)
        {
            var mimes = new Dictionary<string, string>
            {
                { ".html", "text/html; charset=UTF-8" },
                { ".jpg", "image/jpeg" },
                { ".png", "image/png" },
                { ".js", "text/javascript" },
                { ".css", "text/css" },
                { ".svg", "image/svg+xml" },
                { ".woff", "application/x-font-woff" },
                { ".woff2", "application/font-woff2" }
            };

            var extension = Path.GetExtension(filename);
            if (extension != null)
            {
                extension = extension.ToLower();
                string rc;
                if (!mimes.TryGetValue(extension, out rc))
                {
                    rc = "text/" + extension.Substring(1);
                }
                return rc;
            }

            return null;
        }

        /// <summary>
        /// Dispose the HTPP listener.
        /// </summary>
        public void Dispose()
        {
            ((IDisposable) httpListener)?.Dispose();
        }
    }
}
