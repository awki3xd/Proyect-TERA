using UnityEngine;

[CreateAssetMenu(fileName = "NuevosDatosGlobalesEnemigos", menuName = "Datos/Global Enemigos")]
public class DatosGlobalesEnemigos : ScriptableObject
{
    [Header("Supervivencia (Base 100 = 100%)")]
    public float vida = 100f;

    [Header("Movilidad")]
    public float velocidadMovimiento = 100f;

    [Header("Estadisticas de Combate")]
    public float daño = 100f;
    public float velocidadAtaque = 100f;

    [ContextMenu("Resetear a Valores por Defecto")]
    public void ResetearAValoresPorDefecto()
    {
        vida = 100f;
        velocidadMovimiento = 100f;
        daño = 100f;
        velocidadAtaque = 100f;
    }
}