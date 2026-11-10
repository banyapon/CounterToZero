using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace CountToZero
{
    public sealed class OrbSpawner : MonoBehaviour
    {
        public readonly List<NumberOrb> Active = new List<NumberOrb>(48);
        [SerializeField] NumberOrb orbPrefab;
        [SerializeField] Transform poolRoot;
        [SerializeField] OrbSpawnPoint[] openingOrbs;
        [SerializeField] Transform[] edgeSpawnPoints;
        ObjectPool<NumberOrb> pool;
        float nextSpawn;
        int count;
        void Awake()
        {
            pool = new ObjectPool<NumberOrb>(Create, null, orb => orb.gameObject.SetActive(false), null, true, 48, 48);
            var warm = new NumberOrb[48]; for (int i=0;i<warm.Length;i++) warm[i]=pool.Get();
            foreach (var orb in warm) pool.Release(orb);
        }
        NumberOrb Create()
        {
            var orb = Instantiate(orbPrefab, poolRoot); orb.Initialize(this); orb.gameObject.SetActive(false); return orb;
        }
        public void Release(NumberOrb orb) { if (Active.Remove(orb)) pool.Release(orb); }
        public void Clear() { for (int i=Active.Count-1;i>=0;i--) Release(Active[i]); count=0; nextSpawn=0; }
        void Spawn(Vector3 point, MathOpType op, int value, int reward=0)
        {
            if (Active.Count>=40) return;
            var orb=pool.Get(); orb.Spawn(point,op,value,reward); Active.Add(orb);
        }
        public void SpawnTraining(Vector3 point,MathOpType op,int value){Spawn(point,op,value);Active[Active.Count-1].Training=true;}
        public void SeedOpening()
        {
            foreach (var point in openingOrbs) if(Unlocks.AdvancedOrbs||point.Operation==MathOpType.Add||point.Operation==MathOpType.Subtract) Spawn(point.transform.position, point.Operation, point.Operand);
        }
        void Update()
        {
            var g=GameManager.Instance;
            if (g.State!=GameState.Playing || g.Training || Time.time<nextSpawn || Active.Count>=28) return;
            nextSpawn=Time.time+.85f;
            RandomOperation(out var op,out int value);
            count++;
            if (count%5==0) g.Rules.HelpfulOperation(g.Player.CurrentValue,g.Rule,out op,out value);
            Vector3 position;
            if (op==MathOpType.Modulo || op==MathOpType.Set) position=new Vector3(Random.Range(-8f,8f),0,Random.Range(-5f,5f));
            else position=edgeSpawnPoints[Random.Range(0,edgeSpawnPoints.Length)].position;
            if (count%5==0) position=g.Player.transform.position+new Vector3(g.Player.transform.position.x>0?-2:2,0,1);
            position.x=Mathf.Clamp(position.x,-9.5f,9.5f); position.z=Mathf.Clamp(position.z,-6.5f,6.5f);
            Spawn(position,op,value);
        }
        void RandomOperation(out MathOpType op,out int value)
        {
            if (Mathf.Abs(GameManager.Instance.Player.CurrentValue)>50 && Random.value<.45f)
            { op=Random.value<.5f?MathOpType.Modulo:MathOpType.Set; value=op==MathOpType.Modulo?10:1; return; }
            int roll=Random.Range(0,Unlocks.AdvancedOrbs?12:8);
            if(roll>=10&&!Unlocks.Jinx)roll=Random.Range(0,8);
            op=roll<4?MathOpType.Add:roll<8?MathOpType.Subtract:roll==8?MathOpType.Multiply:roll==9?MathOpType.Divide:MathOpType.Jinx;
            int[] numbers={1,2,3,5}; value=roll<8?numbers[Random.Range(0,4)]:2;
        }
        public void Reroll()
        {
            foreach (var orb in Active)
            {
                if (orb.Reward>0) continue;
                RandomOperation(out var op,out int value); orb.SetOperation(op,value);
            }
        }
        public void DropRewards(Vector3 position)
        {
            for (int i=0;i<6;i++)
            {
                float angle=i*Mathf.PI/3; var p=position+new Vector3(Mathf.Cos(angle)*1.6f,0,Mathf.Sin(angle)*1.6f);
                p.x=Mathf.Clamp(p.x,-9.5f,9.5f); p.z=Mathf.Clamp(p.z,-6.5f,6.5f);
                Spawn(p,MathOpType.Set,1,i<2?1:2);
            }
        }
    }
}
