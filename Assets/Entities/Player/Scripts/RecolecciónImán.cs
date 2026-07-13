using System.Collections.Generic;
using UnityEngine;

public class RecolecciónImán : MonoBehaviour
{
    [Header("Referencias a Datos")]
    [Tooltip("Inventario de Génesis donde se guardarán los materiales recolectados.")]
    public DatosInventario datosInventario;

    [Header("Configuración de Atracción")]
    [Tooltip("Velocidad con la que se atraen los cristales de Bridgmanita.")]
    public float velocidadAtraccion = 5f;

    private List<Transform> materialesEnRango = new List<Transform>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Detectar si el objeto colisionador es un mineral/cristal recolectable
        if (other.CompareTag("Material"))
        {
            if (!materialesEnRango.Contains(other.transform))
            {
                materialesEnRango.Add(other.transform);
                Debug.Log("Material agregado a la lista de atracción. Total: " + materialesEnRango.Count);
            }
        }
    }

    private void Update()
    {
        // Atracción magnética en cada frame hacia la posición de ESTE transform (el imán del jugador)
        for (int i = materialesEnRango.Count - 1; i >= 0; i--)
        {
            Transform material = materialesEnRango[i];
            
            // Si el objeto fue destruido o recogido por otro lado, limpiamos la lista
            if (material == null)
            {
                materialesEnRango.RemoveAt(i);
                continue;
            }

            // Mueve el material suavemente en dirección al imán (usando transform.position)
            material.position = Vector2.MoveTowards(material.position, transform.position, velocidadAtraccion * Time.deltaTime);

            // Si está muy cerca de la posición del imán, recolectarlo
            float distancia = Vector2.Distance(material.position, transform.position);
            if (distancia < 0.2f)
            {
                Recolectar(material.gameObject);
                materialesEnRango.RemoveAt(i);
            }
        }
    }

    private void Recolectar(GameObject materialObj)
    {
        if (datosInventario != null)
        {
            // Incrementar los materiales directamente a través de la referencia del ScriptableObject
            datosInventario.AñadirMateriales(1);
            Debug.Log("Bridgmanita recolectada en el Inventario. Materiales totales: " + datosInventario.Materiales);

            // Reproducir sonido de recolección de materiales
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SoundID.RecolectarMaterial);
            }
        }
        else
        {
            Debug.LogWarning("DatosInventario no asignado en RecolecciónImán.");
        }
        
        // Destruir el cristal del mapa
        Destroy(materialObj);
    }
}
