using UnityEngine;

namespace CountToZero
{
    public sealed class EncounterDirector : MonoBehaviour
    {
        [SerializeField] ArenaObstacle[] obstacles;
        [SerializeField] MinionController[] minions;
        [SerializeField] HealthPickup[] healthPickups;
        [SerializeField] Transform[] minionSpawns;
        float eventAt,healAt;
        int eventIndex,killCount;
        public int DifficultyTier => Mathf.Clamp(((Campaign.Active?Campaign.Data.level:1)-1)/4,0,9);
        public int BlockWaveSize => Mathf.Min(8,2+DifficultyTier+(GameManager.Instance.Phase-1));
        public float EventInterval => Mathf.Max(3.5f,(10-DifficultyTier*.6f-(GameManager.Instance.Phase-1)) / (Campaign.Active?Mathf.Sqrt(Campaign.Data.difficulty):1));
        public void SummonBlockWave(bool falling)
        {
            int goal=Mathf.Min(obstacles.Length,ActiveObstacles+BlockWaveSize);
            bool conveyor=GameManager.Instance.Hazards!=null && GameManager.Instance.Hazards.Scrolling;
            float escapeLane=Random.Range(-7f,7f);
            // Random scattered sites, with retries for occupied cells; preserve the 1.5s warning.
            for(int attempt=0;attempt<80 && ActiveObstacles<goal;attempt++)
            {
                Vector3 point=new Vector3(Random.Range(-8.5f,8.5f),0,GameManager.Instance.Hazards!=null && GameManager.Instance.Hazards.Scrolling ? Random.Range(3.5f,5.5f) : Random.Range(-5.5f,5.5f));
                if(conveyor && Mathf.Abs(point.x-escapeLane)<1.6f)continue;
                if(Vector3.Distance(point,GameManager.Instance.Player.transform.position)<2)continue;
                SummonObstacle(point,falling);
            }
        }
        public int ActiveObstacles {get{int n=0;foreach(var item in obstacles)if(item.Active)n++;return n;}}
        public int ActiveMinions {get{int n=0;foreach(var item in minions)if(item.Active)n++;return n;}}
        public void ResetRun()
        {
            foreach(var item in obstacles)item.Clear();foreach(var item in minions)item.Clear();foreach(var item in healthPickups)item.Clear();
            eventAt=9;healAt=16;eventIndex=killCount=0;
        }
        void Awake(){ResetRun();}
        public bool Blocked(Vector3 point){foreach(var item in obstacles)if(item.Blocks(point))return true;return false;}
        public Vector3 AutoAim(Vector3 origin)
        {
            Vector3 target=BattleExpansion.Instance!=null?BattleExpansion.Instance.BossAim:GameManager.Instance.Boss.transform.position;float nearest=6;
            foreach(var minion in minions)if(minion.Active)
            {float distance=Vector3.Distance(origin,minion.transform.position);if(distance<nearest){nearest=distance;target=minion.transform.position;}}
            return target;
        }
        public Vector3 Move(Vector3 from,Vector3 delta)
        {
            int steps=Mathf.Max(1,Mathf.CeilToInt(delta.magnitude/.15f));Vector3 step=delta/steps;
            for(int i=0;i<steps;i++)
            {
                var x=from+new Vector3(step.x,0,0);if(!Blocked(x))from=x;
                var z=from+new Vector3(0,0,step.z);if(!Blocked(z))from=z;
            }
            from.x=Mathf.Clamp(from.x,-10.3f,10.3f);from.z=Mathf.Clamp(from.z,-7.3f,7.3f);return from;
        }
        public bool Intercept(Vector3 a,Vector3 b,bool enemy,float damage)
        {
            foreach(var item in obstacles)if(item.HitsSegment(a,b))return true;
            if(!enemy)foreach(var minion in minions)if(minion.Active && BulletPattern.SegmentDistance(minion.transform.position,a,b)<.3f){minion.Hit(damage);return true;}
            return false;
        }
        public void SummonObstacle(Vector3 point,bool falling)
        {
            point.x=Mathf.Clamp(point.x,-8.5f,8.5f);point.z=Mathf.Clamp(point.z,-5.5f,5.5f);point.y=0;
            foreach(var item in obstacles)if(item.Active&&Vector3.Distance(item.Position,point)<2)return;
            foreach(var item in obstacles)if(!item.Active){item.Summon(point,falling);return;}
        }
        public void SummonMinion()
        {
            foreach(var minion in minions)if(!minion.Active)
            {var spawn=minionSpawns[Random.Range(0,minionSpawns.Length)].position;if(!Blocked(spawn))minion.Spawn(spawn);return;}
        }
        public void DropHealth(Vector3 point,bool guaranteed=false)
        {
            if(!guaranteed && ++killCount%2!=0)return;
            if(Blocked(point))point=Vector3.zero;
            foreach(var item in healthPickups)if(!item.Active){item.Spawn(point);return;}
        }
        void Update()
        {
            var g=GameManager.Instance;if(g.State!=GameState.Playing||g.Training)return;
            if(g.Elapsed>=healAt){healAt=g.Elapsed+18;DropHealth(new Vector3(Random.Range(-7f,7f),0,Random.Range(-5f,2f)),true);}
            if(!Unlocks.Minions||g.Elapsed<eventAt)return;
            eventAt=g.Elapsed+EventInterval;eventIndex++;GameAudio.Play(SoundCue.Warning);
            if(eventIndex%3==1||!Unlocks.BlockWaves)
            {
                int count=Mathf.Min(6,1+DifficultyTier/2+(g.Phase>1?1:0));
                for(int i=0;i<count;i++)SummonMinion();
                if(DifficultyTier>=3)SummonBlockWave(true);
                g.Dialogue.Say("minions");
            }
            else
            {
                bool falling=true;
                SummonBlockWave(falling);
                g.Dialogue.Say(falling?"rain":"walls");
            }
        }
    }
}
