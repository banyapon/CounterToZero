using UnityEngine;

namespace CountToZero
{
    // The purple perimeter pillars share the falling blocks' electric palette.
    [RequireComponent(typeof(Renderer))]
    public sealed class ElectricBlockPulse : MonoBehaviour
    {
        Renderer visual;
        MaterialPropertyBlock properties;
        float age;
        void Awake(){visual=GetComponent<Renderer>();properties=new MaterialPropertyBlock();}
        void Update()
        {
            if(GameManager.Instance==null||GameManager.Instance.State!=GameState.Playing)return;
            age+=Time.deltaTime;
            var color=Color.HSVToRGB(Mathf.Repeat(age*.08f+transform.position.z*.06f,1),.65f,.65f+.35f*Mathf.Abs(Mathf.Sin(age*2)));
            visual.GetPropertyBlock(properties);properties.SetColor("_BaseColor",color);properties.SetColor("_Color",color);visual.SetPropertyBlock(properties);
        }
    }
}
