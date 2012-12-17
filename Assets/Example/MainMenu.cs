using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MainMenu : MonoBehaviour
{
   public int buttonHeight = 60;
   public int buttonWidth = 200;
   public int buttonSpacing = 8;

   string scoreString = "100";
   string achieveString = "achievement_id";
   string authStatus = "";

   void AuthenticationStatusChangedHandler(object sender, Carrot.AuthStatus status)
   {
      authStatus = Carrot.authStatusString(status);
   }

   void FriendHighScoreListReceivedHandler(object sender, IList<Carrot.Score> scores, string errors)
   {
      if(errors != null)
      {
         Debug.Log("High score query error: " + errors);
      }
      else
      {
         Debug.Log("High score list recieved:");
         foreach(Carrot.Score score in scores)
         {
            Debug.Log(score);
         }
      }
   }

   void UserAchievementListReceivedHandler(object sender, IList<Carrot.Achievement> achievements, string errors)
   {
      if(errors != null)
      {
         Debug.Log("Achievement query error: " + errors);
      }
      else
      {
         Debug.Log("User achievement list recieved:");
         foreach(Carrot.Achievement achievement in achievements)
         {
            Debug.Log(achievement);
         }
      }
   }

   void Start()
   {
      authStatus = Carrot.authStatusString(Carrot.AuthStatus.Undetermined);

      Carrot.AuthenticationStatusChanged += AuthenticationStatusChangedHandler;
      Carrot.UserAchievementListReceived += UserAchievementListReceivedHandler;
      Carrot.FriendHighScoreListReceived += FriendHighScoreListReceivedHandler;
   }

   void OnGUI()
   {
      GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, Screen.height - 20));

      // Display auth status
      GUILayout.Label(authStatus);

      // Facebook auth
      GUILayout.Space(buttonSpacing);
      if(GUILayout.Button("Facebook SSO", GUILayout.Height(buttonHeight)))
      {
         if(!Carrot.Instance.doFacebookAuth())
         {
            Debug.Log("Facebook SSO did not start successfully.");
         }
      }

      // High Score
      GUILayout.Space(buttonSpacing);
      scoreString = GUILayout.TextField(scoreString, buttonWidth);
      if(GUILayout.Button("Earn High Score", GUILayout.Height(buttonHeight)))
      {
         Carrot.Instance.postHighScore(System.Convert.ToUInt32(scoreString));
      }

      // Get Friend Scores
      GUILayout.Space(buttonSpacing);
      if(GUILayout.Button("List Friend Scores", GUILayout.Height(buttonHeight)))
      {
         Carrot.Instance.getFriendScores();
      }

      // Achievement
      GUILayout.Space(buttonSpacing);
      achieveString = GUILayout.TextField(achieveString, buttonWidth);
      if(GUILayout.Button("Earn Achievement", GUILayout.Height(buttonHeight)))
      {
         Carrot.Instance.postAchievement(achieveString);
      }

      // Get User Achievements
      GUILayout.Space(buttonSpacing);
      if(GUILayout.Button("List User Achievements", GUILayout.Height(buttonHeight)))
      {
         Carrot.Instance.getUserAchievements();
      }

      // Like Game
      GUILayout.Space(buttonSpacing);
      if(GUILayout.Button("Like Game", GUILayout.Height(buttonHeight)))
      {
         Carrot.Instance.likeGame();
      }

      GUILayout.EndArea();
   }
}
