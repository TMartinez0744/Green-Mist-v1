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

    // Pose con la que el baston quedo apoyado en la espalda al arrancar la
    // escena (la diagonal linda). La guardamos para restaurarla cada vez
    // que se guarda, en vez de forzar rotacion 0 (que lo dejaba horizontal
    // atravesando el cuerpo).
    Vector3 backPos;
    Quaternion backRot;

    void Start()
    {
        isEquipped = false;

        if (weapon != null && backSlot != null)
        {
            if (weapon.parent != backSlot)
                weapon.SetParent(backSlot, true); // mantiene el world, no lo mueve

            // Capturamos la pose buena tal como vino puesta en la escena.
            backPos = weapon.localPosition;
            backRot = weapon.localRotation;
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
            // Guardar en la espalda, restaurando la pose diagonal original
            // en vez de identity.
            weapon.SetParent(backSlot, false);
            weapon.localPosition = backPos;
            weapon.localRotation = backRot;
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
