using TMPro;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
namespace CountToZero
{
    public enum CampaignPage { Title, Preload, Story, TurnEnd, StoryEnd }
    [Serializable] public class StoryBook { public string[] intro; public string[] ending; }
    public sealed class CampaignScreen : MonoBehaviour
    {
        [SerializeField] CampaignPage page;
        [SerializeField] TMP_Text heading,body,primaryLabel,secondaryLabel,progressLabel;
        [SerializeField] Button primary,secondary;
        [SerializeField] Slider progress;
        [SerializeField] TextAsset storyJson;
        StoryBook story;int line;string[] lines;bool loading;
        void Start()
        {
            Time.timeScale=1;story=JsonUtility.FromJson<StoryBook>(storyJson.text);
            primary.onClick.AddListener(Primary);secondary.onClick.AddListener(Secondary);
            progress.gameObject.SetActive(page==CampaignPage.Preload);progressLabel.gameObject.SetActive(page==CampaignPage.Preload);
            if(page==CampaignPage.Title){if(GetComponent<TitleMenuView>()==null)heading.text="COUNT TO ZERO";if(body!=null)body.text="Break the deterministic core.\n30 levels. 25 boss patterns. One impossible survivor.";primaryLabel.text="NEW GAME";secondaryLabel.text="CONTINUE";secondary.interactable=Campaign.HasSave;}
            if(page==CampaignPage.Preload){heading.text="PRELOADING SECTOR";body.text="Preparing the next scene...";primary.gameObject.SetActive(false);secondary.gameObject.SetActive(false);StartCoroutine(Load());}
            if(page==CampaignPage.Story||page==CampaignPage.StoryEnd){if(!Campaign.Active)Campaign.Resume();lines=page==CampaignPage.Story?story.intro:story.ending;ShowLine();}
            if(page==CampaignPage.TurnEnd){heading.text=Campaign.LastWon?(Campaign.BlockComplete?"BLOCK COMPLETE":"LEVEL COMPLETE"):"SIGNAL LOST";body.text="LEVEL "+Campaign.Data.level+" / BLOCK "+Campaign.Block+"\n\n"+"TOTAL SCORE  "+Campaign.Data.totalScore+"\nNEXT DIFFICULTY  "+Campaign.Data.difficulty.ToString("0.00")+"x"+(Campaign.BlockComplete?"\nADAPTATION: "+Campaign.PreviousDifficulty.ToString("0.00")+"x -> "+Campaign.Data.difficulty.ToString("0.00")+"x":"");primaryLabel.text=Campaign.LastWon?(Campaign.NeedsEnding?"STORY END":"NEXT LEVEL"):"RETRY LEVEL";secondaryLabel.text="SAVE & TITLE";}
            if(page!=CampaignPage.Preload)EventSystem.current?.SetSelectedGameObject(primary.gameObject);
        }
        void ShowLine(){heading.text=page==CampaignPage.Story?"PROLOGUE":"CORE LIBERATED";body.text=lines[line];primaryLabel.text="NEXT";secondaryLabel.text="SKIP";}
        public void StartDifficulty(int difficulty){Campaign.NewGame(difficulty);Go("Story");}
        public int StoryLine => line;
        public CampaignPage Page => page;
        void EndStory()
        {
            if(page==CampaignPage.Story){Campaign.Data.storySeen=true;Campaign.Save();Go("CountToZero");}
            else{Campaign.Data.endingSeen=true;Campaign.Save();line=lines.Length;heading.text="BEYOND ZERO";body.text="Virus and Super Com defeated.\nKeep protecting the system?\nInfinite sectors await.";primaryLabel.text="YES / ENDLESS";secondaryLabel.text="NO / TITLE";}
        }
        void Primary()
        {
            if(loading)return;
            switch(page){
                case CampaignPage.Title:GetComponent<CampaignExpansionUI>().ChooseDifficulty();break;
                case CampaignPage.Story:case CampaignPage.StoryEnd:
                    if(line>=lines.Length){Campaign.EnableEndless();Go("CountToZero");}
                    else if(++line>=lines.Length)EndStory();else ShowLine();break;
                case CampaignPage.TurnEnd:
                    if(Campaign.NeedsEnding)Go("StoryEnd");else{Campaign.Advance();Go("CountToZero");}break;
            }
        }
        void Secondary()
        {
            if(loading)return;
            if(page==CampaignPage.Title){if(Campaign.Resume()){
                if(Campaign.NeedsEnding||(!Campaign.Data.endless&&Campaign.Data.cleared>=30))Go("StoryEnd");
                else {if(Campaign.Data.cleared==Campaign.Data.level)Campaign.Data.level++;Go(Campaign.Data.storySeen?"CountToZero":"Story");}
            }}
            else if(page==CampaignPage.Story||page==CampaignPage.StoryEnd){if(line<lines.Length)EndStory();else Title();}
            else Title();
        }
        void Title(){Campaign.Leave();SceneManager.LoadScene("Title");}
        void Go(string scene){loading=true;Campaign.NextScene=scene;SceneManager.LoadScene("Preload");}
        IEnumerator Load()
        {
            var operation=SceneManager.LoadSceneAsync(Campaign.NextScene);operation.allowSceneActivation=false;
            while(operation.progress<.9f){progress.value=operation.progress/.9f;progressLabel.text=Mathf.RoundToInt(progress.value*100)+"%";yield return null;}
            progress.value=1;progressLabel.text="100% / READY";yield return new WaitForSecondsRealtime(.25f);operation.allowSceneActivation=true;
        }
    }
}
