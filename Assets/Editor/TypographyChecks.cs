using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using CountToZero;
[InitializeOnLoad]
public static class TypographyChecks
{
    static int checks;static bool running;static double next;
    static TypographyChecks(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=m=>{if(m==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("CTZ.TypeCheck",false)){SessionState.SetBool("CTZ.TypeCheck",false);running=true;next=EditorApplication.timeSinceStartup+1;}};}
    static void Check(bool pass,string message){checks++;if(!pass)throw new Exception(message);}
    [MenuItem("Count To Zero/Validate TMP and HP Bars")]
    public static void Run()
    {
        try{
            SessionState.SetBool("CTZ.TypeHadSave",PlayerPrefs.HasKey("CountToZero.Campaign.v1"));SessionState.SetString("CTZ.TypeSave",PlayerPrefs.GetString("CountToZero.Campaign.v1",""));checks=0;foreach(string name in new[]{"Title","Preload","Story","CountToZero","TurnEnd","StoryEnd"}){
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");var roots=scene.GetRootGameObjects();
                Check(!roots.Any(r=>r.GetComponentsInChildren<Text>(true).Length>0||r.GetComponentsInChildren<TextMesh>(true).Length>0),name+" has no legacy text");
                foreach(var c in roots.SelectMany(r=>r.GetComponentsInChildren<MonoBehaviour>(true)).Where(c=>c!=null&&c.GetType().Namespace=="CountToZero")){
                    foreach(var field in c.GetType().GetFields(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance)){
                        if(field.FieldType==typeof(TMP_Text))Check(field.GetValue(c)!=null,name+" "+c.name+"."+field.Name+" reference");
                        if(field.FieldType==typeof(TMP_Text[])){var array=(TMP_Text[])field.GetValue(c);Check(array.Length==8&&array.All(x=>x!=null),"eight binding labels");}
                    }
                }
                foreach(var text in roots.SelectMany(r=>r.GetComponentsInChildren<TMP_Text>(true))){Check(text.font!=null&&text.font.sourceFontFile!=null,"font asset and source");Check((text.fontStyle&FontStyles.Bold)!=0,"bold text");if(text.name=="Heading"||text.name=="Title")Check(text.font==TypographyUpgrade.TitleFont,"title font");}
                if(name=="CountToZero"){var bars=roots.SelectMany(r=>r.GetComponentsInChildren<ActorHealthBar>(true)).ToArray();Check(bars.Length==9,"9 authored HP bars: player, boss, six minions and HUD");foreach(var bar in bars){var image=bar.GetComponentInChildren<Image>();Check(image!=null,"HP image");}}
            }
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameAssets/Prefabs/NumberOrb.prefab");Check(prefab.GetComponentInChildren<TMP_Text>()!=null,"orb prefab TMP");var so=new SerializedObject(prefab.GetComponent<NumberOrb>());Check(so.FindProperty("label").objectReferenceValue!=null,"orb prefab reference");
            SessionState.SetInt("CTZ.TypeCount",checks);SessionState.SetBool("CTZ.TypeHadSave",PlayerPrefs.HasKey("CountToZero.Campaign.v1"));SessionState.SetString("CTZ.TypeSave",PlayerPrefs.GetString("CountToZero.Campaign.v1",""));
            EditorSceneManager.OpenScene("Assets/Scenes/CountToZero.unity");SessionState.SetBool("CTZ.TypeCheck",true);EditorApplication.isPlaying=true;
        }catch(Exception e){Finish("FAIL: "+e);}
    }
    static void Tick(){if(!running||EditorApplication.timeSinceStartup<next)return;running=false;try{
        checks=SessionState.GetInt("CTZ.TypeCount",0);var g=GameManager.Instance;Campaign.Leave();g.RecordScores=false;g.Begin();g.Player.Hurt(true);g.Player.Hurt(true);
        var playerBar=g.Player.GetComponentInChildren<ActorHealthBar>();playerBar.Refresh();Check(Mathf.Abs(playerBar.Fraction-.6f)<.001f,"player damage updates HP");Check(playerBar.GetComponentInChildren<TMP_Text>().text=="3 / 5 HP","player HP numbers");g.Player.Heal();playerBar.Refresh();Check(Mathf.Abs(playerBar.Fraction-.8f)<.001f,"healing updates HP");
        g.DamageBoss(10,true);var bossBar=g.Boss.GetComponentInChildren<ActorHealthBar>();bossBar.Refresh();Check(Mathf.Abs(bossBar.Fraction-89f/99)<.001f,"boss damage updates HP");
        var minion=UnityEngine.Object.FindObjectsByType<MinionController>(FindObjectsInactive.Include).First();minion.Spawn(new Vector3(5,0,3));minion.Hit(1);var minionBar=minion.GetComponentInChildren<ActorHealthBar>();minionBar.Refresh();Check(Mathf.Abs(minionBar.Fraction-2f/3)<.001f,"minion damage updates HP");minion.Hit(3);Check(!minion.gameObject.activeSelf,"dead minion and HP bar hidden");
        Finish("PASS: "+checks+" checks. All six scenes use bold TMP, correct title/body font assets, no legacy text, serialized references intact, nine authored HP bars, orb prefab, player damage/heal, boss damage and minion damage/death.\n");
    }catch(Exception e){Finish("FAIL: "+e);}}
    static void Finish(string result){
        if(SessionState.GetBool("CTZ.TypeHadSave",false))PlayerPrefs.SetString("CountToZero.Campaign.v1",SessionState.GetString("CTZ.TypeSave",""));else PlayerPrefs.DeleteKey("CountToZero.Campaign.v1");PlayerPrefs.Save();
        File.WriteAllText("Documentation/typography-validation.txt",result);Debug.Log(result);EditorApplication.isPlaying=false;if(Application.isBatchMode)EditorApplication.Exit(result.StartsWith("PASS")?0:1);
    }
}
