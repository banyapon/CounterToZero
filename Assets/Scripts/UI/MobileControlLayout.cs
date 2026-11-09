using UnityEngine;
namespace CountToZero
{
    // Keep the touch surface inside notches and system gesture insets.
    [RequireComponent(typeof(RectTransform))]
    public sealed class MobileControlLayout : MonoBehaviour
    {
        Rect lastSafe; Vector2 lastScreen;
        void OnEnable(){Apply();}
        void Update(){if(lastSafe!=Screen.safeArea || lastScreen!=new Vector2(Screen.width,Screen.height))Apply();}
        void Apply(){if(Screen.width<=0||Screen.height<=0)return;lastSafe=Screen.safeArea;lastScreen=new Vector2(Screen.width,Screen.height);var r=(RectTransform)transform;r.anchorMin=lastSafe.min/lastScreen;r.anchorMax=lastSafe.max/lastScreen;r.offsetMin=r.offsetMax=Vector2.zero;}
    }
}
