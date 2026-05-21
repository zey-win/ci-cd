using System;
using UnityEngine;


public class PlinkoModeManager : MonoBehaviour
{
    public event Action OnBet;

    public void Bet()
    {
        OnBet?.Invoke();
    }
}