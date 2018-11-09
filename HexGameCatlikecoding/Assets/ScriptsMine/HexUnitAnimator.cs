using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexUnitAnimator : MonoBehaviour
{
    [SerializeField] private Animator anim;
    private bool isDead;
    private void Update()
    {
        if(isDead)
            transform.position += Vector3.down * Time.deltaTime * 2;
    }

    public void SetWalking(bool state)
    {
        anim.SetBool("IsWalking", state);
    }
    public void InitAttack()
    {
        anim.SetTrigger("Attack");
    }

    public void TakeDamage()
    {
        anim.SetTrigger("Damage");
    }
    public void Die()
    {
        anim.SetTrigger("Die");
        StartCoroutine(DieTimer());
    }

    IEnumerator DieTimer()
    {
        yield return new WaitForSeconds(4);
        isDead = true;
        yield return new WaitForSeconds(6);
        Destroy(gameObject);
        yield return null;
    }
}
