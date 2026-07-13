using System.Collections;
using UnityEngine;

public class MotosierraController : MonoBehaviour
{
    [Header("Referencias a Datos")]
    [Tooltip("Estadísticas del personaje (armadura, curación, velocidad, etc.).")]
    [System.NonSerialized]
    public DatosPersonaje datosPersonaje;
    [Tooltip("Configuración estática y de balance de la motosierra.")]
    public DatosArma datosArma;

    [Header("Referencias de Colisión Melee")]
    [Tooltip("Colisionador Trigger local que representa el filo giratorio de la motosierra.")]
    public Collider2D hitboxCollider;
    [Tooltip("Componente EntidadDaño local para aplicar el daño continuo.")]
    public EntidadDaño entidadDaño;

    [Header("Configuración de Rangos")]
    [Tooltip("Rango de detección amplio para empezar a apuntar al enemigo antes de estar a rango de corte.")]
    public float distanciaDeteccion = 5f;
    [Tooltip("Rango base para normalizar la escala de ataque.")]
    public float rangoBase = 1.5f;

    [Header("Configuración de Ataque Continuo")]
    [Tooltip("Multiplicador de tamaño extra que gana la motosierra al aserrar.")]
    public float multiplicadorEscalaAtaque = 1.3f;
    [Tooltip("Fuerza o amplitud de la vibración rápida local.")]
    public float fuerzaVibracion = 0.04f;

    [Header("Propiedades")]
    [Tooltip("Indica si la motosierra puede atacar.")]
    public bool puedeDisparar = true;

    // Estadísticas calculadas
    private EstadisticasArma estadisticasCalculadas;
    private float cooldownAtaque;
    private bool inicializado = false;
    private bool esAtacando = false;

    private Vector3 escalaOriginal;
    private Vector3 posicionLocalOriginal;

    private void Start()
    {
        if (!inicializado)
        {
            InicializarEstadisticas();
        }

        // Registrar estados originales de transform
        escalaOriginal = transform.localScale;
        posicionLocalOriginal = transform.localPosition;

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
        // 1. Buscar enemigo en el rango amplio de detección
        GameObject enemigoCercano = BuscarEnemigoEnRangoDeteccion();

        if (enemigoCercano != null)
        {
            // 2. Apuntar/Rotar continuamente hacia el enemigo (incluso si estamos atacando)
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

            // 3. Evaluar distancia de corte
            float distanciaAlEnemigo = Vector2.Distance(posicionArma, posicionEnemigo);
            if (distanciaAlEnemigo <= estadisticasCalculadas.rango && anguloDiferencia <= 10f && puedeDisparar)
            {
                if (!esAtacando)
                {
                    ActivarModoAtaque();
                }

                // Ejecutar la vibración y el temporizador de ticks de daño
                ProcesarEfectoAtaque();
            }
            else
            {
                if (esAtacando)
                {
                    DesactivarModoAtaque();
                }
            }
        }
        else
        {
            if (esAtacando)
            {
                DesactivarModoAtaque();
            }
        }
    }

    private void ActivarModoAtaque()
    {
        esAtacando = true;

        // Reproducir sonido de motosierra activa
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundID.MotosierraSierra);
        }

        // Escalar según el rango real
        float factorRango = rangoBase > 0.05f ? (estadisticasCalculadas.rango / rangoBase) : 1f;
        float escalaAtaque = multiplicadorEscalaAtaque * factorRango;
        transform.localScale = escalaOriginal * escalaAtaque;

        // Inicializamos los datos en la EntidadDaño local (no destruirse al impactar)
        if (entidadDaño != null)
        {
            entidadDaño.Inicializar(estadisticasCalculadas.daño, EntidadDaño.OrigenDaño.Jugador, false);
        }

        cooldownAtaque = 0f; // Activar tick de daño inmediato
    }

    private void ProcesarEfectoAtaque()
    {
        // 1. Vibración local en cada frame para simular aserrado
        Vector3 offsetVibracion = Random.insideUnitCircle * fuerzaVibracion;
        transform.localPosition = posicionLocalOriginal + offsetVibracion;

        // 2. Control de ticks periódicos de colisión (cadencia)
        cooldownAtaque -= Time.deltaTime;
        if (cooldownAtaque <= 0f)
        {
            StartCoroutine(FlashColliderCo());
            cooldownAtaque = estadisticasCalculadas.cadencia;
        }
    }

    private void DesactivarModoAtaque()
    {
        esAtacando = false;

        // Apagar colisionador
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }

        // Restaurar estado de reposo
        transform.localScale = escalaOriginal;
        transform.localPosition = posicionLocalOriginal;
    }

    /// <summary>
    /// Enciende y apaga rápidamente el colisionador para registrar colisiones individuales en cada tick de daño.
    /// </summary>
    private IEnumerator FlashColliderCo()
    {
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = true;
        }
        
        yield return new WaitForSeconds(0.05f); // Mantener encendido un instante
        
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }
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
