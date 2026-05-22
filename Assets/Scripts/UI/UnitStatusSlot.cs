using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitStatusSlot : MonoBehaviour
{
    public Image portrait;
    public TextMeshProUGUI unitNameText;
    public Slider healthSlider;
    public TextMeshProUGUI healthText;

    private CombatUnit linkedUnit;

    public void Setup(CombatUnit unit)
    {
        linkedUnit = unit;
        if (portrait != null && unit.Data.portrait != null)
            portrait.sprite = unit.Data.portrait;
        if (unitNameText != null)
            unitNameText.text = unit.UnitName;
        UpdateHealth();

        // Đăng ký sự kiện
        linkedUnit.OnDamageTaken += OnUnitDamaged;
        linkedUnit.OnDied += OnUnitDied;
    }

    private void OnDestroy()
    {
        if (linkedUnit != null)
        {
            linkedUnit.OnDamageTaken -= OnUnitDamaged;
            linkedUnit.OnDied -= OnUnitDied;
        }
    }

    private void OnUnitDamaged(CombatUnit caster, int damage)
    {
        UpdateHealth();
    }

    private void OnUnitDied()
    {
        UpdateHealth(); // máu = 0
        // Có thể disable slot hoặc làm mờ
    }

    public void UpdateHealth()
    {
        if (linkedUnit == null) return;
        if (healthSlider != null)
            healthSlider.value = (float)linkedUnit.CurrentHP / linkedUnit.MaxHP;
        if (healthText != null)
            healthText.text = $"{linkedUnit.CurrentHP}/{linkedUnit.MaxHP}";
    }

    public CombatUnit GetLinkedUnit() => linkedUnit;
}