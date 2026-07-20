using System.Collections;
using UnityEngine;

public class NodoEstandar : MonoBehaviour
{
    [Header("Referencias a Datos")]
    [Tooltip("Referencia a los datos globales del personaje.")]
    public DatosPersonaje datosPersonaje;

    [Header("Configuración de Vida")]
    public float vidaMaxima = 100f;
    public float vidaActual;
    public float tiempoDesactivacion = 3f;

    [Header("Estado Actual")]
    [Tooltip("Indica si el nodo aporta a la terraformación.")]
    public bool estaActivo = true;
    [Tooltip("Indica si el nodo fue destruido permanentemente.")]
    public bool estaRoto = false;

    public Animator animación;

    private Coroutine corrutinaReactivacion;
    private float tiempoUltimoSonidoCuracion = 0f;

    private void Start()
    {
        vidaActual = vidaMaxima;
    }

    // Esta función es la que lee el gestor de terraformación
    public bool EstaFuncionando()
    {
        return estaActivo;
    }

    public bool EstaRoto()
    {
        return estaRoto;
    }

    public void RecibirDaño(float dañoEntrante)
    {
        animación.SetTrigger("daño");
        if (estaRoto) return;

        // Reproducir sonido de daño al nodo
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundID.DañoNodo);
        }

        // Se calcula el factorResistencia en caliente a partir de la armadura actual del personaje
        // para reflejar instantáneamente mejoras en la tienda o cambios en tiempo de ejecución.
        float armadura = datosPersonaje != null ? datosPersonaje.armadura : 100f;
        float armaduraMinima = Mathf.Max(1f, armadura);
        float factorResistencia = armaduraMinima / 100f;

        // Mitigar daño según el factorResistencia
        float dañoCalculado = dañoEntrante / factorResistencia;

        // Crear número de daño flotante en color rojo
        TextoDañoFlotante.Crear(transform.position, dañoCalculado, Color.red);

        // Restar daño y asegurar límites entre 0 y vidaMaxima
        vidaActual = Mathf.Clamp(vidaActual - dañoCalculado, 0f, vidaMaxima);

        if (vidaActual <= 0f)
        {
            estaRoto = true;
            estaActivo = false;

            if (corrutinaReactivacion != null)
            {
                StopCoroutine(corrutinaReactivacion);
                corrutinaReactivacion = null;
            }
        }
        else
        {
            estaActivo = false;

            if (corrutinaReactivacion != null)
            {
                StopCoroutine(corrutinaReactivacion);
            }
            corrutinaReactivacion = StartCoroutine(ReactivarCo());
        }
    }

    private IEnumerator ReactivarCo()
    {
        yield return new WaitForSeconds(tiempoDesactivacion);

        if (!estaRoto)
        {
            estaActivo = true;
        }

        corrutinaReactivacion = null;
    }

    public void Curar(float cantidad)
    {
        if (estaRoto) return;

        vidaActual = Mathf.Clamp(vidaActual + cantidad, 0f, vidaMaxima);

        // Reproducir sonido de curación periódicamente si el nodo no está curado al máximo
        if (vidaActual < vidaMaxima && Time.time - tiempoUltimoSonidoCuracion >= 0.4f)
        {
            tiempoUltimoSonidoCuracion = Time.time;
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SoundID.CurarNodo);
            }
        }
    }
}