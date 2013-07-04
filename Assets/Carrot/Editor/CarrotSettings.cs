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

using MiniJSON;
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

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
            string appId = value.Trim();
            if(appId != mCarrotAppId)
            {
                mCarrotAppId = appId;
                SaveSettings();
            }
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
            string appSecret = value.Trim();
            if(appSecret != mCarrotAppSecret)
            {
                mCarrotAppSecret = appSecret;
                SaveSettings();
            }
        }
    }

    [MenuItem("Edit/Carrot")]
    public static void ShowWindow()
    {
        LoadSettings();
        CarrotSettings settingsWindow = (CarrotSettings)GetWindow<CarrotSettings>(false, "Carrot Settings", false);
        settingsWindow.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Settings", EditorStyles.boldLabel);
        CarrotAppId = EditorGUILayout.TextField("Carrot App Id", mCarrotAppId);
        CarrotAppSecret = EditorGUILayout.TextField("Carrot App Secret", mCarrotAppSecret);

        if(GUILayout.Button("Get a Carrot Account", GUILayout.Height(25)))
        {
            Application.OpenURL("https://gocarrot.com/developers/sign_up?referrer=unity");
        }
    }

    static void LoadSettings()
    {
        if(!mSettingsLoaded)
        {
            TextAsset carrotJson = Resources.Load("carrot") as TextAsset;
            if(carrotJson != null)
            {
                Dictionary<string, object> carrotConfig = null;
                carrotConfig = Json.Deserialize(carrotJson.text) as Dictionary<string, object>;
                mCarrotAppId = carrotConfig["carrotAppId"] as string;
                mCarrotAppSecret = carrotConfig["carrotAppSecret"] as string;
            }
            mSettingsLoaded = true;
        }
    }

    static void SaveSettings()
    {
        Dictionary<string, object> carrotConfig = new Dictionary<string, object>();
        carrotConfig["carrotAppId"] = mCarrotAppId;
        carrotConfig["carrotAppSecret"] = mCarrotAppSecret;
        carrotConfig["appBundleVersion"] = PlayerSettings.bundleVersion.ToString();

        System.IO.Directory.CreateDirectory(Application.dataPath + "/Resources");
        File.WriteAllText(Application.dataPath + "/Resources/carrot.bytes", Json.Serialize(carrotConfig));
        AssetDatabase.Refresh();
    }

    static string mCarrotAppId = "";
    static string mCarrotAppSecret = "";
    static bool mSettingsLoaded = false;
}
