using System.IO;
using CountToZero;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// One-time authoring utility. The saved scene and prefab assets are the runtime source of truth.
public static class HierarchySceneBuilder
{
    const string Root = "Assets/GameAssets";
    static Font font;
    static Color Ink => new Color(.035f,.055f,.09f,.97f);
    static Transform Group(string name, Transform parent = null)
    { var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform; }
    static void Wire(Object target,string field,Object value)
    {
        var so=new SerializedObject(target);
        var property=so.FindProperty(field) ?? so.FindProperty("<"+field+">k__BackingField");
        if(property==null)throw new System.Exception("Missing serialized field: "+field);
        property.objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();
    }
    static void WireArray(Object target,string field,Object[] values)
    {
        var so=new SerializedObject(target);var p=so.FindProperty(field);p.arraySize=values.Length;
        for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    static Sprite SaveSprite(Sprite source,string name)
    {
        string path=Root+"/Sprites/"+name+".png";
        if(!File.Exists(path))File.WriteAllBytes(path,source.texture.EncodeToPNG());
        AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spritePixelsPerUnit=64;
        importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static AudioClip SaveTone(string name,float frequency,float duration)
    {
        string path=Root+"/Audio/"+name+".wav";
        if(!File.Exists(path))
        {
            int count=Mathf.RoundToInt(44100*duration);
            using(var writer=new BinaryWriter(File.Create(path)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);
                writer.Write((short)1);writer.Write((short)1);writer.Write(44100);writer.Write(88200);writer.Write((short)2);writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
                for(int i=0;i<count;i++)writer.Write((short)(Mathf.Sin(i/44100f*frequency*Mathf.PI*2)*(1-(float)i/count)*.35f*32767));
            }
        }
        AssetDatabase.ImportAsset(path);return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
    static Material SaveMaterial(Material source,string name)
    {
        string path=Root+"/Materials/"+name+".mat";var existing=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(existing!=null)return existing;
        var material=new Material(source);AssetDatabase.CreateAsset(material,path);return material;
    }
    static TextMesh Label(Transform parent,string name,float height,Color color,float size,string text)
    {var label=SceneArtBake.Label(parent,height,color,size);label.name=name;label.text=text;return label;}

    [MenuItem("Count To Zero/Create Hierarchy Scene")]
    public static void Build()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play mode before authoring the scene.");
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        foreach(string dir in new[]{"Sprites","Audio","Materials","Prefabs"})Directory.CreateDirectory(Root+"/"+dir);
        AssetDatabase.Refresh();
        var white=new Texture2D(2,2);white.SetPixels(new[]{Color.white,Color.white,Color.white,Color.white});white.Apply();
        SaveSprite(Sprite.Create(white,new UnityEngine.Rect(0,0,2,2),Vector2.one*.5f),"UIWhite");
        var disc=SaveSprite(SceneArtBake.Disc,"NumberDisc");
        var playerSprite=SaveSprite(SceneArtBake.Player,"Glitch");var bossSprite=SaveSprite(SceneArtBake.Boss,"Omega");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var environment=Group("Environment");SceneArtBake.Build(environment);
        foreach(var renderer in environment.GetComponentsInChildren<Renderer>())
        {
            var original=renderer.sharedMaterial;
            renderer.sharedMaterial=SaveMaterial(original,"Arena_"+ColorUtility.ToHtmlStringRGBA(original.color));
            Object.DestroyImmediate(original);
        }
        var camera=Group("Arena Camera").gameObject.AddComponent<Camera>();camera.tag="MainCamera";
        camera.transform.position=new Vector3(0,21,-18);camera.transform.LookAt(Vector3.zero);
        camera.orthographic=true;camera.orthographicSize=11.5f;camera.farClipPlane=100;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.012f,.019f,.04f);
        camera.gameObject.AddComponent<AudioListener>();
        var spawn=Group("Spawn Points");
        var playerSpawn=Group("Player Spawn",spawn);playerSpawn.position=new Vector3(0,0,-4);
        var bossSpawn=Group("Boss Spawn",spawn);bossSpawn.position=new Vector3(0,0,4.4f);
        var actors=Group("Actors");var player=Group("GLITCH-0",actors).gameObject.AddComponent<PlayerController>();player.transform.position=playerSpawn.position;
        var ps=SceneArtBake.Sprite(player.transform,"Glitch sprite",playerSprite,ArenaVisuals.Cyan,1.15f,Vector3.up*.7f);
        Wire(player,"sprite",ps);Wire(player,"label",Label(player.transform,"Current Value",1.65f,ArenaVisuals.Cyan,.15f,"+1"));Wire(player,"spawnPoint",playerSpawn);
        var shadow=SceneArtBake.Sprite(player.transform,"Ground shadow",disc,new Color(0,0,0,.5f),1,Vector3.up*.03f);
        Object.DestroyImmediate(shadow.GetComponent<Billboard>());shadow.transform.rotation=Quaternion.Euler(90,0,0);
        var boss=Group("OMEGA-99",actors).gameObject.AddComponent<BossController>();boss.transform.position=bossSpawn.position;
        Wire(boss,"sprite",SceneArtBake.Sprite(boss.transform,"Omega sprite",bossSprite,ArenaVisuals.Red,2.7f,Vector3.up*1.2f));
        Wire(boss,"label",Label(boss.transform,"Current Value",2.9f,ArenaVisuals.Red,.14f,"OMEGA-99 [50]"));Wire(boss,"spawnPoint",bossSpawn);
        var systems=Group("Systems");var manager=Group("Game Manager",systems).gameObject.AddComponent<GameManager>();
        var orbs=Group("Orb Spawner",systems).gameObject.AddComponent<OrbSpawner>();var bullets=Group("Bullet Patterns",systems).gameObject.AddComponent<BulletPattern>();
        var pools=Group("Runtime Pools");Wire(orbs,"poolRoot",Group("Orbs",pools));Wire(bullets,"poolRoot",Group("Projectiles",pools));
        var orb=Group("Number Orb").gameObject.AddComponent<NumberOrb>();
        Wire(orb,"sprite",SceneArtBake.Sprite(orb.transform,"Orb sprite",disc,ArenaVisuals.Cyan,.85f,Vector3.up*.55f));
        var orbLabel=Label(orb.transform,"Operation",.64f,Color.white,.15f,"+3");orbLabel.transform.localPosition+=Vector3.back*.07f;Wire(orb,"label",orbLabel);
        var prefab=PrefabUtility.SaveAsPrefabAsset(orb.gameObject,Root+"/Prefabs/NumberOrb.prefab");Wire(orbs,"orbPrefab",prefab.GetComponent<NumberOrb>());Object.DestroyImmediate(orb.gameObject);
        var projectile=Group("Projectile");SceneArtBake.Sprite(projectile,"Projectile sprite",disc,ArenaVisuals.Red,.27f,Vector3.up*.55f);
        Wire(bullets,"projectilePrefab",PrefabUtility.SaveAsPrefabAsset(projectile.gameObject,Root+"/Prefabs/Projectile.prefab"));Object.DestroyImmediate(projectile.gameObject);
        var opening=Group("Opening Orbs",spawn);var points=new Object[5];
        Vector3[] positions={new Vector3(-2,0,-4),new Vector3(2,0,-4),new Vector3(-4,0,-1),new Vector3(4,0,-1),Vector3.zero};
        int[] operands={3,3,2,5,2};MathOpType[] ops={MathOpType.Add,MathOpType.Add,MathOpType.Subtract,MathOpType.Add,MathOpType.Divide};
        for(int i=0;i<5;i++){var point=Group("Orb "+(i+1)+" / "+ops[i]+" "+operands[i],opening).gameObject.AddComponent<OrbSpawnPoint>();point.transform.position=positions[i];point.Operation=ops[i];point.Operand=operands[i];points[i]=point;}
        WireArray(orbs,"openingOrbs",points);
        var edges=Group("Edge Orb Spawns",spawn);var edgePoints=new Object[12];
        for(int i=0;i<12;i++){var point=Group("Edge "+(i+1),edges);float a=i*Mathf.PI/6;point.position=new Vector3(Mathf.Cos(a)*9.4f,0,Mathf.Sin(a)*6.5f);edgePoints[i]=point;}
        WireArray(orbs,"edgeSpawnPoints",edgePoints);
        var fx=Group("Effects");var feedback=fx.gameObject.AddComponent<FeedbackManager>();
        var source=fx.gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.volume=.2f;Wire(feedback,"audioSource",source);
        Wire(feedback,"pickup",SaveTone("Pickup",700,.07f));Wire(feedback,"lucky",SaveTone("Lucky",260,.3f));Wire(feedback,"hit",SaveTone("Hit",90,.17f));
        var laser=Group("Overload Beam",fx).gameObject.AddComponent<LineRenderer>();laser.positionCount=2;laser.startWidth=.24f;laser.endWidth=.08f;
        var beamMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));beamMaterial.color=ArenaVisuals.Green;
        laser.sharedMaterial=SaveMaterial(beamMaterial,"OverloadBeam");Object.DestroyImmediate(beamMaterial);laser.enabled=false;Wire(feedback,"laser",laser);
        var floating=Label(fx,"Floating Feedback",0,ArenaVisuals.Green,.22f,"LUCKY!");floating.gameObject.SetActive(false);Wire(feedback,"floating",floating);
        var hazard=SceneArtBake.Sprite(fx,"Overflow Danger Zone",disc,new Color(1,.16f,.3f,.4f),5,Vector3.zero);
        Object.DestroyImmediate(hazard.GetComponent<Billboard>());hazard.transform.rotation=Quaternion.Euler(90,0,0);hazard.gameObject.SetActive(false);
        Wire(manager,"View",camera);Wire(manager,"Player",player);Wire(manager,"Boss",boss);Wire(manager,"Orbs",orbs);Wire(manager,"Bullets",bullets);Wire(manager,"Feedback",feedback);Wire(manager,"hazard",hazard);
        BuildHUD();
        foreach(var billboard in Object.FindObjectsByType<Billboard>(FindObjectsInactive.Include,FindObjectsSortMode.None))billboard.transform.rotation=camera.transform.rotation;
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/CountToZero.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/CountToZero.unity",true)};
        AssetDatabase.SaveAssets();Selection.activeGameObject=environment.gameObject;
        SceneView.lastActiveSceneView?.LookAt(Vector3.zero,Quaternion.Euler(50,0,0),22);
        GameplayUpgrade.Upgrade();
        Debug.Log("COUNT TO ZERO: Hierarchy scene saved with persistent sprites, materials, audio, prefabs and Canvas.");
    }
    static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
    {
        var go=new GameObject(name,typeof(RectTransform));var rt=(RectTransform)go.transform;rt.SetParent(parent,false);
        rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(x,-y);rt.sizeDelta=new Vector2(w,h);return rt;
    }
    static Image Panel(string name,Transform parent,float x,float y,float w,float h,Color color)
    {var image=Rect(name,parent,x,y,w,h).gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return image;}
    static Text Text(string name,Transform parent,float x,float y,float w,float h,string text,int size,Color color)
    {
        var label=Rect(name,parent,x,y,w,h).gameObject.AddComponent<Text>();label.font=font;label.fontSize=size;label.text=text;label.color=color;
        label.raycastTarget=false;label.verticalOverflow=VerticalWrapMode.Overflow;return label;
    }
    static Image Bar(string name,Transform parent,float x,float y,float w,Color color)
    {
        var back=Panel(name,parent,x,y,w,8,new Color(.1f,.15f,.2f));var fill=Panel("Fill",back.transform,0,0,w,8,color);
        fill.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Sprites/UIWhite.png");fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;return fill;
    }
    static void BuildHUD()
    {
        font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var canvas=Group("HUD Canvas").gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=canvas.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,800);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        canvas.gameObject.AddComponent<GraphicRaycaster>();var hud=canvas.gameObject.AddComponent<HUDController>();
        // Center a fixed reference layout within wider/taller windows without changing authored positions.
        var layout=Rect("Reference Layout",canvas.transform,0,0,1280,800);layout.anchorMin=layout.anchorMax=layout.pivot=new Vector2(.5f,.5f);
        var top=Panel("Header",layout,24,20,1232,100,Ink).transform;
        Text("Kicker",top,20,10,280,22,"THE DETERMINISTIC CORE",13,Color.gray);
        Text("Title",top,20,35,300,45,"COUNT TO ZERO",30,Color.white);
        Wire(hud,"bossTitle",Text("Boss Phase",top,336,10,600,25,"OMEGA-99 / PHASE 01",13,Color.white));
        Wire(hud,"healthFill",Bar("Boss HP",top,336,48,470,ArenaVisuals.Red));
        Wire(hud,"bossHealth",Text("Boss HP Value",top,822,35,130,40,"99 / 99",18,ArenaVisuals.Red));
        Wire(hud,"score",Text("Score",top,976,10,240,80,"SCORE / x1.0\n000000",24,Color.white));
        var rule=Panel("Lucky Rule",layout,24,136,244,82,Ink).transform;
        Wire(hud,"ruleClock",Text("Rule Clock",rule,16,7,220,24,"LUCKY RULE / 15s",13,ArenaVisuals.Gold));
        Wire(hud,"rule",Text("Rule",rule,16,39,224,32,"EXACTLY 7",18,ArenaVisuals.Gold));
        Wire(hud,"ruleFill",Bar("Rule Timer",rule,0,78,244,ArenaVisuals.Gold));
        var bottom=Panel("Player HUD",layout,24,652,1232,126,Ink).transform;
        Text("Player Label",bottom,20,13,280,24,"GLITCH-0 / CURRENT VALUE",13,Color.white);
        Wire(hud,"currentValue",Text("Current Value",bottom,20,34,155,75,"+1",58,ArenaVisuals.Cyan));
        Wire(hud,"integrity",Text("Integrity",bottom,176,45,160,65,"INTEGRITY\n5 / 5",18,ArenaVisuals.Red));
        Text("Dash Label",bottom,359,17,245,24,"SPACE / INVERT DASH",18,Color.white);
        Wire(hud,"dashFill",Bar("Dash Cooldown",bottom,359,53,220,ArenaVisuals.Cyan));
        Wire(hud,"dashStatus",Text("Dash Status",bottom,359,69,245,40,"READY / FLIPS YOUR SIGN",13,Color.white));
        Text("Focus Label",bottom,633,17,240,24,"E / BULLET TIME",18,Color.white);
        Wire(hud,"focusFill",Bar("Focus Charge",bottom,633,53,220,ArenaVisuals.Violet));
        Wire(hud,"focusStatus",Text("Focus Status",bottom,633,69,245,40,"GRAZE BULLETS TO CHARGE",13,Color.white));
        Text("Controls",bottom,918,17,300,100,"WASD / ARROWS   MOVE\n\nMOUSE + LMB   AIM / FIRE\n\nJ   FIRE AT BOSS    ESC   PAUSE",13,Color.white);
        Wire(hud,"announcement",Text("Announcement",layout,310,599,700,32,"",18,ArenaVisuals.Green));
        Wire(hud,"status",Text("Status",layout,900,140,340,70,"",18,ArenaVisuals.Gold));
        var overlay=Panel("Menu Overlay",layout,0,0,1280,800,new Color(.008f,.014f,.03f,.86f));overlay.raycastTarget=true;
        var card=Panel("Menu Card",overlay.transform,285,142,710,510,Ink).transform;
        Panel("Accent",card,0,0,710,3,ArenaVisuals.Cyan);
        Text("Kicker",card,42,33,630,25,"GLITCH-0 // LUCK IS A SYSTEM ERROR",13,ArenaVisuals.Cyan);
        Wire(hud,"overlayTitle",Text("Heading",card,42,69,640,80,"COUNT TO ZERO",52,Color.white));
        Wire(hud,"overlayBody",Text("Instructions",card,42,165,640,240,"Collect orbs. Match the Lucky Rule. Survive OMEGA-99.",18,Color.white));
        var buttonImage=Panel("Start Resume Restart",card,42,408,626,58,new Color(.08f,.19f,.23f));buttonImage.raycastTarget=true;
        var button=buttonImage.gameObject.AddComponent<Button>();button.targetGraphic=buttonImage;
        var label=Text("Button Label",button.transform,0,0,626,58,"ENTER THE CORE [ ENTER ]",20,Color.white);label.alignment=TextAnchor.MiddleCenter;
        Wire(hud,"actionButton",button);Wire(hud,"actionLabel",label);Wire(hud,"overlay",overlay.gameObject);
        var events=Group("EventSystem");events.gameObject.AddComponent<EventSystem>();events.gameObject.AddComponent<InputSystemUIInputModule>();
    }
}
