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

using System.IO;
using UnityEngine;
using UnityEditor;

public class CarrotSettings : EditorWindow
{
    public static string CarrotAppId
    {
        get
        {
            LoadSettings();
            return mCarrotAppId;
        }
        private set
        {
            mCarrotAppId = value.Trim();
            SaveSettings();
            if(mInstance) mInstance.UpdateCarrotGameObject();
        }
    }

    public static string CarrotAppSecret
    {
        get
        {
            LoadSettings();
            return mCarrotAppSecret;
        }
        private set
        {
            mCarrotAppSecret = value.Trim();
            SaveSettings();
            if(mInstance) mInstance.UpdateCarrotGameObject();
        }
    }

    [MenuItem("Edit/Carrot")]
    public static void ShowWindow()
    {
        LoadSettings();
        CarrotSettings settingsWindow = (CarrotSettings)GetWindow<CarrotSettings>(false, "Carrot Settings", false);
        mInstance = settingsWindow;
        settingsWindow.UpdateCarrotGameObject();
        settingsWindow.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Settings", EditorStyles.boldLabel);
        CarrotAppId = EditorGUILayout.TextField("Carrot App Id", mCarrotAppId);
        CarrotAppSecret = EditorGUILayout.TextField("Carrot App Secret", mCarrotAppSecret);

        if(!CarrotPostProcessScene.WillCreatePrefab)
        {
            if(GUILayout.Button("Create Carrot GameObject", GUILayout.Height(25)))
            {
                UpdateCarrotGameObject();
            }
        }

        if(GUILayout.Button("Get a Carrot Account", GUILayout.Height(25)))
        {
            Application.OpenURL("https://gocarrot.com/developers/sign_up?referrer=unity");
        }
    }

    void UpdateCarrotGameObject()
    {
        mCarrotGameObject = GameObject.Find("CarrotGameObject");

        if(mCarrotGameObject == null && !CarrotPostProcessScene.WillCreatePrefab)
        {
            Object prefab = AssetDatabase.LoadAssetAtPath("Assets/Carrot/CarrotGameObject.prefab", typeof(GameObject));
            mCarrotGameObject =  PrefabUtility.InstantiatePrefab(prefab as GameObject) as GameObject;
        }

        if(mCarrotGameObject)
        {
            Carrot carrot = mCarrotGameObject.GetComponent<Carrot>();
            carrot.FacebookAppId = mCarrotAppId;
            carrot.CarrotAppSecret = mCarrotAppSecret;
        }
    }

    static void LoadSettings()
    {
        mCarrotAppId = EditorPrefs.GetString(ProjectName + "-CarrotAppId");
        mCarrotAppSecret = EditorPrefs.GetString(ProjectName + "-CarrotAppSecret");
    }

    static void SaveSettings()
    {
        if(!string.IsNullOrEmpty(mCarrotAppId)) EditorPrefs.SetString(ProjectName + "-CarrotAppId", mCarrotAppId.Trim());
        if(!string.IsNullOrEmpty(mCarrotAppSecret))EditorPrefs.SetString(ProjectName + "-CarrotAppSecret", mCarrotAppSecret.Trim());
    }

    static string ProjectName
    {
        get
        {
            string[] dp = Application.dataPath.Split('/');
            return dp[dp.Length - 2];
        }
    }

    static string mCarrotAppId = "";
    static string mCarrotAppSecret = "";
    static CarrotSettings mInstance;
    GameObject mCarrotGameObject = null;
}
