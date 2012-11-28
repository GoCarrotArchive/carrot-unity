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

   public enum AuthStatus : int
   {
      NotAuthorized = -1,
      Undetermined = 0,
      ReadOnly = 1,
      Ready = 2
   }

   public enum FacebookAuthPermission : int
   {
      Read = 0,
      PublishActions = 1,
      ReadWrite = 2 // Will fall back to iOS < 6 Facebook SSO
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

   public delegate void AuthenticationStatusChangedHandler(object sender, AuthStatus status);
   public delegate void ApplicationLinkRecievedHandler(object sender, string targetURL);

   public static event AuthenticationStatusChangedHandler AuthenticationStatusChanged;
   public static event ApplicationLinkRecievedHandler ApplicationLinkRecieved;

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
      /// <value><c>true</c> if Carrot is authenticated and sending requests; <c>false</c> otherwise.</value>
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
      /// Post a high score to Carrot.
      /// </summary>
      /// <param name="achievementId">Score.</param>
      /// <param name="leaderboardId">Leaderboard Id.</param>
      /// <returns><c>true</c> if the high score request has been cached, and will be sent to the server; <c>false</c> otherwise.</returns>
      public bool postHighScore(uint score, string leaderboardId = null)
      {
#if UNITY_ANDROID && !UNITY_EDITOR
         if(leaderboardId == null) leaderboardId = "";
         using(AndroidJavaObject leaderboardIdString = new AndroidJavaObject("java.lang.String", leaderboardId))
         {
            return mCarrot.Call<bool>("postHighScore", (int)score, leaderboardIdString);
         }
#elif !UNITY_EDITOR
         return (Carrot_PostHighScore(score, leaderboardId) == 1);
#else
         Debug.Log("Carrot::postHighScore(" + score + (leaderboardId != null ? ", '" + leaderboardId + "')" : ")"));
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
      /// Perform Facebook Authentication.
      /// </summary>
      /// <param name="allowLoginUI">(iOS only) Allow the login UI to be shown if the Application is not authenticated.</param>
      /// <param name="permission">(iOS only) Specify the permissions being requested. FB/iOS standards suggest that you should first ask only for read permissions, and then ask for write permissions at the time when they are needed.</param>
      /// <returns><c>false</c> if there are no Facebook accounts registered with the device (iOS 6 only), or the Intent was not defined in AndroidManifest.xml (Android only); <c>true</c> otherwise.</returns>
      public bool doFacebookAuth(bool allowLoginUI = true, FacebookAuthPermission permission = FacebookAuthPermission.ReadWrite)
      {
#if UNITY_ANDROID && !UNITY_EDITOR
         return mCarrot.Call<bool>("doFacebookAuth");
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
#if UNITY_ANDROID && !UNITY_EDITOR
         mCarrot.Call("setUnityHandler", delegateObject.name);
#elif !UNITY_EDITOR
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
      private extern static int Carrot_PostHighScore(uint score,
         [MarshalAs(UnmanagedType.LPStr)] string leaderboardId);

      [DllImport(DLL_IMPORT_TARGET)]
      private extern static int Carrot_DoFacebookAuth(
         int allowLoginUI, int permission);

      [DllImport(DLL_IMPORT_TARGET)]
      private extern static void Carrot_AssignUnityDelegate(
         [MarshalAs(UnmanagedType.LPStr)] string objectName);

      [DllImport(DLL_IMPORT_TARGET)]
      private extern static int Carrot_LikeGame();
      #endregion
#endif

      #region Member Variables
#if UNITY_ANDROID && !UNITY_EDITOR
      AndroidJavaObject mCarrot;
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

   void OnApplicationQuit()
   {
      Destroy(this);
   }
   #endregion

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

   public void applicationLinkRecieved(string message)
   {
      if(ApplicationLinkRecieved != null)
      {
         ApplicationLinkRecieved(this, message);
      }
   }
   #endregion

   #region Member Variables
   private CarrotBridge mCarrot;
   private static Carrot mInstance = null;
   #endregion
}
