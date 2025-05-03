using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmScript : MonoBehaviour
{
    private Animator animator;
    public WeaponData weaponData;
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void PlayAttackAnimation()
    {
        if (weaponData == null) return;

        switch (weaponData.weaponName)
        {
            case "Sword":
                animator.SetTrigger("SwordOverlay");
                break;
            case "Bow":
                animator.SetTrigger("BowOverlay");
                break;
            case "Hammer":
                animator.SetTrigger("HammerOverlay");
                break;
            case "Scythe":
                animator.SetTrigger("ScytheOverlay");
                break;
        }
    }
}
