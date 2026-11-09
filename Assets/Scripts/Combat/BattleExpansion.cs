using System.Collections.Generic;
using TMPro;
using UnityEngine;
namespace CountToZero
{
 public sealed class BattleExpansion:MonoBehaviour
 {
  public static BattleExpansion Instance {get;private set;}
  [SerializeField] Sprite[] assistantFrames;
  [SerializeField] Sprite pixel;
  [SerializeField] Material lineMaterial;
  [SerializeField] TMP_Text lossNotice;
  sealed class Ally {public Transform root;public SpriteRenderer visual;public TMP_Text label;public float hp=3,shot,immune;public int style;}
  sealed class Debris {public SpriteRenderer visual;public Vector3 velocity;public float age;public Color color;public bool alternate;}
  readonly List<Ally> allies=new();readonly List<Debris> debris=new();readonly List<SpriteRenderer> segments=new();readonly List<float> segmentHP=new();
  LineRenderer bolt,boltGlow,ring;float spawnAt,lightningAt,lightningAge=-1,noticeUntil;Vector3 strike;bool struck;Vector3 bossScale;
  public bool LightningActive=>lightningAge>=1.5f;
  public void SummonLightning(Vector3 position){lightningAge=0;strike=position;struck=false;}
  public int AssistantCount=>allies.Count;
  public int SegmentsAlive=>segments.Count;
  public Vector3 BossAim {get{var p=segments.Count>0?segments[segments.Count-1].transform.position:G.Boss.transform.position;p.y=0;return p;}}
  GameManager G=>GameManager.Instance;
  void Awake(){Instance=this;bolt=Line("Tracking lightning",.12f);boltGlow=Line("Lightning blue outline",.3f);boltGlow.sortingOrder=-1;ring=Line("Lightning warning",.06f);bolt.enabled=boltGlow.enabled=ring.enabled=false;}
  void OnDestroy(){if(Instance==this)Instance=null;}
  LineRenderer Line(string name,float width){var l=new GameObject(name).AddComponent<LineRenderer>();l.transform.SetParent(transform);l.sharedMaterial=lineMaterial;l.widthMultiplier=width;l.useWorldSpace=true;return l;}
  SpriteRenderer Sprite(string name,Vector3 p,Sprite art,float scale){var s=new GameObject(name).AddComponent<SpriteRenderer>();s.transform.SetParent(transform);s.transform.position=p;s.sprite=art;s.transform.localScale=Vector3.one*scale;s.sortingOrder=8;return s;}
  public void ResetBattle(){snakeCreated=false;foreach(var a in allies)Destroy(a.root.gameObject);allies.Clear();foreach(var s in segments)Destroy(s.gameObject);segments.Clear();segmentHP.Clear();foreach(var d in debris)Destroy(d.visual.gameObject);debris.Clear();spawnAt=4;lightningAt=12;lightningAge=-1;if(bolt!=null)bolt.enabled=boltGlow.enabled=ring.enabled=false;if(lossNotice!=null)lossNotice.text="";}
  public bool SpawnAssistant(){if(allies.Count>=4||assistantFrames==null||assistantFrames.Length<16)return false;int style=allies.Count%2;var root=new GameObject("Assistant").transform;root.SetParent(transform);root.position=G.Player.transform.position+Vector3.right*(allies.Count%2==0?-1:1);var v=Sprite("Assistant Visual",root.position+Vector3.up*.7f,assistantFrames[style*8],.85f);v.transform.SetParent(root,true);var text=new GameObject("Assistant label").AddComponent<TextMeshPro>();text.transform.SetParent(v.transform,false);text.transform.localPosition=new Vector3(0,1,0);text.fontSize=2.8f;text.alignment=TextAlignmentOptions.Center;text.rectTransform.sizeDelta=new Vector2(5,1);text.color=Color.white;allies.Add(new Ally{root=root,visual=v,label=text,style=style});return true;}
  // Surviving squad members turn toward the camera before the result panel.
  public void CelebrateVictory()
  {
   foreach(var ally in allies){ally.visual.sprite=assistantFrames[ally.style*8];ally.visual.transform.rotation=G.View.transform.rotation;ally.visual.color=Color.white;ally.label.gameObject.SetActive(false);}
  }
  public void Burst(Vector3 position,Color color,bool alternate,int count=40){for(int i=0;i<count;i++){var s=Sprite("Pixel debris",position,pixel,Random.Range(.06f,.18f));s.color=color;debris.Add(new Debris{visual=s,velocity=Random.onUnitSphere*Random.Range(2,6),color=color,alternate=alternate});}if(alternate)GameAudio.Play(SoundCue.Explosion);}
  void HurtAlly(Ally a,float damage=1){if(Time.time<a.immune)return;a.immune=Time.time+1;a.hp-=damage;if(a.hp>0)return;Burst(a.root.position,Color.white,true);Destroy(a.root.gameObject);allies.Remove(a);if(lossNotice!=null){lossNotice.text="Warning!!!";noticeUntil=Time.unscaledTime+4;}}
  public bool HitAssistant(Vector3 a,Vector3 b){foreach(var ally in allies)if(BulletPattern.SegmentDistance(ally.root.position,a,b)<.3f){HurtAlly(ally);return true;}return false;}
  public bool HitSegment(Vector3 from,Vector3 to,float damage){for(int i=0;i<segments.Count;i++)if(BulletPattern.SegmentDistance(new Vector3(segments[i].transform.position.x,0,segments[i].transform.position.z),from,to)<.6f){segmentHP[i]-=damage;if(segmentHP[i]<=0){Burst(segments[i].transform.position,Color.white,true,24);Destroy(segments[i].gameObject);segments.RemoveAt(i);segmentHP.RemoveAt(i);}return true;}return false;}
  public void DamageSegment(float damage){if(segments.Count==0)return;int i=segments.Count-1;segmentHP[i]-=damage;if(segmentHP[i]>0)return;Burst(segments[i].transform.position,Color.white,true,24);Destroy(segments[i].gameObject);segments.RemoveAt(i);segmentHP.RemoveAt(i);}
  bool snakeCreated;
  int lastLevel=-1;
  void Update()
  {
   if(G==null)return;
   for(int i=debris.Count-1;i>=0;i--){var d=debris[i];if(G.State==GameState.Paused)continue;d.age+=Time.unscaledDeltaTime;d.visual.transform.position+=d.velocity*Time.unscaledDeltaTime;d.visual.transform.rotation=G.View.transform.rotation;var c=d.alternate?(Mathf.FloorToInt(d.age*14)%2==0?Color.white:Color.black):d.color;c.a=1-d.age/.85f;d.visual.color=c;if(d.age>=.85f){Destroy(d.visual.gameObject);debris.RemoveAt(i);}}
   if(lossNotice!=null && Time.unscaledTime>noticeUntil)lossNotice.text="";
   if(G.State==GameState.Victory||G.State==GameState.Won){foreach(var ally in allies)ally.visual.transform.rotation=G.View.transform.rotation;}
   if(G.State!=GameState.Playing||G.Training){bolt.enabled=boltGlow.enabled=ring.enabled=false;return;}
   if(lastLevel!=Campaign.Data.level){lastLevel=Campaign.Data.level;snakeCreated=false;}
   bool snake=G.Boss.PatternIndex==21;
   if(snake&&!snakeCreated){snakeCreated=true;for(int i=0;i<5;i++){segments.Add(Sprite("Virus segment",G.Boss.transform.position+Vector3.back*(i+1)*1.2f+Vector3.up*.65f,G.Boss.GetComponentInChildren<SpriteRenderer>().sprite,.8f));segmentHP.Add(5);}}
   var bossVisual=G.Boss.GetComponentInChildren<SpriteRenderer>();if(bossScale==Vector3.zero)bossScale=bossVisual.transform.localScale;bossVisual.transform.localScale=snake?bossScale*.55f:bossScale;
   for(int i=0;i<segments.Count;i++){var s=segments[i];var target=i==0?G.Boss.transform.position+Vector3.up*.65f:segments[i-1].transform.position;var delta=target-s.transform.position;if(delta.magnitude>1.1f)s.transform.position+=delta.normalized*(delta.magnitude-1.1f)*Mathf.Min(1,Time.deltaTime*8);s.transform.rotation=G.View.transform.rotation;s.color=Color.Lerp(Color.magenta,Color.white,.25f);if(Vector3.Distance(new Vector3(s.transform.position.x,0,s.transform.position.z),G.Player.transform.position)<.65f)G.Player.Hurt(false,"Virus segment");}
   if(G.Elapsed>=spawnAt){spawnAt=G.Elapsed+12;SpawnAssistant();}
   for(int i=allies.Count-1;i>=0;i--){var a=allies[i];var offset=new Vector3(Mathf.Cos(i*Mathf.PI*.5f)*1.8f,0,Mathf.Sin(i*Mathf.PI*.5f)*1.8f-1);var delta=G.Player.transform.position+offset-a.root.position;var previous=a.root.position;a.root.position=G.Encounters.Move(a.root.position,Vector3.ClampMagnitude(delta,1)*Time.deltaTime*4);a.visual.transform.rotation=G.View.transform.rotation;bool front=a.root.position.z>G.Boss.transform.position.z;a.visual.sprite=assistantFrames[a.style*8+(front?0:4)+(int)(Time.time*8)%4];a.label.text="Assistant No "+(i+1)+"\n"+new string('|',Mathf.CeilToInt(a.hp));if(Time.time>a.shot){a.shot=Time.time+.8f;var target=G.Encounters.AutoAim(a.root.position);G.Bullets.Fire(a.root.position,(target-a.root.position).normalized,false,12);}if(G.Encounters.Blocked(a.root.position))HurtAlly(a);else if(G.Hazards!=null && G.Hazards.BeamActive && G.Hazards.CrossesBeam(previous,a.root.position))HurtAlly(a,1.5f);}
   if(Campaign.Data.level<4)return;
   if(lightningAge<0 && G.Elapsed>=lightningAt){SummonLightning(G.Player.transform.position);}
   if(lightningAge<0)return;lightningAge+=Time.deltaTime;ring.enabled=true;ring.positionCount=41;float radius=1.1f;for(int i=0;i<=40;i++){float a=i*Mathf.PI/20;ring.SetPosition(i,strike+new Vector3(Mathf.Cos(a)*radius,.06f,Mathf.Sin(a)*radius));}ring.startColor=ring.endColor=lightningAge<1.5f?Color.red:Color.cyan;
   if(lightningAge>=1.5f){if(!struck){struck=true;GameAudio.Play(SoundCue.Warning);}strike=Vector3.MoveTowards(strike,G.Player.transform.position,Time.deltaTime*(.8f+Mathf.Min(1.2f,Campaign.Data.level*.03f)));bolt.enabled=boltGlow.enabled=true;boltGlow.positionCount=9;boltGlow.startColor=boltGlow.endColor=Color.cyan;bolt.positionCount=9;bolt.startColor=bolt.endColor=(int)(lightningAge*16)%2==0?Color.white:Color.cyan;for(int i=0;i<9;i++)bolt.SetPosition(i,strike+Vector3.up*i*1.3f+(i==0?Vector3.zero:Vector3.right*Random.Range(-.35f,.35f)));for(int i=0;i<9;i++)boltGlow.SetPosition(i,bolt.GetPosition(i));if(Vector3.Distance(strike,G.Player.transform.position)<.8f)G.Player.Hurt(false,"Tracking lightning");for(int i=allies.Count-1;i>=0;i--)if(Vector3.Distance(strike,allies[i].root.position)<1)HurtAlly(allies[i]);}
   if(lightningAge>5){lightningAge=-1;lightningAt=G.Elapsed+Mathf.Max(5,12-Campaign.Data.level*.15f);bolt.enabled=boltGlow.enabled=ring.enabled=false;}
  }
 }
}
