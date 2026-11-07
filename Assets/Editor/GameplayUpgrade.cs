using TMPro;
using System.IO;
using CountToZero;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class GameplayUpgrade
{
    const string Root="Assets/GameAssets";
    static Transform Group(string name,Transform parent){var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;}
    static void Wire(Object target,string field,Object value)
    {var so=new SerializedObject(target);var p=so.FindProperty(field)??so.FindProperty("<"+field+">k__BackingField");p.objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    static void Array(Object target,string field,Object[] values)
    {var so=new SerializedObject(target);var p=so.FindProperty(field);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedPropertiesWithoutUndo();}
    static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
    {var go=new GameObject(name,typeof(RectTransform));var rt=(RectTransform)go.transform;rt.SetParent(parent,false);rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(x,-y);rt.sizeDelta=new Vector2(w,h);return rt;}
    static Image Panel(string name,Transform parent,float x,float y,float w,float h,Color color,bool raycast=false)
    {var image=Rect(name,parent,x,y,w,h).gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=raycast;return image;}
    static TMP_Text Label(string name,Transform parent,float x,float y,float w,float h,string content,int size=18)
    {var text=Rect(name,parent,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();text.font=TypographyUpgrade.BodyFont;text.fontSize=size;text.text=content;text.color=Color.white;text.raycastTarget=false;text.overflowMode=TextOverflowModes.Overflow;return text;}
    static Button Button(string name,Transform parent,float x,float y,float w,float h,string content)
    {var panel=Panel(name,parent,x,y,w,h,new Color(.07f,.2f,.24f),true);var button=panel.gameObject.AddComponent<Button>();button.targetGraphic=panel;var label=Label("Label",panel.transform,0,0,w,h,content);label.alignment=TextAlignmentOptions.Center;return button;}
    static Material Material(string name,Color color)
    {
        string path=Root+"/Materials/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));mat.color=color;AssetDatabase.CreateAsset(mat,path);}return mat;
    }
    [MenuItem("Count To Zero/Upgrade Gameplay")]
    public static void Upgrade()
    {
        if(EditorApplication.isPlaying)throw new System.Exception("Stop Play Mode first.");
        var g=Object.FindFirstObjectByType<GameManager>();if(g==null)throw new System.Exception("Open CountToZero scene first.");
        if(Object.FindFirstObjectByType<PlayerInputRouter>()!=null){Debug.Log("Gameplay upgrade already installed.");return;}
        var canvas=Object.FindFirstObjectByType<HUDController>().transform;
        var layout=canvas.Find("Reference Layout");var systems=g.transform.parent;
        var router=Group("Player Input",systems).gameObject.AddComponent<PlayerInputRouter>();Wire(g,"Controls",router);
        // Touch controls are authored in the scene; visibility is selected at runtime.
        var touch=Rect("Touch Controls",layout,0,0,1280,800);
        var joystickPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Joystick Pack/Prefabs/Fixed Joystick.prefab");
        var joystickObject=(GameObject)PrefabUtility.InstantiatePrefab(joystickPrefab,touch);
        var joyRect=(RectTransform)joystickObject.transform;joyRect.anchorMin=joyRect.anchorMax=joyRect.pivot=new Vector2(0,0);joyRect.anchoredPosition=new Vector2(60,155);joyRect.sizeDelta=new Vector2(160,160);
        var joystick=joystickObject.GetComponent<Joystick>();joystick.DeadZone=.15f;
        Wire(router,"joystick",joystick);Wire(router,"touchRoot",touch.gameObject);
        var fire=Button("Fire",touch,1080,480,150,90,"FIRE");Wire(router,"fireButton",fire.gameObject.AddComponent<TouchHoldButton>());
        var dash=Button("Dash",touch,950,585,130,62,"DASH");UnityEditor.Events.UnityEventTools.AddPersistentListener(dash.onClick,router.TouchDash);
        var focus=Button("Focus",touch,1100,585,130,62,"FOCUS");UnityEditor.Events.UnityEventTools.AddPersistentListener(focus.onClick,router.TouchFocus);
        touch.gameObject.SetActive(false);
        // Keep navigation controls above the menu overlay and below the options modal.
        var nav=Rect("Navigation",layout,0,0,1280,800);
        var pause=Button("Pause",nav,1115,135,140,45,"PAUSE");var open=Button("Options",nav,1115,190,140,45,"OPTIONS");
        var options=Group("Options Controller",systems).gameObject.AddComponent<OptionsController>();Wire(options,"input",router);Wire(options,"open",open);Wire(options,"pause",pause);
        var modal=Panel("Options Modal",layout,0,0,1280,800,new Color(.02f,.035f,.06f,.98f),true);Wire(options,"panel",modal.gameObject);
        Label("Title",modal.transform,310,105,700,50,"OPTIONS / KEY LISTENING",32);
        Label("Help",modal.transform,310,157,700,55,"Select an action, then press a key. Duplicate keys swap.\nArrow keys remain available. Settings are saved on this device.",16);
        var rows=new Object[8];var texts=new Object[8];
        for(int i=0;i<8;i++)
        {var button=Button("Bind "+(ControlAction)i,modal.transform,310+(i/4)*350,225+(i%4)*60,320,48,((ControlAction)i).ToString());rows[i]=button;texts[i]=button.GetComponentInChildren<TMP_Text>();}
        Array(options,"bindingButtons",rows);Array(options,"bindingLabels",texts);
        Wire(options,"listeningLabel",Label("Listening Status",modal.transform,310,482,720,45,"Select a binding to listen."));
        Label("Gamepad Help",modal.transform,310,535,720,70,"GAMEPAD: Left stick / D-pad move | Right stick aim | RT/RB fire\nA dash | X bullet time | Start pause | D-pad + A navigate menus\nMOBILE: Joystick Pack + on-screen buttons",16);
        Wire(options,"reset",Button("Restore Defaults",modal.transform,310,630,215,50,"DEFAULTS"));
        Wire(options,"cancel",Button("Cancel Listening",modal.transform,540,630,215,50,"CANCEL LISTEN"));
        Wire(options,"close",Button("Close Options",modal.transform,770,630,215,50,"BACK"));modal.gameObject.SetActive(false);
        // English dialogue is data, not source strings.
        var dialog=Group("Dialogue Controller",systems).gameObject.AddComponent<DialogueController>();Wire(g,"Dialogue",dialog);
        var dialogPanel=Panel("Dialogue",layout,350,125,550,80,new Color(.025f,.045f,.08f,.92f));
        Wire(dialog,"panel",dialogPanel.gameObject);Wire(dialog,"speaker",Label("Speaker",dialogPanel.transform,15,6,520,24,"GLITCH-0",16));
        Wire(dialog,"body",Label("Line",dialogPanel.transform,15,30,520,48,"",16));Wire(dialog,"englishJson",AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/Dialogues/en.json"));dialogPanel.gameObject.SetActive(false);
        // Cut-in occupies half of the screen width and the lower 30%; it never catches input.
        var cutin=Group("Lucky Action",systems).gameObject.AddComponent<LuckyCutIn>();Wire(g,"CutIn",cutin);
        var cutPanel=Panel("Lucky Action Cut-in",canvas,0,0,0,0,new Color(.015f,.1f,.13f,.95f));
        var cutRect=cutPanel.rectTransform;cutRect.anchorMin=Vector2.zero;cutRect.anchorMax=new Vector2(.5f,.3f);cutRect.pivot=Vector2.zero;cutRect.offsetMin=cutRect.offsetMax=Vector2.zero;
        var cg=cutPanel.gameObject.AddComponent<CanvasGroup>();cg.blocksRaycasts=false;cg.interactable=false;Wire(cutin,"panel",cutRect);Wire(cutin,"group",cg);
        var portrait=Panel("Glitch Action Pose",cutPanel.transform,25,15,200,220,Color.white);portrait.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Sprites/Glitch.png");portrait.color=ArenaVisuals.Cyan;portrait.preserveAspect=true;portrait.transform.localRotation=Quaternion.Euler(0,0,-18);
        var slash=Panel("Energy Strike",cutPanel.transform,140,50,250,12,ArenaVisuals.Green);slash.transform.localRotation=Quaternion.Euler(0,0,25);
        Label("Action Text",cutPanel.transform,245,90,340,100,"LUCKY!\nCOUNT TO ZERO",27);cutPanel.gameObject.SetActive(false);
        var follow=g.View.gameObject.AddComponent<FollowCamera>();Wire(follow,"target",g.Player.transform);
        g.View.transform.position=new Vector3(2,20,-20);g.View.transform.LookAt(Vector3.zero);
        // Persistent pattern assets can be tuned without editing the controller.
        Directory.CreateDirectory(Root+"/Patterns");AssetDatabase.Refresh();var patterns=new Object[6];
        string[] names={"RADIAL SWAY","HUNTER FAN","ORBIT SPIRAL","CROSS SWEEP","SINE WAVE","BROKEN HALO"};
        BossMotion[] motions={BossMotion.Sway,BossMotion.Pursue,BossMotion.Orbit,BossMotion.Sweep,BossMotion.FigureEight,BossMotion.Orbit};
        for(int i=0;i<6;i++)
        {
            var pattern=ScriptableObject.CreateInstance<BossPatternDefinition>();pattern.displayName=names[i];pattern.shot=(BossShot)i;pattern.motion=motions[i];
            pattern.count=i==1?5:i==4?9:16;pattern.interval=i==2?.22f:i==3?.75f:.95f;pattern.speed=i==1?4.2f:3.3f;pattern.duration=7;
            AssetDatabase.CreateAsset(pattern,Root+"/Patterns/"+i+"_"+names[i].Replace(' ','_')+".asset");patterns[i]=pattern;
        }
        Array(g.Boss,"patterns",patterns);
        var encounter=Group("Encounter Director",systems).gameObject.AddComponent<EncounterDirector>();Wire(g,"Encounters",encounter);
        var encounters=Group("Encounter Actors",null);
        var obstacleRoot=Group("Cube Pool",encounters);var blocks=new Object[10];
        for(int i=0;i<blocks.Length;i++)
        {
            var actor=Group("Cube "+(i+1),obstacleRoot).gameObject.AddComponent<ArenaObstacle>();
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.name="Solid Cube";cube.transform.SetParent(actor.transform,false);cube.transform.localScale=Vector3.one*1.6f;
            Object.DestroyImmediate(cube.GetComponent<Collider>());cube.GetComponent<Renderer>().sharedMaterial=Material("ObstacleCube",new Color(.19f,.12f,.3f));Wire(actor,"cube",cube.transform);
            var warning=SceneArtBake.Sprite(actor.transform,"Warning Tile",AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Sprites/NumberDisc.png"),ArenaVisuals.Red,2,Vector3.up*.05f);
            Object.DestroyImmediate(warning.GetComponent<Billboard>());warning.transform.rotation=Quaternion.Euler(90,0,0);Wire(actor,"warning",warning.gameObject);actor.gameObject.SetActive(false);blocks[i]=actor;
        }
        Array(encounter,"obstacles",blocks);
        var minionRoot=Group("Sentinel Pool",encounters);var minions=new Object[6];
        for(int i=0;i<minions.Length;i++)
        {
            var actor=Group("Sentinel "+(i+1),minionRoot).gameObject.AddComponent<MinionController>();
            Wire(actor,"sprite",SceneArtBake.Sprite(actor.transform,"Sentinel Sprite",AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Sprites/Omega.png"),ArenaVisuals.Violet,.9f,Vector3.up*.6f));
            var label=SceneArtBake.Label(actor.transform,1.3f,ArenaVisuals.Gold,.07f);label.text="SENTINEL";Wire(actor,"label",label);actor.gameObject.SetActive(false);minions[i]=actor;
        }
        Array(encounter,"minions",minions);
        var healthRoot=Group("Health Item Pool",encounters);var heals=new Object[5];
        for(int i=0;i<heals.Length;i++)
        {
            var actor=Group("Repair Item "+(i+1),healthRoot).gameObject.AddComponent<HealthPickup>();
            var visual=SceneArtBake.Sprite(actor.transform,"Repair Sprite",AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Sprites/NumberDisc.png"),ArenaVisuals.Green,1,Vector3.up*.65f);
            var label=SceneArtBake.Label(actor.transform,.72f,Color.white,.15f);label.text="+HP";Wire(actor,"visual",visual.transform);actor.gameObject.SetActive(false);heals[i]=actor;
        }
        Array(encounter,"healthPickups",heals);
        var spawnRoot=GameObject.Find("Spawn Points").transform;var spawnGroup=Group("Minion Spawns",spawnRoot);var spawnPoints=new Object[4];
        for(int i=0;i<4;i++){var point=Group("Sentinel Entry "+i,spawnGroup);point.position=new Vector3(i%2==0?-8:8,0,i<2?5:-4);spawnPoints[i]=point;}Array(encounter,"minionSpawns",spawnPoints);
        layout.Find("Player HUD/Dash Label").GetComponent<TMP_Text>().text="INVERT DASH";
        layout.Find("Player HUD/Focus Label").GetComponent<TMP_Text>().text="BULLET TIME";
        layout.Find("Player HUD/Controls").GetComponent<TMP_Text>().text="KEYBOARD / MOUSE / GAMEPAD\n\nOPTIONS TO CHANGE KEYS\n\nTOUCH CONTROLS ON MOBILE";
        EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);
        AssetDatabase.SaveAssets();Debug.Log("COUNT TO ZERO: Gameplay upgrade saved to Hierarchy.");
    }
}
