using System.Net;
using Everest;
using Everest.Content;

namespace NGM.CasClient.Client.Utils {
    /// <summary>
    /// A helper utility class to facilitate outbound HTTP GET and POST request
    /// </summary>
    /// <author>Scott Holodak</author>
    internal static class HttpUtil {
        /// <summary>
        /// Executes an HTTP GET request against the Url specified, returning the 
        /// entire response body in string form.
        /// </summary>
        /// <param name="url">The URL to request</param>
        /// <param name="requireHttp200">
        /// Boolean indicating whether or not to return 
        /// null if the repsonse status code is not 200 (OK).
        /// </param>
        /// <returns>
        /// The response body or null if the response status is required to 
        /// be 200 (OK) but is not
        /// </returns>
        internal static string PerformHttpGet(string url, bool requireHttp200) {
            var restClient = new RestClient();
            var response = restClient.Get(url);

            if (requireHttp200 || response.StatusCode != HttpStatusCode.OK)
                return null;

            return response.Body;
        }

        /// <summary>
        /// Executes an HTTP POST against the Url specified with the supplied post data, 
        /// returning the entire response body in string form.
        /// </summary>
        /// <param name="url">The URL to post to</param>
        /// <param name="postData">The x-www-form-urlencoded data to post to the URL</param>
        /// <param name="requireHttp200">
        /// Boolean indicating whether or not to return 
        /// null if the repsonse status code is not 200 (OK).
        /// </param>
        /// <returns>
        /// The response body or null if the response status is required to 
        /// be 200 (OK) but is not
        /// </returns>
        internal static string PerformHttpPost(string url, string postData, bool requireHttp200) {
            var restClient = new RestClient();
            BodyContent content = new WwwFormUrlEncodedContent(postData);

            var response = restClient.Post(url, content);

            if (requireHttp200 || response.StatusCode != HttpStatusCode.OK)
                return null;

            return response.Body;
        }
    }
}