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
/// A <see cref="UnityEngine.MonoBehaviour"/> which can be attached to a Unity <see cref="UnityEngine.GameObject"/>
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
   /// Gets the <see cref="CarrotBridge"/> singleton.
   /// </summary>
   /// <value> The <see cref="CarrotBridge"/> singleton.</value>
   public static CarrotBridge Instance
   {
      get
      {
         if(mInstance == null)
            mInstance = FindObjectOfType(typeof(Carrot)) as Carrot;
         return mInstance.mCarrot;
      }
   }

   public enum AuthStatus : int
   {
      Denied = -1,
      Undetermined = 0,
      ReadOnly = 1,
      Ready = 2
   }

   public enum FacebookAuthPermission : int
   {
      Read = 0,
      PublishActions = 1,
      ReadWrite = 2 // Not suggested
   }

   public delegate void AuthenticationStatusChangedHandler(object sender, AuthStatus status);
   public delegate void ApplicationLinkRecievedHandler(object sender, string targetURL);

   public event AuthenticationStatusChangedHandler AuthenticationStatusChanged;
   public event ApplicationLinkRecievedHandler ApplicationLinkRecieved;

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
#if UNITY_ANDROID
         string hostname = null;
         string debugUDID = null;

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
      /// <value><c>true</c> if Carrot is authenticated and sending requests; <c>false</c> otherwise.</value>
      public AuthStatus Status
      {
         get
         {
#if UNITY_ANDROID
            return (AuthStatus)mCarrot.Call<int>("getStatus");
#else
            return (AuthStatus)Carrot_AuthStatus();
#endif
         }
      }

      /// <summary>
      /// Assign a Facebook user access token to allow posting of Carrot events.
      /// </summary>
      /// <param name="accessToken">Facebook user access token.</param>
      public void setAccessToken(string accessToken)
      {
#if UNITY_ANDROID
         using(AndroidJavaObject accessTokenString = new AndroidJavaObject("java.lang.String", accessToken))
         {
            mCarrot.Call("setAccessToken", accessTokenString);
         }
#else
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
#if UNITY_ANDROID
         using(AndroidJavaObject achievementIdString = new AndroidJavaObject("java.lang.String", achievementId))
         {
            return mCarrot.Call<bool>("postAchievement", achievementIdString);
         }
#else
         return (Carrot_PostAchievement(achievementId) == 1);
#endif
      }

      /// <summary>
      /// Post a high score to Carrot.
      /// </summary>
      /// <param name="achievementId">Score.</param>
      /// <param name="leaderboardId">Leaderboard Id.</param>
      /// <returns><c>true</c> if the high score request has been cached, and will be sent to the server; <c>false</c> otherwise.</returns>
      public bool postHighScore(uint score, string leaderboardId = null)
      {
#if UNITY_ANDROID
         using(AndroidJavaObject leaderboardIdString = new AndroidJavaObject("java.lang.String", leaderboardId))
         {
            return mCarrot.Call<bool>("postHighScore", (int)score, leaderboardIdString);
         }
#else
         return (Carrot_PostHighScore(score, leaderboardId) == 1);
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
         string actionPropertiesJson = (actionProperties == null ? null : Json.Serialize(actionProperties));
#if UNITY_ANDROID
         using(AndroidJavaObject actionIdString = new AndroidJavaObject("java.lang.String", actionId),
                                 actionPropertiesString = new AndroidJavaObject("java.lang.String", actionPropertiesJson),
                                 objectInstanceIdString = new AndroidJavaObject("java.lang.String", objectInstanceId))
         {
            return mCarrot.Call<bool>("postJsonAction", actionIdString, actionPropertiesString, objectInstanceIdString);
         }
#else
         return (Carrot_PostInstanceAction(actionId, actionPropertiesJson, objectInstanceId) == 1);
#endif
      }

      /// <summary>
      /// Sends an Open Graph action which will create a new object from the properties provided.
      /// </summary>
      /// <param name="actionId">Carrot action id.</param>
      /// <param name="objectId">Carrot object id.</param>
      /// <param name="objectProperties">Parameters to be submitted with the action.</param>
      public bool postAction(string actionId, string objectId, IDictionary objectProperties)
      {
         return postAction(actionId, null, objectId, objectProperties);
      }

      /// <summary>
      /// Sends an Open Graph action which will create a new object from the properties provided.
      /// </summary>
      /// <param name="actionId">Carrot action id.</param>
      /// <param name="actionProperties">Parameters to be submitted with the action.</param>
      /// <param name="objectId">Carrot object id.</param>
      /// <param name="objectProperties">Parameters to be submitted with the action.</param>
      /// <returns><c>true</c> if the action request has been cached, and will be sent to the server; <c>false</c> otherwise.</returns>
      public bool postAction(string actionId, IDictionary actionProperties, string objectId, IDictionary objectProperties)
      {
         string actionPropertiesJson = (actionProperties == null ? null : Json.Serialize(actionProperties));
         string objectPropertiesJson = Json.Serialize(objectProperties);
#if UNITY_ANDROID
         using(AndroidJavaObject actionIdString = new AndroidJavaObject("java.lang.String", actionId),
                                 actionPropertiesString = new AndroidJavaObject("java.lang.String", actionPropertiesJson),
                                 objectIdString = new AndroidJavaObject("java.lang.String", objectId),
                                 objectPropertiesString = new AndroidJavaObject("java.lang.String", objectPropertiesJson))
         {
            return mCarrot.Call<bool>("postJsonAction", actionIdString, actionPropertiesString, objectIdString, objectPropertiesString);
         }
#else
         return (Carrot_PostCreateAction(actionId, actionPropertiesJson, objectId, objectPropertiesJson) == 1);
#endif
      }

      /// <summary>
      /// Perform Facebook Authentication.
      /// </summary>
      /// <param name="allowLoginUI">(iOS only) Allow the login UI to be shown if the Application is not authenticated.</param>
      /// <param name="permission">(iOS only) Specify the permissions being requested. FB/iOS standards suggest that you should first ask only for read permissions, and then ask for write permissions at the time when they are needed.</param>
      /// <returns><c>false</c> if there are no Facebook accounts registered with the device (iOS 6 only), or the Intent was not defined in AndroidManifest.xml (Android only); <c>true</c> otherwise.</returns>
      public bool doFacebookAuth(bool allowLoginUI = true, FacebookAuthPermission permission = FacebookAuthPermission.ReadWrite)
      {
#if UNITY_ANDROID
         return mCarrot.Call<bool>("doFacebookAuth");
#else
         return (Carrot_DoFacebookAuth(allowLoginUI ? 1 : 0, (int)permission) == 1);
#endif
      }

