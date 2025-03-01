
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System;
using SGet.Properties;
using Newtonsoft.Json;
using System.Linq;
using System.Windows;

namespace SGet
{
    public class LocalApiServer
    {
        private readonly HttpListener _listener;
        private CancellationTokenSource _cancellationTokenSource;

        public LocalApiServer()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{Settings.Default.ApiPort}/api/");
        }

        public void Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => Listen(_cancellationTokenSource.Token));
            Console.WriteLine($"[Server] Listening on http://localhost:{Settings.Default.ApiPort}/api/");
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _listener.Stop();
            Console.WriteLine("[Server] Stopped.");
        }

        private async Task Listen(CancellationToken cancellationToken)
        {
            _listener.Start();

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    ProcessRequest(context);
                }
                catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        private async void ProcessRequest(HttpListenerContext context)
        {
            if (context.Request.HttpMethod == "POST")
            {
                using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
                {
                    string requestBody = await reader.ReadToEndAsync();
                    var methodName = context.Request.Url.AbsolutePath.TrimEnd('/').Split('/').LastOrDefault().ToLower();
                    switch (methodName)
                    {
                        case "addfile":
                            AddFile(context, requestBody);
                            break;
                        case "start":
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                DownloadManager.mainWindow.cmStartAll_Click(null, null);
                                SendResponse(context, 200, "");
                            });
                            break;
                        default:
                            break;
                    }


                }
            }
            else
            {
                SendResponse(context, 404, "Not found");
            }
        }

        private void AddFile(HttpListenerContext context, string requestBody)
        {
            var requestData = JsonConvert.DeserializeObject<AddFileDto>(requestBody);
            if (requestData != null && !string.IsNullOrWhiteSpace(requestData.Url))
            {
                if (requestData.ShowDialog)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                {
                    NewDownload newDownloadDialog = new NewDownload(requestData, DownloadManager.mainWindow);
                    newDownloadDialog.ShowDialog();
                });
                    SendResponse(context, 200, "");
                }
                else
                {
                    var result = DownloadManager.Add(requestData);
                    SendResponse(context, 200, result);
                }
            }
            else
            {
                SendResponse(context, 400, "Invalid request");
            }
        }

        private void SendResponse(HttpListenerContext context, int statusCode, string message)
        {
            context.Response.StatusCode = statusCode;
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.Close();
        }
    }
}
