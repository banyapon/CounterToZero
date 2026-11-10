using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Exceptions;
using UnityEngine;

namespace CountToZero
{
    // Anonymous Unity Authentication plus the count_to_zero_best leaderboard.
    // The .lb file must be deployed once from Services > Deployment.
    public static class CloudRanks
    {
        public const string LeaderboardId = "count_to_zero_best";
        public const string CallsignKey = "CountToZero.Callsign";
        const string SentNameKey = "CountToZero.CallsignSent";
        static int generation, flight, pendingScore;
        static bool refresh;
        public readonly struct Row
        {
            public readonly int Rank;
            public readonly string Name;
            public readonly int Score;
            public Row(int rank, string name, int score) { Rank = rank; Name = name; Score = score; }
        }

        public static bool Busy { get; private set; }
        public static string Status { get; private set; } = "CLOUD RANK OFFLINE";
        public static string BoardText { get; private set; } = "";
        public static Row[] Rows { get; private set; } = Array.Empty<Row>();
        public static int PlayerRank { get; private set; }
        public static int PlayerScore { get; private set; }
        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            generation++;
            flight = 0;
            pendingScore = 0;
            refresh = false;
            Busy = false;
            Status = "CLOUD RANK OFFLINE";
            BoardText = "";
            Rows = Array.Empty<Row>();
            PlayerRank = 0;
            PlayerScore = 0;
            Changed = null;
        }

        public static string Callsign
        {
            get
            {
                var saved = PlayerPrefs.GetString(CallsignKey, "");
                if (saved.Length >= 3) return saved;
                saved = "PILOT" + (Mathf.Abs(Environment.TickCount) % 9000 + 1000);
                PlayerPrefs.SetString(CallsignKey, saved);
                PlayerPrefs.Save();
                return saved;
            }
        }

