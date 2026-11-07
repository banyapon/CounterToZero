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
        TutorialText text;Vector3 origin;float age,fireAt,hitAt;int shots,dashes;
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
        public void Skip(){if(Game.State==GameState.Intro){Game.SkipIntro();return;}if(Active&&Game.State==GameState.Playing)Complete();}
        void Complete(){Step=TutorialStep.Off;marker.gameObject.SetActive(false);panel.SetActive(false);if(Campaign.Active){Campaign.Data.tutorialComplete=true;Campaign.Save();}Game.Begin();Game.Feedback.Announce("PRACTICE COMPLETE / LIVE ROUND",ArenaVisuals.Green);GameAudio.Play(SoundCue.Phase);}
        void Update()
        {
            bool intro=Game.State==GameState.Intro;
            panel.SetActive((intro||Active)&&Game.State!=GameState.Paused&&!Game.Controls.OptionsOpen);
            marker.gameObject.SetActive(Active&&!intro&&(Step==TutorialStep.Move||Step==TutorialStep.Lucky));
            if(intro){Show("intro");skipLabel.text="SKIP INTRO";progress.fillAmount=0;var k=Keyboard.current;var p=Gamepad.current;if((k!=null&&(k.enterKey.wasPressedThisFrame||k.spaceKey.wasPressedThisFrame))||(p!=null&&p.buttonSouth.wasPressedThisFrame))Game.SkipIntro();return;}
            if(!Active||Game.State!=GameState.Playing)return;
            if(Gamepad.current!=null&&Gamepad.current.selectButton.wasPressedThisFrame){Skip();return;}
            age+=Time.deltaTime;skipLabel.text="SKIP TRAINING";progress.fillAmount=((int)Step-1)/6f;Show(Step.ToString().ToLowerInvariant());
            marker.transform.position=orbSpawn.position+Vector3.up*.04f;
            if(Step==TutorialStep.Move && Vector3.Distance(origin,Game.Player.transform.position)>.9f){Game.Orbs.SpawnTraining(orbSpawn.position,MathOpType.Add,6);SetStep(TutorialStep.Lucky);}
            else if(Step==TutorialStep.Celebrate&&age>1.3f)SetStep(TutorialStep.Fire);
            else if(Step==TutorialStep.Fire&&Game.Player.ShotsFired-shots>=3)SetStep(TutorialStep.Dash);
            else if(Step==TutorialStep.Dash&&Game.Player.DashesUsed>dashes)SetStep(TutorialStep.Dodge);
            else if(Step==TutorialStep.Dodge){
                progress.fillAmount=Mathf.Lerp(5f/6,1,age/dodgeDuration);
                if(age>=dodgeDuration){Complete();return;}
                if(age>=fireAt){fireAt=age+1.4f;var aim=(Game.Player.transform.position-Game.Boss.transform.position).normalized;for(int i=-1;i<=1;i++)Game.Bullets.Fire(Game.Boss.transform.position,Quaternion.Euler(0,i*22,0)*aim,true,2.4f);}
            }
        }
        void Show(string id){foreach(var line in text.steps)if(line.id==id){heading.text=line.title;instruction.text=line.text.Replace("{MOVE}",Game.Controls.GetBinding(0)+" / "+Game.Controls.GetBinding(1)+" / "+Game.Controls.GetBinding(2)+" / "+Game.Controls.GetBinding(3)).Replace("{FIRE}",Game.Controls.GetBinding(4).ToString()).Replace("{DASH}",Game.Controls.GetBinding(5).ToString());return;}}
    }
}
