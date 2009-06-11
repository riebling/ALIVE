using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace HttpServer.Handlers
{
    public class FileHandler
    {
        private HttpListener _server;
        private readonly string _baseUri;
        private readonly string _basePath;
        private readonly bool _useLastModifiedHeader;
        private readonly IDictionary<string, string> _mimeTypes = new Dictionary<string, string>();
        private static readonly string[] DefaultForbiddenChars = new[] { "\\", "..", ":" };
        private string[] _forbiddenChars;
        private static readonly string PathSeparator = Path.DirectorySeparatorChar.ToString();

        /// <summary>
        /// List with all mime-type that are allowed. 
        /// </summary>
        /// <remarks>All other mime types will result in a Forbidden HTTP status code.</remarks>
        public IDictionary<string, string> MimeTypes
        {
            get { return _mimeTypes; }
        }

        /// <summary>
        /// Characters that may not exist in the request path.
        /// </summary>
        /// <example>
        /// fileMod.ForbiddenChars = new string[]{ "\\", "..", ":" };
        /// </example>
        public string[] ForbiddenChars
        {
            get { return _forbiddenChars; }
            set { _forbiddenChars = value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileHandler"/> class.
        /// </summary>
        /// <param name="server">HTTP server to attach to</param>
        /// <param name="baseUri">Uri to serve, for instance "/files/"</param>
        /// <param name="basePath">Path on hard drive where we should start looking for files</param>
        /// <param name="useLastModifiedHeader">If true a Last-Modifed header will be sent upon requests urging webbrowser to cache files</param>
        public FileHandler(HttpListener server, string baseUri, string basePath, bool useLastModifiedHeader)
        {
            Check.Require(server, "server");
            Check.Require(baseUri, "baseUri");
            Check.Require(basePath, "basePath");

            _server = server;
        	_useLastModifiedHeader = useLastModifiedHeader;
            _baseUri = baseUri;
            _basePath = basePath;
			if (!_basePath.EndsWith(PathSeparator))
				_basePath += PathSeparator;
            ForbiddenChars = DefaultForbiddenChars;

            AddDefaultMimeTypes();

            server.AddHandler("GET", null, "^" + baseUri, RequestHandler);
        }

        /// <summary>
        /// MIME types that this class can handle per default
        /// </summary>
        private void AddDefaultMimeTypes()
        {
            MimeTypes.Add("default", "application/octet-stream");
            MimeTypes.Add("txt", "text/plain");
            MimeTypes.Add("html", "text/html");
            MimeTypes.Add("htm", "text/html");
            MimeTypes.Add("jpg", "image/jpg");
            MimeTypes.Add("jpeg", "image/jpg");
            MimeTypes.Add("bmp", "image/bmp");
            MimeTypes.Add("gif", "image/gif");
            MimeTypes.Add("png", "image/png");

            MimeTypes.Add("ico", "image/vnd.microsoft.icon");
            MimeTypes.Add("css", "text/css");
            MimeTypes.Add("gzip", "application/x-gzip");
            MimeTypes.Add("zip", "multipart/x-zip");
            MimeTypes.Add("tar", "application/x-tar");
            MimeTypes.Add("pdf", "application/pdf");
            MimeTypes.Add("rtf", "application/rtf");
            MimeTypes.Add("xls", "application/vnd.ms-excel");
            MimeTypes.Add("ppt", "application/vnd.ms-powerpoint");
            MimeTypes.Add("doc", "application/application/msword");
            MimeTypes.Add("js", "application/javascript");
            MimeTypes.Add("au", "audio/basic");
            MimeTypes.Add("snd", "audio/basic");
            MimeTypes.Add("es", "audio/echospeech");
            MimeTypes.Add("mp3", "audio/mpeg");
            MimeTypes.Add("mp2", "audio/mpeg");
            MimeTypes.Add("mid", "audio/midi");
            MimeTypes.Add("wav", "audio/x-wav");
            MimeTypes.Add("swf", "application/x-shockwave-flash");
            MimeTypes.Add("avi", "video/avi");
            MimeTypes.Add("rm", "audio/x-pn-realaudio");
            MimeTypes.Add("ram", "audio/x-pn-realaudio");
            MimeTypes.Add("aif", "audio/x-aiff");
        }

        /// <summary>
        /// Determines if the request should be handled by this module.
        /// Invoked by the HttpServer
        /// </summary>
        /// <param name="uri"></param>
        /// <returns>true if this module should handle it.</returns>
        public bool CanHandle(Uri uri)
        {
            if (Contains(uri.AbsolutePath, _forbiddenChars))
                return false;

            string path = GetPath(uri);
            return
                uri.AbsolutePath.StartsWith(_baseUri) && // Correct directory
                File.Exists(path) && // File exists
                (File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0; // Not a symlink
        }

        private string GetPath(Uri uri)
        {
            if (Contains(uri.AbsolutePath, _forbiddenChars))
                return null;

            string path = _basePath + uri.AbsolutePath.Substring(_baseUri.Length);
            return path.Replace('/', Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Check if source contains any of the chars.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="chars"></param>
        /// <returns></returns>
        private static bool Contains(string source, IEnumerable<string> chars)
        {
            foreach (string s in chars)
            {
                if (source.Contains(s))
                    return true;
            }

            return false;
        }

        private bool RequestHandler(IHttpClientContext context, IHttpRequest request, IHttpResponse response)
        {
            response.Status = HttpStatusCode.NotFound;

            if (!CanHandle(request.Uri))
                return true;

            try
            {
                string path = GetPath(request.Uri);
                if (path == null)
                    return true;
                string extension = GetFileExtension(path);
                if (extension == null)
                    return true;

                if (MimeTypes.ContainsKey(extension))
                    response.ContentType = MimeTypes[extension];
                else
                    return true;

                using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    response.Status = HttpStatusCode.OK;

                    if (!string.IsNullOrEmpty(request.Headers["if-Modified-Since"]))
                    {
                        DateTime lastRequest = DateTime.Parse(request.Headers["if-Modified-Since"]);
                        if (lastRequest.CompareTo(File.GetLastWriteTime(path)) <= 0)
                            response.Status = HttpStatusCode.NotModified;
                    }

                    if (_useLastModifiedHeader)
                        response.AddHeader("Last-modified", File.GetLastWriteTime(path).ToString("r"));
                    response.ContentLength = stream.Length;
                    response.SendHeaders();

                    if (request.Method != "Headers" && response.Status != HttpStatusCode.NotModified)
                    {
                        byte[] buffer = new byte[8192];
                        int bytesRead = stream.Read(buffer, 0, 8192);
                        while (bytesRead > 0)
                        {
                            response.SendBody(buffer, 0, bytesRead);
                            bytesRead = stream.Read(buffer, 0, 8192);
                        }
                    }
                }
            }
            catch (FileNotFoundException)
            {
                response.Status = HttpStatusCode.NotFound;
            }

            return true;
        }

        /// <summary>
        /// return a file extension from an absolute uri path (or plain filename)
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        private static string GetFileExtension(string uri)
        {
            int pos = uri.LastIndexOf('.');
            return pos == -1 ? null : uri.Substring(pos + 1);
        }
    }
}
