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
   public static string authStatusString(GoCarrotInc.Carrot.AuthStatus authStatus)
   {
      switch(authStatus)
      {
         case GoCarrotInc.Carrot.AuthStatus.NotAuthorized: return "Carrot user has not authorized the application.";
         case GoCarrotInc.Carrot.AuthStatus.Undetermined: return "Carrot user status is undetermined.";
         case GoCarrotInc.Carrot.AuthStatus.ReadOnly: return "Carrot user has not allowed the 'publish_actions' permission.";
         case GoCarrotInc.Carrot.AuthStatus.Ready: return "Carrot user is authorized.";
         default: return "Invalid Carrot AuthStatus.";
      }
   }

   /// <summary>
   /// The delegate type for the <see cref="AuthenticationStatusChanged"/> event.
   /// </summary>
   /// <param name="sender">The object which dispatched the <see cref="AuthenticationStatusChanged"/> event.</param>
   /// <param name="status">The new authentication status.</param>
   public delegate void AuthenticationStatusChangedHandler(object sender, GoCarrotInc.Carrot.AuthStatus status);

   /// <summary>
   /// The delegate type for the <see cref="ApplicationLinkReceived"/> event.
   /// </summary>
   /// <param name="sender">The object which dispatched the <see cref="ApplicationLinkReceived"/> event.</param>
   /// <param name="targetURL">The target URL specified by the deep-link.</param>
   public delegate void ApplicationLinkReceivedHandler(object sender, string targetURL);

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
#elif UNITY_EDITOR || (!UNITY_ANDROID && !UNITY_IPHONE)
         mCarrot = new GoCarrotInc.Carrot(appId, appSecret, null);
         mAuthStatusUpdates = new List<GoCarrotInc.Carrot.AuthStatus>();
#endif
      }

      /// <summary>
      /// Check the authentication status of the current Carrot user.
      /// </summary>
      /// <value>The <see cref="GoCarrotInc.Carrot.AuthStatus"/> of the current Carrot user.</value>
      public GoCarrotInc.Carrot.AuthStatus Status
      {
         get
         {
#if UNITY_ANDROID  && !UNITY_EDITOR
            return (GoCarrotInc.Carrot.AuthStatus)mCarrot.Call<int>("getStatus");
#elif UNITY_IPHONE && !UNITY_EDITOR
            return (GoCarrotInc.Carrot.AuthStatus)Carrot_AuthStatus();
#else
            return mCarrot.Status;
#endif
         }
      }

#if UNITY_EDITOR || (!UNITY_ANDROID && !UNITY_IPHONE)
      /// <summary>
      /// The user id for the current Carrot user.
      /// </summary>
      /// <value>The user id of the current Carrot user.</value>
      public string UserId
      {
         get
         {
            return mCarrot.UserId;
         }
         set
         {
            mCarrot.UserId = value;
         }
      }
#endif

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
#elif UNITY_IPHONE && !UNITY_EDITOR
         Carrot_SetAccessToken(accessToken);
#else
         mCarrot.validateUserAsync(accessToken);
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
#elif UNITY_IPHONE && !UNITY_EDITOR
         return (Carrot_PostAchievement(achievementId) == 1);
#else
         mCarrot.postAchievementAsync(achievementId);
         return true;
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
#elif UNITY_IPHONE && !UNITY_EDITOR
         return (Carrot_PostHighScore(score) == 1);
#else
         mCarrot.postHighScoreAsync(score);
         return true;
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
#elif UNITY_IPHONE && !UNITY_EDITOR
         string actionPropertiesJson = (actionProperties == null ? null : Json.Serialize(actionProperties));
         return (Carrot_PostInstanceAction(actionId, actionPropertiesJson, objectInstanceId) == 1);
#else
         mCarrot.postActionAsync(actionId, actionProperties, objectInstanceId);
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
#if UNITY_ANDROID && !UNITY_EDITOR
         string objectPropertiesJson = Json.Serialize(objectProperties);
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
#elif UNITY_IPHONE && !UNITY_EDITOR
         string objectPropertiesJson = Json.Serialize(objectProperties);
         string actionPropertiesJson = (actionProperties == null ? null : Json.Serialize(actionProperties));
         return (Carrot_PostCreateAction(actionId, actionPropertiesJson, objectId, objectPropertiesJson, objectInstanceId) == 1);
