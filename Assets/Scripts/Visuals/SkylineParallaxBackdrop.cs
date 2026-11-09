using System.Collections.Generic;
using UnityEngine;

namespace CountToZero
{
    [ExecuteAlways, DefaultExecutionOrder(300), RequireComponent(typeof(Camera))]
    public sealed class SkylineParallaxBackdrop : MonoBehaviour
    {
        [SerializeField] Texture2D sky;
        [SerializeField] Texture2D[] buildings;
        [SerializeField] Material backdropMaterial;
        [SerializeField, Range(.05f,.3f)] float skyFraction=.1f;
        [SerializeField, Range(6,20)] int cityRows=12;
        sealed class Layer
        {
            public Transform transform;
            public MeshRenderer renderer;
            public MaterialPropertyBlock properties=new MaterialPropertyBlock();
            public Texture texture;
            public float bottom,height,phase,depth,speed,parallax;
            public Color tint;
            public bool sky;
        }
        readonly List<Layer> layers=new List<Layer>();
        Camera view;Transform generated;Mesh quad;
        bool rebuild=true;
        float lastYaw,continuousYaw;
        static readonly int MainTex=Shader.PropertyToID("_MainTex"),TexST=Shader.PropertyToID("_MainTex_ST"),Tint=Shader.PropertyToID("_Color");

        void OnEnable(){view=GetComponent<Camera>();lastYaw=transform.eulerAngles.y;continuousYaw=0;rebuild=true;}
        void OnValidate(){rebuild=true;}
        void OnDisable()=>Clear();
        void Release(Object value){if(value==null)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
        void Clear(){if(generated!=null)Release(generated.gameObject);if(quad!=null)Release(quad);generated=null;quad=null;layers.Clear();}
        void Build()
        {
            Clear();
            if(backdropMaterial==null||sky==null||buildings==null||buildings.Length==0)return;
            generated=new GameObject("Skyline Parallax"){hideFlags=HideFlags.DontSave}.transform;
            generated.SetParent(transform,false);
            quad=new Mesh{name="Skyline Billboard Quad",hideFlags=HideFlags.DontSave};
            quad.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)};
            quad.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};quad.triangles=new[]{0,2,1,0,3,2};quad.RecalculateBounds();
            Add("Distant City Haze",Texture2D.whiteTexture,0,1,0,0,new Color(.065f,.09f,.15f),false);
            Add("Sky / Top 10 Percent",sky,1-skyFraction,skyFraction,.0015f,.18f,new Color(.85f,.9f,1),true);
            for(int i=0;i<cityRows;i++)
            {
                float t=i/(float)(cityRows-1),height=.22f+t*.12f;
                float top=1-skyFraction-t*.78f;
                Add("City Billboard Row "+(i+1),buildings[i%buildings.Length],top-height,height,.002f+t*.012f,.35f+t*1.5f,Color.Lerp(Color.white,new Color(.55f,.68f,.85f),t),false);
            }
            rebuild=false;
        }
        void Add(string name,Texture texture,float bottom,float height,float speed,float parallax,Color tint,bool isSky)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)){hideFlags=HideFlags.DontSave};go.transform.SetParent(generated,false);
            go.GetComponent<MeshFilter>().sharedMesh=quad;
            var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=backdropMaterial;renderer.sortingOrder=-500+layers.Count;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            layers.Add(new Layer{transform=go.transform,renderer=renderer,texture=texture,bottom=bottom,height=height,speed=speed,parallax=parallax,tint=tint,sky=isSky,phase=layers.Count*.173f,depth=.96f-layers.Count*.003f});
        }
        public void RefreshBackdrop()=>LateUpdate();
        void LateUpdate()
        {
            if(view==null)view=GetComponent<Camera>();
            if(rebuild||generated==null)Build();
            if(generated==null)return;
            float yaw=transform.eulerAngles.y;continuousYaw+=Mathf.DeltaAngle(lastYaw,yaw);lastYaw=yaw;
            float height=view.orthographicSize*2,width=height*view.aspect;
            bool animate=Application.isPlaying&&(GameManager.Instance==null||GameManager.Instance.State!=GameState.Paused);
            foreach(var layer in layers)
            {
                if(animate)layer.phase=Mathf.Repeat(layer.phase+Time.unscaledDeltaTime*layer.speed,1);
                // Camera-local quads are true billboards, even through a full intro orbit.
                layer.transform.localPosition=new Vector3(0,(layer.bottom+layer.height*.5f-.5f)*height,view.farClipPlane*layer.depth);
                layer.transform.localRotation=Quaternion.identity;layer.transform.localScale=new Vector3(width,height*layer.height,1);
                float sampleHeight=layer.sky?.22f:1;
                float repeats=layer.texture==Texture2D.whiteTexture?1:width*layer.texture.height/(height*layer.height*layer.texture.width)*sampleHeight;
                float movement=Vector3.Dot(transform.position,transform.right)*.008f*layer.parallax;
                float offset=Mathf.Repeat(layer.phase+continuousYaw/360*layer.parallax+movement,1);
                layer.properties.SetTexture(MainTex,layer.texture);
                layer.properties.SetVector(TexST,new Vector4(repeats,sampleHeight,offset,layer.sky?.48f:0));
                layer.properties.SetColor(Tint,layer.tint);layer.renderer.SetPropertyBlock(layer.properties);
            }
        }
    }
}
