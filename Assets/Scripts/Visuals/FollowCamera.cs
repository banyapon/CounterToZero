using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
namespace CountToZero
{
    [DefaultExecutionOrder(50)]
    public sealed class FollowCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector3 offset=new Vector3(0,21,-18);
        [SerializeField,Range(0,1)] float followAmount=.28f;
        [SerializeField] float smoothTime=.2f, size=9.6f;
        [SerializeField] float minZoom=6, maxZoom=22, zoomSpeed=5, introDuration=6;
        [SerializeField] float nearBossDistance=4, farBossDistance=10, closeZoomRatio=.85f;
        float proximity=1, proximityVelocity;
        Vector3 velocity,focus; Camera view; float zoom=1,introTime; bool touring;
        public bool Touring=>touring;
        void Awake(){view=GetComponent<Camera>();view.orthographic=true;Snap();}
        public void Snap(){touring=false;proximity=1;proximityVelocity=0;focus=target.position*followAmount;velocity=Vector3.zero;Apply();}
        public void StartIntro(){introTime=0;touring=true;}
        void LateUpdate()
        {
            var g=GameManager.Instance;
            if(touring){
                introTime+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(introTime/introDuration);
                Vector3 endFocus=target.position*followAmount;endFocus.y=0;
                if(t<.4f){float a=t/.4f*Mathf.PI*2;transform.position=new Vector3(Mathf.Sin(a)*22,18,-Mathf.Cos(a)*22);transform.LookAt(Vector3.zero);view.orthographicSize=16;}
                else if(t<.75f){float u=Mathf.SmoothStep(0,1,(t-.4f)/.35f);float a=(t-.4f)/.35f*Mathf.PI*2;Vector3 center=Vector3.Lerp(Vector3.zero,target.position,u);float radius=Mathf.Lerp(22,10,u);transform.position=center+new Vector3(Mathf.Sin(a)*radius,Mathf.Lerp(18,10,u),-Mathf.Cos(a)*radius);transform.LookAt(center);view.orthographicSize=Mathf.Lerp(16,8,u);}
                else {float u=Mathf.SmoothStep(0,1,(t-.75f)/.25f);Vector3 start=target.position+new Vector3(0,10,-10);transform.position=Vector3.Lerp(start,endFocus+offset,u);transform.rotation=Quaternion.Slerp(Quaternion.LookRotation(new Vector3(0,-10,10)),Quaternion.LookRotation(-offset),u);view.orthographicSize=Mathf.Lerp(8,DefaultSize(),u);}
                if(t>=1){Snap();g.CompleteIntro();}return;
            }
            if(g.State==GameState.Playing){
                float distance=Vector3.Distance(target.position,g.Boss.transform.position);
                float near=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(nearBossDistance,farBossDistance,distance));
                proximity=Mathf.SmoothDamp(proximity,Mathf.Lerp(1,closeZoomRatio,near),ref proximityVelocity,.5f);
                Vector3 framing=Vector3.Lerp(target.position*followAmount,(target.position+g.Boss.transform.position)*.5f,near*.7f);
                focus=Vector3.SmoothDamp(focus,framing,ref velocity,smoothTime,Mathf.Infinity,Time.deltaTime);
                var k=Keyboard.current;float delta=0;
                if(k!=null&&(k.leftCtrlKey.isPressed||k.rightCtrlKey.isPressed))delta=((k.upArrowKey.isPressed?1:0)-(k.downArrowKey.isPressed?1:0))*zoomSpeed*Time.unscaledDeltaTime;
                if(Mouse.current!=null && !(EventSystem.current!=null&&EventSystem.current.IsPointerOverGameObject()))delta+=Mathf.Clamp(Mouse.current.scroll.ReadValue().y,-120,120)/120f;
                zoom=Mathf.Clamp(zoom-delta/size,minZoom/size,maxZoom/size);
            }Apply();
        }
        float DefaultSize()=>Mathf.Clamp(Mathf.Max(size,14f/view.aspect)*zoom*proximity,minZoom,maxZoom);
        void Apply(){
            // Keep the complete arena inside the HUD-safe region, even at proximity zoom.
            float verticalProjection=offset.y/offset.magnitude;
            float fit=Mathf.Max(11.3f/(view.aspect*.92f),8.3f*verticalProjection/.78f);
            float framedSize=Mathf.Max(DefaultSize(),fit);
            focus.x=Mathf.Clamp(focus.x,-Mathf.Max(0,framedSize*view.aspect*.92f-11.3f),Mathf.Max(0,framedSize*view.aspect*.92f-11.3f));
            float zRoom=Mathf.Max(0,framedSize*.78f/verticalProjection-8.3f);
            focus.z=Mathf.Clamp(focus.z,-zRoom,zRoom);
            focus.y=0;transform.position=focus+offset;transform.rotation=Quaternion.LookRotation(-offset,Vector3.up);if(GameManager.Instance!=null)transform.position+=GameManager.Instance.Feedback.ShakeOffset;view.orthographicSize=framedSize;}
    }
}
