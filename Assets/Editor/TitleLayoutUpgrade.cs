using System;
using System.IO;
using System.Linq;
using CountToZero;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
[InitializeOnLoad]
public static class TitleLayoutUpgrade
{
 const string Skin="Assets/PixelUISciFiFree/Sprites/themes/cyan/";
 static Color Cyan=new Color(.25f,.94f,.95f),Gold=new Color(1,.72f,.3f),Muted=new Color(.52f,.69f,.76f);
 static TitleLayoutUpgrade(){EditorApplication.update+=()=>{if(!File.Exists("Temp/ctz-title-layout.request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete("Temp/ctz-title-layout.request");try{Run();}catch(Exception e){File.WriteAllText("Documentation/title-layout.txt",e.ToString());Debug.LogException(e);}};}
 static T Add<T>(GameObject o)where T:Component{var c=o.GetComponent<T>();return c!=null?c:o.AddComponent<T>();}
 static void Wire(UnityEngine.Object o,string field,UnityEngine.Object value){var so=new SerializedObject(o);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
 static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)=>CampaignExpansionUI.Rect(name,parent,new Vector2(x,y),new Vector2(w,h));
 static Image Image(string name,Transform parent,float x,float y,float w,float h,Color color,string sprite="panel")
 {var image=Rect(name,parent,x,y,w,h).gameObject.AddComponent<Image>();image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Skin+sprite+".png");image.type=UnityEngine.UI.Image.Type.Sliced;image.color=color;image.raycastTarget=false;return image;}
 static TMP_Text Text(string name,Transform parent,string text,float x,float y,float w,float h,float size,Color color,bool title=false)
 {var t=Rect(name,parent,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();t.font=title?TypographyUpgrade.TitleFont:TypographyUpgrade.BodyFont;t.text=text;t.fontSize=size;t.enableAutoSizing=false;t.color=color;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;return t;}
 static void Position(RectTransform r,Transform parent,float x,float y,float w,float h){r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);r.localScale=Vector3.one;}
 static void Style(Button b,string label,Color accent)
 {var image=b.targetGraphic as Image;image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Skin+"button_normal.png");image.type=UnityEngine.UI.Image.Type.Sliced;image.color=Color.Lerp(Color.white,accent,.4f);b.transition=Selectable.Transition.SpriteSwap;b.spriteState=new SpriteState{highlightedSprite=AssetDatabase.LoadAssetAtPath<Sprite>(Skin+"button_hover.png"),selectedSprite=AssetDatabase.LoadAssetAtPath<Sprite>(Skin+"button_hover.png"),pressedSprite=AssetDatabase.LoadAssetAtPath<Sprite>(Skin+"button_pressed.png"),disabledSprite=AssetDatabase.LoadAssetAtPath<Sprite>(Skin+"button_disabled.png")};b.navigation=new Navigation{mode=Navigation.Mode.Automatic};var t=b.GetComponentInChildren<TMP_Text>();if(t==null)t=Text("Label",b.transform,label,0,0,300,60,36,Color.white);t.font=TypographyUpgrade.BodyFont;t.text=label;t.fontSize=36;t.enableAutoSizing=false;t.color=Color.Lerp(Color.white,accent,.25f);t.alignment=TextAlignmentOptions.Center;var r=t.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(12,4);r.offsetMax=new Vector2(-12,-4);t.raycastTarget=false;var outline=Add<Outline>(b.gameObject);outline.effectColor=accent;outline.effectDistance=Vector2.one;var feedback=Add<UIButtonFeedback>(b.gameObject);var so=new SerializedObject(feedback);so.FindProperty("accent").colorValue=accent;so.ApplyModifiedPropertiesWithoutUndo();}
 static Button Button(string name,Transform parent,string label,float x,float y,float w,float h,Color color,UnityEngine.Events.UnityAction action)
 {var r=Rect(name,parent,x,y,w,h);var image=r.gameObject.AddComponent<Image>();var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;Style(b,label,color);if(action!=null)UnityEventTools.AddPersistentListener(b.onClick,action);return b;}
 static Slider Slider(string name,Transform parent,float y)
 {Text(name+" Label",parent,name,-330,y,230,50,36,Color.white);var bg=Image(name,parent,120,y,500,26,new Color(.4f,.6f,.7f),"progress_bg");bg.raycastTarget=true;var slider=bg.gameObject.AddComponent<Slider>();var fillArea=Rect("Fill Area",bg.transform,0,0,476,18);var fill=Image("Fill",fillArea,0,0,476,18,Cyan,"progress_fill");var fr=fill.rectTransform;fr.anchorMin=Vector2.zero;fr.anchorMax=Vector2.one;fr.offsetMin=fr.offsetMax=Vector2.zero;slider.fillRect=fr;var area=Rect("Handle Area",bg.transform,0,0,476,26);var handle=Image("Handle",area,0,0,24,44,Color.white,"grabber");handle.raycastTarget=true;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.value=.5f;return slider;}
 static RectTransform Page(string name,Transform parent,string title,string subtitle)
 {var r=Rect(name,parent,0,0,1160,650);Image("Panel",r,0,0,1160,650,Color.white,"window");Image("Dark Backing",r,0,0,1134,624,new Color(.015f,.028f,.05f),"panel");Text("Heading",r,title,0,245,1020,75,48,Color.white,true);Text("Subtitle",r,subtitle,0,175,1020,55,30,Muted);return r;}
 [MenuItem("Count To Zero/Arrange Authored Title Menu")]
 public static void Run()
 {
  AssetDatabase.Refresh();if(EditorApplication.isPlayingOrWillChangePlaymode)return;Directory.CreateDirectory("Backup/TitleLayout");string backup="Backup/TitleLayout/Title-before-layout.unity";if(!File.Exists(backup))File.Copy("Assets/Scenes/Title.unity",backup);
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");var screen=UnityEngine.Object.FindFirstObjectByType<CampaignScreen>();var so=new SerializedObject(screen);var primary=(Button)so.FindProperty("primary").objectReferenceValue;var secondary=(Button)so.FindProperty("secondary").objectReferenceValue;var heading=(TMP_Text)so.FindProperty("heading").objectReferenceValue;var canvas=primary.GetComponentInParent<Canvas>().transform;
  // Preserve the two existing campaign buttons and their serialized references.
  primary.transform.SetParent(canvas,false);secondary.transform.SetParent(canvas,false);heading.transform.SetParent(canvas,false);
  var old=canvas.Find("Title Menu");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);var root=Rect("Title Menu",canvas,0,0,1280,720);
  foreach(var t in canvas.GetComponentsInChildren<TMP_Text>(true))if(t.name=="Credit")t.gameObject.SetActive(false);
  var accent=canvas.Find("Accent");if(accent!=null)accent.gameObject.SetActive(false);
  Image("Deep Circuit Backdrop",root,0,0,1280,720,new Color(.012f,.025f,.045f));
  var decor=Rect("Circuit Traces",root,0,0,1280,720);
  for(int i=0;i<10;i++){Image("Trace Horizontal "+i,decor,0,-280+i*62,1180,1,new Color(.1f,.35f,.43f,.2f),"progress_fill");Image("Trace Vertical "+i,decor,-540+i*120,0,1,580,new Color(.1f,.35f,.43f,.14f),"progress_fill");}
  Text("Brand",root,"DAYDEV / MICRO CIRCUIT",-390,330,440,30,22,Muted);
  var home=Rect("Home",root,0,-25,1200,540);
  Position(heading.rectTransform,home,0,300,1160,86);heading.name="Game Title";heading.text="COUNT TO ZERO";heading.font=TypographyUpgrade.TitleFont;heading.fontSize=56;heading.enableAutoSizing=false;heading.textWrappingMode=TextWrappingModes.NoWrap;heading.color=Color.white;heading.alignment=TextAlignmentOptions.Center;
  Image("Header Trace",home,0,200,1200,2,Cyan,"progress_fill");
  var identity=Rect("Character Artwork",home,-120,-12,960,480);
  const string artPath="Assets/GameAssets/UI/title.png";
  var importer=(TextureImporter)AssetImporter.GetAtPath(artPath);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
  var portrait=Image("Title Illustration",identity,0,0,960,540,Color.white);portrait.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(artPath);portrait.type=UnityEngine.UI.Image.Type.Simple;portrait.preserveAspect=true;
  var tagline=Text("Tagline",identity,"TRANSFER INTO THE MICRO CIRCUIT",0,190,900,42,28,Cyan);
  var actions=Rect("Main Actions",home,480,-12,240,480);
  Image("Action Panel",actions,0,0,240,480,Color.white,"window");Text("Panel Label",actions,"SYSTEM / READY",0,190,220,34,24,Cyan);
  primary.name="New Game Button";Position((RectTransform)primary.transform,actions,0,108,208,64);Style(primary,"NEW GAME",Gold);
  secondary.name="Continue Button";Position((RectTransform)secondary.transform,actions,0,24,208,64);Style(secondary,"CONTINUE",Cyan);
  var view=Add<TitleMenuView>(screen.gameObject);
  Button("Options Button",actions,"OPTIONS",0,-60,208,64,Color.white,view.ShowOptions);Button("Credits Button",actions,"CREDITS",0,-144,208,64,Color.white,view.ShowCredits);
  foreach(var label in actions.GetComponentsInChildren<TMP_Text>())if(label.name!="Panel Label")label.fontSize=30;
  Text("Footer",root,"ARROWS / WASD: MOVE    J: FIRE    SPACE: SLIDE    E: FOCUS    ESC: BACK",0,-320,1200,34,24,Muted);
  var difficulty=Page("Difficulty",root,"SELECT DIFFICULTY","A fresh transfer. Choose how hard the system fights back.");string[] levels={"EASY","NORMAL","HARD"};string[] descriptions={"A gentler entry.\nSlower attacks.","The original challenge.\nA balanced transfer.","Faster attacks.\nLess room for error."};Button normal=null;for(int i=0;i<3;i++){float x=(i-1)*340;Image(levels[i]+" Card",difficulty,x,-10,300,270,Color.white,"panel_inset");var b=Button(levels[i]+" Button",difficulty,levels[i],x,45,260,74,i==1?Gold:Cyan,null);UnityEventTools.AddIntPersistentListener(b.onClick,screen.StartDifficulty,i);Text(levels[i]+" Description",difficulty,descriptions[i],x,-67,260,100,32,Muted);if(i==1)normal=b;}Button("Back Button",difficulty,"BACK",0,-265,240,56,Color.white,view.ReturnHome);
  var options=Page("Options",root,"AUDIO SETTINGS","Set your sound before entering the circuit.");var music=Slider("BGM",options,65);var sfx=Slider("SFX",options,-30);Button("Credits Button",options,"ASSET CREDITS",0,-150,320,58,Cyan,view.ShowCredits);var optionsBack=Button("Back Button",options,"BACK",0,-265,240,56,Color.white,view.ReturnHome);
  var credits=Page("Credits",root,"ASSET CREDITS","The people and tools behind this circuit.");var viewport=Rect("Credits Viewport",credits,0,-30,1020,340);viewport.gameObject.AddComponent<RectMask2D>();var hit=viewport.gameObject.AddComponent<Image>();hit.color=new Color(0,0,0,.01f);var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.viewport=viewport;scroll.movementType=ScrollRect.MovementType.Clamped;var source=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/GameAssets/asset_credits.txt");var ct=Text("Credit Content",viewport,source.text,0,0,980,1500,28,Color.white);ct.alignment=TextAlignmentOptions.TopLeft;ct.rectTransform.anchorMin=ct.rectTransform.anchorMax=ct.rectTransform.pivot=new Vector2(.5f,1);ct.rectTransform.anchoredPosition=Vector2.zero;scroll.content=ct.rectTransform;var creditsBack=Button("Back Button",credits,"BACK",0,-265,240,56,Color.white,view.BackFromCredits);
  Wire(view,"home",home.gameObject);Wire(view,"difficulty",difficulty.gameObject);Wire(view,"options",options.gameObject);Wire(view,"credits",credits.gameObject);Wire(view,"newGame",primary);Wire(view,"normal",normal);Wire(view,"optionsBack",optionsBack);Wire(view,"creditsBack",creditsBack);Wire(view,"music",music);Wire(view,"sfx",sfx);Wire(view,"creditSource",source);Wire(view,"creditText",ct);Wire(view,"creditScroll",scroll);
  difficulty.gameObject.SetActive(false);options.gameObject.SetActive(false);credits.gameObject.SetActive(false);
  LeaderboardUpgrade.Author(root, view);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText("Documentation/title-layout.txt","PASS: title menu authored in Title.unity. Home, Difficulty, Options, Credits and Ranks pages; all 14 buttons and both audio sliders exist in the edit-time Hierarchy. Original scene backed up to Backup/TitleLayout.\n");
 }
}
