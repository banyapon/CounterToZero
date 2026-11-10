using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CountToZero
{
    public sealed class LeaderboardPanel : MonoBehaviour
    {
        static readonly Color Gold = new Color(1f, .78f, .35f);
        static readonly Color Cyan = new Color(.25f, .94f, .95f);

        [SerializeField] TMP_Text youRank, youScore, status, empty;
        [SerializeField] TMP_Text[] rowRank, rowName, rowScore;
        [SerializeField] TMP_InputField callsign;
        [SerializeField] Button refresh;

        void OnEnable()
        {
            if (callsign != null) callsign.SetTextWithoutNotify(CloudRanks.Callsign);
            if (refresh != null) refresh.onClick.AddListener(CommitAndRefresh);
            CloudRanks.Changed += Paint;
            Paint();
            CloudRanks.Refresh();
        }

        void OnDisable()
        {
            CloudRanks.Changed -= Paint;
            if (refresh != null) refresh.onClick.RemoveListener(CommitAndRefresh);
            if (callsign != null) CloudRanks.SetCallsign(callsign.text);
        }

        public void CommitAndRefresh()
        {
            if (callsign != null && CloudRanks.Sanitize(callsign.text).Length < 3)
            {
                CloudRanks.SetCallsign(callsign.text);
                Paint();
                return;
            }
            if (callsign != null) CloudRanks.SetCallsign(callsign.text);
            CloudRanks.Refresh();
        }

        void Paint()
        {
            int best = PlayerPrefs.GetInt(GameManager.BestScoreKey, 0);
            int shownScore = CloudRanks.PlayerScore > 0 ? CloudRanks.PlayerScore : best;
            if (youRank != null) youRank.text = CloudRanks.PlayerRank > 0 ? "#" + CloudRanks.PlayerRank.ToString("00") : "#--";
            if (youScore != null) youScore.text = "BEST  " + shownScore.ToString("N0");
            var rows = CloudRanks.Rows;
            int count = rows == null ? 0 : rows.Length;
            int slots = rowRank == null ? 0 : rowRank.Length;
            for (int i = 0; i < slots; i++)
            {
                bool on = i < count && rowName != null && i < rowName.Length && rowScore != null && i < rowScore.Length;
                var row = on ? rows[i] : default;
                bool mine = on && IsMine(row.Name);
                var rowObject = rowRank[i] != null ? rowRank[i].transform.parent.gameObject : null;
                if (rowObject != null) rowObject.SetActive(on);
                if (!on) continue;
                rowRank[i].text = "#" + row.Rank.ToString("00");
                rowRank[i].color = mine ? Gold : Cyan;
                rowName[i].text = row.Name;
                rowName[i].color = mine ? Gold : Color.white;
                rowScore[i].text = row.Score.ToString("N0");
                rowScore[i].color = Gold;
            }
            if (empty != null)
            {
                empty.gameObject.SetActive(count == 0);
                if (count == 0) empty.text = string.IsNullOrEmpty(CloudRanks.Status) ? "NO SCORES YET" : CloudRanks.Status;
            }
            if (status != null)
            {
                var line = CloudRanks.Status ?? "";
                int split = line.IndexOf('\n');
                status.text = split >= 0 ? line.Substring(0, split) : line;
            }
        }

        static bool IsMine(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            var callsign = CloudRanks.Callsign;
            return name == callsign || name.StartsWith(callsign + "#", StringComparison.Ordinal);
        }
    }
}
