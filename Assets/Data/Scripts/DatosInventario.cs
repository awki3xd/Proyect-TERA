using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NuevoInventario", menuName = "Datos/Inventario")]
public class DatosInventario : ScriptableObject
{
    [Header("Habilidad Especial")]
    [Tooltip("Ranura exclusiva para la habilidad tactica activa (ej. Dash, Bengala).")]
    public GameObject habilidadEspecial;

    [Header("Armas de Fuego (Automaticas)")]
    [Tooltip("Limite estricto de 4 armas que disparan en simultaneo.")]
    public GameObject[] armasEquipadas = new GameObject[4];

    [Header("Bolsa General")]
    [Tooltip("Inventario dinamico sin limite para acumular recursos o armamento inactivo.")]
    public GameObject[] bolsa = new GameObject[15];

    [Header("Economía")]
    [Tooltip("Cantidad de materiales (Bridgmanita) recolectados por el jugador.")]
    [SerializeField] private int materiales = 0;

    [Header("Estados de Juego")]
    [Tooltip("Indica si Génesis está actualmente reparando un nodo, lo que desactiva las ranuras 3 y 4.")]
    public bool estaReparando = false;

    // Propiedad pública encapsulada para acceder a los materiales (Solo lectura externa)
    public int Materiales => materiales;

    /// <summary>
    /// Añade materiales a la bolsa del inventario de forma segura.
    /// </summary>
    public void AñadirMateriales(int cantidad)
    {
        materiales += cantidad;
    }

    /// <summary>
    /// Asigna directamente la cantidad total de materiales desde el servidor/red.
    /// </summary>
    public void EstablecerMateriales(int cantidad)
    {
        materiales = Mathf.Max(0, cantidad);
    }

    /// <summary>
    /// Descuenta materiales si hay suficiente saldo disponible.
    /// Devuelve true si se pudo realizar la compra.
    /// </summary>
    public bool GastarMateriales(int cantidad)
    {
        if (materiales >= cantidad)
        {
            materiales -= cantidad;
            return true;
        }
        return false;
    }

    [ContextMenu("Resetear a Valores por Defecto")]
    public void ResetearAValoresPorDefecto()
    {
        habilidadEspecial = null;
        armasEquipadas = new GameObject[4];
        bolsa = new GameObject[15];
        materiales = 0;
        estaReparando = false;
    }
}