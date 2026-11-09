using System;
using System.IO;
using System.Linq;
using CountToZero;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
[InitializeOnLoad]
public static class LuckyChargeUpgrade
{
 static LuckyChargeUpgrade(){EditorApplication.update+=()=>{if(!File.Exists("Temp/ctz-lucky-charge.request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete("Temp/ctz-lucky-charge.request");try{Run();}catch(Exception e){File.WriteAllText("Documentation/lucky-charge.txt",e.ToString());Debug.LogException(e);}};}
 [MenuItem("Count To Zero/Apply Lucky Charge Artwork")]
 public static void Run(){AssetDatabase.Refresh();if(EditorApplication.isPlayingOrWillChangePlaymode)return;
 const string path="Assets/GameAssets/UI/aim_pixel.png";
 var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.spritePixelsPerUnit=100;
 importer.SaveAndReimport();if(EditorApplication.isPlayingOrWillChangePlaymode){File.WriteAllText("Temp/ctz-lucky-charge.request","retry");return;}var frames=new[]{AssetDatabase.LoadAssetAtPath<Sprite>(path)};
 Directory.CreateDirectory("Backup/LuckyCharge");if(!File.Exists("Backup/LuckyCharge/CountToZero.unity"))File.Copy("Assets/Scenes/CountToZero.unity","Backup/LuckyCharge/CountToZero.unity");
 var scene=EditorSceneManager.OpenScene("Assets/Scenes/CountToZero.unity");var cutin=new SerializedObject(UnityEngine.Object.FindAnyObjectByType<LuckyCutIn>());var portrait=(Image)cutin.FindProperty("portrait").objectReferenceValue;portrait.sprite=frames[0];portrait.color=Color.white;portrait.preserveAspect=true;portrait.rectTransform.anchoredPosition=new Vector2(12,-15);portrait.rectTransform.sizeDelta=new Vector2(200,220);portrait.raycastTarget=false;
 var array=cutin.FindProperty("punchFrames");array.arraySize=1;for(int i=0;i<1;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=frames[i];cutin.ApplyModifiedPropertiesWithoutUndo();
 var panel=(RectTransform)cutin.FindProperty("panel").objectReferenceValue;panel.Find("Energy Strike").gameObject.SetActive(false);var text=panel.Find("Action Text").GetComponent<TMPro.TMP_Text>();text.rectTransform.anchoredPosition=new Vector2(235,-65);text.rectTransform.sizeDelta=new Vector2(360,120);text.text="LUCKY!\nCHARGE SHOT";text.fontSize=30;text.enableAutoSizing=false;
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText("Documentation/lucky-charge.txt","PASS: aim_pixel.png is the single point-filtered Lucky portrait; aspect ratio preserved and old animation frames replaced.\n");
 }
}
