using System.Collections.Generic;
using UnityEngine;

public class CofreController : MonoBehaviour
{
    [Header("Referencias de Renderizado")]
    [Tooltip("SpriteRenderer del cofre donde se asignará el diseño visual aleatorio.")]
    public SpriteRenderer spriteRenderer;

    [Tooltip("Lista/Arreglo de los 24 sprites de cofres disponibles para dar variedad visual.")]
    public Sprite[] spritesCofres;

    [Header("Configuración de Recompensas de Armas")]
    [Tooltip("Lista/Arreglo de prefabs de armas disponibles que este cofre puede contener.")]
    public GameObject[] armasDisponibles;

    [Tooltip("Arma asignada aleatoriamente a este cofre específico al nacer.")]
    public GameObject armaAsignada;

    private void Awake()
    {
        InicializarCofre();
    }

    private void Start()
    {
        // Respaldo en caso de instanciación manual
        if (armaAsignada == null || (spriteRenderer != null && spriteRenderer.sprite == null))
        {
            InicializarCofre();
        }
    }

    public void InicializarCofre()
    {
        // 1. Asignar SpriteRenderer si no fue vinculado en Inspector
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        // 2. Elegir y aplicar un sprite aleatorio de la lista de 24 sprites
        if (spriteRenderer != null && spritesCofres != null && spritesCofres.Length > 0)
        {
            int indexSprite = Random.Range(0, spritesCofres.Length);
            if (spritesCofres[indexSprite] != null)
            {
                spriteRenderer.sprite = spritesCofres[indexSprite];
            }
        }

        // 3. Elegir y asignar un arma aleatoria si no hay una preasignada
        if (armaAsignada == null && armasDisponibles != null && armasDisponibles.Length > 0)
        {
            int indexArma = Random.Range(0, armasDisponibles.Length);
            armaAsignada = armasDisponibles[indexArma];
        }
    }
}
