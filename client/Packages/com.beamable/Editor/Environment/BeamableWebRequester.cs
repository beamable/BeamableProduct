using System;
using System.Net;
using UnityEngine;

namespace Beamable.Editor.Environment
{
    public static class BeamableWebRequester
    {
        public static string BlogSpotUrl { get; private set; }
        
        public static bool IsBlogSpotAvailable(string version)
        {
            version = version.Replace(".", "-");
            var url = $"{BeamableConstants.URL_BEAMABLE_BLOG_RELEASES_UNITY_SDK}-{version}";
            BlogSpotUrl = string.Empty;
            
            if (string.IsNullOrEmpty(url))
            {
                Debug.LogError("Url is not set up correctly!");
                return false;
            }

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                var response = (HttpWebResponse)request.GetResponse();
                response.Close();

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return false;
                }
                
                BlogSpotUrl = url;
                return true;
            }
            catch (WebException)
            {
                return false;
            }
        }
    }
}