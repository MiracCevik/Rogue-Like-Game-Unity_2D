using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SkillIconController : MonoBehaviour
{
    public Image skillIcon;
    public Material grayscaleMaterial; 
    private Material runtimeMaterial; 
    private float cooldownTime;

    public void Initialize(SkillData skillData)
    {
        skillIcon.sprite = skillData.icon;
        gameObject.SetActive(skillData.isUnlocked); 
    }

    public void StartCooldown(float cooldown)
    {
        cooldownTime = cooldown;

        runtimeMaterial = new Material(grayscaleMaterial);
        skillIcon.material = runtimeMaterial;
        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < cooldownTime)
        {
            elapsedTime += Time.deltaTime;
            float intensity = Mathf.Lerp(1f, 0f, elapsedTime / cooldownTime); 
            runtimeMaterial.SetFloat("_Intensity", intensity);
            yield return null;
        }

        skillIcon.material = null;
    }

    public void ActivateSkillIcon()
    {
        skillIcon.enabled = true; 
    }
}
