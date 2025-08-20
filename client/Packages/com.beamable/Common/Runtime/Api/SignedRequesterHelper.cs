// this file was copied from nuget package Beamable.Common@5.3.0
// https://www.nuget.org/packages/Beamable.Common/5.3.0

using System;
using System.Text;
namespace Beamable.Common.Api
{
    /// <summary>
    /// The <see cref="ISignedRequester"/> is a requester that makes Beamable signed requests
    /// </summary>
    public interface ISignedRequester : IRequester
    {
        /// <summary>
        /// Signed Requests are always authorized but some Beamable API calls require user context by way of a playerId.
        /// This method will set the playerId included for all subsequent requests. 
        /// </summary>
        /// <param name="playerId">
        /// A value of `null` allows the system to use the default playerId as the gamerTag. In a C#MS context, this will be whoever initiated the request.
        /// A value of `0` or `""` implies NO playerId should be sent.
        /// Any other value will be interpreted as a playerId
        /// </param>
        void SetPlayerId(string playerId);
    }

    public static class SignedRequesterExtensions
    {
        /// <summary>
        /// Configure the <see cref="ISignedRequester"/> to not send any playerId information.
        /// </summary>
        /// <param name="requester"></param>
        public static void UseNoPlayerId(this ISignedRequester requester) => requester.SetPlayerId(null);
        
        /// <summary>
        /// Configure the <see cref="ISignedRequester"/> to use whatever default playerId is available.
        /// </summary>
        /// <param name="requester"></param>
        public static void UseDefaultPlayerId(this ISignedRequester requester) => requester.SetPlayerId(0);
        
        /// <inheritdoc cref="ISignedRequester.SetPlayerId(string)"/>
        public static void SetPlayerId(this ISignedRequester requester, long playerId) =>
            requester.SetPlayerId(playerId.ToString());
    }

    
    public static class SignedRequesterHelper
    {
        /// <summary>
        /// The body object only needs to be included in the signature if
        /// the HTTP method traditionally includes a body,
        /// AND if the body is not null. 
        /// </summary>
        /// <param name="method"></param>
        /// <param name="body"></param>
        /// <param name="serializer"></param>
        /// <returns>
        ///  The serialized body string to include in the signature, or null. 
        /// </returns>
        public static string GetBodyToSign(Method method, object body, Func<object, string> serializer)
        {
            switch (method)
            {
                case Method.GET:
                case Method.DELETE:
                    return null;
                default:
                    if (body is string strBody)
                    {
                        return strBody;
                    }
                    
                    if (body != null)
                    {
                        return serializer(body);
                    }

                    return null;
            }
        }
        
        /// <summary>
        /// Calculate the Beamable signature for a given request. This signature
        /// can be used in the X-BEAM-SIGNATURE header of a HTTP request to make
        /// a privileged request to the Beamable API.
        /// </summary>
        /// <param name="pid">Project ID of the realm ("DE_...")</param>
        /// <param name="secret">The realm secret, which is a UUID</param>
        /// <param name="uriPathAndQuery">Path and query parts of the URI</param>
        /// <param name="body">Request body, if any</param>
        /// <param name="version">Should be "1"</param>
        /// <returns>Signature as a short Base64 string</returns>
        public static string CalculateSignature(string pid, string secret, string uriPathAndQuery, string body = null, string version="1")
        {
            var md5 = System.Security.Cryptography.MD5.Create();
            var dataToSign = $"{secret}{pid}{version}{uriPathAndQuery}";
            if (body != null) dataToSign += body;

            byte[] data = Encoding.UTF8.GetBytes(dataToSign);
            byte[] hash = md5.ComputeHash(data);
            return Convert.ToBase64String(hash);
        }
    }
}