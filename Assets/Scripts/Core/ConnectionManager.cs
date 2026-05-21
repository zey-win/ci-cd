using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebase;
using Firebase.RemoteConfig;
using UnityEngine;

namespace Server
{
    public sealed class ConnectionManager : MonoBehaviour
    {

        public float MaxMultiplier => _maxMultiplier;
        [SerializeField] private float _maxMultiplier = 4f;
        private const string RC_MAX_MULTIPLIER = "max_multiplier";

        // чтобы не фетчить/инициализировать на каждый объект
        private bool _rcReady;
        private bool _rcInitRunning;

        private BalanceManager _balanceManager;



        private IEnumerator Start()
        {
            _balanceManager = GetComponent<BalanceManager>();
            yield return RemoteConfigManager.EnsureReadyCoroutine();
        }

        public void CreateBall(float bet, Action<string> callback = null)
        {
            _balanceManager.AddBet(bet);
            callback?.Invoke("transaction_id");
        }

        private List<float> GetMultipliers(int rows)
        {
            var row = GetMultiRow(rows);
            float max = RemoteConfigManager.MaxMultiplier;
            return row.Select(r => r * max).ToList();
        }

        private List<float> GetMultiRow(int rowCount)
        {
            var array = Array.ConvertAll(new float[rowCount + 1], i => 1f);
            var arrayCount = array.Length;

            for (var i = 0; i < arrayCount * 0.5f; i++)
            {
                var value = array[i] * 1 - (i / (arrayCount * 0.5f));
                value = Mathf.Clamp(value, 0f, 1f);
                array[i] = value;
            }

            for (var i = Mathf.FloorToInt(arrayCount * 0.5f); i < arrayCount; i++)
            {
                var value = array[i] * 1 - ((arrayCount - i) / (arrayCount * 0.5f));
                value = Mathf.Clamp(value, 0f, 1f);
                array[i] = value;
            }

            return array.ToList();
        }

        public void ChangeBoard(int rows, Action<List<float>> callback = null)
        {
            var response = new BoardData()
            {
                gate_values = GetMultipliers(rows)
            };

            callback?.Invoke(response.gate_values);
        }
    }
}

[Serializable]
public class BoardData
{
    public List<float> gate_values;
}