using UnityEngine;
namespace CountToZero
{
    public sealed class RobotAppearance : MonoBehaviour
    {
        [SerializeField] HeroAnimator animator;
        [SerializeField] Sprite[] fronts, backs;
        public int Variant {get;private set;}
        public void Choose()
        {
            int count=Mathf.Min(fronts.Length,backs.Length)/4;if(count==0)return;
            Variant=Random.Range(0,count);
            var front=new Sprite[4];var back=new Sprite[4];
            System.Array.Copy(fronts,Variant*4,front,0,4);System.Array.Copy(backs,Variant*4,back,0,4);
            animator.SetFrames(front,back);
        }
    }
}
