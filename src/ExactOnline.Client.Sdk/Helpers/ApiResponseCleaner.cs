using ExactOnline.Client.Sdk.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace ExactOnline.Client.Sdk.Helpers
{
    /// <summary>
    /// Class for stripping unnecessary Json tags from API Response
    /// </summary>
    public class ApiResponseCleaner
    {
        #region Public methods

        /// <summary>
        /// Fetch Json Object (Json within ['d'] name/value pair) from response
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static string GetJsonObject(string response)
        {
            var oldCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            string output;
            try
            {
                var dict = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(response);
                var d = dict["d"];
                output = NormalizeJson(d).ToString(Formatting.None);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = oldCulture;
            }
            return output;
        }

        public static string GetSkipToken(string response)
        {
            var oldCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            string token = string.Empty;
            try
            {
                var dict = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(response);
                var innerPart = dict["d"];
                if (innerPart.ContainsKey("__next")) {
                    var next = innerPart["__next"].ToObject<string>();

                    // Skiptoken has format "$skiptoken=xyz" in the url and we want to extract xyz.
                    var match = Regex.Match(next ?? "", @"\$skiptoken=([^&#]*)");

                    // Extract the skip token
                    token = match.Success ? match.Groups[1].Value : null;
                }
            }
            catch (Exception e)
            {
                throw new IncorrectJsonException(e.Message);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = oldCulture;
            }
            return token;
        }

        /// <summary>
        /// Fetch Json Array (Json within ['d']['results']) from response
        /// </summary>
        public static string GetJsonArray(string response)
        {
            var oldCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            try
            {
                var dict = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(response);
                var innerPart = dict["d"];
                if(!innerPart.ContainsKey("results")) throw new InvalidDataException("JSON does not contain a result");

                var results = NormalizeJson(innerPart);
                return results.ToString(Formatting.None);
            }
            catch (Exception e)
            {
                throw new IncorrectJsonException(e.Message);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = oldCulture;
            }

        }

        #endregion

        #region Private methods

        private static JContainer NormalizeJson(JContainer item) {
            if (!(item is JObject obj)) return item;

            if (obj.ContainsKey("results")) {
                var rawResults = item["results"].ToObject<List<JObject>>();
                var results = new JArray();

                foreach (var rawResult in rawResults) {
                    var result = NormalizeJson(rawResult);
                    results.Add(result);
                }

                return results;
            } else if(obj.ContainsKey("__deferred")) {
                return new JArray();
            } else {
                foreach (var kvp in obj) {
                    if (kvp.Value is JObject value)
                        obj[kvp.Key] = NormalizeJson(value);
                }

                return obj;
            }
        }

        #endregion

    }
}
