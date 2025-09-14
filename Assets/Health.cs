using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    public float maxHp = 100f;
    public float currentHp;
    public event Action<float,float> OnHealthChanged;
    public event Action OnDied;

    void Awake(){ currentHp = maxHp; OnHealthChanged?.Invoke(currentHp,maxHp); }
    public bool IsDead => currentHp <= 0f;

    public void TakeDamage(float amount){
        if (IsDead) return;
        currentHp = Mathf.Max(0f, currentHp - amount);
        OnHealthChanged?.Invoke(currentHp, maxHp);
        if (IsDead) OnDied?.Invoke();
    }
}
