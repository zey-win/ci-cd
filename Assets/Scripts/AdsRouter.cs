using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;

// 50/50 interstitial по пользователю (стабильно)
// {
//   "version": 1,
//   "networks": { "AdMob": { "enabled": true }, "ZeyWin": { "enabled": true } },
//   "formats": {
//     "interstitial": {
//       "enabled": true,
//       "strategy": "weighted",
//       "stickiness": "user",
//       "weights": [ { "net": "AdMob", "w": 50 }, { "net": "ZeyWin", "w": 50 } ],
//       "fallback": true,
//       "count_on": "impression"
//     }
//   }
// }


// AdMob 1 раз, потом всегда ZeyWin
// {
//   "version": 1,
//   "formats": {
//     "interstitial": {
//       "enabled": true,
//       "strategy": "sequence",
//       "count_on": "shown",
//       "steps": [
//         { "net": "AdMob", "count": 1 },
//         { "net": "ZeyWin", "count": -1 }
//       ],
//       "fallback": true
//     }
//   }
// }



public sealed class AdsRouter
{
    public enum AdsEvent { Shown, Impression, Closed, Reward }

    // -------------------- Public --------------------

    public bool IsLoaded { get; private set; }
    public string ConfigHash { get; private set; }

    public void LoadFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogWarning("[AdsRouter] Empty json, router disabled.");
            IsLoaded = false;
            return;
        }

        try
        {
            _cfg = JsonConvert.DeserializeObject<AdsRoutingConfig>(json);
            if (_cfg == null)
                throw new Exception("Deserialize returned null.");

            ConfigHash = ShortHash(json);
            IsLoaded = true;

            // чтобы не тащить async (Installation ID), используем локальный userId (сохраняется один раз)
            _userId = GetOrCreateLocalUserId();

            Debug.Log($"[AdsRouter] Loaded. version={_cfg.version} hash={ConfigHash}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AdsRouter] Failed to parse json, router disabled. {e}");
            IsLoaded = false;
        }
    }

    /// <summary>
    /// Выбор сети. Если strategy=waterfall и передан isReady, выберет первую готовую.
    /// </summary>
    public AdManager.AdNetwork Choose(AdManager.AdFormat format, Func<AdManager.AdNetwork, bool> isReady = null)
    {
        if (!IsLoaded || _cfg == null)
            return DefaultNetFor(format);

        var fmtKey = FormatKey(format);
        if (!_cfg.formats.TryGetValue(fmtKey, out var fmt) || fmt == null || !fmt.enabled)
            return DefaultNetFor(format);

        var candidates = BuildCandidates(format, fmt);

        // фильтрация по enabled и капам
        candidates = candidates
            .Where(n => IsNetworkEnabled(n))
            .Where(n => PassCooldown(format, n, fmt))
            .Where(n => PassMaxPerSession(format, n, fmt))
            .Distinct()
            .ToList();

        if (candidates.Count == 0)
            return DefaultNetFor(format);

        // Для waterfall (и вообще) удобно если isReady передан — выбрать готовую
        if (isReady != null)
        {
            foreach (var n in candidates)
                if (isReady(n))
                    return n;
        }

        // иначе отдаём первый (а AdManager уже сам сделает fallback если не ready)
        return candidates[0];
    }

    public void Record(AdManager.AdFormat format, AdManager.AdNetwork net, AdsEvent evt)
    {
        if (!IsLoaded || _cfg == null) return;

        var fmtKey = FormatKey(format);
        if (!_cfg.formats.TryGetValue(fmtKey, out var fmt) || fmt == null) return;
        if (!fmt.enabled) return;

        // общие счётчики / капы
        if (evt == AdsEvent.Shown)
        {
            IncSessionCount(format, net);
            SetLastShownUnix(format, net, NowUnix());
        }

        // sequence: двигаем прогресс на событие count_on
        if (string.Equals(fmt.strategy, "sequence", StringComparison.OrdinalIgnoreCase))
        {
            var countOn = NormalizeCountOn(fmt.count_on);
            if (EventMatchesCountOn(evt, countOn))
            {
                AdvanceSequence(format, fmt);
            }
        }
    }

    // -------------------- Internal config models --------------------

    [Serializable]
    private sealed class AdsRoutingConfig
    {
        public int version = 1;
        public Dictionary<string, NetworkCfg> networks = new();
        public Dictionary<string, FormatCfg> formats = new();
    }

    [Serializable]
    private sealed class NetworkCfg
    {
        public bool enabled = true;
    }

    [Serializable]
    private sealed class FormatCfg
    {
        public bool enabled = true;
        public string strategy = "fixed";          // fixed / weighted / sequence / waterfall
        public bool fallback = true;

        // fixed
        public string fixed_net;

        // weighted
        public string stickiness = "user";         // user / session / none
        public List<WeightEntry> weights;

        // sequence
        public string count_on = "impression";     // shown / impression / closed
        public List<SequenceStep> steps;

        // waterfall
        public List<string> order;

        // caps
        public int cooldown_seconds = 0;
        public int max_per_session = 0;
    }

    [Serializable]
    private sealed class WeightEntry
    {
        public string net;
        public int w = 1;
    }

    [Serializable]
    private sealed class SequenceStep
    {
        public string net;
        public int count = -1; // -1 = infinite
    }

    // -------------------- Strategy: candidate list --------------------

    private List<AdManager.AdNetwork> BuildCandidates(AdManager.AdFormat format, FormatCfg fmt)
    {
        var strategy = (fmt.strategy ?? "fixed").Trim().ToLowerInvariant();

        List<AdManager.AdNetwork> result = strategy switch
        {
            "fixed" => CandidatesFixed(fmt),
            "weighted" => CandidatesWeighted(format, fmt),
            "sequence" => CandidatesSequence(format, fmt),
            "waterfall" => CandidatesWaterfall(fmt),
            _ => CandidatesFixed(fmt)
        };

        if (fmt.fallback)
        {
            // Добавим другую сеть в конец, если не присутствует.
            foreach (var n in AllNetworks())
                if (!result.Contains(n))
                    result.Add(n);
        }

        return result;
    }

    private List<AdManager.AdNetwork> CandidatesFixed(FormatCfg fmt)
    {
        var n = ParseNetwork(fmt.fixed_net);
        return new List<AdManager.AdNetwork> { n };
    }

    private List<AdManager.AdNetwork> CandidatesWaterfall(FormatCfg fmt)
    {
        var order = fmt.order ?? new List<string>();
        var list = new List<AdManager.AdNetwork>();

        foreach (var s in order)
            list.Add(ParseNetwork(s));

        if (list.Count == 0)
            list.Add(DefaultNetFor(AdManager.AdFormat.Interstitial));

        return list;
    }

    private List<AdManager.AdNetwork> CandidatesWeighted(AdManager.AdFormat format, FormatCfg fmt)
    {
        var weights = (fmt.weights ?? new List<WeightEntry>())
            .Where(x => x != null && x.w > 0 && !string.IsNullOrWhiteSpace(x.net))
            .ToList();

        if (weights.Count == 0)
            return new List<AdManager.AdNetwork> { DefaultNetFor(format) };

        var choice = PickWeighted(format, fmt, weights);
        return new List<AdManager.AdNetwork> { choice };
    }

    private List<AdManager.AdNetwork> CandidatesSequence(AdManager.AdFormat format, FormatCfg fmt)
    {
        var steps = fmt.steps ?? new List<SequenceStep>();
        if (steps.Count == 0)
            return new List<AdManager.AdNetwork> { DefaultNetFor(format) };

        var state = GetSequenceState(format, fmt);

        // гарантируем валидный индекс
        if (state.stepIndex < 0) state.stepIndex = 0;
        if (state.stepIndex >= steps.Count) state.stepIndex = steps.Count - 1;

        var step = steps[state.stepIndex];
        var net = ParseNetwork(step.net);

        return new List<AdManager.AdNetwork> { net };
    }

    // -------------------- Weighted pick --------------------

    private AdManager.AdNetwork PickWeighted(AdManager.AdFormat format, FormatCfg fmt, List<WeightEntry> weights)
    {
        var stick = (fmt.stickiness ?? "user").Trim().ToLowerInvariant();
        int total = weights.Sum(w => w.w);

        int roll;

        if (stick == "user")
        {
            // стабильное распределение по пользователю
            roll = StableRoll($"{_userId}:{ConfigHash}:{FormatKey(format)}", total);
        }
        else if (stick == "session")
        {
            // стабильно в рамках запуска
            var k = $"ads_w_session_{ConfigHash}_{FormatKey(format)}";
            if (!_sessionStableRoll.TryGetValue(k, out roll))
            {
                roll = UnityEngine.Random.Range(0, total);
                _sessionStableRoll[k] = roll;
            }
        }
        else
        {
            // каждый раз рандом
            roll = UnityEngine.Random.Range(0, total);
        }

        int acc = 0;
        foreach (var e in weights)
        {
            acc += e.w;
            if (roll < acc)
                return ParseNetwork(e.net);
        }

        return ParseNetwork(weights[0].net);
    }

    // -------------------- Sequence state --------------------

    private sealed class SeqState
    {
        public int stepIndex;
        public int shownInStep;
        public string keyBase;
        public int stepsCount;
        public int stepTarget; // count для текущего шага
    }

    private void AdvanceSequence(AdManager.AdFormat format, FormatCfg fmt)
    {
        var steps = fmt.steps ?? new List<SequenceStep>();
        if (steps.Count == 0) return;

        var st = GetSequenceState(format, fmt);

        st.shownInStep++;

        // infinite?
        if (st.stepTarget < 0)
        {
            SaveSeqState(st);
            return;
        }

        if (st.shownInStep >= st.stepTarget)
        {
            st.stepIndex = Mathf.Min(st.stepIndex + 1, steps.Count - 1);
            st.shownInStep = 0;

            // обновим target
            var newTarget = steps[st.stepIndex].count;
            st.stepTarget = newTarget;
        }

        SaveSeqState(st);
    }

    private SeqState GetSequenceState(AdManager.AdFormat format, FormatCfg fmt)
    {
        var steps = fmt.steps ?? new List<SequenceStep>();
        var keyBase = $"ads_seq_{ConfigHash}_{FormatKey(format)}";

        // если количество шагов поменялось — сбросим прогресс (иначе можно “улететь”)
        var savedStepsCount = PlayerPrefs.GetInt(keyBase + "_stepsCount", -1);
        if (savedStepsCount != steps.Count)
        {
            PlayerPrefs.SetInt(keyBase + "_stepsCount", steps.Count);
            PlayerPrefs.SetInt(keyBase + "_step", 0);
            PlayerPrefs.SetInt(keyBase + "_shown", 0);
            PlayerPrefs.Save();
        }

        int stepIndex = PlayerPrefs.GetInt(keyBase + "_step", 0);
        int shown = PlayerPrefs.GetInt(keyBase + "_shown", 0);

        stepIndex = Mathf.Clamp(stepIndex, 0, Math.Max(0, steps.Count - 1));
        var target = steps.Count > 0 ? steps[stepIndex].count : -1;

        return new SeqState
        {
            stepIndex = stepIndex,
            shownInStep = shown,
            keyBase = keyBase,
            stepsCount = steps.Count,
            stepTarget = target
        };
    }

    private void SaveSeqState(SeqState st)
    {
        PlayerPrefs.SetInt(st.keyBase + "_step", st.stepIndex);
        PlayerPrefs.SetInt(st.keyBase + "_shown", st.shownInStep);
        PlayerPrefs.Save();
    }

    // -------------------- Caps --------------------

    private bool PassCooldown(AdManager.AdFormat format, AdManager.AdNetwork net, FormatCfg fmt)
    {
        int cd = fmt.cooldown_seconds;
        if (cd <= 0) return true;

        long last = GetLastShownUnix(format, net);
        long now = NowUnix();
        return (now - last) >= cd;
    }

    private bool PassMaxPerSession(AdManager.AdFormat format, AdManager.AdNetwork net, FormatCfg fmt)
    {
        int cap = fmt.max_per_session;
        if (cap <= 0) return true;

        var k = $"{FormatKey(format)}:{net}";
        _sessionShown.TryGetValue(k, out var cnt);
        return cnt < cap;
    }

    private void IncSessionCount(AdManager.AdFormat format, AdManager.AdNetwork net)
    {
        var k = $"{FormatKey(format)}:{net}";
        _sessionShown.TryGetValue(k, out var cnt);
        _sessionShown[k] = cnt + 1;
    }

    private long GetLastShownUnix(AdManager.AdFormat format, AdManager.AdNetwork net)
    {
        var k = $"ads_last_{FormatKey(format)}_{net}";
        var s = PlayerPrefs.GetString(k, "0");
        return long.TryParse(s, out var v) ? v : 0;
    }

    private void SetLastShownUnix(AdManager.AdFormat format, AdManager.AdNetwork net, long unix)
    {
        var k = $"ads_last_{FormatKey(format)}_{net}";
        PlayerPrefs.SetString(k, unix.ToString());
        PlayerPrefs.Save();
    }

    // -------------------- Helpers --------------------

    private AdsRoutingConfig _cfg;
    private string _userId;

    private readonly Dictionary<string, int> _sessionStableRoll = new();
    private readonly Dictionary<string, int> _sessionShown = new();

    private static IEnumerable<AdManager.AdNetwork> AllNetworks()
    {
        yield return AdManager.AdNetwork.ZeyWin;
        yield return AdManager.AdNetwork.AdMob;
    }

    private bool IsNetworkEnabled(AdManager.AdNetwork n)
    {
        if (_cfg?.networks == null || _cfg.networks.Count == 0)
            return true;

        var key = n == AdManager.AdNetwork.AdMob ? "AdMob" : "ZeyWin";

        if (_cfg.networks.TryGetValue(key, out var nc))
            return nc == null || nc.enabled;

        return true;
    }

    private static AdManager.AdNetwork ParseNetwork(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return AdManager.AdNetwork.ZeyWin;

        var t = s.Trim().ToLowerInvariant();
        return t switch
        {
            "admob" => AdManager.AdNetwork.AdMob,
            "zeywin" => AdManager.AdNetwork.ZeyWin,
            _ => AdManager.AdNetwork.ZeyWin
        };
    }

    private static string FormatKey(AdManager.AdFormat f) =>
        f switch
        {
            AdManager.AdFormat.Banner => "banner",
            AdManager.AdFormat.Interstitial => "interstitial",
            AdManager.AdFormat.Rewarded => "rewarded",
            _ => "unknown"
        };

    private static AdManager.AdNetwork DefaultNetFor(AdManager.AdFormat f) => AdManager.AdNetwork.ZeyWin;

    private static string GetOrCreateLocalUserId()
    {
        const string k = "ads_user_id";
        var v = PlayerPrefs.GetString(k, "");
        if (!string.IsNullOrEmpty(v)) return v;

        v = Guid.NewGuid().ToString("N");
        PlayerPrefs.SetString(k, v);
        PlayerPrefs.Save();
        return v;
    }

    private static int StableRoll(string input, int modulo)
    {
        if (modulo <= 0) return 0;

        using var sha1 = SHA1.Create();
        var bytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
        // берём 4 байта
        int x = (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
        if (x < 0) x = -x;
        return x % modulo;
    }

    private static string ShortHash(string input)
    {
        using var md5 = MD5.Create();
        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(bytes).Replace("-", "").Substring(0, 8);
    }

    private static long NowUnix() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    private static string NormalizeCountOn(string s)
    {
        var t = (s ?? "impression").Trim().ToLowerInvariant();
        return t switch
        {
            "shown" => "shown",
            "impression" => "impression",
            "closed" => "closed",
            _ => "impression"
        };
    }

    private static bool EventMatchesCountOn(AdsEvent evt, string countOn) =>
        (evt == AdsEvent.Shown && countOn == "shown") ||
        (evt == AdsEvent.Impression && countOn == "impression") ||
        (evt == AdsEvent.Closed && countOn == "closed");
}
