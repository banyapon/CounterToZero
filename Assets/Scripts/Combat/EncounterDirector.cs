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
            Vector3 target=GameManager.Instance.Boss.transform.position;float nearest=6;
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
            if(!enemy)foreach(var minion in minions)if(minion.Active && BulletPattern.SegmentDistance(minion.transform.position,a,b)<.6f){minion.Hit(damage);return true;}
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
            var g=GameManager.Instance;if(g.State!=GameState.Playing)return;
            if(g.Elapsed>=healAt){healAt=g.Elapsed+18;DropHealth(new Vector3(Random.Range(-7f,7f),0,Random.Range(-5f,2f)),true);}
            if(g.Elapsed<eventAt)return;
            eventAt=g.Elapsed+(g.Phase==1?10:8);eventIndex++;
            if(eventIndex%3==1){SummonMinion();if(g.Phase>1)SummonMinion();g.Dialogue.Say("minions");}
            else if(eventIndex%3==2)
            {
                var p=g.Player.transform.position+Vector3.forward*2.5f;
                SummonObstacle(p+Vector3.left*2.2f,false);SummonObstacle(p+Vector3.right*2.2f,false);
                g.Dialogue.Say("walls");
            }
            else
            {
                var p=g.Player.transform.position;SummonObstacle(p,true);
                if(g.Phase>1){SummonObstacle(p+new Vector3(3,0,1),true);SummonObstacle(p+new Vector3(-3,0,-1),true);}
                g.Dialogue.Say("rain");
            }
        }
    }
}
