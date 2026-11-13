using System.Collections;
using UnityEngine;

// Congela el tiempo una fraccion de segundo al conectar un golpe.
// Es el truco mas barato que existe para que un impacto se SIENTA:
// el cerebro lee la micro-pausa como peso.
//
// Es estatico y se auto-instancia, asi cualquier script puede llamar
// HitStop.Freeze(0.06f) sin tener que referenciar nada en el Inspector.
public class HitStop : MonoBehaviour {
    static HitStop _instancia;
    Coroutine _rutina;

    static HitStop Instancia {
        get {
            if (_instancia == null) {
                var go = new GameObject("HitStop");
                DontDestroyOnLoad(go);
                _instancia = go.AddComponent<HitStop>();
            }
            return _instancia;
        }
    }

    public static void Freeze(float segundos) {
        Instancia.Do(segundos);
    }

    void Do(float segundos) {
        // Si ya hay un hit stop corriendo lo reiniciamos, en vez de
        // encadenar dos y dejar el juego congelado el doble.
        if (_rutina != null) StopCoroutine(_rutina);
        _rutina = StartCoroutine(FreezeRoutine(segundos));
    }

    IEnumerator FreezeRoutine(float segundos) {
        float previo = Time.timeScale;

        // No pisamos una pausa real (timeScale 0 del menu).
        if (previo <= 0f) yield break;

        Time.timeScale = 0f;

        // WaitForSecondsRealtime porque el tiempo escalado esta congelado:
        // si usaramos el normal, la espera nunca avanzaria.
        yield return new WaitForSecondsRealtime(segundos);

        // Solo restauramos si seguimos congelados por NOSOTROS. Si en el
        // medio se abrio el menu de pausa, no lo despausamos de prepo.
        if (Time.timeScale == 0f) Time.timeScale = previo;
        _rutina = null;
    }
}
