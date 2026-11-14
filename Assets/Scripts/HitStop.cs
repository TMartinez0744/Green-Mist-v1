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

    // Para cuando llegan dos golpes casi juntos (ver FreezeRoutine).
    bool _congeladoPorHitStop;  // true mientras el timeScale 0 lo pusimos nosotros
    float _escalaAntes = 1f;    // el timeScale que habia antes de congelar

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
        // Si el golpe le pega a dos enemigos en el mismo instante, Freeze
        // se llama dos veces seguidas. La segunda vez el timeScale YA es 0
        // (lo puso la primera), asi que no hay que confundirlo con el menu
        // ni guardarlo como "valor anterior": antes pasaba eso, la rutina
        // salia sin restaurar nada y el juego quedaba congelado para siempre.
        bool yaCongelado = _congeladoPorHitStop && Time.timeScale == 0f;

        if (!yaCongelado) {
            float previo = Time.timeScale;

            // No pisamos una pausa real (timeScale 0 del menu).
            if (previo <= 0f) yield break;

            _escalaAntes = previo;
            _congeladoPorHitStop = true;
            Time.timeScale = 0f;
        }

        // WaitForSecondsRealtime porque el tiempo escalado esta congelado:
        // si usaramos el normal, la espera nunca avanzaria.
        yield return new WaitForSecondsRealtime(segundos);

        // Solo restauramos si seguimos congelados por NOSOTROS. Si en el
        // medio se abrio el menu de pausa, no lo despausamos de prepo.
        if (Time.timeScale == 0f) Time.timeScale = _escalaAntes;
        _congeladoPorHitStop = false;
        _rutina = null;
    }
}
