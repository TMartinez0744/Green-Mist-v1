using UnityEngine;

// Encierra el puntero en la ventana mientras se juega.
//
// Sin esto el mouse es libre: al girar la camara se sale de la pantalla,
// y cuando llega al borde el eje deja de moverse. Por eso se siente que
// hay que arrastrar muchisimo para dar la vuelta.
//
// Se libera solo cuando el juego esta pausado. PauseMenu, MainMenuUI y
// PlayerDeath marcan esos momentos con Time.timeScale = 0, asi que
// alcanza con mirar eso y no hace falta tocar ninguno de los tres.
public class CursorLock : MonoBehaviour
{
    bool bloqueado;

    void OnEnable()
    {
        Aplicar(Time.timeScale > 0f);
    }

    void OnDisable()
    {
        // Al salir de Play o al desactivarse, devolvemos el mouse.
        Aplicar(false);
    }

    void Update()
    {
        bool deberia = Time.timeScale > 0f;
        if (deberia != bloqueado) Aplicar(deberia);
    }

    void Aplicar(bool bloquear)
    {
        bloqueado = bloquear;
        Cursor.lockState = bloquear ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !bloquear;
    }
}
