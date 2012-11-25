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

using CarrotInc;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

public class CarrotPostProcessScene
{
   [PostProcessScene]
   public static void OnPostprocessScene()
   {
      Carrot carrot = Object.FindObjectOfType(typeof(Carrot)) as Carrot;
      if(carrot == null)
      {
         GameObject carrotGameObject = GameObject.Find("__CarrotGameObject__");
         if(carrotGameObject == null)
         {
            carrotGameObject =  new GameObject("__CarrotGameObject__");
         }

         carrot = carrotGameObject.GetComponent<Carrot>();
         if(carrot == null)
         {
            carrotGameObject.AddComponent<Carrot>();
            carrot = carrotGameObject.GetComponent<Carrot>();
         }
      }

      carrot.FacebookAppId = CarrotSettings.CarrotAppId;
      carrot.CarrotAppSecret = CarrotSettings.CarrotAppSecret;
   }
}
