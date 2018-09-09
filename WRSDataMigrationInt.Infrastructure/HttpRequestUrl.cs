using System;
using System.IO;
using System.Net;
using System.Text;

namespace WRSDataMigrationInt.Infrastructure
{
    public class HttpRequestUrl
    {
        public static string PostRequest(string remoteUrl, string postData, int timeOut = 60000, string encode = "UTF-8", string contentType = "application/x-www-form-urlencoded")
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            string str = "";
            System.Net.HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(remoteUrl);
            byte[] bytes = Encoding.GetEncoding(encode).GetBytes(postData);
            httpWebRequest.Method = "Post";
            httpWebRequest.ContentType = contentType;
            httpWebRequest.ContentLength = (long)bytes.Length;
            httpWebRequest.Timeout = timeOut;
            Stream requestStream = httpWebRequest.GetRequestStream();
            requestStream.Write(bytes, 0, bytes.Length);
            requestStream.Close();
            Stream responseStream = httpWebRequest.GetResponse().GetResponseStream();
            if (responseStream != null)
            {
                StreamReader streamReader = new StreamReader(responseStream, Encoding.GetEncoding(encode));
                str = streamReader.ReadToEnd();
                streamReader.Close();
                responseStream.Close();
            }
            return str;
        }

        public static string PostSecurityRequest(string remoteUrl, string postData, string userName, string userSecret, string domain, int timeOut = 60000, string encode = "UTF-8", string contentType = "application/x-www-form-urlencoded")
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            string str = "";
            System.Net.HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(remoteUrl);
            byte[] bytes = Encoding.GetEncoding(encode).GetBytes(postData);
            httpWebRequest.Method = "Post";
            httpWebRequest.ContentType = contentType;
            httpWebRequest.ContentLength = (long)bytes.Length;
            httpWebRequest.Timeout = timeOut;
            httpWebRequest.UseDefaultCredentials = false;
            httpWebRequest.PreAuthenticate = false;
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;


            //// Skip validation of SSL/TLS certificate
            //ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            if (domain != string.Empty)
            {
                httpWebRequest.Credentials = new NetworkCredential(userName, userSecret, domain);
            }
            else
            {
                httpWebRequest.Credentials = new NetworkCredential(userName, userSecret);
            }
            Stream requestStream = httpWebRequest.GetRequestStream();
            requestStream.Write(bytes, 0, bytes.Length);
            requestStream.Close();
            Stream responseStream = httpWebRequest.GetResponse().GetResponseStream();
            if (responseStream != null)
            {
                StreamReader streamReader = new StreamReader(responseStream, Encoding.GetEncoding(encode));
                str = streamReader.ReadToEnd();
                streamReader.Close();
                responseStream.Close();
            }
            return str;
        }

        public static string GetRequest(string remoteUrl, string AuthHeader = "", bool isProxy = false, int timeOut = 60000, string encode = "UTF-8", string contentType = "application/json")
        {
            string responseText = "";
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(remoteUrl);
                httpWebRequest.Method = "GET";
                httpWebRequest.ContentType = contentType;
                if (!string.IsNullOrEmpty(AuthHeader))
                {
                    httpWebRequest.Headers.Add(HttpRequestHeader.Authorization, AuthHeader);
                }
                httpWebRequest.Timeout = timeOut;
                if (isProxy)
                {
                    //var proxyAddress = ConfigurationManager.AppSettings["proxyAddress"];
                    //var proxyPort = ConfigurationManager.AppSettings["proxyPort"];
                    //if (!string.IsNullOrEmpty(proxyAddress))
                    //{
                    //    WebProxy webProxy = new WebProxy();
                    //    Uri url = new Uri("http://" + proxyAddress + ":" + proxyPort);
                    //    webProxy.Address = url;
                    //    httpWebRequest.Proxy = webProxy;
                    //}
                }
                HttpWebResponse response = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var reader = new System.IO.StreamReader(response.GetResponseStream(), Encoding.GetEncoding(encode)))
                {
                    responseText = reader.ReadToEnd();
                }
                response.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return responseText;
        }

        public static string GetSecurityRequest(string remoteUrl, string userName, string password, string domain,
            bool isProxy = false, int timeOut = 60000, string encode = "UTF-8", string contentType = "application/json")
        {
            string responseText = "";
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(remoteUrl);
                httpWebRequest.Method = "GET";
                httpWebRequest.UseDefaultCredentials = false;
                httpWebRequest.PreAuthenticate = false;
                httpWebRequest.Credentials = new NetworkCredential(userName, password, domain);
                httpWebRequest.Timeout = timeOut;
                HttpWebResponse response = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var reader = new System.IO.StreamReader(response.GetResponseStream(), Encoding.GetEncoding(encode)))
                {
                    responseText = reader.ReadToEnd();
                }
                response.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return responseText;
        }
    }
}