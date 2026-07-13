using System.Collections;
using UnityEngine;

public class SableController : MonoBehaviour
{
    [Header("Referencias a Datos")]
    [Tooltip("Estadísticas del personaje (armadura, curación, velocidad, etc.).")]
    [System.NonSerialized]
    public DatosPersonaje datosPersonaje;
    [Tooltip("Configuración estática y de balance del sable.")]
    public DatosArma datosArma;

    [Header("Referencias de Colisión Melee")]
    [Tooltip("Colisionador Trigger local que representa el filo de la espada.")]
    public Collider2D hitboxCollider;
    [Tooltip("Componente EntidadDaño local para aplicar el daño.")]
    public EntidadDaño entidadDaño;

    [Header("Configuración de Rangos")]
    [Tooltip("Rango de detección amplio para empezar a apuntar al enemigo antes de estar a rango de ataque.")]
    public float distanciaDeteccion = 5f;
    [Tooltip("Rango base para normalizar la escala de ataque.")]
    public float rangoBase = 1.5f;

    [Header("Configuración del Swing")]
    [Tooltip("Duración visual y física del tajo en segundos.")]
    public float duracionSwing = 0.5f;
    [Tooltip("Multiplicador de tamaño extra que gana el arma durante el tajo.")]
    public float multiplicadorEscalaAtaque = 1.5f;

    [Header("Propiedades")]
    [Tooltip("Indica si el sable puede atacar.")]
    public bool puedeDisparar = true;

    // Estadísticas calculadas
    private EstadisticasArma estadisticasCalculadas;
    private float cooldownAtaque;
    private bool inicializado = false;
    private bool esAtacando = false;

    private void Start()
    {
        if (!inicializado)
        {
            InicializarEstadisticas();
        }

        // Asegurarnos de que el collider inicie apagado
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }
    }

    /// <summary>
    /// Calcula las estadísticas finales de rango, daño y cadencia.
    /// </summary>
    private void InicializarEstadisticas()
    {
        inicializado = true;

        if (datosArma != null)
        {
            estadisticasCalculadas.rango = datosArma.rango;
            estadisticasCalculadas.daño = datosArma.daño;
            estadisticasCalculadas.cadencia = datosArma.cadencia;
            estadisticasCalculadas.velocidadRotacion = datosArma.velocidadRotacion;

            if (datosPersonaje != null)
            {
                float multRango = datosPersonaje.rangoAtaque / 100f;
                float multDaño = datosPersonaje.daño / 100f;
                float multVelocidadAtaque = datosPersonaje.velocidadAtaque / 100f;

                estadisticasCalculadas.rango *= multRango;
                estadisticasCalculadas.daño *= multDaño;
                estadisticasCalculadas.cadencia *= multVelocidadAtaque;
            }
        }
        else
        {
            Debug.LogWarning("DatosArma no asignado en " + gameObject.name);
        }

        cooldownAtaque = 0f;
    }

    private void Update()
    {
        // Si está ejecutando el ataque físico (corrutina de swing), pausamos la rotación de apuntado
        if (esAtacando)
        {
            return;
        }

        // 1. Descontar el cooldown de ataque progresivamente
        if (cooldownAtaque > 0f)
        {
            cooldownAtaque -= Time.deltaTime;
        }

        // 2. Buscar enemigo en el rango amplio de detección
        GameObject enemigoCercano = BuscarEnemigoEnRangoDeteccion();

        if (enemigoCercano != null)
        {
            // 3. Rotar hacia el enemigo (apuntar)
            Vector2 posicionArma = transform.position;
            Vector2 posicionEnemigo = enemigoCercano.transform.position;
            Vector2 vectorHaciaEnemigo = (posicionEnemigo - posicionArma).normalized;
            Vector2 direccionActual = transform.right;

            float dot = Vector2.Dot(direccionActual, vectorHaciaEnemigo);
            dot = Mathf.Clamp(dot, -1f, 1f);
            float anguloDiferencia = Mathf.Acos(dot) * Mathf.Rad2Deg;

            float cross = (direccionActual.x * vectorHaciaEnemigo.y) - (direccionActual.y * vectorHaciaEnemigo.x);
            float sentidoGiro = cross >= 0f ? 1f : -1f;

            if (anguloDiferencia > 0.5f)
            {
                float pasoRotacion = estadisticasCalculadas.velocidadRotacion * Time.deltaTime;
                float rotacionPaso = Mathf.Min(pasoRotacion, anguloDiferencia);
                transform.Rotate(0f, 0f, rotacionPaso * sentidoGiro);
            }

            // 4. Evaluar si está a rango de ataque y alineado para golpear
            float distanciaAlEnemigo = Vector2.Distance(posicionArma, posicionEnemigo);
            if (cooldownAtaque <= 0f && distanciaAlEnemigo <= estadisticasCalculadas.rango && anguloDiferencia <= 10f && puedeDisparar)
            {
                StartCoroutine(AtaqueMeleeCo());
            }
        }
    }

    /// <summary>
    /// Corrutina de tajo (swing): escala el arma, activa el trigger, barre un arco de 90 grados y reinicia el cooldown al terminar.
    /// </summary>
    private IEnumerator AtaqueMeleeCo()
    {
        esAtacando = true;

        Vector3 escalaOriginal = transform.localScale;
        float originalZ = transform.localEulerAngles.z;

        // Escalar según el rango real
        float factorRango = rangoBase > 0.05f ? (estadisticasCalculadas.rango / rangoBase) : 1f;
        float escalaAtaque = multiplicadorEscalaAtaque * factorRango;
        transform.localScale = escalaOriginal * escalaAtaque;

        // Inicializar daño
        if (entidadDaño != null)
        {
            entidadDaño.Inicializar(estadisticasCalculadas.daño, EntidadDaño.OrigenDaño.Jugador, false);
        }

        // Encender colisionador de impacto
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = true;
        }

        // Barrido físico/visual de 90 grados (-45 a +45 de la rotación inicial)
        float startZ = originalZ - 90f;
        float endZ = originalZ + 90f;
        float elapsed = 0f;

        while (elapsed < duracionSwing)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duracionSwing;
            float currentZ = Mathf.Lerp(startZ, endZ, t);
            transform.localRotation = Quaternion.Euler(0f, 0f, currentZ);
            yield return null;
        }

        // Apagar colisionador
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }

        // Restaurar estado original
        transform.localScale = escalaOriginal;
        transform.localRotation = Quaternion.Euler(0f, 0f, originalZ);

        // INICIAR EL COOLDOWN DE ATAQUE ÚNICAMENTE AL COMPLETAR EL SWING
        cooldownAtaque = estadisticasCalculadas.cadencia;
        esAtacando = false;
    }

    private GameObject BuscarEnemigoEnRangoDeteccion()
    {
        GameObject[] enemigos = GameObject.FindGameObjectsWithTag("Enemigo");
        GameObject closest = null;
        float minDist = distanciaDeteccion;

        foreach (var enemigo in enemigos)
        {
            if (enemigo != null)
            {
                float dist = Vector2.Distance(transform.position, enemigo.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = enemigo;
                }
            }
        }
        return closest;
    }
}
