using UnityEngine;

[CreateAssetMenu(fileName = "NuevosDatosArma", menuName = "Datos/Arma")]
public class DatosArma : ScriptableObject
{
    [Header("Identificación")]
    [Tooltip("Nombre de fantasía o clave de identificación para el arma.")]
    public string nombreArma;

    [Header("Estadísticas Base de Balance")]
    [Tooltip("Distancia de ataque del arma.")]
    public float rango;
    [Tooltip("Daño base infligido por disparo.")]
    public float daño;
    [Tooltip("Cadencia de disparo en balas por segundo.")]
    public float cadencia;
    [Tooltip("Velocidad de rotación en grados por segundo para apuntar al objetivo.")]
    public float velocidadRotacion;

    [Header("Prefabs e Instanciación")]
    [Tooltip("Prefab del proyectil (Bala) o zona de colisión que dispara esta arma.")]
    public GameObject prefabBala;
}
