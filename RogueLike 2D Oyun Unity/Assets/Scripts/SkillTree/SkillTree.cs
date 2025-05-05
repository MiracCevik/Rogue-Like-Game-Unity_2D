using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SkillTreeManager : MonoBehaviour
{
    [Header("Skill Tree Settings")]
    public GameObject skillTreePanel;
    public GameObject AeEPrefab;
    public TextMeshProUGUI goldText;
    public KarakterHareket karakter;
    public Transform character;
    private SkillData skillData;
   
    [Header("Skill List")]
    public List<SkillData> skills;
    public List<SkillIconController> skillIcons;
    private int playerGold;

    private bool isSkillTreeOpen = false;
    private bool isHealOnCooldown = true;
    private bool isChargeAttackOnCooldown = true;
    private bool canDash = true;
    private bool AoEAttackCooldown = true;

    void Start()
    {
        playerGold = karakter.gold;

        for (int i = 0; i < skills.Count; i++)
        {
            if (i < skillIcons.Count)
            {
                skillIcons[i].Initialize(skills[i]);

            }
        }
    }

    void Update()
    {
        if (character != null)
        {
            transform.position = character.position ;
            transform.rotation = Quaternion.identity;
            Vector3 fixedScale = transform.localScale;
            fixedScale.x = Mathf.Abs(fixedScale.x);
            transform.localScale = fixedScale;
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            if (isSkillTreeOpen) CloseSkillTree();
            else OpenSkillTree();
        }
    }

    public void OpenSkillTree()
    {
        skillTreePanel.SetActive(true);
        Time.timeScale = 0f;
        isSkillTreeOpen = true;
    }

    public void CloseSkillTree()
    {
        skillTreePanel.SetActive(false);
        Time.timeScale = 1f;
        isSkillTreeOpen = false;
    }

    public void BuySkill(int skillIndex)
    {
        SkillData skill = skills[skillIndex];
        if (skill.isUnlocked)
        {
            return;
        }

        if (karakter.gold >= skill.cost)
        {
            karakter.gold -= skill.cost;
            skill.isUnlocked = true;
            skillIcons[skillIndex].ActivateSkillIcon();
            UpdateGoldUI(karakter.gold);
            SaveManager.Instance.SaveGame();
        }
       
    }

    public void UpdateGoldUI(int currentGold)
    {
        goldText.text = "Gold: " + currentGold.ToString();
    }

    public void UseSkill(int skillIndex, GameObject player)
    {
        SkillData skill = skills[skillIndex];
        if (!skill.isUnlocked) return;

        SkillIconController iconController = skillIcons[skillIndex];
        iconController.StartCooldown(skill.cooldown);

        switch (skill.skillType)
        {
            case SkillData.SkillType.ChargeAttack:
                if (!isChargeAttackOnCooldown)
                    Debug.Log("charge cooldownda");
                    
                else
                    StartCoroutine(ChargeAttack(player));
                break;
            case SkillData.SkillType.AoEAttack:
                if (!AoEAttackCooldown)
                    Debug.Log("AoE cooldownda");
                else
                    StartCoroutine(AoEAttack(player));
                break;
            case SkillData.SkillType.Dash:
                if (!canDash)
                    Debug.Log("dash cooldownda");
                else
                StartCoroutine(Dash(player));
                break;
            case SkillData.SkillType.Heal:
                if (!isHealOnCooldown)
                    Debug.Log("�ifa cooldownda");
                 else   
                StartCoroutine(Heal(player));
                break;
            default:
                Debug.LogError($"Unknown SkillType: {skill.skillType}");
                break;
        }
    }

    private IEnumerator ChargeAttack(GameObject player)
    {
        isChargeAttackOnCooldown = false;
        WeaponsScript weaponsScript = player.GetComponentInChildren<WeaponsScript>();
        weaponsScript.PlayAttackAnimation();
        yield return new WaitForSeconds(1f);

        weaponsScript.Attack(2 * weaponsScript.weaponData.damage);
        yield return new WaitForSeconds(5f); 

        isChargeAttackOnCooldown = true;
    }


    private IEnumerator AoEAttack(GameObject player)
    {
        AoEAttackCooldown = false;
        GameObject aoe = Instantiate(AeEPrefab, player.transform.position, Quaternion.identity);
        yield return new WaitForSeconds(0.5f);
        Destroy(aoe);
        yield return new WaitForSeconds(10f);
        AoEAttackCooldown = true;
    }

    private IEnumerator Heal(GameObject player)
    {
        isHealOnCooldown = false;
        KarakterHareket karakterHareket = player.GetComponent<KarakterHareket>();
        karakterHareket.currentHealth += 50;
        if (karakterHareket.currentHealth > karakterHareket.CharHealth)
        {
            karakterHareket.currentHealth = karakterHareket.CharHealth;
        }
        karakterHareket.UpdateHealthBar();
        yield return new WaitForSeconds(60f);
        isHealOnCooldown = true;
    }

    private IEnumerator Dash(GameObject player)
    {
        canDash = false;
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        Collider2D playerCollider = player.GetComponent<Collider2D>();
        float dashDirection = player.transform.localScale.x > 0 ? 1f : -1f;
        player.layer = LayerMask.NameToLayer("Invulnerable");
        rb.velocity = Vector2.zero;
        rb.AddForce(Vector2.right * dashDirection * 15f, ForceMode2D.Impulse);
        float dashDuration = 0.3f;
        float elapsedTime = 0f;
        int dashDamage = 10;

        while (elapsedTime < dashDuration)
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(player.transform.position, Vector2.right * dashDirection, 1f);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider != null && hit.collider.CompareTag("Enemy"))
                {
                    Enemies enemy = hit.collider.GetComponent<Enemies>();
                    if (enemy != null)
                    {
                        enemy.TakeDamageServerRpc(dashDamage);

                        Physics2D.IgnoreCollision(playerCollider, hit.collider, true);
                        yield return new WaitForSeconds(0.2f);
                        Physics2D.IgnoreCollision(playerCollider, hit.collider, false);
                    }
                }
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        player.layer = LayerMask.NameToLayer("Default");
        yield return new WaitForSeconds(1f);
        canDash = true;
    }

}
