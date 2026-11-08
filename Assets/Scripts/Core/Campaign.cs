using System;
using UnityEngine;

namespace CountToZero
{
    [Serializable] public class CampaignSave
    {
        public int hazardSeed;
        public int selectedDifficulty=1;
        public int level=1, cleared, blocks, totalScore, failures, blockWins, blockDamage;
        public float difficulty=1, blockSeconds;
        public bool endless, storySeen, endingSeen, introSeen, tutorialComplete;
    }
    public static class Campaign
    {
        const string SaveKey="CountToZero.Campaign.v1";
        public static CampaignSave Data { get; private set; } = new CampaignSave();
        public static bool Active { get; private set; }
        public static bool LastWon { get; private set; }
        public static bool BlockComplete { get; private set; }
        public static bool NeedsEnding => Data.cleared>=30 && !Data.endless && !Data.endingSeen;
        public static string LastCause="";
        public static float PreviousDifficulty=1;
        public static string NextScene="Story";
        public static int PatternIndex => (Data.level-1)%25;
        public static int Block => (Data.level-1)/20+1;
        public static bool HasSave => PlayerPrefs.HasKey(SaveKey);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics(){Active=false;Data=new CampaignSave();NextScene="Story";LastWon=BlockComplete=false;}
        public static void NewGame(int difficulty=1){Data=new CampaignSave { hazardSeed=UnityEngine.Random.Range(1,1000000), selectedDifficulty=difficulty, difficulty=difficulty==0?.75f:difficulty==2?1.4f:1 };LastWon=BlockComplete=false;Active=true;Save();}
        public static bool Resume()
        {
            try { var saved=JsonUtility.FromJson<CampaignSave>(PlayerPrefs.GetString(SaveKey,""));
                if(saved==null||saved.level<1||saved.cleared<0||saved.cleared>saved.level||float.IsNaN(saved.difficulty)||saved.difficulty<.75f||saved.difficulty>2.5f)return false;
                Data=saved;Active=true;return true;
            }catch(ArgumentException){return false;}
        }
        public static float Adapt(float current,float seconds,int damage,int failures)
        {
            float performance=Mathf.Clamp((75-seconds/20)/150f,-.12f,.12f)+Mathf.Clamp((30-damage)/200f,-.1f,.1f)-Mathf.Min(.2f,failures*.025f);
            return Mathf.Clamp(current+Mathf.Clamp(.06f+performance,-.12f,.12f),.75f,2.5f);
        }
        public static void Complete(bool won,float seconds,int damage,int score)
        {
            if(!Active)return;LastWon=won;BlockComplete=false;
            if(won){Data.cleared=Data.level;Data.totalScore+=score;Data.blockWins++;Data.blockSeconds+=seconds;Data.blockDamage+=damage;
                if(Data.level%20==0){BlockComplete=true;Data.blocks++;PreviousDifficulty=Data.difficulty;Data.difficulty=Adapt(Data.difficulty,Data.blockSeconds,Data.blockDamage,Data.failures);Data.blockWins=Data.blockDamage=Data.failures=0;Data.blockSeconds=0;}}
            else Data.failures++;
            Save();
        }
        public static void Advance(){if(LastWon)Data.level++;Save();}
        public static void EnableEndless(){Data.endless=true;Data.endingSeen=true;Data.level=Mathf.Max(31,Data.cleared+1);Save();}
        public static void Save(){PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(Data));PlayerPrefs.Save();}
        public static void Leave(){Active=false;Time.timeScale=1;}
    }
}
