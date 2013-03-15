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
    string actionString = "action_id";
    string objectString = "object_id";

    void Start()
    {
        authStatus = Carrot.authStatusString(Carrot.AuthStatus.Undetermined);

        Carrot.AuthenticationStatusChanged += (object sender, Carrot.AuthStatus status) => {
            authStatus = Carrot.authStatusString(status);
        };

        Carrot.Instance.UserId = "zerostride@gmail.com";
        Carrot.Instance.validateUser("532815528");
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, Screen.height - 20));

        // Display auth status
        GUILayout.Label(authStatus);

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

        // Action Post
        GUILayout.Space(buttonSpacing);
        actionString = GUILayout.TextField(actionString, buttonWidth);
        objectString = GUILayout.TextField(objectString, buttonWidth);
        if(GUILayout.Button("Post Object/Action", GUILayout.Height(buttonHeight)))
        {
            Carrot.Instance.postAction(actionString, objectString);
        }

        // Dynamic Action Post
        GUILayout.Space(buttonSpacing);
        if(GUILayout.Button("Post Screenshot", GUILayout.Height(buttonHeight)))
        {
            StartCoroutine(postScreenshotCoroutine());
        }

        GUILayout.EndArea();
    }

    IEnumerator postScreenshotCoroutine()
    {
        yield return new WaitForEndOfFrame();

        Texture2D tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false );
        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        tex.Apply();

        Carrot.ViralObject newObject = new Carrot.ViralObject("feature", "Dynamic Objects",
            "A screenshot uploaded from Unity.", tex);
        Carrot.Instance.postAction("demo", null, newObject);
        Destroy(tex);
    }
}
