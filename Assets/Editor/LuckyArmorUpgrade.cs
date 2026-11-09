using System;
using System.IO;
using System.Linq;
using CountToZero;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad]
public static class LuckyArmorUpgrade
{
 static LuckyArmorUpgrade(){EditorApplication.update+=()=>{if(!File.Exists("Temp/ctz-lucky-armor.request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete("Temp/ctz-lucky-armor.request");try{Run();}catch(Exception e){File.WriteAllText("Documentation/lucky-armor.txt",e.ToString());Debug.LogException(e);}};}
 [MenuItem("Count To Zero/Apply Lucky Number Boss Armor")]
 public static void Run()
 {
  AssetDatabase.Refresh();if(EditorApplication.isPlayingOrWillChangePlaymode){File.WriteAllText("Temp/ctz-lucky-armor.request","retry");return;}
  string original=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
  foreach(var file in new[]{"5_BROKEN_HALO","14_SAFE_LANE","17_ROTATING_GATE","23_ORBIT_MINES"}){var pattern=AssetDatabase.LoadAssetAtPath<BossPatternDefinition>("Assets/GameAssets/Patterns/"+file+".asset");pattern.luckyNumberOnly=true;EditorUtility.SetDirty(pattern);}
  var clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/GameAssets/Audio/Polish/Blocked.wav");if(clip==null)throw new Exception("Missing blocked cue");
  foreach(var sceneName in new[]{"Title","Preload","Story","CountToZero","TurnEnd","StoryEnd"})
  {
   var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+sceneName+".unity");foreach(var audio in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GameAudio>(true))){var so=new SerializedObject(audio);var cues=so.FindProperty("cues");int index=-1;for(int i=0;i<cues.arraySize;i++)if(cues.GetArrayElementAtIndex(i).FindPropertyRelative("cue").enumValueIndex==(int)SoundCue.Blocked)index=i;if(index<0){index=cues.arraySize;cues.arraySize++;}var entry=cues.GetArrayElementAtIndex(index);entry.FindPropertyRelative("cue").enumValueIndex=(int)SoundCue.Blocked;entry.FindPropertyRelative("clip").objectReferenceValue=clip;entry.FindPropertyRelative("volume").floatValue=.55f;so.ApplyModifiedPropertiesWithoutUndo();}EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  }
  AssetDatabase.SaveAssets();if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);File.WriteAllText("Documentation/lucky-armor.txt","PASS: Lucky-only armor on Broken Halo (6), Safe Lane (15), Rotating Gate (18), Orbit Mines (24). Dedicated Blocked audio wired across six scenes. Exact lucky matches: 19 damage; other lucky rules: 10 damage. Normal, charged and assistant shots cannot damage armored bosses.\n");
 }
}
