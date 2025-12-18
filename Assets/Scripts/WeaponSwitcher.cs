using UnityEngine;

public class WeaponSwitcher : MonoBehaviour
{
    [Header("Referencias")]
    public Transform backSlot;   // Empty en la espalda
    public Transform handSlot;   // Empty en la mano
    public Transform weapon;     // El bambú

    [Header("Config")]
    public KeyCode toggleKey = KeyCode.Q;  // tecla para equipar/guardar

    bool isEquipped = false; // empieza en la espalda
    
    public bool IsEquipped => isEquipped;

    void Start()
{
    // Si ya está en BackSlot en la escena, NO lo resetees preventivamente.
    // Solo asegurate que arranca "en espalda" sin tocar transform.
    isEquipped = false;

    // Opcional: si querés forzar que sea hijo del backSlot pero SIN resetear:
    if (weapon != null && backSlot != null && weapon.parent != backSlot)
    {
        weapon.SetParent(backSlot, true); // mantiene el world (no lo mueve)
    }
}


    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleWeapon();
        }
    }

    void ToggleWeapon()
    {
        if (weapon == null || backSlot == null || handSlot == null) return;

        if (isEquipped)
        {
            // Guardar en la espalda
            weapon.SetParent(backSlot, false);
            weapon.localPosition = Vector3.zero;
            weapon.localRotation = Quaternion.identity;
            isEquipped = false;
        }
        else
        {
            // Equipar en la mano
            weapon.SetParent(handSlot, false);
            weapon.localPosition = Vector3.zero;
            weapon.localRotation = Quaternion.identity;
            isEquipped = true;
        }
    }
}
