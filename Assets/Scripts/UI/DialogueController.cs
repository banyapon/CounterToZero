using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace CountToZero
{
    [Serializable] public class DialogueLine {public string id,speaker,text;public float duration=3;}
    [Serializable] public class DialogueData {public DialogueLine[] lines;}
    public sealed class DialogueController : MonoBehaviour
    {
        [SerializeField] TextAsset englishJson;
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text speaker,body;
        DialogueData data;float remaining;
        public string CurrentId {get;private set;}
        void Awake(){data=JsonUtility.FromJson<DialogueData>(englishJson.text);panel.SetActive(false);}
        public void Say(string id)
        {
            foreach(var line in data.lines)if(line.id==id){CurrentId=id;speaker.text=line.speaker;body.text=line.text;remaining=line.duration;panel.SetActive(true);return;}
        }
        public void Clear(){remaining=0;panel.SetActive(false);}
        void Update()
        {
            if(GameManager.Instance.State!=GameState.Playing||GameManager.Instance.Training){panel.SetActive(false);return;}
            remaining-=Time.deltaTime;panel.SetActive(remaining>0);
        }
    }
}
