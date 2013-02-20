/* Carrot -- Copyright (C) 2012 GoCarrot Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#if UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_DASHBOARD_WIDGET || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_WEBPLAYER || UNITY_WII || UNITY_IPHONE || UNITY_ANDROID || UNITY_PS3 || UNITY_XBOX360 || UNITY_NACL || UNITY_FLASH
#  define UNITY
#endif

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Security;
using System.Threading;
using System.Collections;
using System.Security.Cryptography;
using System.Collections.Generic;

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

using MiniJSON;
using CarrotInc.Amazon.Util;

namespace GoCarrotInc
{
    public partial class Carrot
    {
        /// <summary>
        /// Represents a Carrot authentication status for a user.
        /// </summary>
        /// <remarks>
        /// </remarks>
        public enum AuthStatus : int
        {
            /// <summary>The current user has not yet authorized the app, or has deauthorized the app.</summary>
            NotAuthorized = -1,

            /// <summary>The current authentication status has not been determined.</summary>
            Undetermined = 0,

            /// <summary>The current user has not granted the 'publish_actions' permission, or has removed the permission.</summary>
            ReadOnly = 1,

            /// <summary>The current user has granted all needed permissions and Carrot will send events to the Carrot server.</summary>
            Ready = 2
        }

        public enum Response
        {
            OK,

            /// <summary>User has not authorized 'publish_actions', read only.</summary>
            ReadOnly,

            /// <summary>Service tier exceeded, not posted.</summary>
            UserLimitHit,

            /// <summary>Authentication error, app secret incorrect.</summary>
            BadAppSecret,

            /// <summary>Resource not found.</summary>
            NotFound,

            /// <summary>User is not authorized for Facebook App.</summary>
            NotAuthorized,

            /// <summary>Dynamic OG object not created due to parameter error.</summary>
            ParameterError,

            /// <summary>Undetermined error.</summary>
            UnknownError
        }

        public delegate void AsynchValidateComplete(object sender, AuthStatus response);
        public delegate void AsynchCallComplete(object sender, Response response);

#if !UNITY
        public Carrot(string appId, SecureString appSecret, string userId = null, string hostname = "gocarrot.com")
        {
            mAppId = appId;
            mAppSecret = appSecret;
            mUserId = userId;
            mHostname = hostname;
            mAuthStatus = AuthStatus.Undetermined;
        }
#endif

        public Carrot(string appId, string appSecret, string userId, string hostname = "gocarrot.com")
        {
#if UNITY
            mAppSecret = appSecret;
#else
            mAppSecret = new SecureString();
            foreach(char c in appSecret)
            {
                mAppSecret.AppendChar(c);
            }
#endif
            mAppId = appId;
            mUserId = userId;
            mHostname = hostname;
            mAuthStatus = AuthStatus.Undetermined;
        }

        /// <summary>
        /// Check the authentication status of the current Carrot user.
        /// </summary>
        /// <value>The <see cref="AuthStatus"/> of the current Carrot user.</value>
        public AuthStatus Status
        {
            get
            {
                return mAuthStatus;
            }
            private set
            {
                if(value != mAuthStatus)
                {
                    mAuthStatus = value;
                    if(AuthenticationStatusChanged != null)
                    {
                        AuthenticationStatusChanged(this, mAuthStatus);
                    }
                }
            }
        }

        /// <summary>
        /// The user id for the current Carrot user.
        /// </summary>
        /// <value>The user id of the current Carrot user.</value>
        public string UserId
        {
            get
            {
                return mUserId;
            }
            set
            {
                mUserId = value;
                Status = AuthStatus.Undetermined;
            }
        }

        /// <summary>
        /// The delegate type for the <see cref="AuthenticationStatusChanged"/> event.
        /// </summary>
        /// <param name="sender">The object which dispatched the <see cref="AuthenticationStatusChanged"/> event.</param>
        /// <param name="status">The new authentication status.</param>
        public delegate void AuthenticationStatusChangedHandler(object sender, AuthStatus status);

        /// <summary>
        /// An event which will notify listeners when the authentication status for the Carrot user has changed.
        /// </summary>
        public event AuthenticationStatusChangedHandler AuthenticationStatusChanged;

        /// <summary>
        /// Asynchronously get the validation status of the current Carrot user.
        /// </summary>
        /// <param name="accessToken">Facebook user access token.</param>
        /// <param name="complete">Delegate which is called upon completion of the call.</param>
        public void validateUserAsync(string accessToken, AsynchValidateComplete complete = null)
        {
            ThreadPool.QueueUserWorkItem((object state) => {
                AuthStatus ret = validateUser(accessToken);
                if(complete != null) complete(this, ret);
            });
        }

        /// <summary>
        /// Get the validation status of the current Carrot user.
        /// </summary>
        /// <param name="accessToken">Facebook user access token.</param>
        /// <returns><c>AuthStatus</c> representing the current user's validation status.</returns>
        public AuthStatus validateUser(string accessToken)
        {
            AuthStatus ret = AuthStatus.Undetermined;
            if(string.IsNullOrEmpty(mUserId))
            {
                // throw?
            }

            string payload = String.Format("access_token={0}&api_key={1}",
                Uri.EscapeDataString(accessToken), Uri.EscapeDataString(mUserId));
            byte[] bytePayload = Encoding.ASCII.GetBytes(payload);

            ServicePointManager.ServerCertificateValidationCallback = CarrotCertValidator;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(String.Format("https://{0}/games/{1}/users.json", mHostname, mAppId));
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = bytePayload.Length;

            Stream stream = request.GetRequestStream();
            stream.Write(bytePayload, 0, bytePayload.Length);
            stream.Close();

            int statusCode = 0;
            //string reply = null;
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                statusCode = (int)response.StatusCode;
                /*using(StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    reply = reader.ReadToEnd();
                }*/
            }
            catch(WebException we)
            {
                statusCode = (int)((HttpWebResponse)we.Response).StatusCode;
                switch(statusCode)
                {
                    case 401: // User has not authorized 'publish_actions', read only
                    case 405: // User is not authorized for Facebook App
                    case 422: // User was not created
                        /*using(StreamReader reader = new StreamReader(((HttpWebResponse)we.Response).GetResponseStream()))
                        {
                            reply = reader.ReadToEnd();
                        }*/
                        break;

                    case 500: // Internal Server Error
                        break;

                    default:
                        throw we;
                }
            }

            switch(statusCode)
            {
                case 201:
                case 200: // Successful
                    ret = AuthStatus.Ready;
                    break;

                case 401: // User has not authorized 'publish_actions', read only
                    ret = AuthStatus.ReadOnly;
                    break;

                case 405: // User is not authorized for Facebook App
                case 422: // User was not created
                    ret = AuthStatus.NotAuthorized;
                    break;
            }

            // Update auth status
            Status = ret;

            return ret;
        }

        /// <summary>
        /// Asynchronously post an achievement to Carrot.
        /// </summary>
        /// <param name="achievementId">Carrot achievement id.</param>
        /// <param name="complete">Delegate which is called upon completion of the call.</param>
        /// <exception cref="T:System.ArgumentNullException"/>
        public void postAchievementAsync(string achievementId, AsynchCallComplete complete = null)
        {
            ThreadPool.QueueUserWorkItem((object state) => {
                Response ret = postAchievement(achievementId);
                if(complete != null) complete(this, ret);
            });
        }

        /// <summary>
        /// Post an achievement to Carrot.
        /// </summary>
        /// <param name="achievementId">Carrot achievement id.</param>
        /// <exception cref="T:System.ArgumentNullException"/>
        /// <returns>A <see cref="Response"/> indicating the reply.</returns>
        public Response postAchievement(string achievementId)
        {
            if(string.IsNullOrEmpty(achievementId))
            {
                throw new ArgumentNullException("achievementId must not be null or empty string.", "achievementId");
            }

            return postSignedRequest("/me/achievements.json", new Dictionary<string, object>() {
                {"achievement_id", achievementId}
            });
        }

        /// <summary>
        /// Asynchronously post a high score to Carrot.
        /// </summary>
        /// <param name="score">Score.</param>
        /// <param name="complete">Delegate which is called upon completion of the call.</param>
        public void postHighScoreAsync(uint score, AsynchCallComplete complete = null)
        {
            ThreadPool.QueueUserWorkItem((object state) => {
                Response ret = postHighScore(score);
                if(complete != null) complete(this, ret);
            });
        }

        /// <summary>
        /// Post a high score to Carrot.
        /// </summary>
        /// <param name="score">Score.</param>
        /// <returns>A <see cref="Response"/> indicating the reply.</returns>
        public Response postHighScore(uint score)
        {
            return postSignedRequest("/me/scores.json", new Dictionary<string, object>() {
                {"value", score}
            });
        }

        /// <summary>
        /// Asynchronously sends an Open Graph action which will use an existing object.
        /// </summary>
        /// <param name="actionId">Carrot action id.</param>
        /// <param name="objectInstanceId">Carrot object instance id.</param>
        /// <param name="complete">Delegate which is called upon completion of the call.</param>
        public void postActionAsync(string actionId, string objectInstanceId, AsynchCallComplete complete = null)
        {
            postActionAsync(actionId, null, objectInstanceId, complete);
        }

        /// <summary>
        /// Sends an Open Graph action which will use an existing object.
        /// </summary>
        /// <param name="actionId">Carrot action id.</param>
        /// <param name="objectInstanceId">Carrot object instance id.</param>
        /// <returns>A <see cref="Response"/> indicating the reply.</returns>
        public Response postAction(string actionId, string objectInstanceId)
        {
            return postAction(actionId, null, objectInstanceId);
        }

        /// <summary>
        /// Asynchronously sends an Open Graph action which will use an existing object.
        /// </summary>
        /// <param name="actionId">Carrot action id.</param>
        /// <param name="actionProperties">Parameters to be submitted with the action.</param>
        /// <param name="objectInstanceId">Carrot object instance id.</param>
        /// <param name="complete">Delegate which is called upon completion of the call.</param>
        public void postActionAsync(string actionId, IDictionary actionProperties, string objectInstanceId, AsynchCallComplete complete = null)
        {
            ThreadPool.QueueUserWorkItem((object state) => {
                Response ret = postAction(actionId, actionProperties, objectInstanceId);
                if(complete != null) complete(this, ret);
            });
        }

        /// <summary>
        /// Sends an Open Graph action which will use an existing object.
        /// </summary>
        /// <param name="actionId">Carrot action id.</param>
        /// <param name="actionProperties">Parameters to be submitted with the action.</param>
        /// <param name="objectInstanceId">Carrot object instance id.</param>
        /// <exception cref="T:System.ArgumentNullException"/>
        /// <returns>A <see cref="Response"/> indicating the reply.</returns>
        public Response postAction(string actionId, IDictionary actionProperties, string objectInstanceId)
        {
            if(string.IsNullOrEmpty(objectInstanceId))
            {
                throw new ArgumentNullException("objectInstanceId must not be null or empty string.", "objectInstanceId");
            }

            if(string.IsNullOrEmpty(actionId))
            {
                throw new ArgumentNullException("actionId must not be null or empty string.", "actionId");
            }

            Dictionary<string, object> parameters = new Dictionary<string, object>() {
                {"action_id", actionId},
                {"action_properties", actionProperties == null ? new Dictionary<string, object>() : actionProperties},
                {"object_properties", new Dictionary<string, object>()}
            };
            if(objectInstanceId != null) parameters["object_instance_id"] = objectInstanceId;
            return postSignedRequest("/me/actions.json", parameters);
        }

        /// <summary>
        /// Asynchronously sends an Open Graph action which will create a new object from the properties provided.
        /// </summary>
        /// <param name="actionId">Carrot action id.</param>
        /// <param name="objectId">Carrot object id.</param>
        /// <param name="objectProperties">Parameters to be submitted with the action.</param>
        /// <param name="objectInstanceId">Object instance id to create or re-use.</param>
        /// <param name="complete">Delegate which is called upon completion of the call.</param>
        public void postActionAsync(string actionId, string objectId, IDictionary objectProperties, string objectInstanceId, AsynchCallComplete complete = null)
        {
            postActionAsync(actionId, null, objectId, objectProperties, objectInstanceId);
        }

        /// <summary>
        /// Sends an Open Graph action which will create a new object from the properties provided.
        /// </summary>
        /// <param name="actionId">Carrot action id.</param>
        /// <param name="objectId">Carrot object id.</param>
        /// <param name="objectProperties">Parameters to be submitted with the action.</param>
        /// <param name="objectInstanceId">Object instance id to create or re-use.</param>
        /// <returns>A <see cref="Response"/> indicating the reply.</returns>
        public Response postAction(string actionId, string objectId, IDictionary objectProperties, string objectInstanceId = null)
        {
            return postAction(actionId, null, objectId, objectProperties, objectInstanceId);
        }

        /// <summary>
        /// Asynchronously sends an Open Graph action which will create a new object from the properties provided.
        /// </summary>
        /// <param name="actionId">Carrot action id.</param>
        /// <param name="actionProperties">Parameters to be submitted with the action.</param>
        /// <param name="objectId">Carrot object id.</param>
        /// <param name="objectProperties">Parameters to be submitted with the action.</param>
        /// <param name="objectInstanceId">Object instance id to create or re-use.</param>
        /// <param name="complete">Delegate which is called upon completion of the call.</param>
        /// <exception cref="T:System.ArgumentNullException"/>
        /// <exception cref="T:System.ArgumentException"/>
        public void postActionAsync(string actionId, IDictionary actionProperties, string objectId, IDictionary objectProperties, string objectInstanceId = null, AsynchCallComplete complete = null)
        {
            ThreadPool.QueueUserWorkItem((object state) => {
                Response ret = postAction(actionId, actionProperties, objectId, objectProperties, objectInstanceId);
                if(complete != null) complete(this, ret);
            });
        }

        /// <summary>
        /// Sends an Open Graph action which will create a new object from the properties provided.
        /// </summary>
        /// <param name="actionId">Carrot action id.</param>
        /// <param name="actionProperties">Parameters to be submitted with the action.</param>
        /// <param name="objectId">Carrot object id.</param>
        /// <param name="objectProperties">Parameters to be submitted with the action.</param>
        /// <param name="objectInstanceId">Object instance id to create or re-use.</param>
        /// <exception cref="T:System.ArgumentNullException"/>
        /// <exception cref="T:System.ArgumentException"/>
        /// <returns>A <see cref="Response"/> indicating the reply.</returns>
        public Response postAction(string actionId, IDictionary actionProperties, string objectId, IDictionary objectProperties, string objectInstanceId = null)
        {
            if(string.IsNullOrEmpty(objectId))
            {
                throw new ArgumentNullException("objectId must not be null or empty string.", "objectId");
            }

            if(string.IsNullOrEmpty(actionId))
            {
                throw new ArgumentNullException("actionId must not be null or empty string.", "actionId");
            }

            if(objectProperties == null)
            {
                throw new ArgumentNullException("objectProperties must not be null.", "objectProperties");
            }
            else if(!objectProperties.Contains("title") ||
                    !objectProperties.Contains("description") ||
                    !objectProperties.Contains("image_url"))
            {
                throw new ArgumentException("objectProperties must contain keys for 'title', 'description', and 'image_url'.", "objectProperties");
            }

            objectProperties["object_type"] = objectId;
            if(!string.IsNullOrEmpty(objectInstanceId)) objectProperties["object_instance_id"] = objectInstanceId;
            Dictionary<string, object> parameters = new Dictionary<string, object>() {
                {"action_id", actionId},
                {"action_properties", actionProperties == null ? new Dictionary<string, object>() : actionProperties},
                {"object_properties", objectProperties}
            };
            return postSignedRequest("/me/actions.json", parameters);
        }

        /// <summary>
        /// Asynchronously ost a 'Like' action that likes the Game's Facebook Page.
        /// </summary>
        /// <param name="complete">Delegate which is called upon completion of the call.</param>
        public void likeGameAsync(AsynchCallComplete complete = null)
        {
            ThreadPool.QueueUserWorkItem((object state) => {
                Response ret = likeGame();
                if(complete != null) complete(this, ret);
            });
        }

        /// <summary>
        /// Post a 'Like' action that likes the Game's Facebook Page.
        /// </summary>
        /// <returns>A <see cref="Response"/> indicating the reply.</returns>
        public Response likeGame()
        {
            return postSignedRequest("/me/like.json", new Dictionary<string, object>() {
                {"object", "game"}
            });
        }

        /// <summary>
        /// Asynchronously ost a 'Like' action that likes the Publisher's Facebook Page.
        /// </summary>
        /// <param name="complete">Delegate which is called upon completion of the call.</param>
        public void likePublisherAsync(AsynchCallComplete complete = null)
        {
            ThreadPool.QueueUserWorkItem((object state) => {
                Response ret = likePublisher();
                if(complete != null) complete(this, ret);
            });
        }

        /// <summary>
        /// Post a 'Like' action that likes the Publisher's Facebook Page.
        /// </summary>
        /// <returns>A <see cref="Response"/> indicating the reply.</returns>
        public Response likePublisher()
        {
            return postSignedRequest("/me/like.json", new Dictionary<string, object>() {
                {"object", "publisher"}
            });
        }

        /// <summary>
        /// Asynchronously post a 'Like' action that likes an achievement.
        /// </summary>
        /// <param name="achievementId">The achievement identifier.</param>
        /// <param name="complete">Delegate which is called upon completion of the call.</param>
        public void likeAchievementAsync(string achievementId, AsynchCallComplete complete = null)
        {
            ThreadPool.QueueUserWorkItem((object state) => {
                Response ret = likeAchievement(achievementId);
                if(complete != null) complete(this, ret);
            });
        }

        /// <summary>
        /// Post a 'Like' action that likes an achievement.
        /// </summary>
        /// <param name="achievementId">The achievement identifier.</param>
        /// <returns>A <see cref="Response"/> indicating the reply.</returns>
        public Response likeAchievement(string achievementId)
        {
            return postSignedRequest("/me/like.json", new Dictionary<string, object>() {
                {"object", "achievement:" + achievementId}
            });
        }

        /// <summary>
        /// Asynchronously post a 'Like' action that likes an Open Graph object.
        /// </summary>
        /// <param name="objectId">The instance id of the Carrot object.</param>
        /// <param name="complete">Delegate which is called upon completion of the call.</param>
        public void likeObjectAsync(string objectId, AsynchCallComplete complete = null)
        {
            ThreadPool.QueueUserWorkItem((object state) => {
                Response ret = likeObject(objectId);
                if(complete != null) complete(this, ret);
            });
        }

        /// <summary>
        /// Post a 'Like' action that likes an Open Graph object.
        /// </summary>
        /// <param name="objectId">The instance id of the Carrot object.</param>
        /// <returns>A <see cref="Response"/> indicating the reply.</returns>
        public Response likeObject(string objectId)
        {
            return postSignedRequest("/me/like.json", new Dictionary<string, object>() {
                {"object", "object:" + objectId}
            });
        }

        #region Signed Response POST/GET
        private Response postSignedRequest(string endpoint, Dictionary<string, object> parameters)
        {
            string replyString = null;
            return makeSignedRequest("POST", endpoint, parameters, ref replyString);
        }

        private Response getSignedRequest(string endpoint, Dictionary<string, object> parameters, ref string replyString)
        {
            return makeSignedRequest("GET", endpoint, parameters, ref replyString);
        }

        private Response makeSignedRequest(string method, string endpoint, Dictionary<string, object> parameters, ref string replyString)
        {
            Response ret = Response.UnknownError;
            Dictionary<string, object> urlParams = new Dictionary<string, object> {
                {"api_key", mUserId},
                {"game_id", mAppId},
                {"request_date", (int)((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime()).TotalSeconds)},
                {"request_id", System.Guid.NewGuid().ToString()}
            };

            // Merge params
            if(parameters != null)
            {
                foreach(KeyValuePair<string, object> entry in parameters)
                {
                    urlParams[entry.Key] = entry.Value;
                }
            }

            // Build sorted list of key-value pairs
            string[] keys = new string[urlParams.Keys.Count];
            urlParams.Keys.CopyTo(keys, 0);
            Array.Sort(keys);
            List<string> kvList = new List<string>();
            foreach(string key in keys)
            {
                string asStr;
                if((asStr = urlParams[key] as string) != null)
                {
                    kvList.Add(String.Format("{0}={1}", key, asStr));
                }
                else
                {
                    kvList.Add(String.Format("{0}={1}", key,
                        Json.Serialize(urlParams[key])));
                }
            }
            string payload = String.Join("&", kvList.ToArray());
            string signString = String.Format("{0}\n{1}\n{2}\n{3}", method, mHostname, endpoint, payload);
            string sig = AWSSDKUtils.HMACSign(signString, mAppSecret, KeyedHashAlgorithm.Create("HMACSHA256"));

            // URI Encoded payload
            kvList = new List<string>();
            foreach(string key in keys)
            {
                string asStr;
                if((asStr = urlParams[key] as string) != null)
                {
                    kvList.Add(String.Format("{0}={1}", key,
                        Uri.EscapeDataString(asStr)));
                }
                else
                {
                    kvList.Add(String.Format("{0}={1}", key,
                        Uri.EscapeDataString(Json.Serialize(urlParams[key]))));
                }
            }
            payload = String.Join("&", kvList.ToArray()) + "&sig=" + Uri.EscapeDataString(sig);

            ServicePointManager.ServerCertificateValidationCallback = CarrotCertValidator;
            HttpWebRequest request = null;
            if(method == "POST")
            {
                byte[] bytePayload = Encoding.ASCII.GetBytes(payload);

                request = (HttpWebRequest)WebRequest.Create(String.Format("https://{0}{1}", mHostname, endpoint));
                request.Method = method;
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = bytePayload.Length;

                Stream stream = request.GetRequestStream();
                stream.Write(bytePayload, 0, bytePayload.Length);
                stream.Close();
            }
            else
            {
                request = (HttpWebRequest)WebRequest.Create(String.Format("https://{0}{1}?{2}",
                    mHostname, endpoint, payload));
            }

            int statusCode = 0;
            string reply = null;
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                statusCode = (int)response.StatusCode;
                using(StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    reply = reader.ReadToEnd();
                }
            }
            catch(WebException we)
            {
                statusCode = (int)((HttpWebResponse)we.Response).StatusCode;
                switch(statusCode)
                {
                    case 401: // User has not authorized 'publish_actions', read only
                    case 402: // Service tier exceeded, not posted
                    case 403: // Authentication error, app secret incorrect
                    case 404: // Resource not found
                    case 405: // User is not authorized for Facebook App
                    case 424: // Dynamic OG object not created due to parameter error
                        using(StreamReader reader = new StreamReader(((HttpWebResponse)we.Response).GetResponseStream()))
                        {
                            reply = reader.ReadToEnd();
                        }
                        break;

                    case 500: // Internal Server Error
                        break;

                    default:
                        throw we;
                }
            }

            switch(statusCode)
            {
                case 201:
                case 200: // Successful
                    ret = Response.OK;
                    Status = AuthStatus.Ready;
                    break;

                case 401: // User has not authorized 'publish_actions', read only
                    ret = Response.ReadOnly;
                    Status = AuthStatus.ReadOnly;
                    break;

                case 402: // Service tier exceeded, not posted
                    ret = Response.UserLimitHit;
                    Status = AuthStatus.Ready;
                    break;

                case 403: // Authentication error, app secret incorrect
                    ret = Response.BadAppSecret;
                    Status = AuthStatus.Ready;
                    break;

                case 404: // Resource not found
                    ret = Response.NotFound;
                    Status = AuthStatus.Ready;
                    break;

                case 405: // User is not authorized for Facebook App
                    ret = Response.NotAuthorized;
                    Status = AuthStatus.NotAuthorized;
                    break;

                case 424: // Dynamic OG object not created due to parameter error
                    ret = Response.ParameterError;
                    Status = AuthStatus.Ready;
                    break;
            }

            if(replyString != null) replyString = reply;
            return ret;
        }
        #endregion

        #region SSL Cert Validator
        private static bool CarrotCertValidator(object sender, X509Certificate certificate,
                                                X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // This is not ideal
            return true;
        }
        #endregion

        private string mUserId;
        private string mAppId;
        private string mHostname;
#if UNITY
        private string mAppSecret;
#else
        private SecureString mAppSecret;
#endif
        private AuthStatus mAuthStatus;
    }
}
