using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Threading;

namespace SGet
{
    public class DownloadManager
    {
        // Class instance, used to access non-static members
        private static DownloadManager instance = new DownloadManager();
        public static MainWindow mainWindow;
        public static DownloadManager Instance
        {
            get
            {
                return instance;
            }
        }

        private static NumberFormatInfo numberFormat = NumberFormatInfo.InvariantInfo;

        // Collection which contains all download clients, bound to the DataGrid control
        public ObservableCollection<WebDownloadClient> DownloadsList = new ObservableCollection<WebDownloadClient>();

        #region Properties

        // Number of currently active downloads
        public int ActiveDownloads
        {
            get
            {
                int active = 0;
                foreach (WebDownloadClient d in DownloadsList)
                {
                    if (!d.HasError)
                        if (d.Status == DownloadStatus.Waiting || d.Status == DownloadStatus.Downloading)
                            active++;
                }
                return active;
            }
        }

        // Number of completed downloads
        public int CompletedDownloads
        {
            get
            {
                int completed = 0;
                foreach (WebDownloadClient d in DownloadsList)
                {
                    if (d.Status == DownloadStatus.Completed)
                        completed++;
                }
                return completed;
            }
        }

        // Total number of downloads in the list
        public int TotalDownloads
        {
            get
            {
                return DownloadsList.Count;
            }
        }

        #endregion

        #region Methods

        // Format file size or downloaded size string
        public static string FormatSizeString(long byteSize)
        {
            double kiloByteSize = (double)byteSize / 1024D;
            double megaByteSize = kiloByteSize / 1024D;
            double gigaByteSize = megaByteSize / 1024D;

            if (byteSize < 1024)
                return String.Format(numberFormat, "{0} B", byteSize);
            else if (byteSize < 1048576)
                return String.Format(numberFormat, "{0:0.00} kB", kiloByteSize);
            else if (byteSize < 1073741824)
                return String.Format(numberFormat, "{0:0.00} MB", megaByteSize);
            else
                return String.Format(numberFormat, "{0:0.00} GB", gigaByteSize);
        }

        // Format download speed string
        public static string FormatSpeedString(int speed)
        {
            float kbSpeed = (float)speed / 1024F;
            float mbSpeed = kbSpeed / 1024F;

            if (speed <= 0)
                return String.Empty;
            else if (speed < 1024)
                return speed.ToString() + " B/s";
            else if (speed < 1048576)
                return kbSpeed.ToString("#.00", numberFormat) + " kB/s";
            else
                return mbSpeed.ToString("#.00", numberFormat) + " MB/s";
        }

        // Format time span string so it can display values of more than 24 hours
        public static string FormatTimeSpanString(TimeSpan span)
        {
            string hours = ((int)span.TotalHours).ToString();
            string minutes = span.Minutes.ToString();
            string seconds = span.Seconds.ToString();
            if ((int)span.TotalHours < 10)
                hours = "0" + hours;
            if (span.Minutes < 10)
                minutes = "0" + minutes;
            if (span.Seconds < 10)
                seconds = "0" + seconds;

            return String.Format("{0}:{1}:{2}", hours, minutes, seconds);
        }
        public static string Add(AddFileDto dto)
        {
            WebDownloadClient download = new WebDownloadClient(dto.Url.Trim());

            download.FileName = dto.FileName.Trim();

            // Register WebDownloadClient events
            download.DownloadProgressChanged += download.DownloadProgressChangedHandler;
            download.DownloadCompleted += download.DownloadCompletedHandler;
            download.PropertyChanged += mainWindow.PropertyChangedHandler;
            download.StatusChanged += mainWindow.StatusChangedHandler;
            download.DownloadCompleted += mainWindow.DownloadCompletedHandler;

            // Create path to temporary file
            if (!Directory.Exists(dto.SaveFolder))
            {
                Directory.CreateDirectory(dto.SaveFolder);
            }
            string filePath = Path.Combine(dto.SaveFolder, download.FileName);
            string tempPath = filePath + ".tmp";
            if (File.Exists(filePath))
            {
                return "There is already a file with the same name, please change the file name or download folder.";
            }
            // Check if there is already an ongoing download on that path
            if (File.Exists(tempPath))
            {
                return "There is already a download in progress at the specified path.";
            }
            // Set username and password if HTTP authentication is required
            if (dto.ServerLogin && (dto.UserName.Trim().Length > 0) && (dto.Password.Trim().Length > 0))
            {
                download.ServerLogin = new NetworkCredential(dto.UserName.Trim(), dto.Password.Trim());
            }

            // Check the URL
            download.CheckUrl();
            //if (download.HasError)
            //    return;

            download.TempDownloadPath = tempPath;

            download.AddedOn = DateTime.UtcNow;
            download.CompletedOn = DateTime.MinValue;
            download.OpenFileOnCompletion = dto.OpenFileOnCompletion;

            // Add the download to the downloads list
            Application.Current.Dispatcher.Invoke(() => Instance.DownloadsList.Add(download));
            // Start downloading the file
            if (dto.StartImmediately)
                download.Start();
            else
                download.Status = DownloadStatus.Paused;
            return "";
        }
        #endregion
    }
}
