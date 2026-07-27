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
    [Tooltip("Referencia a la barra de vida de la antena/nodo (asignada en inspector).")]
    public BarraVida barraVida;

    public Sprite spriteDestruido;
    public SpriteRenderer spriteRenderer;

    private Coroutine corrutinaReactivacion;
    private float tiempoUltimoSonidoCuracion = 0f;

    private void Start()
    {
        vidaActual = vidaMaxima;

        // Inicializar la barra de vida del nodo si está asignada en el inspector
        if (barraVida != null)
        {
            barraVida.Inicializar(() => vidaActual, () => vidaMaxima);
        }
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
            spriteRenderer.sprite = spriteDestruido;

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

        float vidaAntes = vidaActual;
        vidaActual = Mathf.Clamp(vidaActual + cantidad, 0f, vidaMaxima);
        float curacionEfectiva = vidaActual - vidaAntes;

        // Reproducir sonido de curación y mostrar texto flotante si el nodo recibe curación
        if (curacionEfectiva > 0.1f && Time.time - tiempoUltimoSonidoCuracion >= 0.4f)
        {
            tiempoUltimoSonidoCuracion = Time.time;
            TextoDañoFlotante.CrearCuracion(transform.position, curacionEfectiva);

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SoundID.CurarNodo);
            }
        }
    }
}