        public static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            var chars = new char[Mathf.Min(16, value.Length)];
            int count = 0;
            foreach (var letter in value.ToUpperInvariant())
            {
                if (count >= chars.Length) break;
                if ((letter >= 'A' && letter <= 'Z') || (letter >= '0' && letter <= '9')) chars[count++] = letter;
            }
            return new string(chars, 0, count);
        }

        public static bool SetCallsign(string value)
        {
            var clean = Sanitize(value);
            if (clean.Length < 3) { Status = "CALLSIGN NEEDS 3 LETTERS"; Raise(); return false; }
            if (clean == PlayerPrefs.GetString(CallsignKey, "")) return false;
            PlayerPrefs.SetString(CallsignKey, clean);
            PlayerPrefs.DeleteKey(SentNameKey);
            PlayerPrefs.Save();
            return true;
        }

        public static void Submit(int score)
        {
            if (score <= 0) return;
            pendingScore = Mathf.Max(pendingScore, score);
            Kick();
        }

        public static void Refresh()
        {
            refresh = true;
            if (!Busy)
            {
                Status = "CONTACTING UNITY CLOUD";
                Raise();
            }
            Kick();
        }

        static void Kick()
        {
            if (Interlocked.CompareExchange(ref flight, 1, 0) != 0) return;
            _ = Pump(generation);
        }

        static async Task Pump(int generationAtStart)
        {
            var token = Application.exitCancellationToken;
            try
            {
                do
                {
                    int score = pendingScore;
                    bool load = refresh;
                    pendingScore = 0;
                    refresh = false;
                    Busy = true;
                    Status = "CONTACTING UNITY CLOUD";
                    Raise();
                    await EnsureSignedIn(token);
                    if (generationAtStart != generation || token.IsCancellationRequested) return;
                    await SyncName();
                    int best = Mathf.Max(score, load ? PlayerPrefs.GetInt(GameManager.BestScoreKey, 0) : 0);
                    if (best > 0)
                    {
                        var entry = await LeaderboardsService.Instance.AddPlayerScoreAsync(LeaderboardId, best);
                        PlayerRank = ShownRank(entry.Rank);
                        PlayerScore = Mathf.RoundToInt((float)entry.Score);
                        Status = "SCORE SENT  /  #" + PlayerRank.ToString("00");
                    }
                    if (load) await LoadBoard();
                    Busy = false;
                    Raise();
                }
                while (generationAtStart == generation && !token.IsCancellationRequested && (pendingScore > 0 || refresh));
            }
            catch (OperationCanceledException) { }
            catch (LeaderboardsException exception)
            {
                Fail(Explain(exception));
                Debug.LogWarning("Leaderboard: " + exception.Reason + " " + exception.Message);
            }
            catch (RequestFailedException exception)
            {
                Fail(Explain(exception));
                Debug.LogWarning("Leaderboard: " + exception.ErrorCode + " " + exception.Message);
            }
            catch (Exception exception)
            {
                Fail("CLOUD UNREACHABLE");
                Debug.LogWarning("Leaderboard: " + exception.Message);
            }
            finally
            {
                Busy = false;
                Interlocked.Exchange(ref flight, 0);
                Raise();
                if (generationAtStart == generation && (pendingScore > 0 || refresh)) Kick();
            }
        }

        static async Task EnsureSignedIn(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
                await UnityServices.InitializeAsync();
            token.ThrowIfCancellationRequested();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        static async Task SyncName()
        {
            var name = Callsign;
            if (PlayerPrefs.GetString(SentNameKey, "") == name && !string.IsNullOrEmpty(AuthenticationService.Instance.PlayerName)) return;
            try
            {
                await AuthenticationService.Instance.UpdatePlayerNameAsync(name);
                PlayerPrefs.SetString(SentNameKey, name);
                PlayerPrefs.Save();
            }
            catch (RequestFailedException exception)
            {
                Debug.LogWarning("Callsign: " + exception.Message);
                Status = "CALLSIGN REJECTED";
            }
        }

        static async Task LoadBoard()
        {
            var page = await LeaderboardsService.Instance.GetScoresAsync(LeaderboardId, new GetScoresOptions { Limit = 10 });
            BoardText = Format(page.Results);
            try
            {
                var mine = await LeaderboardsService.Instance.GetPlayerScoreAsync(LeaderboardId);
                PlayerRank = ShownRank(mine.Rank);
                PlayerScore = Mathf.RoundToInt((float)mine.Score);
                Status = "CLOUD RANK  #" + PlayerRank.ToString("00");
            }
            catch (LeaderboardsException exception) when (exception.Reason == LeaderboardsExceptionReason.EntryNotFound)
            {
                PlayerRank = 0;
                Status = "NOT RANKED YET";
            }
        }

        // Service ranks are zero-based. The board shows the first place as #01.
        public static int ShownRank(int rank) => rank + 1;

        static string Format(System.Collections.Generic.List<Unity.Services.Leaderboards.Models.LeaderboardEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                Rows = Array.Empty<Row>();
                return "NO SCORES YET";
            }
            var built = new Row[entries.Count];
            var lines = new StringBuilder();
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var name = string.IsNullOrEmpty(entry.PlayerName) ? "UNKNOWN" : entry.PlayerName;
                int score = Mathf.RoundToInt((float)entry.Score);
                int rank = ShownRank(entry.Rank);
                built[i] = new Row(rank, name, score);
                if (name.Length > 22) name = name.Substring(0, 22);
                lines.Append('#').Append(rank.ToString("00")).Append("  ").Append(name).Append("   ").Append(score.ToString("N0")).Append('\n');
            }
            Rows = built;
            return lines.ToString().TrimEnd();
        }

        static void Fail(string status)
        {
            Status = status;
            BoardText = "";
            Rows = Array.Empty<Row>();
        }

        static string Explain(LeaderboardsException exception)
        {
            switch (exception.Reason)
            {
                case LeaderboardsExceptionReason.LeaderboardNotFound: return "BOARD IS NOT ON UNITY CLOUD\nDEPLOY count_to_zero_best\nSERVICES  >  DEPLOYMENT";
                case LeaderboardsExceptionReason.EntryNotFound: return "NOT RANKED YET";
                case LeaderboardsExceptionReason.Unauthorized: return "SIGN IN WITH UNITY HUB";
                case LeaderboardsExceptionReason.NoInternetConnection: return "CLOUD UNREACHABLE";
                default: return Explain((RequestFailedException)exception);
            }
        }

        static string Explain(RequestFailedException exception)
        {
            var message = exception.Message ?? "";
            if (message.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0 || message.IndexOf("does not exist", StringComparison.OrdinalIgnoreCase) >= 0)
                return "BOARD IS NOT ON UNITY CLOUD\nDEPLOY count_to_zero_best\nSERVICES  >  DEPLOYMENT";
            if (message.IndexOf("unauth", StringComparison.OrdinalIgnoreCase) >= 0 || message.IndexOf("permission", StringComparison.OrdinalIgnoreCase) >= 0)
                return "SIGN IN WITH UNITY HUB";
            return "CLOUD UNREACHABLE";
        }

        static void Raise() { Changed?.Invoke(); }
    }
}
