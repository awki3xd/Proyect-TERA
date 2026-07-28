using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AcidoController : MonoBehaviour
{
    [Header("Configuración de Daño y Ralentización")]
    [Tooltip("Cantidad de daño base por cada pulso de ácido.")]
    public float dañoBase = 5f;

    [Tooltip("Intervalo base en segundos entre pulsos de daño al estar dentro del ácido.")]
    public float intervaloBase = 0.5f;

    [Tooltip("Factor de velocidad al estar en el ácido (0.5 = 50% de ralentización).")]
    public float factorRalentizacion = 0.5f;

    [Header("Configuración de Duración")]
    [Tooltip("Tiempo de vida en segundos antes de autodestruirse el charco de ácido.")]
    public float duracionVida = 15f;

    private float dañoCalculado = 5f;
    private float intervaloCalculado = 0.5f;
    private Dictionary<PlayerController, float> cooldownsJugadores = new Dictionary<PlayerController, float>();

    private void Awake()
    {
        dañoCalculado = dañoBase;
        intervaloCalculado = intervaloBase;
    }

    private void Start()
    {
        // Autodestruir el objeto tras 15 segundos (las animaciones visuales ocurren solas en el Animator)
        Destroy(gameObject, duracionVida);
    }

    /// <summary>
    /// Escala el daño y la frecuencia de los pulsos del charco en base a las estadísticas globales del enemigo.
    /// </summary>
    public void Inicializar(DatosGlobalesEnemigos datosGlobales)
    {
        float multDaño = datosGlobales != null ? datosGlobales.daño / 100f : 1f;
        float multVelAtaque = datosGlobales != null ? datosGlobales.velocidadAtaque / 100f : 1f;

        dañoCalculado = dañoBase * Mathf.Max(0.1f, multDaño);
        // A mayor velocidad de ataque (ej: 200%), menor es el intervalo entre pulsos (ej: 0.5s / 2 = 0.25s)
        intervaloCalculado = intervaloBase / Mathf.Max(0.1f, multVelAtaque);

        Debug.Log($"[Acido] Inicializado con Daño: {dañoCalculado} e Intervalo de Pulso: {intervaloCalculado:F2}s");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.multiplicadorRalentizacion = factorRalentizacion;
                if (!cooldownsJugadores.ContainsKey(player))
                {
                    cooldownsJugadores.Add(player, 0f);
                }
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                // Aplicar ralentización a la mitad (50%) mientras permanezca sobre el ácido
                player.multiplicadorRalentizacion = factorRalentizacion;

                // Control de pulso de daño utilizando el intervalo calculado por la velocidad de ataque
                if (!cooldownsJugadores.ContainsKey(player))
                {
                    cooldownsJugadores[player] = 0f;
                }

                if (Time.time >= cooldownsJugadores[player])
                {
                    cooldownsJugadores[player] = Time.time + intervaloCalculado;
                    player.RecibirDaño(dañoCalculado);
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                // Restablecer la velocidad normal al salir del ácido
                player.multiplicadorRalentizacion = 1f;
                if (cooldownsJugadores.ContainsKey(player))
                {
                    cooldownsJugadores.Remove(player);
                }
            }
        }
    }

    private void OnDestroy()
    {
        // Restaurar velocidad si el charco se destruye mientras el jugador estaba dentro
        foreach (var kvp in cooldownsJugadores)
        {
            if (kvp.Key != null)
            {
                kvp.Key.multiplicadorRalentizacion = 1f;
            }
        }
    }
}
