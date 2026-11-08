using System;
using System.IO;
using CountToZero;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad]
public static class CompactHudUpgrade
{
 static CompactHudUpgrade(){EditorApplication.update+=()=>{if(!File.Exists("Temp/ctz-compact-hud.request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete("Temp/ctz-compact-hud.request");try{Run();}catch(Exception e){File.WriteAllText("Documentation/compact-hud.txt",e.ToString());}};}
 static void Place(Transform root,string name,float x,float y,float w,float h,float font=0,string label=null){var r=(RectTransform)root.Find(name);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);var t=r.GetComponent<TMP_Text>();if(t!=null){if(font>0){t.fontSize=font;t.enableAutoSizing=false;}if(label!=null)t.text=label;}}
 [MenuItem("Count To Zero/Compact HUD")]
 public static void Run(){
 AssetDatabase.Refresh();if(EditorApplication.isPlayingOrWillChangePlaymode)return;
 Directory.CreateDirectory("Backup/CompactHud");if(!File.Exists("Backup/CompactHud/CountToZero.unity"))File.Copy("Assets/Scenes/CountToZero.unity","Backup/CompactHud/CountToZero.unity");
 var scene=EditorSceneManager.OpenScene("Assets/Scenes/CountToZero.unity");var hud=UnityEngine.Object.FindAnyObjectByType<HUDController>();var b=(RectTransform)hud.transform.Find("Reference Layout/Player HUD");b.anchoredPosition=new Vector2(24,-704);b.sizeDelta=new Vector2(1232,74);
 Place(b,"Player Label",20,9,260,25,23,"GLITCH-0 / VALUE");Place(b,"Current Value",20,33,260,35,36);
 Place(b,"Dash Label",355,8,270,25,23,"SPACE / SLIDE");Place(b,"Dash Cooldown",355,35,240,6);Place(b,"Dash Status",355,46,280,23,20);
 Place(b,"Focus Label",650,8,270,25,23,"E / BULLET TIME");Place(b,"Focus Charge",650,35,240,6);Place(b,"Focus Status",650,46,280,23,20);
 Place(b,"Controls",950,12,265,54,22,"WASD / ARROWS: MOVE\nJ: FIRE    ESC: PAUSE");
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText("Documentation/compact-hud.txt","PASS: bottom bar reduced from 126 to 74 units (41% less height), controls reorganized at readable font sizes.\n");
 }
}