#if UNITY_ANDROID
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
#if UNITY_ANDROID
         mCarrot.Call("setUnityHandler", delegateObject.name);
#else
         Carrot_AssignUnityDelegate(delegateObject.name);
#endif
      }

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
#if UNITY_ANDROID
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

#if !UNITY_ANDROID
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
      private extern static int Carrot_PostHighScore(uint score,
         [MarshalAs(UnmanagedType.LPStr)] string leaderboardId);

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
         [MarshalAs(UnmanagedType.LPStr)] string objectPropertiesJson);

      [DllImport(DLL_IMPORT_TARGET)]
      private extern static int Carrot_DoFacebookAuth(
         int allowLoginUI, int permission);

      [DllImport(DLL_IMPORT_TARGET)]
      private extern static void Carrot_AssignUnityDelegate(
         [MarshalAs(UnmanagedType.LPStr)] string objectName);

      #endregion
#endif

      #region Member Variables
#if UNITY_ANDROID
      AndroidJavaObject mCarrot;
#endif
      bool mIsDisposed;
      #endregion
   }

   #region MonoBehaviour
   void Awake()
   {
      if(mDestroying) return;

      mInstance = this;
      DontDestroyOnLoad(this);
      mCarrot = new CarrotBridge(FacebookAppId, CarrotAppSecret);
      mCarrot.setDelegateObject(this);
   }

   void OnDestroy()
   {
      mDestroying = true;
      mCarrot.Dispose();
   }

#if UNITY_ANDROID
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
      mDestroying = true;
      Destroy(this);
   }
   #endregion

   #region UnitySendMessage Handlers
   public void authenticationStatusChanged(string message)
   {
      if(AuthenticationStatusChanged != null)
      {
         AuthenticationStatusChanged(this, (AuthStatus)int.Parse(message));
      }
   }

   public void applicationLinkRecieved(string message)
   {
      if(ApplicationLinkRecieved != null)
      {
         ApplicationLinkRecieved(this, message);
      }
   }
   #endregion

   #region Member Variables
   private bool mDestroying = false;
   private CarrotBridge mCarrot;
   private static Carrot mInstance = null;
   #endregion
}

