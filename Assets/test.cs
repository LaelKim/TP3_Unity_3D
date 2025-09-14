using UnityEngine;

public class DamageDebugHotkeys : MonoBehaviour
{
    public Health playerHp;
    public Health zombieHp;
    public float amount = 10f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K) && zombieHp) zombieHp.TakeDamage(amount); // K blesse le zombie
        if (Input.GetKeyDown(KeyCode.L) && playerHp) playerHp.TakeDamage(amount); // L blesse le player
    }
}
