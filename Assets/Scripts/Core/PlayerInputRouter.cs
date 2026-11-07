using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CountToZero
{
    public enum ControlAction { Up, Down, Left, Right, Fire, Dash, Focus, Pause }
    [Serializable] public class KeyBindings
    {
        public Key[] keys = { Key.W, Key.S, Key.A, Key.D, Key.J, Key.Space, Key.E, Key.Escape };
    }
    [DefaultExecutionOrder(-200)]
    public sealed class PlayerInputRouter : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern int CTZ_IsMobileBrowser();
#endif
        public const string SaveKey = "CountToZero.Controls.v1";
        [SerializeField] Joystick joystick;
        [SerializeField] GameObject touchRoot;
        [SerializeField] TouchHoldButton fireButton;
        [SerializeField] bool showTouchInEditor;
        [SerializeField] KeyBindings defaults = new KeyBindings();
        KeyBindings bindings;
        int listening = -1, listenFrame;
        float listenUntil;
        public bool OptionsOpen { get; set; }
        public bool PersistBindings { get; set; } = true;
        public bool Listening => listening >= 0;
        public string ListenStatus { get; private set; }
        public bool TouchVisible { get; private set; }
        public Key GetBinding(int index) => bindings.keys[index];
        public Vector2 Move { get; private set; }
        public bool FireHeld { get; private set; }
        public bool DashPressed { get; private set; }
        public bool FocusPressed { get; private set; }
        public bool PausePressed { get; private set; }
        public bool MouseAiming { get; private set; }
        public Vector2 StickAim { get; private set; }
        bool touchDash, touchFocus;
        void Awake()
        {
            RestoreDefaults(false);
            try
            {
                var saved = JsonUtility.FromJson<KeyBindings>(PlayerPrefs.GetString(SaveKey,""));
                if (saved != null && saved.keys != null && saved.keys.Length == 8)
                {
                    bool valid = true;
                    foreach (var key in saved.keys) if (key == Key.None || !Enum.IsDefined(typeof(Key),key)) valid=false;
                    if (valid) bindings=saved;
                }
            }
            catch (ArgumentException) { }
            TouchVisible = Application.isMobilePlatform || (Application.platform == RuntimePlatform.WebGLPlayer && SystemInfo.deviceType == DeviceType.Handheld) || showTouchInEditor;
#if UNITY_WEBGL && !UNITY_EDITOR
            TouchVisible |= CTZ_IsMobileBrowser() != 0;
#endif
        }
        public void RestoreDefaults(bool save = true)
        { bindings = new KeyBindings { keys=(Key[])defaults.keys.Clone() }; listening=-1; ListenStatus="Default controls restored."; if(save)Save(); }
        void Save() { if(!PersistBindings)return;PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(bindings));PlayerPrefs.Save(); }
        public void Listen(int index)
        { listening=index;listenFrame=Time.frameCount;listenUntil=Time.unscaledTime+10;ListenStatus="Press a key for "+(ControlAction)index+" (10s)."; }
        public void CancelListen() {listening=-1;ListenStatus="Binding cancelled.";}
        public void Assign(int index,Key key,bool save = true)
        {
            if (key == Key.None) return;
            // Swap duplicate primary bindings so that neither action becomes inaccessible.
            for(int i=0;i<bindings.keys.Length;i++)if(i!=index && bindings.keys[i]==key)bindings.keys[i]=bindings.keys[index];
            bindings.keys[index]=key;listening=-1;ListenStatus=(ControlAction)index+" = "+key;if(save)Save();
        }
        bool Held(ControlAction action) => Keyboard.current != null && Keyboard.current[bindings.keys[(int)action]].isPressed;
        bool Pressed(ControlAction action) => Keyboard.current != null && Keyboard.current[bindings.keys[(int)action]].wasPressedThisFrame;
        public void TouchDash() {touchDash=true;}
        public void TouchFocus() {touchFocus=true;}
        void Update()
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed) TouchVisible=true;
            bool playing=GameManager.Instance.State==GameState.Playing;
            bool showTouch=TouchVisible && playing && !OptionsOpen;
            if(touchRoot.activeSelf && !showTouch)joystick.OnPointerUp(null);
            touchRoot.SetActive(showTouch);
            Move=Vector2.zero;FireHeld=DashPressed=FocusPressed=PausePressed=MouseAiming=false;StickAim=Vector2.zero;
            if(Listening)
            {
                if(Time.unscaledTime>listenUntil)CancelListen();
                else if(Time.frameCount>listenFrame && Keyboard.current!=null)
                    foreach(var key in Keyboard.current.allKeys)if(key.wasPressedThisFrame){Assign(listening,key.keyCode);break;}
                touchDash=touchFocus=false;return;
            }
            var pad=Gamepad.current;var k=Keyboard.current;
            PausePressed=Pressed(ControlAction.Pause)||(pad!=null&&pad.startButton.wasPressedThisFrame);
            if(OptionsOpen || !playing){touchDash=touchFocus=false;return;}
            Move=new Vector2((Held(ControlAction.Right)?1:0)-(Held(ControlAction.Left)?1:0),(Held(ControlAction.Up)?1:0)-(Held(ControlAction.Down)?1:0));
            if(k!=null && !k.leftCtrlKey.isPressed && !k.rightCtrlKey.isPressed)Move+=new Vector2((k.rightArrowKey.isPressed?1:0)-(k.leftArrowKey.isPressed?1:0),(k.upArrowKey.isPressed?1:0)-(k.downArrowKey.isPressed?1:0));
            if(pad!=null)
            {
                var stick=pad.leftStick.ReadValue();if(stick.magnitude>.18f)Move+=stick;Move+=pad.dpad.ReadValue();
                StickAim=pad.rightStick.ReadValue();if(StickAim.magnitude<.2f)StickAim=Vector2.zero;
            }
            if(TouchVisible)Move+=joystick.Direction;
            Move=Vector2.ClampMagnitude(Move,1);
            MouseAiming=Mouse.current!=null && Mouse.current.leftButton.isPressed && !TouchVisible && (EventSystem.current==null || !EventSystem.current.IsPointerOverGameObject());
            FireHeld=Held(ControlAction.Fire)||MouseAiming||fireButton.Held||(pad!=null&&(pad.rightTrigger.isPressed||pad.rightShoulder.isPressed));
            DashPressed=Pressed(ControlAction.Dash)||touchDash||(pad!=null&&pad.buttonSouth.wasPressedThisFrame);
            FocusPressed=Pressed(ControlAction.Focus)||touchFocus||(pad!=null&&pad.buttonWest.wasPressedThisFrame);
            touchDash=touchFocus=false;
        }
        public Vector3 WorldDirection(Vector2 input)
        {
            var view=GameManager.Instance.View.transform;
            var forward=Vector3.ProjectOnPlane(view.forward,Vector3.up).normalized;
            return view.right*input.x+forward*input.y;
        }
    }
}
