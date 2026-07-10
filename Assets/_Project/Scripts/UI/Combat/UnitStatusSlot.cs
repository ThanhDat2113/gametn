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

        // Đăng ký sự kiện - sửa delegate để khớp
        linkedUnit.OnDamageTaken += OnUnitDamaged;
        linkedUnit.OnHealed += OnUnitHealed;
        linkedUnit.OnDied += OnUnitDied;
    }

    private void OnDestroy()
    {
        if (linkedUnit != null)
        {
            linkedUnit.OnDamageTaken -= OnUnitDamaged;
            linkedUnit.OnHealed -= OnUnitHealed;
            linkedUnit.OnDied -= OnUnitDied;
        }
    }

    private void OnUnitHealed(int amount)
    {
        UpdateHealth();
    }

    // Sửa: thêm tham số DamageType (có thể không dùng)
    private void OnUnitDamaged(CombatUnit caster, int damage, DamageType damageType)
    {
        UpdateHealth();
    }

    private void OnUnitDied()
    {
        UpdateHealth();
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