/* Carrot -- Copyright (C) 2012 Carrot Inc.
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

using System;
using MiniJSON;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

/// <summary>
/// A MonoBehaviour which can be attached to a Unity GameObject
/// to provide singleton access to a <see cref="Carrot.CarrotBridge"/>.
/// </summary>
public class Carrot : MonoBehaviour
{
   /// <summary>
   /// The Facebook Application Id for your application.
   /// </summary>
   public string FacebookAppId;

   /// <summary>
   /// The Carrot Application Secret for your application.
   /// </summary>
   public string CarrotAppSecret;

   /// <summary>
   /// Represents a Carrot authentication status for a user.
   /// </summary>
   /// <remarks>
   /// This value can be obtained using the <see cref="CarrotBridge.Status"/> property. You will
   /// also be notified of any change in authentication status by signing up for the
   /// <see cref="CarrotBridge.AuthenticationStatusChangedHandler"/> event.
   /// <p>
   /// Note that even though the status may not be <see cref="AuthStatus.Ready"/> Carrot will
   /// store the scores, achievements, and likes in a client-side cache and they will be sent
   /// when the authentication status is <see cref="AuthStatus.Ready"/>.
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

   /// <summary>
   /// The kind of authentication status to request for Facebook SSO (iOS only).
   /// </summary>
   /// <remarks>
   /// For best practices information about Facebook SSO on iOS see: https://developers.facebook.com/blog/post/640/
   /// </remarks>
   public enum FacebookAuthPermission : int
   {
      /// <summary>Read-only permissions.</summary>
      Read = 0,

      /// <summary>The 'publish_actions' permission required for Carrot.</summary>
      PublishActions = 1,
   }

   /// <summary>
   /// Gets the <see cref="CarrotBridge"/> singleton.
   /// </summary>
   /// <value> The <see cref="CarrotBridge"/> singleton.</value>
   public static CarrotBridge Instance
   {
      get
      {
         if(mInstance == null)
         {
            mInstance = FindObjectOfType(typeof(Carrot)) as Carrot;

            if(mInstance == null)
            {
               GameObject carrotGameObject = GameObject.Find("CarrotGameObject");
               if(carrotGameObject != null)
               {
                  mInstance = carrotGameObject.GetComponent<Carrot>();
               }
            }

            if(mInstance == null) throw new NullReferenceException("No Carrot instance found in current scene!");
         }
         return mInstance.mCarrot;
      }
   }

   /// <summary>
   /// Return the string value of an <see cref="AuthStatus"/> value.
   /// </summary>
   /// <returns>The string description of an <see cref="AuthStatus"/>.</returns>
   public static string authStatusString(AuthStatus authStatus)
   {
      switch(authStatus)
      {
         case AuthStatus.NotAuthorized: return "Carrot user has not authorized the application.";
         case AuthStatus.Undetermined: return "Carrot user status is undetermined.";
         case AuthStatus.ReadOnly: return "Carrot user has not allowed the 'publish_actions' permission.";
         case AuthStatus.Ready: return "Carrot user is authorized.";
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
   /// The delegate type for the <see cref="ApplicationLinkReceived"/> event.
   /// </summary>
   /// <param name="sender">The object which dispatched the <see cref="ApplicationLinkReceived"/> event.</param>
   /// <param name="targetURL">The target URL specified by the deep-link.</param>
   public delegate void ApplicationLinkReceivedHandler(object sender, string targetURL);

   /// <summary>
   /// The delegate type for the <see cref="UserAchievementListReceived"/> event.
   /// </summary>
   /// <param name="sender">The object which dispatched the <see cref="UserAchievementListReceived"/> event.</param>
   /// <param name="achievements">The achievements which the current Carrot user has earned.</param>
   public delegate void UserAchievementListReceivedHandler(object sender, IList<Achievement> achievements, string errors);

   /// <summary>
   /// The delegate type for the <see cref="FriendHighScoreListReceived"/> event.
   /// </summary>
   /// <param name="sender">The object which dispatched the <see cref="FriendHighScoreListReceived"/> event.</param>
   /// <param name="achievements">The high scores for the current Carrot user's friends.</param>
   public delegate void FriendHighScoreListReceivedHandler(object sender, IList<Score> scores, string errors);

   /// <summary>
   /// An event which will notify listeners when the authentication status for the Carrot user has changed.
   /// </summary>
   public static event AuthenticationStatusChangedHandler AuthenticationStatusChanged;

   /// <summary>
   /// An event which will notify listeners when the application recieves a deep-link from Facebook.
   /// </summary>
   /// <remarks>
   /// For more information about deep-linking see: https://developers.facebook.com/blog/post/2012/02/21/improving-app-distribution-on-ios/
   /// </remarks>
   public static event ApplicationLinkReceivedHandler ApplicationLinkReceived;

   /// <summary>
   /// An event which will notify listeners when the list of user achievements has been received.
   /// </summary>
   public static event UserAchievementListReceivedHandler UserAchievementListReceived;

   /// <summary>
   /// An event which will notify listeners when the list of friend high scores has been received.
   /// </summary>
   public static event FriendHighScoreListReceivedHandler FriendHighScoreListReceived;

   /// <summary>
   /// C# Representation of a Carrot Achievement.
   /// </summary>
   public class Achievement
   {
      public string Description;
      public string Identifier;
      public string ImageUrl;
      public string Title;

      public Achievement(IDictionary<string, object> fromJson)
      {
         Description = fromJson["description"] as string;
         Identifier = fromJson["identifier"] as string;
         ImageUrl = fromJson["image_url"] as string;
         Title = fromJson["title"] as string;
      }

      public override string ToString()
      {
         return string.Format("identifier: '{0}', title: '{1}', image: '{2}', description: '{3}'",
            Identifier, Title, ImageUrl, Description);
      }
   }

   /// <summary>
   /// C# Representation of a Carrot high score.
   /// </summary>
   public class Score
   {
      public long Value;
      public string Name;
      public string FacebookId;
      public bool IsCurrentUser;

      public Score(IDictionary<string, object> fromJson)
      {
         Dictionary<string, object> userJson = fromJson["user"] as Dictionary<string, object>;
         Value = (long)fromJson["score"];
         Name = userJson["name"] as string;
         FacebookId = userJson["id"] as string;
         IsCurrentUser = (bool)userJson["is_current_user"];
      }

      public override string ToString()
      {
         return string.Format("'{0}' ({1}): {2}, is current user? {3}",
            Name, FacebookId, Value, IsCurrentUser);
      }
   }

   /// <summary>
   /// A C# bridge to the native Carrot SDK.
   /// </summary>
   public class CarrotBridge : IDisposable
   {
      /// <summary>
      /// Construct a C# bridge to the native Carrot SDK.
      /// </summary>
      /// <param name="appId">Facebook Application Id.</param>
      /// <param name="appSecret">Carrot Application Secret.</param>
      public CarrotBridge(string appId, string appSecret)
      {
         mIsDisposed = false;
#if UNITY_ANDROID && !UNITY_EDITOR
         string hostname = "";
         string debugUDID = "";

         using(AndroidJavaClass playerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
         {
            using(AndroidJavaObject activity = playerClass.GetStatic<AndroidJavaObject>("currentActivity"),
                                    appIdString = new AndroidJavaObject("java.lang.String", appId),
                                    appSecretString = new AndroidJavaObject("java.lang.String", appSecret),
                                    hostnameString = new AndroidJavaObject("java.lang.String", hostname),
                                    debugUDIDString = new AndroidJavaObject("java.lang.String", debugUDID))
            {
               mCarrot = new AndroidJavaObject("com.CarrotInc.Carrot.Carrot", activity, appIdString,
                                               appSecretString, hostnameString, debugUDIDString);
            }
         }
#endif
      }

      /// <summary>
      /// Check the authentication status of the current Carrot user.
      /// </summary>
      /// <value>The <see cref="AuthStatus"/> of the current Carrot user.</value>
      public AuthStatus Status
      {
         get
         {
#if UNITY_ANDROID  && !UNITY_EDITOR
            return (AuthStatus)mCarrot.Call<int>("getStatus");
#elif !UNITY_EDITOR
            return (AuthStatus)Carrot_AuthStatus();
#else
            return AuthStatus.Undetermined;
#endif
         }
      }

      /// <summary>
      /// Assign a Facebook user access token to allow posting of Carrot events.
      /// </summary>
      /// <param name="accessToken">Facebook user access token.</param>
      public void setAccessToken(string accessToken)
      {
#if UNITY_ANDROID && !UNITY_EDITOR
         using(AndroidJavaObject accessTokenString = new AndroidJavaObject("java.lang.String", accessToken))
         {
            mCarrot.Call("setAccessToken", accessTokenString);
         }
#elif !UNITY_EDITOR
         Carrot_SetAccessToken(accessToken);
#endif
      }

      /// <summary>
      /// Post an achievement to Carrot.
      /// </summary>
      /// <param name="achievementId">Carrot achievement id.</param>
      /// <returns><c>true</c> if the achievement request has been cached, and will be sent to the server; <c>false</c> otherwise.</returns>
      public bool postAchievement(string achievementId)
      {
#if UNITY_ANDROID && !UNITY_EDITOR
         using(AndroidJavaObject achievementIdString = new AndroidJavaObject("java.lang.String", achievementId))
         {
            return mCarrot.Call<bool>("postAchievement", achievementIdString);
         }
#elif !UNITY_EDITOR
         return (Carrot_PostAchievement(achievementId) == 1);
#else
         Debug.Log("Carrot:postAchievement('" + achievementId + "')");
         return true;
#endif
      }

      /// <summary>
      /// Get the list of achievements the current Carrot user has earned.
      /// </summary>
      public void getUserAchievements()
      {
#if UNITY_ANDROID && !UNITY_EDITOR
         mCarrot.Call("getUserAchievementsUnity", mDelegateObject.name);
#elif !UNITY_EDITOR
         Carrot_GetUserAchievementsUnity(mDelegateObject.name);
#else
         Debug.Log("Carrot:getUserAchievements()");
         mDelegateObject.SendMessage("userAchievementListReceived", "{\"code\":200,\"data\":[{\"description\":\"\\\"That\'s not how a chicken dances, chickens don\'t clap!\\\"\",\"identifier\":\"chicken\",\"image_url\":\"https://d2h7sc2qwu171k.cloudfront.net/assets/gob-bluth-gif-7-1.gif\",\"points\":10,\"title\":\"Chicken\"},{\"description\":\"Here drink this, now write the check.\",\"identifier\":\"funded\",\"image_url\":\"https://d2h7sc2qwu171k.cloudfront.net/assets/1eyhp.gif\",\"points\":20,\"title\":\"Funded\"}]}");
#endif
      }

      /// <summary>
      /// Post a high score to Carrot.
      /// </summary>
      /// <param name="score">Score.</param>
      /// <returns><c>true</c> if the high score request has been cached, and will be sent to the server; <c>false</c> otherwise.</returns>
      public bool postHighScore(uint score)
      {
#if UNITY_ANDROID && !UNITY_EDITOR
         return mCarrot.Call<bool>("postHighScore", (int)score);
#elif !UNITY_EDITOR
         return (Carrot_PostHighScore(score) == 1);
#else
         Debug.Log("Carrot::postHighScore(" + score + ")");
         return true;
#endif
      }

      /// <summary>
      /// Get the the scores for the current Carrot user and their Facebook friends.
      /// </summary>
      public void getFriendScores()
      {
#if UNITY_ANDROID && !UNITY_EDITOR
         mCarrot.Call("getFriendHighScoresUnity", mDelegateObject.name);
#elif !UNITY_EDITOR
         Carrot_GetFriendScoresUnity(mDelegateObject.name);
#else
         Debug.Log("Carrot:getFriendScores()");
         mDelegateObject.SendMessage("friendHighScoresReceived", "{\"code\":200,\"data\":[{\"score\": 10000000, \"user\": {\"is_current_user\": true, \"name\": \"Pat Wilson\", \"id\": \"532815528\"}}, {\"score\": 0, \"user\": {\"is_current_user\": false, \"name\": \"Mark McCoy\", \"id\": \"581337186\"}}]}");
#endif
      }

      /// <summary>
      /// Sends an Open Graph action which will use an existing object.
      /// </summary>
      /// <param name="actionId">Carrot action id.</param>
      /// <param name="objectInstanceId">Carrot object instance id.</param>
      public bool postAction(string actionId, string objectInstanceId)
      {
         return postAction(actionId, null, objectInstanceId);
      }

      /// <summary>
      /// Sends an Open Graph action which will use an existing object.
      /// </summary>
      /// <param name="actionId">Carrot action id.</param>
      /// <param name="actionProperties">Parameters to be submitted with the action.</param>
      /// <param name="objectInstanceId">Carrot object instance id.</param>
      public bool postAction(string actionId, IDictionary actionProperties, string objectInstanceId)
      {
#if UNITY_ANDROID && !UNITY_EDITOR
         string actionPropertiesJson = (actionProperties == null ? "" : Json.Serialize(actionProperties));
         using(AndroidJavaObject actionIdString = new AndroidJavaObject("java.lang.String", actionId),
                                 actionPropertiesString = new AndroidJavaObject("java.lang.String", actionPropertiesJson),
                                 objectInstanceIdString = new AndroidJavaObject("java.lang.String", objectInstanceId))
         {
            return mCarrot.Call<bool>("postJsonAction", actionIdString, actionPropertiesString, objectInstanceIdString);
         }
#elif !UNITY_EDITOR
         string actionPropertiesJson = (actionProperties == null ? null : Json.Serialize(actionProperties));
         return (Carrot_PostInstanceAction(actionId, actionPropertiesJson, objectInstanceId) == 1);
#else
         string actionPropertiesJson = (actionProperties == null ? "" : Json.Serialize(actionProperties));
         Debug.Log("Carrot::postAction('" + actionId + "', " + actionPropertiesJson + ", '" + objectInstanceId + "')");
         return true;
#endif
      }

      /// <summary>
      /// Sends an Open Graph action which will create a new object from the properties provided.
      /// </summary>
      /// <param name="actionId">Carrot action id.</param>
      /// <param name="objectId">Carrot object id.</param>
      /// <param name="objectProperties">Parameters to be submitted with the action.</param>
      /// <param name="objectInstanceId">Object instance id to create or re-use.</param>
      public bool postAction(string actionId, string objectId, IDictionary objectProperties, string objectInstanceId = null)
      {
         return postAction(actionId, null, objectId, objectProperties, objectInstanceId);
      }

      /// <summary>
      /// Sends an Open Graph action which will create a new object from the properties provided.
      /// </summary>
      /// <param name="actionId">Carrot action id.</param>
      /// <param name="actionProperties">Parameters to be submitted with the action.</param>
      /// <param name="objectId">Carrot object id.</param>
      /// <param name="objectProperties">Parameters to be submitted with the action.</param>
      /// <param name="objectInstanceId">Object instance id to create or re-use.</param>
      /// <returns><c>true</c> if the action request has been cached, and will be sent to the server; <c>false</c> otherwise.</returns>
      public bool postAction(string actionId, IDictionary actionProperties, string objectId, IDictionary objectProperties, string objectInstanceId = null)
      {
         string objectPropertiesJson = Json.Serialize(objectProperties);
#if UNITY_ANDROID && !UNITY_EDITOR
         if(objectInstanceId == null) objectInstanceId = "";
         string actionPropertiesJson = (actionProperties == null ? "" : Json.Serialize(actionProperties));
         using(AndroidJavaObject actionIdString = new AndroidJavaObject("java.lang.String", actionId),
                                 actionPropertiesString = new AndroidJavaObject("java.lang.String", actionPropertiesJson),
                                 objectIdString = new AndroidJavaObject("java.lang.String", objectId),
                                 objectPropertiesString = new AndroidJavaObject("java.lang.String", objectPropertiesJson),
                                 objectInstanceIdString = new AndroidJavaObject("java.lang.String", objectInstanceId))
         {
            return mCarrot.Call<bool>("postJsonAction", actionIdString, actionPropertiesString, objectIdString, objectPropertiesString, objectInstanceIdString);
         }
#elif !UNITY_EDITOR
         string actionPropertiesJson = (actionProperties == null ? null : Json.Serialize(actionProperties));
         return (Carrot_PostCreateAction(actionId, actionPropertiesJson, objectId, objectPropertiesJson, objectInstanceId) == 1);
#else
         string actionPropertiesJson = (actionProperties == null ? "" : Json.Serialize(actionProperties));
         Debug.Log("Carrot::postAction('" + actionId + "', " + actionPropertiesJson + ", '" + objectId + "', " + objectPropertiesJson + ")");
         return true;
#endif
      }

      /// <summary>
      /// Post a 'Like' action that likes the Game's Facebook Page.
      /// </summary>
      /// <returns><c>true</c> if the action request has been cached, and will be sent to the server; <c>false</c> otherwise.</returns>
      public bool likeGame()
      {
#if UNITY_ANDROID && !UNITY_EDITOR
         return mCarrot.Call<bool>("likeGame");
#elif !UNITY_EDITOR
         return (Carrot_LikeGame() == 1);
#else
         Debug.Log("Carrot::likeGame()");
         return true;
#endif
      }

      /// <summary>
      /// Post a 'Like' action that likes the Publisher's Facebook Page.
      /// </summary>
      /// <returns><c>true</c> if the action request has been cached, and will be sent to the server; <c>false</c> otherwise.</returns>
      public bool likePublisher()
      {
#if UNITY_ANDROID && !UNITY_EDITOR
         return mCarrot.Call<bool>("likePublisher");
#elif !UNITY_EDITOR
         return (Carrot_LikePublisher() == 1);
#else
         Debug.Log("Carrot::likePublisher()");
         return true;
#endif
      }

      /// <summary>
      /// Post a 'Like' action that likes an achievement.
      /// </summary>
      /// <param name="achievementId">The achievement identifier.</param>
      /// <returns><c>true</c> if the action request has been cached, and will be sent to the server; <c>false</c> otherwise.</returns>
      public bool likeAchievement(string achievementId)
      {
#if UNITY_ANDROID && !UNITY_EDITOR
         using(AndroidJavaObject achievementIdString = new AndroidJavaObject("java.lang.String", achievementId))
         {
            return mCarrot.Call<bool>("likeAchievement", achievementIdString);
         }
#elif !UNITY_EDITOR
         return (Carrot_LikeAchievement(achievementId) == 1);
#else
         Debug.Log("Carrot::likeAchievement('" + achievementId + "')");
         return true;
#endif
      }

      /// <summary>
      /// Post a 'Like' action that likes an Open Graph object.
      /// </summary>
      /// <param name="objectId">The instance id of the Carrot object.</param>
      /// <returns><c>true</c> if the action request has been cached, and will be sent to the server; <c>false</c> otherwise.</returns>
      public bool likeObject(string objectId)
      {
#if UNITY_ANDROID && !UNITY_EDITOR
         using(AndroidJavaObject objectIdString = new AndroidJavaObject("java.lang.String", objectId))
         {
            return mCarrot.Call<bool>("likeObject", objectIdString);
         }
#elif !UNITY_EDITOR
         return (Carrot_LikeObject(objectId) == 1);
#else
         Debug.Log("Carrot::likeObject('" + objectId + "')");
         return true;
#endif
      }

      /// <summary>
      /// Perform Facebook Authentication.
      /// </summary>
      /// <param name="allowLoginUI">Allow the login UI to be shown if the Application is not authenticated.</param>
      /// <param name="permission">Specify the permissions being requested. FB standards suggest that you should first ask only for read permissions, and then ask for write permissions at the time when they are needed.</param>
      /// <returns><c>false</c> if there are no Facebook accounts registered with the device (iOS 6 only), or the Intent was not defined in AndroidManifest.xml (Android only); <c>true</c> otherwise.</returns>
      public bool doFacebookAuth(bool allowLoginUI = true, FacebookAuthPermission permission = FacebookAuthPermission.ReadWrite)
      {
#if UNITY_ANDROID && !UNITY_EDITOR
         return mCarrot.Call<bool>("doFacebookAuth", allowLoginUI, (int)permission);
#elif !UNITY_EDITOR
         return (Carrot_DoFacebookAuth(allowLoginUI ? 1 : 0, (int)permission) == 1);
#else
         Debug.Log("Carrot::doFacebookAuth");
         return true;
#endif
      }

#if UNITY_ANDROID && !UNITY_EDITOR
      internal void setActivity()
      {
         using(AndroidJavaClass playerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
         {
            using(AndroidJavaObject activity = playerClass.GetStatic<AndroidJavaObject>("currentActivity"))
            {
               mCarrot.Call("setActivity", activity);
            }
         }
      }
#endif

      internal void setDelegateObject(MonoBehaviour delegateObject)
      {
         mDelegateObject = delegateObject;
#if UNITY_ANDROID && !UNITY_EDITOR
         mCarrot.Call("setUnityHandler", mDelegateObject.name);
#elif !UNITY_EDITOR
         Carrot_AssignUnityDelegate(mDelegateObject.name);
#endif
      }

      /// @cond hide_from_doxygen
      #region IDisposable
      public void Dispose()
      {
         Dispose(true);
         GC.SuppressFinalize(this); 
      }

      protected virtual void Dispose(bool disposing)
      {
         if(!mIsDisposed)
         {
#if UNITY_ANDROID && !UNITY_EDITOR
            if(disposing)
            {
               if(mCarrot != null)
               {
                  mCarrot.Call("close");
                  mCarrot.Dispose();
                  mCarrot = null;
               }
            }
#endif
         }
         mIsDisposed = true;
      }

      ~CarrotBridge()
      {
         Dispose(false);
      }
      #endregion
      /// @endcond

#if !UNITY_ANDROID && !UNITY_EDITOR
      #region Dll Imports
#if UNITY_IPHONE
      private const string DLL_IMPORT_TARGET = "__Internal";
#else
      private const string DLL_IMPORT_TARGET = "Carrot";
#endif
      [DllImport(DLL_IMPORT_TARGET)]
      private extern static int Carrot_AuthStatus();

      [DllImport(DLL_IMPORT_TARGET)]
      private extern static void Carrot_SetAccessToken(
         [MarshalAs(UnmanagedType.LPStr)] string accessToken);

      [DllImport(DLL_IMPORT_TARGET)]
      private extern static int Carrot_PostAchievement(
         [MarshalAs(UnmanagedType.LPStr)] string achievementId);

      [DllImport(DLL_IMPORT_TARGET)]
      private extern static void Carrot_GetUserAchievementsUnity(
         [MarshalAs(UnmanagedType.LPStr)] string objectName);

      [DllImport(DLL_IMPORT_TARGET)]
      private extern static int Carrot_PostHighScore(uint score);

      [DllImport(DLL_IMPORT_TARGET)]
      private extern static void Carrot_GetFriendScoresUnity(
         [MarshalAs(UnmanagedType.LPStr)] string objectName);

      [DllImport(DLL_IMPORT_TARGET)]
      private extern static int Carrot_PostInstanceAction(
         [MarshalAs(UnmanagedType.LPStr)] string actionId,
         [MarshalAs(UnmanagedType.LPStr)] string actionPropertiesJson,
         [MarshalAs(UnmanagedType.LPStr)] string objectInstanceId);

      [DllImport(DLL_IMPORT_TARGET)]
      private extern static int Carrot_PostCreateAction(
         [MarshalAs(UnmanagedType.LPStr)] string actionId,
         [MarshalAs(UnmanagedType.LPStr)] string actionPropertiesJson,
         [MarshalAs(UnmanagedType.LPStr)] string objectId,
         [MarshalAs(UnmanagedType.LPStr)] string objectPropertiesJson,
         [MarshalAs(UnmanagedType.LPStr)] string objectInstanceId);

      [DllImport(DLL_IMPORT_TARGET)]
      private extern static int Carrot_DoFacebookAuth(
         int allowLoginUI, int permission);

      [DllImport(DLL_IMPORT_TARGET)]
      private extern static void Carrot_AssignUnityDelegate(
         [MarshalAs(UnmanagedType.LPStr)] string objectName);

      [DllImport(DLL_IMPORT_TARGET)]
      private extern static int Carrot_LikeGame();

      [DllImport(DLL_IMPORT_TARGET)]
      private extern static int Carrot_LikePublisher();

      [DllImport(DLL_IMPORT_TARGET)]
      private extern static int Carrot_LikeAchievement(
         [MarshalAs(UnmanagedType.LPStr)] string achievementId);

      [DllImport(DLL_IMPORT_TARGET)]
      private extern static int Carrot_LikeObject(
         [MarshalAs(UnmanagedType.LPStr)] string objectId);

      #endregion
#endif

      #region Member Variables
#if UNITY_ANDROID && !UNITY_EDITOR
      AndroidJavaObject mCarrot;
#endif
      bool mIsDisposed;
      MonoBehaviour mDelegateObject;
      #endregion
   }

   #region MonoBehaviour
   void Start()
   {
      mInstance = this;
      DontDestroyOnLoad(this);
      mCarrot = new CarrotBridge(FacebookAppId, CarrotAppSecret);
      mCarrot.setDelegateObject(this);
   }

   void OnDestroy()
   {
      if(mCarrot != null) mCarrot.Dispose();
   }

#if UNITY_ANDROID && !UNITY_EDITOR
   void OnApplicationPause(bool paused)
   {
      if(!paused)
      {
         mCarrot.setActivity();
      }
   }
#endif

   void OnApplicationQuit()
   {
      Destroy(this);
   }
   #endregion

   /// @cond hide_from_doxygen
   #region UnitySendMessage Handlers
   public void authenticationStatusChanged(string message)
   {
      AuthStatus updatedStatus = (AuthStatus)int.Parse(message);
      if(Debug.isDebugBuild)
      {
         Debug.Log("[Carrot] Auth Status: " + Carrot.authStatusString(updatedStatus));
      }

      if(AuthenticationStatusChanged != null)
      {
         AuthenticationStatusChanged(this, updatedStatus);
      }
   }

   public void applicationLinkReceived(string message)
   {
      if(ApplicationLinkReceived != null)
      {
         ApplicationLinkReceived(this, message);
      }
   }

   public void userAchievementListReceived(string message)
   {
      Dictionary<string, object> reply = Json.Deserialize(message) as Dictionary<string, object>;
      List<Achievement> achievements = new List<Achievement>();

      if(reply.ContainsKey("data"))
      {
         List<object> achievementsJson = reply["data"] as List<object>;
         foreach(Dictionary<string, object> jsonAchievement in achievementsJson)
         {
            achievements.Add(new Achievement(jsonAchievement));
         }
      }

      if(Debug.isDebugBuild)
      {
         if(reply.ContainsKey("error"))
         {
            Dictionary<string, object> error = reply["error"] as Dictionary<string, object>;
            Debug.Log("Error retrieving user achievement list (" + error["code"] + "): " + error["message"]);
         }
      }

      if(UserAchievementListReceived != null)
      {
         if(reply.ContainsKey("error"))
         {
            Dictionary<string, object> error = reply["error"] as Dictionary<string, object>;
            UserAchievementListReceived(this, null, error["message"] as string);
         }
         else
         {
            UserAchievementListReceived(this, achievements, null);
         }
      }
   }

   public void friendHighScoresReceived(string message)
   {
      Dictionary<string, object> reply = Json.Deserialize(message) as Dictionary<string, object>;
      List<Score> scores = new List<Score>();

      if(reply.ContainsKey("data"))
      {
         List<object> scoresJson = reply["data"] as List<object>;
         foreach(Dictionary<string, object> jsonScore in scoresJson)
         {
            scores.Add(new Score(jsonScore));
         }
      }

      if(Debug.isDebugBuild)
      {
         if(reply.ContainsKey("error"))
         {
            Dictionary<string, object> error = reply["error"] as Dictionary<string, object>;
            Debug.Log("Error retrieving high score list (" + error["code"] + "): " + error["message"]);
         }
      }

      if(FriendHighScoreListReceived != null)
      {
         if(reply.ContainsKey("error"))
         {
            Dictionary<string, object> error = reply["error"] as Dictionary<string, object>;
            FriendHighScoreListReceived(this, null, error["message"] as string);
         }
         else
         {
            FriendHighScoreListReceived(this, scores, null);
         }
      }
   }
   #endregion
   /// @endcond

   #region Member Variables
   private CarrotBridge mCarrot;
   private static Carrot mInstance = null;
   #endregion
}
