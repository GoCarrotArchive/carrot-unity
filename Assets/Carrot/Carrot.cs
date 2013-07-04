#region License
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
#endregion

#region References
using System;
using MiniJSON;
using System.Net;
using UnityEngine;
using System.Security;
using System.Collections;
using System.Net.Security;
using CarrotInc.Amazon.Util;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Security.Cryptography.X509Certificates;
#endregion

/// <summary>
/// A MonoBehaviour which can be attached to a Unity GameObject to
/// provide access to Carrot functionality.
/// </summary>
public partial class Carrot : MonoBehaviour
{
    /// <summary>
    /// Gets the <see cref="Carrot"/> singleton.
    /// </summary>
    /// <value> The <see cref="Carrot"/> singleton.</value>
    public static Carrot Instance
    {
        get
        {
            if(mInstance == null)
            {
                mInstance = FindObjectOfType(typeof(Carrot)) as Carrot;

                if(mInstance == null)
                {
                    GameObject carrotGameObject = GameObject.Find("CarrotGameObject");
                    if(carrotGameObject == null)
                    {
                        carrotGameObject = new GameObject("CarrotGameObject");
                        carrotGameObject.AddComponent("Carrot");
                    }
                    mInstance = carrotGameObject.GetComponent<Carrot>();

                    TextAsset carrotJson = Resources.Load("carrot") as TextAsset;
                    if(carrotJson == null)
                    {
                        throw new NullReferenceException("Carrot text asset not found. Use the configuration tool in the 'Edit/Carrot' menu to generate it.");
                    }
                    else
                    {
                        Dictionary<string, object> carrotConfig = null;
                        carrotConfig = Json.Deserialize(carrotJson.text) as Dictionary<string, object>;
                        mInstance.mFacebookAppId = carrotConfig["carrotAppId"] as string;
                        mInstance.mCarrotAppSecret = carrotConfig["carrotAppSecret"] as string;
                        mInstance.mBundleVersion = carrotConfig["appBundleVersion"] as string;
                    }
                }
            }
            return mInstance;
        }
    }

    /// <summary>
    /// Represents a Carrot authentication status for a user.
    /// </summary>
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

    /// <summary>
    /// Responses to Carrot requests.
    /// </summary>
    public enum Response
    {
        /// <summary>Successful.</summary>
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

        /// <summary>Network error.</summary>
        NetworkError,

        /// <summary>Undetermined error.</summary>
        UnknownError,
    }

    /// <summary>Carrot SDK version.</summary>
    public static readonly string SDKVersion = "1.1.0";

    /// <summary>
    /// Carrot debug users which can be assigned to UserId in order to simulate
    /// different cases for use.
    /// </summary>
    public class DebugUser
    {
        /// <summary>A user which never exists.</summary>
        public static readonly string NoSuchUser = "nosuchuser";

        /// <summary>A user which has not authorized the 'publish_actions' permission.</summary>
        public static readonly string ReadOnlyUser = "nopublishactions";

        /// <summary>A user which deauthorized the Facebook application.</summary>
        public static readonly string DeauthorizedUser = "deauthorized";
    }

