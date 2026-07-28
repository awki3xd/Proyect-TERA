using System.Collections.Generic;
using UnityEngine;

public class RecolecciónImán : MonoBehaviour
{
    [Header("Referencias a Datos")]
    [Tooltip("Inventario de Génesis donde se guardarán los materiales y armas recolectados.")]
    public DatosInventario datosInventario;

    [Tooltip("ScriptableObject de datos del nivel para escalar la cantidad de materiales.")]
    public DatosNivel datosNivel;

    [Header("Configuración de Atracción")]
    [Tooltip("Velocidad con la que se atraen los cristales y cofres hacia Génesis.")]
    public float velocidadAtraccion = 5f;

    private List<Transform> objetosEnRango = new List<Transform>();
    private PlayerController playerController;

    private void Start()
    {
        playerController = GetComponentInParent<PlayerController>();
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }

        if (datosNivel == null)
        {
            datosNivel = Resources.Load<DatosNivel>("DatosNivel");
        }

        if (datosInventario == null && playerController != null)
        {
            datosInventario = playerController.datosInventario;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Detectar si el objeto es un cristal de material o un cofre de recompensa
        if (other.CompareTag("Material") || other.CompareTag("Cofre"))
        {
            if (!objetosEnRango.Contains(other.transform))
            {
                objetosEnRango.Add(other.transform);
                Debug.Log($"[Imán] Objeto {other.tag} agregado a la lista de atracción.");
            }
        }
    }

    private void Update()
    {
        // Atracción magnética en cada frame hacia la posición de ESTE transform (el imán del jugador)
        for (int i = objetosEnRango.Count - 1; i >= 0; i--)
        {
            Transform objeto = objetosEnRango[i];
            
            // Si el objeto fue destruido o recogido por otro lado, limpiamos la lista
            if (objeto == null)
            {
                objetosEnRango.RemoveAt(i);
                continue;
            }

            // Mueve el objeto suavemente en dirección al imán
            objeto.position = Vector2.MoveTowards(objeto.position, transform.position, velocidadAtraccion * Time.deltaTime);

            // Si está muy cerca de la posición del imán, recolectarlo
            float distancia = Vector2.Distance(objeto.position, transform.position);
            if (distancia < 0.2f)
            {
                if (objeto.CompareTag("Cofre"))
                {
                    RecolectarCofre(objeto.gameObject);
                }
                else
                {
                    RecolectarMaterial(objeto.gameObject);
                }

                objetosEnRango.RemoveAt(i);
            }
        }
    }

    private int CalcularCantidadMateriales()
    {
        int nivelActual = datosNivel != null ? Mathf.Max(1, datosNivel.numeroNivel) : 1;
        int n = Mathf.Clamp(nivelActual, 1, 20);
        float t = (n - 1f) / 19f;

        int minMats = Mathf.RoundToInt(Mathf.Lerp(5f, 10f, t));
        int maxMats = Mathf.RoundToInt(Mathf.Lerp(50f, 100f, t));

        return Random.Range(minMats, maxMats + 1);
    }

    private void RecolectarMaterial(GameObject materialObj)
    {
        int cantidadMateriales = CalcularCantidadMateriales();

        if (datosInventario != null)
        {
            datosInventario.AñadirMateriales(cantidadMateriales);
            Debug.Log($"Bridgmanita recolectada (+{cantidadMateriales}). Total: " + datosInventario.Materiales);

            TextoDañoFlotante.CrearMaterial(transform.position, cantidadMateriales);

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SoundID.RecolectarMaterial);
            }
        }
        else
        {
            Debug.LogWarning("DatosInventario no asignado en RecolecciónImán.");
        }
        
        Destroy(materialObj);
    }

    private void RecolectarCofre(GameObject cofreObj)
    {
        CofreController cofre = cofreObj.GetComponent<CofreController>();
        GameObject nuevaArma = cofre != null ? cofre.armaAsignada : null;

        if (nuevaArma != null && datosInventario != null)
        {
            bool equipadaDirecto = false;

            // 1. Intentar equipar en un slot libre de armas equipadas (Slots 1 a 4)
            for (int i = 0; i < datosInventario.armasEquipadas.Length; i++)
            {
                if (datosInventario.armasEquipadas[i] == null)
                {
                    datosInventario.armasEquipadas[i] = nuevaArma;
                    equipadaDirecto = true;
                    Debug.Log($"[Cofre] Arma {nuevaArma.name} equipada directamente en la ranura {i + 1}.");
                    break;
                }
            }

            // 2. Si las 4 ranuras equipadas están llenas, intentar guardar en la bolsa
            if (!equipadaDirecto)
            {
                for (int i = 0; i < datosInventario.bolsa.Length; i++)
                {
                    if (datosInventario.bolsa[i] == null)
                    {
                        datosInventario.bolsa[i] = nuevaArma;
                        Debug.Log($"[Cofre] Arma {nuevaArma.name} guardada en la bolsa general (Slot {i + 1}).");
                        break;
                    }
                }
            }

            // 3. Recargar el armamento físico del jugador si se equipó directamente
            if (equipadaDirecto && playerController != null)
            {
                playerController.RecargarArmasEquipadas();
            }

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SoundID.RecolectarMaterial);
            }
        }

        Destroy(cofreObj);
    }
}
