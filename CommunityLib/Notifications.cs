//!CompilerOption|AddRef|System.Web.dll
using System;
using System.IO;
using System.Net;
using System.Web;

namespace CommunityLib
{
    public static class Notifications
    {
        /// <summary>
        /// Sends a notification using Prowl service (iPhone)
        /// </summary>
        /// <param name="apikey"></param>
        /// <param name="pluginName">Name of the Plugin</param>
        /// <param name="ev">Event name</param>
        /// <param name="description">Event description</param>
        /// <param name="priority">Notification Priority</param>
        /// <returns>NotificationResult enum entry</returns>
        public static Results.NotificationResult Prowl(string apikey, string pluginName, string ev, string description, NotificationPriority priority)
        {
            if (!CheckApiToken(apikey))
                return Results.NotificationResult.ApiKeyError;

            string url = "https://prowl.weks.net/publicapi/add";
            url += "?apikey=" + HttpUtility.UrlEncode(apikey.Trim()) +
                   "&application=" + HttpUtility.UrlEncode(pluginName) +
                   "&description=" + HttpUtility.UrlEncode(description) +
                   "&event=" + HttpUtility.UrlEncode(ev) +
                   "&priority=" + HttpUtility.UrlEncode(priority.ToString());

            return Send(url);
        }

        /// <summary>
        /// Sends a notification using Pushover service
        /// </summary>
        /// <param name="token">Application Token/Key</param>
        /// <param name="apikey"></param>
        /// <param name="description">Message to send</param>
        /// <param name="ev">Title</param>
        /// <param name="p">Notification Priority</param>
        /// <returns></returns>
        public static Results.NotificationResult Pushover(string token, string apikey, string description, string ev, NotificationPriority p)
        {
            if (!CheckApiToken(token))
                return Results.NotificationResult.TokenError;

            if (!CheckApiToken(apikey))
                return Results.NotificationResult.ApiKeyError;

            string url = "https://api.pushover.net/1/messages.json";
            url += "?token=" + HttpUtility.UrlEncode(token.Trim()) +
                   "&user=" + HttpUtility.UrlEncode(apikey.Trim()) +
                   "&message=" + HttpUtility.UrlEncode(description) +
                   "&title=" + HttpUtility.UrlEncode(ev) +
                   "&priority=" + HttpUtility.UrlEncode(p.ToString());
            
            return Send(url);
        }

        /// <summary>
        /// Sends a notification using Pushbullet service
        /// </summary>
        /// <param name="apikey"></param>
        /// <param name="description">Body of the notification</param>
        /// <param name="ev">Title of the notification</param>
        /// <returns></returns>
        public static Results.NotificationResult Pushbullet(string apikey, string description, string ev)
        {
            if (!CheckApiToken(apikey))
                return Results.NotificationResult.ApiKeyError;

            const string url = "https://api.pushbullet.com/api/pushes";
            var myCreds = new CredentialCache { { new Uri(url), "Basic", new NetworkCredential(apikey, "") } };
            string postData = "type=note" +
                              "&body=" + HttpUtility.UrlEncode(description) +
                              "&title=" + HttpUtility.UrlEncode(ev);

            return Send(url, postData, myCreds);
        }

        //returns the result to parse it in the main function
        private static Results.NotificationResult Send(string uri, string postData = "", CredentialCache creds = null)
        {
            WebRequest request = WebRequest.Create(uri);
            request.ContentLength = 0;
            request.ContentType = "application/x-www-form-urlencoded";
            request.Method = "POST";

            if (creds != null)
                request.Credentials = creds;

            if (!string.IsNullOrEmpty(postData))
            {
                var sw = new StreamWriter(request.GetRequestStream());
                sw.Write(postData);
                sw.Close();
            }

            var postResponse = default(WebResponse);

            try
            {
                postResponse = request.GetResponse();
            }
            catch(WebException ex)
            {
                var test = (HttpWebResponse)ex.Response;
                if (test.StatusCode != HttpStatusCode.OK)
                    return Results.NotificationResult.WebRequestError;
            }
            finally
            {
                postResponse?.Close();
            }

            return Results.NotificationResult.None;
        }

        private static bool CheckApiToken(string shit)
        {
            return !string.IsNullOrEmpty(shit);
        }

        public enum NotificationPriority : sbyte
        {
            VeryLow = -2,
            Moderate = -1,
            Normal = 0,
            High = 1,
            Emergency = 2
        }
    }
}
