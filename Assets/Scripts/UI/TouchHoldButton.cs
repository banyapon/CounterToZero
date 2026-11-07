using UnityEngine;
using UnityEngine.EventSystems;

namespace CountToZero
{
    public sealed class TouchHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public bool Held {get;private set;}
        public void OnPointerDown(PointerEventData data) {Held=true;}
        public void OnPointerUp(PointerEventData data) {Held=false;}
        void OnDisable(){Held=false;}
    }
}