    /// <summary>
    /// Return the string value of an <see cref="AuthStatus"/> value.
    /// </summary>
    /// <returns>The string description of an <see cref="AuthStatus"/>.</returns>
    public static string authStatusString(AuthStatus authStatus)
    {
        switch(authStatus)
        {
            case Carrot.AuthStatus.NotAuthorized: return "Carrot user has not authorized the application.";
            case Carrot.AuthStatus.Undetermined: return "Carrot user status is undetermined.";
            case Carrot.AuthStatus.ReadOnly: return "Carrot user has not allowed the 'publish_actions' permission.";
            case Carrot.AuthStatus.Ready: return "Carrot user is authorized.";
            default: return "Invalid Carrot AuthStatus.";
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
    public static event AuthenticationStatusChangedHandler AuthenticationStatusChanged;

    /// <summary>
    /// The callback delegate type for Carrot requests.
    /// </summary>
    public delegate void CarrotRequestResponse(Response response, string errorText);

    /// <summary>
    /// Check the authentication status of the current Carrot user.
    /// </summary>
    /// <value>The <see cref="GoCarrotInc.Carrot.AuthStatus"/> of the current Carrot user.</value>
    public AuthStatus Status
    {
        get
        {
            return mAuthStatus;
        }
        private set
        {
            if(mAuthStatus != value)
            {
                mAuthStatus = value;
                if(AuthenticationStatusChanged != null)
                {
                    AuthenticationStatusChanged(this, mAuthStatus);
                }

                foreach(CarrotCache.CachedRequest request in mCarrotCache.RequestsInCache(mAuthStatus))
                {
                    StartCoroutine(signedRequestCoroutine(request, cachedRequestHandler(request, null)));
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
            if(mUserId != value)
            {
                mUserId = value;
                this.Status = AuthStatus.Undetermined;
            }
        }
    }

    /// <summary>
    /// Validate a Facebook user to allow posting of Carrot events.
    /// </summary>
    /// <remarks>
    /// This method will trigger notification of authentication status using the <see cref="AuthenticationStatusChanged"/> event.
    /// </remarks>
    /// <param name="accessTokenOrFacebookId">Facebook user access token or Facebook User Id.</param>
    public void validateUser(string accessTokenOrFacebookId)
    {
        StartCoroutine(validateUserCoroutine(accessTokenOrFacebookId));
    }

    /// <summary>
    /// Post an achievement to Carrot.
    /// </summary>
    /// <param name="achievementId">Carrot achievement id.</param>
    /// <param name="callback">Optional <see cref="CarrotRequestResponse"/> which will be used to deliver the reply.</param>
    public void postAchievement(string achievementId, CarrotRequestResponse callback = null)
    {
        if(string.IsNullOrEmpty(achievementId))
        {
            throw new ArgumentNullException("achievementId must not be null or empty string.", "achievementId");
        }

        StartCoroutine(cachedRequestCoroutine("/me/achievements.json", new Dictionary<string, object>() {
                {"achievement_id", achievementId}
        }, callback));
    }

    /// <summary>
    /// Post a high score to Carrot.
    /// </summary>
    /// <param name="score">Score.</param>
    /// <param name="callback">Optional <see cref="CarrotRequestResponse"/> which will be used to deliver the reply.</param>
    public void postHighScore(uint score, CarrotRequestResponse callback = null)
    {
        StartCoroutine(cachedRequestCoroutine("/me/scores.json", new Dictionary<string, object>() {
                {"value", score}
        }, callback));
    }

    /// <summary>
    /// Sends an Open Graph action which will use an existing object.
    /// </summary>
    /// <param name="actionId">Carrot action id.</param>
    /// <param name="objectInstanceId">Carrot object instance id.</param>
    /// <param name="callback">Optional <see cref="CarrotRequestResponse"/> which will be used to deliver the reply.</param>
    public void postAction(string actionId, string objectInstanceId, CarrotRequestResponse callback = null)
    {
        postAction(actionId, null, objectInstanceId, callback);
    }

    /// <summary>
    /// Sends an Open Graph action which will use an existing object.
    /// </summary>
    /// <param name="actionId">Carrot action id.</param>
    /// <param name="actionProperties">Parameters to be submitted with the action.</param>
    /// <param name="objectInstanceId">Carrot object instance id.</param>
    /// <param name="callback">Optional <see cref="CarrotRequestResponse"/> which will be used to deliver the reply.</param>
    public void postAction(string actionId, IDictionary actionProperties, string objectInstanceId,
                           CarrotRequestResponse callback = null)
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

        StartCoroutine(cachedRequestCoroutine("/me/actions.json", parameters, callback));
    }

    /// <summary>
    /// Describes a viral object which is to be created.
    /// </summary>
    public class ViralObject
    {
        /// <summary>
        /// The title of the viral object to be created.
        /// </summary>
        public string Title
        {
            get
            {
                return mObjectProperties["title"] as string;
            }
            set
            {
                mObjectProperties["title"] = value;
            }
        }

        /// <summary>
        /// The description of the viral object to be created.
        /// </summary>
        public string Description
        {
            get
            {
                return mObjectProperties["description"] as string;
            }
            set
            {
                mObjectProperties["description"] = value;
            }
        }

        /// <summary>
        /// The object instance id of the viral object to create or re-use.
        /// If ObjectInstanceId is not specified, GUID will be generated instead.
        /// </summary>
        public string ObjectInstanceId
        {
            get
            {
                return mObjectProperties["object_instance_id"] as string;
            }
            set
            {
                mObjectProperties["object_instance_id"] = value;
            }
        }

        /// <summary>
        /// The URL of the image, or a Texture2D which will be uploaded and
        /// used for the viral object.
        /// </summary>
        public object Image
        {
            get
            {
                return mObjectProperties["image"];
            }
            set
            {
                mObjectProperties["image"] = value;
            }
        }

        /// <summary>
        /// Assignment of user defined fields for the viral object to created.
        /// </summary>
        public Dictionary<string, object> Fields
        {
            get
            {
                return mObjectProperties["fields"] as Dictionary<string, object>;
            }
            set
            {
                mObjectProperties["fields"] = value;
            }
        }

        /// <summary>
        /// Specify the parameters for a viral object using a Texture2D to upload.
        /// </summary>
        /// <param name="objectTypeId">Carrot object type id.</param>
        /// <param name="title">Title of the new viral object.</param>
        /// <param name="description">Description for the new viral object.</param>
        /// <param name="image">Texture2D to upload for the new viral object.</param>
        /// <param name="objectInstanceId">Optional object instance id to create, or re-use.</params>
        public ViralObject(string objectTypeId, string title, string description,
                           Texture2D image, string objectInstanceId = null)
        {
            if(string.IsNullOrEmpty(objectTypeId))
            {
                throw new ArgumentNullException("objectTypeId must not be null or empty string.", "objectTypeId");
            }

            if(string.IsNullOrEmpty(title))
            {
                throw new ArgumentNullException("title must not be null or empty string.", "title");
            }

            if(string.IsNullOrEmpty(description))
            {
                throw new ArgumentNullException("description must not be null or empty string.", "description");
            }

            if(image == null)
            {
                throw new ArgumentNullException("image must not be null.", "image");
            }

            mObjectProperties = new Dictionary<string, object>();
            mObjectProperties["object_type"] = objectTypeId;
            this.Title = title;
            this.Description = description;
            this.Image = image;
            if(objectInstanceId != null) this.ObjectInstanceId = objectInstanceId;
        }

        /// <summary>
        /// Specify the parameters for a viral object with a remote image URL.
        /// </summary>
        /// <param name="objectTypeId">Carrot object type id.</param>
        /// <param name="title">Title of the new viral object.</param>
        /// <param name="description">Description for the new viral object.</param>
        /// <param name="imageUrl">Image URL for the new viral object.</param>
        /// <param name="objectInstanceId">Optional object instance id to create, or re-use.</params>
        public ViralObject(string objectTypeId, string title, string description,
                           string imageUrl, string objectInstanceId = null)
        {
            if(string.IsNullOrEmpty(objectTypeId))
            {
                throw new ArgumentNullException("objectTypeId must not be null or empty string.", "objectTypeId");
            }

            if(string.IsNullOrEmpty(title))
            {
                throw new ArgumentNullException("title must not be null or empty string.", "title");
            }

            if(string.IsNullOrEmpty(description))
            {
                throw new ArgumentNullException("description must not be null or empty string.", "description");
            }

            if(string.IsNullOrEmpty(imageUrl))
            {
                throw new ArgumentNullException("imageUrl must not be null or empty string.", "imageUrl");
            }

            mObjectProperties = new Dictionary<string, object>();
            mObjectProperties["object_type"] = objectTypeId;
            this.Title = title;
            this.Description = description;
            this.Image = imageUrl;
            if(objectInstanceId != null) this.ObjectInstanceId = objectInstanceId;
        }

        public Dictionary<string, object> toDictionary()
        {
            return mObjectProperties;
        }

        private Dictionary<string, object> mObjectProperties;
    }

    /// <summary>
    /// Sends an Open Graph action which will create a new object.
    /// </summary>
    /// <param name="actionId">Carrot action id.</param>
    /// <param name="viralObject">A <see cref="ViralObject"/> describing the object to be created.</param>
    /// <param name="callback">Optional <see cref="CarrotRequestResponse"/> which will be used to deliver the reply.</param>
    public void postAction(string actionId, ViralObject viralObject,
                           CarrotRequestResponse callback = null)
    {
        postAction(actionId, null, viralObject, callback);
    }

    /// <summary>
    /// Sends an Open Graph action which will create a new object.
    /// </summary>
    /// <param name="actionId">Carrot action id.</param>
    /// <param name="actionProperties">Parameters to be submitted with the action.</param>
    /// <param name="viralObject">A <see cref="ViralObject"/> describing the object to be created.</param>
    /// <param name="callback">Optional <see cref="CarrotRequestResponse"/> which will be used to deliver the reply.</param>
    public void postAction(string actionId, IDictionary actionProperties,
                           ViralObject viralObject,
                           CarrotRequestResponse callback = null)
    {
        if(string.IsNullOrEmpty(actionId))
        {
            throw new ArgumentNullException("actionId must not be null or empty string.", "actionId");
        }

        if(viralObject == null)
        {
            throw new ArgumentNullException("viralObject must not be null.", "viralObject");
        }

        Dictionary<string, object> parameters = new Dictionary<string, object>() {
            {"action_id", actionId},
            {"action_properties", actionProperties == null ? new Dictionary<string, object>() : actionProperties},
            {"object_properties", viralObject.toDictionary()}
        };
        StartCoroutine(cachedRequestCoroutine("/me/actions.json", parameters, callback));
    }

    /// <summary>
    /// Post a 'Like' action that likes the Game's Facebook Page.
    /// </summary>
    /// <param name="callback">Optional <see cref="CarrotRequestResponse"/> which will be used to deliver the reply.</param>
    public void likeGame(CarrotRequestResponse callback = null)
    {
        StartCoroutine(cachedRequestCoroutine("/me/like.json", new Dictionary<string, object>() {
            {"object", "game"}
        }, callback));
    }

    /// <summary>
    /// Post a 'Like' action that likes the Publisher's Facebook Page.
    /// </summary>
    /// <param name="callback">Optional <see cref="CarrotRequestResponse"/> which will be used to deliver the reply.</param>
    public void likePublisher(CarrotRequestResponse callback = null)
    {
        StartCoroutine(cachedRequestCoroutine("/me/like.json", new Dictionary<string, object>() {
            {"object", "publisher"}
        }, callback));
    }

    /// <summary>
    /// Post a 'Like' action that likes an achievement.
    /// </summary>
    /// <param name="achievementId">The achievement identifier.</param>
    /// <param name="callback">Optional <see cref="CarrotRequestResponse"/> which will be used to deliver the reply.</param>
    public void likeAchievement(string achievementId, CarrotRequestResponse callback = null)
    {
        if(string.IsNullOrEmpty(achievementId))
        {
            throw new ArgumentNullException("achievementId must not be null or empty string.", "achievementId");
        }

        StartCoroutine(cachedRequestCoroutine("/me/like.json", new Dictionary<string, object>() {
            {"object", "achievement:" + achievementId}
        }, callback));
    }

    /// <summary>
    /// Post a 'Like' action that likes an Open Graph object.
    /// </summary>
    /// <param name="objectId">The instance id of the Carrot object.</param>
    /// <param name="callback">Optional <see cref="CarrotRequestResponse"/> which will be used to deliver the reply.</param>
    public void likeObject(string objectId, CarrotRequestResponse callback = null)
    {
        if(string.IsNullOrEmpty(objectId))
        {
            throw new ArgumentNullException("objectId must not be null or empty string.", "objectId");
        }

        StartCoroutine(cachedRequestCoroutine("/me/like.json", new Dictionary<string, object>() {
            {"object", "object:" + objectId}
        }, callback));
    }

    #region Internal
    /// @cond hide_from_doxygen
    Carrot()
    {
        mCarrotCache = new CarrotCache();
    }

    private CarrotRequestResponse cachedRequestHandler(CarrotCache.CachedRequest cachedRequest,
                                                       CarrotRequestResponse callback)
    {
        return (Response ret, string errorText) => {
                switch(ret)
                {
                    case Response.OK:
                    case Response.NotFound:
                    case Response.ParameterError:
                        cachedRequest.RemoveFromCache();
                        break;

                    default:
                        cachedRequest.AddRetryInCache();
                        break;
                }
                if(callback != null) callback(ret, errorText);
        };
    }
    /// @endcond
    #endregion

    #region MonoBehaviour
    /// @cond hide_from_doxygen
    void Start()
    {
        DontDestroyOnLoad(this);
    }

    void OnApplicationQuit()
    {
        mCarrotCache.Dispose();
        Destroy(this);
    }
    /// @endcond
    #endregion

    #region Carrot request coroutines
    /// @cond hide_from_doxygen
    private IEnumerator validateUserCoroutine(string accessTokenOrFacebookId)
    {
        AuthStatus ret = AuthStatus.Undetermined;
        if(string.IsNullOrEmpty(mUserId))
        {
            throw new NullReferenceException("UserId is empty. Assign a UserId before calling validateUser");
        }

        ServicePointManager.ServerCertificateValidationCallback = CarrotCertValidator;

        UnityEngine.WWWForm payload = new UnityEngine.WWWForm();
        payload.AddField("access_token", accessTokenOrFacebookId);
        payload.AddField("api_key", mUserId);

        UnityEngine.WWW request = new UnityEngine.WWW(String.Format("https://{0}/games/{1}/users.json", mHostname, mFacebookAppId), payload);
        yield return request;

        int statusCode = 0;
        if(request.error != null)
        {
            Match match = Regex.Match(request.error, "^([0-9]+)");
            if(match.Success)
            {
                statusCode = int.Parse(match.Value);
            }
            else
            {
                Debug.Log(request.error);
            }
        }
        else
        {
            // TODO: Change if JSON updates to include code
            // Dictionary<string, object> reply = Json.Deserialize(request.text) as Dictionary<string, object>;
            // statusCode = (int)((long)reply["code"]);
            statusCode = 200;
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

            case 404:
            case 405: // User is not authorized for Facebook App
            case 422: // User was not created
                ret = AuthStatus.NotAuthorized;
                break;
        }
        this.Status = ret;

        yield return ret;
    }

    private IEnumerator cachedRequestCoroutine(string endpoint,
                                               Dictionary<string, object> parameters,
                                               CarrotRequestResponse callback = null)
    {
        CarrotCache.CachedRequest cachedRequest = mCarrotCache.CacheRequest(endpoint, parameters);
        if(mAuthStatus == AuthStatus.Ready)
        {
            yield return StartCoroutine(signedRequestCoroutine(cachedRequest, cachedRequestHandler(cachedRequest, callback)));
        }
        else
        {
            if(callback != null) callback(Response.UnknownError, authStatusString(mAuthStatus));
            yield return null;
        }
    }

    private IEnumerator signedRequestCoroutine(CarrotCache.CachedRequest cachedRequest,
                                               CarrotRequestResponse callback = null)
    {
        Response ret = Response.UnknownError;
        string errorText = null;

        if(string.IsNullOrEmpty(mUserId))
        {
            throw new NullReferenceException("UserId is empty. Assign a UserId before calling validateUser");
        }

        ServicePointManager.ServerCertificateValidationCallback = CarrotCertValidator;

        Dictionary<string, object> urlParams = new Dictionary<string, object> {
            {"api_key", mUserId},
            {"game_id", mFacebookAppId},
            {"request_date", cachedRequest.RequestDate},
            {"request_id", cachedRequest.RequestId}
        };
        Dictionary<string, object> parameters = cachedRequest.Parameters;

        // If this has an attached image, bytes will be placed here.
        byte[] imageBytes = null;

        if(parameters != null)
        {
            // Check for image on dynamic objects
            if(parameters.ContainsKey("object_properties"))
            {
                IDictionary objectProperties = parameters["object_properties"] as IDictionary;
                object image = objectProperties["image"];
                Texture2D imageTex2D;
                if((imageTex2D = image as Texture2D) != null)
                {
                    imageBytes = imageTex2D.EncodeToPNG();
                    using(SHA256 sha256 = SHA256Managed.Create())
                    {
                        objectProperties["image_sha"] = System.Text.Encoding.UTF8.GetString(sha256.ComputeHash(imageBytes));
                    }
                }
                else if(image is string)
                {
                    objectProperties["image_url"] = image;
                }
                objectProperties.Remove("image");
            }

            // Merge params
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
        string signString = String.Format("{0}\n{1}\n{2}\n{3}", "POST", mHostname.Split(new char[]{':'})[0], cachedRequest.Endpoint, payload);
        string sig = AWSSDKUtils.HMACSign(signString, mCarrotAppSecret, KeyedHashAlgorithm.Create("HMACSHA256"));

        UnityEngine.WWWForm formPayload = new UnityEngine.WWWForm();
        foreach(string key in keys)
        {
            string asStr;
            if((asStr = urlParams[key] as string) != null)
            {
                formPayload.AddField(key, asStr);
            }
            else
            {
                formPayload.AddField(key,
                    Json.Serialize(urlParams[key]));
            }
        }
        formPayload.AddField("sig", sig);

        // Attach image
        if(imageBytes != null)
        {
            formPayload.AddBinaryData("image_bytes", imageBytes);
        }

        UnityEngine.WWW request = new UnityEngine.WWW(String.Format("https://{0}{1}", mHostname, cachedRequest.Endpoint), formPayload);
        yield return request;

        int statusCode = 0;
        if(request.error != null)
        {
            Match match = Regex.Match(request.error, "^([0-9]+)");
            if(match.Success)
            {
                statusCode = int.Parse(match.Value);
            }
            else
            {
                errorText = request.error;
                Debug.Log(request.error);
            }
        }
        else
        {
            Dictionary<string, object> reply = Json.Deserialize(request.text) as Dictionary<string, object>;
            statusCode = (int)((long)reply["code"]);
        }

        switch(statusCode)
        {
            case 201:
            case 200: // Successful
                ret = Response.OK;
                this.Status = AuthStatus.Ready;
                break;

            case 401: // User has not authorized 'publish_actions', read only
                ret = Response.ReadOnly;
                this.Status = AuthStatus.ReadOnly;
                break;

            case 402: // Service tier exceeded, not posted
                ret = Response.UserLimitHit;
                this.Status = AuthStatus.Ready;
                break;

            case 403: // Authentication error, app secret incorrect
                ret = Response.BadAppSecret;
                this.Status = AuthStatus.Ready;
                break;

            case 404: // Resource not found
                ret = Response.NotFound;
                this.Status = AuthStatus.Ready;
                break;

            case 405: // User is not authorized for Facebook App
                ret = Response.NotAuthorized;
                this.Status = AuthStatus.NotAuthorized;
                break;

            case 424: // Dynamic OG object not created due to parameter error
                ret = Response.ParameterError;
                this.Status = AuthStatus.Ready;
                break;
        }
        if(callback != null) callback(ret, errorText);
    }
    /// @endcond
    #endregion

    #region SSL Cert Validator
    /// @cond hide_from_doxygen
    private static bool CarrotCertValidator(object sender, X509Certificate certificate,
                                            X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        // This is not ideal
        return true;
    }
    /// @endcond
    #endregion

    #region Member Variables
    private static Carrot mInstance = null;
    private AuthStatus mAuthStatus;
    private string mUserId;
    private string mHostname = "gocarrot.com";
    private string mFacebookAppId;
    private string mCarrotAppSecret;
    private string mBundleVersion;
    private CarrotCache mCarrotCache;
    #endregion
}