#else
         mCarrot.postActionAsync(actionId, actionProperties, objectId, objectProperties, objectInstanceId);
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
#elif UNITY_IPHONE && !UNITY_EDITOR
         return (Carrot_LikeGame() == 1);
#else
         mCarrot.likeGameAsync();
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
#elif UNITY_IPHONE && !UNITY_EDITOR
         return (Carrot_LikePublisher() == 1);
#else
         mCarrot.likePublisherAsync();
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
#elif UNITY_IPHONE && !UNITY_EDITOR
         return (Carrot_LikeAchievement(achievementId) == 1);
#else
         mCarrot.likeAchievementAsync(achievementId);
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
#elif UNITY_IPHONE && !UNITY_EDITOR
         return (Carrot_LikeObject(objectId) == 1);
#else
         mCarrot.likeObjectAsync(objectId);
         return true;
#endif
      }

      /// <summary>
      /// Perform Facebook Authentication.
      /// </summary>
      /// <param name="allowLoginUI">Allow the login UI to be shown if the Application is not authenticated.</param>
      /// <param name="permission">Specify the permissions being requested. FB standards suggest that you should first ask only for read permissions, and then ask for write permissions at the time when they are needed.</param>
      /// <returns><c>false</c> if there are no Facebook accounts registered with the device (iOS 6 only), or the Intent was not defined in AndroidManifest.xml (Android only); <c>true</c> otherwise.</returns>
      public bool doFacebookAuth(bool allowLoginUI = true, FacebookAuthPermission permission = FacebookAuthPermission.Read)
      {
#if UNITY_ANDROID && !UNITY_EDITOR
         return mCarrot.Call<bool>("doFacebookAuth", allowLoginUI, (int)permission);
#elif UNITY_IPHONE && !UNITY_EDITOR
         return (Carrot_DoFacebookAuth(allowLoginUI ? 1 : 0, (int)permission) == 1);
#elif UNITY_EDITOR
         Debug.Log("Carrot::doFacebookAuth");
         return true;
#else
         Debug.Log("Carrot Facebook Authentication wrapping is not available outside of iOS/Android.");
         return false;
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

      internal void setDelegateObject(Carrot delegateObject)
      {
#if UNITY_ANDROID && !UNITY_EDITOR
         mDelegateObject = delegateObject;
         mCarrot.Call("setUnityHandler", mDelegateObject.name);
#elif UNITY_IPHONE && !UNITY_EDITOR
         mDelegateObject = delegateObject;
         Carrot_AssignUnityDelegate(mDelegateObject.name);
#else
         mCarrot.AuthenticationStatusChanged += (object sender, GoCarrotInc.Carrot.AuthStatus status) => {
            lock(mAuthStatusUpdates)
            {
               mAuthStatusUpdates.Add(status);
            }
         };
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

#if UNITY_IPHONE && !UNITY_EDITOR
      #region Dll Imports
      private const string DLL_IMPORT_TARGET = "__Internal";

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
#if UNITY_IPHONE && !UNITY_EDITOR
      Carrot mDelegateObject;
#elif UNITY_ANDROID && !UNITY_EDITOR
      AndroidJavaObject mCarrot;
      Carrot mDelegateObject;
#elif UNITY_EDITOR || (!UNITY_ANDROID && !UNITY_IPHONE)
      GoCarrotInc.Carrot mCarrot;
      internal List<GoCarrotInc.Carrot.AuthStatus> mAuthStatusUpdates;
#endif
      bool mIsDisposed;
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

#if UNITY_EDITOR && (!UNITY_ANDROID && !UNITY_IPHONE)
   void FixedUpdate()
   {
      // Ensure the events are triggered from the main thread
      if(mCarrot.mAuthStatusUpdates.Count > 0)
      {
         lock(mCarrot.mAuthStatusUpdates)
         {
            if(AuthenticationStatusChanged != null)
            {
               foreach(GoCarrotInc.Carrot.AuthStatus status in mCarrot.mAuthStatusUpdates)
               {
                  AuthenticationStatusChanged(this, status);
               }
            }
            mCarrot.mAuthStatusUpdates.Clear();
         }
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
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IPHONE)
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
#endif
   #endregion
   /// @endcond

   #region Member Variables
   private CarrotBridge mCarrot;
   private static Carrot mInstance = null;
   #endregion
}
