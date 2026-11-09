using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
namespace CountToZero
{
    public enum TutorialStep { Off, Move, Lucky, Celebrate, Fire, Dash, Dodge }
    [Serializable] public class TutorialLine { public string id,title,text; }
    [Serializable] public class TutorialText { public TutorialLine[] steps; }
    public sealed class TutorialDirector : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text heading,instruction,skipLabel;
        [SerializeField] Button skip;
        [SerializeField] Image progress;
        [SerializeField] Transform orbSpawn;
        [SerializeField] SpriteRenderer marker;
        [SerializeField] TextAsset englishJson;
        [SerializeField] float dodgeDuration=12;
        TutorialText text;Vector3 origin,countAt;float age,fireAt,hitAt;int shots,dashes,countOrbs;
        public TutorialStep Step {get;private set;}
        public bool Active=>Step!=TutorialStep.Off;
        public bool CanFire=>!Active||Step>=TutorialStep.Fire;
        public bool CanDash=>!Active||Step>=TutorialStep.Dash;
        GameManager Game=>GameManager.Instance;
        void Awake(){text=JsonUtility.FromJson<TutorialText>(englishJson.text);skip.onClick.AddListener(Skip);panel.SetActive(false);marker.gameObject.SetActive(false);}
        public void ResetForRun()
        {
            Step=TutorialStep.Off;age=0;panel.SetActive(false);marker.gameObject.SetActive(false);
            if(!Campaign.Active||Campaign.Data.level!=1||Campaign.Data.tutorialComplete)return;
            Game.Orbs.Clear();Game.Encounters.ResetRun();origin=Game.Player.transform.position;SetStep(TutorialStep.Move);
        }
        void SetStep(TutorialStep step){Step=step;age=0;fireAt=0;hitAt=0;shots=Game.Player.ShotsFired;dashes=Game.Player.DashesUsed;}
        public void LuckyMatched(){if(Step==TutorialStep.Lucky)SetStep(TutorialStep.Celebrate);}
        public void PracticeHit(){if(Step==TutorialStep.Dodge&&Time.unscaledTime>hitAt){hitAt=Time.unscaledTime+1;Game.Feedback.Announce("PRACTICE HIT / KEEP MOVING",ArenaVisuals.Gold);GameAudio.Play(SoundCue.Hit);}}
        // Skip = straight into the live game: drop the camera intro and the whole training.
        public void Skip(){if(Active&&(Game.State==GameState.Intro||Game.State==GameState.Playing)){SkipToGame();return;}if(Game.State==GameState.Intro)Game.SkipIntro();}
        void SkipToGame(){if(Game.CountingDown)return;Step=TutorialStep.Off;marker.gameObject.SetActive(false);panel.SetActive(false);if(Campaign.Active){Campaign.Data.tutorialComplete=true;Campaign.Save();}Game.Begin();Game.SkipIntro();}
        void Complete(){Step=TutorialStep.Off;marker.gameObject.SetActive(false);panel.SetActive(false);if(Campaign.Active){Campaign.Data.tutorialComplete=true;Campaign.Save();}Game.Begin();Game.Feedback.Announce("PRACTICE COMPLETE / LIVE ROUND",ArenaVisuals.Green);GameAudio.Play(SoundCue.Phase);}
        void Update()
        {
            // During the 3-2-1 countdown the card names the stop and its lucky number.
            if(Game.CountingDown){panel.SetActive(Game.State!=GameState.Paused);marker.gameObject.SetActive(false);ShowStop();progress.fillAmount=0;return;}
            bool intro=Game.State==GameState.Intro;
            panel.SetActive((intro||Active)&&Game.State!=GameState.Paused&&!Game.Controls.OptionsOpen);
            marker.gameObject.SetActive(Active&&!intro&&(Step==TutorialStep.Move||Step==TutorialStep.Lucky));
            if(intro){ShowStop();skipLabel.text=Active?"SKIP TUTORIAL":"SKIP INTRO";progress.fillAmount=0;var k=Keyboard.current;var p=Gamepad.current;if((k!=null&&(k.enterKey.wasPressedThisFrame||k.spaceKey.wasPressedThisFrame))||(p!=null&&p.buttonSouth.wasPressedThisFrame))Game.SkipIntro();return;}
            if(!Active||Game.State!=GameState.Playing)return;
            if((Keyboard.current!=null&&Keyboard.current.tabKey.wasPressedThisFrame)||(Gamepad.current!=null&&Gamepad.current.selectButton.wasPressedThisFrame)){Skip();return;}
            age+=Time.deltaTime;skipLabel.text="SKIP TRAINING";progress.fillAmount=((int)Step-1)/6f;Show(Step.ToString().ToLowerInvariant());
            marker.transform.position=(Step==TutorialStep.Lucky?countAt:orbSpawn.position)+Vector3.up*.04f;
            if(Step==TutorialStep.Move && Vector3.Distance(origin,Game.Player.transform.position)>.9f){SetStep(TutorialStep.Lucky);countOrbs=0;SpawnCount(orbSpawn.position);}
            else if(Step==TutorialStep.Lucky&&Game.Orbs.Active.Count==0&&Game.Player.CurrentValue!=Lucky)SpawnCount(countOrbs%2==1?orbSpawn.position+Vector3.right*3.2f:orbSpawn.position);
            else if(Step==TutorialStep.Celebrate&&age>1.3f)SetStep(TutorialStep.Fire);
            else if(Step==TutorialStep.Fire&&Game.Player.ShotsFired-shots>=3)SetStep(TutorialStep.Dash);
            else if(Step==TutorialStep.Dash&&Game.Player.DashesUsed>dashes)SetStep(TutorialStep.Dodge);
            else if(Step==TutorialStep.Dodge){
                progress.fillAmount=Mathf.Lerp(5f/6,1,age/dodgeDuration);
                if(age>=dodgeDuration){Complete();return;}
                if(age>=fireAt){fireAt=age+1.4f;var aim=(Game.Player.transform.position-Game.Boss.transform.position).normalized;for(int i=-1;i<=1;i++)Game.Bullets.Fire(Game.Boss.transform.position,Quaternion.Euler(0,i*22,0)*aim,true,2.4f);}
            }
        }
        int Lucky=>Game.Rule.TargetValue;
        // Counting lesson: first orb gets you part of the way, the next orb is exactly what is missing.
        void SpawnCount(Vector3 at)
        {
            int need=Lucky-Game.Player.CurrentValue;
            int step=countOrbs==0&&need>3?2:need;
            countAt=at;countOrbs++;
            Game.Orbs.SpawnTraining(at,step>=0?MathOpType.Add:MathOpType.Subtract,Mathf.Abs(step));
        }
        void ShowStop()
        {
            var stop=WorldTour.Current;int level=Campaign.Active?Campaign.Data.level:1;
            heading.text="LEVEL "+level.ToString("00")+" / "+stop.Place;
            instruction.text=stop.Lore+"\nLUCKY NUMBER "+stop.Number+"  /  ENTER, SPACE or SKIP to fight";
        }
        void Show(string id){foreach(var line in text.steps)if(line.id==id){heading.text=Fill(line.title);instruction.text=Fill(line.text);return;}}
        string Fill(string raw){int value=Game.Player.CurrentValue,need=Lucky-value;return raw.Replace("{VALUE}",value.ToString()).Replace("{LUCKY}",Lucky.ToString()).Replace("{NEED}",(need>=0?"+":"-")+Mathf.Abs(need)).Replace("{MOVE}",Game.Controls.GetBinding(0)+" / "+Game.Controls.GetBinding(1)+" / "+Game.Controls.GetBinding(2)+" / "+Game.Controls.GetBinding(3)).Replace("{FIRE}",Game.Controls.GetBinding(4).ToString()).Replace("{DASH}",Game.Controls.GetBinding(5).ToString());}
    }
}
