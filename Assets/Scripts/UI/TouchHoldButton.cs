using UnityEngine;
using UnityEngine.EventSystems;

namespace CountToZero
{
    public sealed class TouchHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        int pointerId=int.MinValue;
        public bool Held {get;private set;}
        public void OnPointerDown(PointerEventData data) {if(Held)return;pointerId=data!=null?data.pointerId:int.MinValue;Held=true;}
        public void OnPointerUp(PointerEventData data) {if(data!=null && data.pointerId!=pointerId)return;Held=false;pointerId=int.MinValue;}
        void OnDisable(){Held=false;pointerId=int.MinValue;}
    }
}
