using UnityEngine;
using System.Collections;

public class MainMenu : MonoBehaviour
{
   public int buttonHeight = 50;
   public int buttonWidth = 200;
   public int buttonSpacing = 8;

   string scoreString = "100";
   string achieveString = "achievement_id";
   string authStatus = "";

   void AuthenticationStatusChangedHandler(object sender, Carrot.AuthStatus status)
   {
      authStatus = Carrot.authStatusString(status);
   }

   void Start()
   {
      authStatus = Carrot.authStatusString(Carrot.AuthStatus.Undetermined);
      Carrot.AuthenticationStatusChanged += AuthenticationStatusChangedHandler;
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

      // Achievement
      GUILayout.Space(buttonSpacing);
      achieveString = GUILayout.TextField(achieveString, buttonWidth);
      if(GUILayout.Button("Earn Achievement", GUILayout.Height(buttonHeight)))
      {
         Carrot.Instance.postAchievement(achieveString);
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
