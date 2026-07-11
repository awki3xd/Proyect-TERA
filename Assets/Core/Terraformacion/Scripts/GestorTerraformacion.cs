using UnityEngine;
using UnityEngine.U2D;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteShapeController))]
public class GestorTerraformacion : MonoBehaviour
{
    [Header("Referencias Externas")]
    [Tooltip("Arrastra aca el objeto que tiene el script GeneradorNodos")]
    [SerializeField] private GeneradorNodos generadorNodos;
    [Tooltip("Arrastra aca el ScriptableObject con los datos del nivel")]
    [SerializeField] private DatosNivel datosNivel;

    [Header("Configuracion Inicial")]
    [Range(0.01f, 1f)]
    public float escalaInicial = 0.10f;

    [Header("Parametros de Expansion")]
    public float distanciaPorSalto = 0.5f;
    public float tiempoBaseLatido = 1.0f;
    
    private SpriteShapeController shapePasto;
    private Vector2[] puntosOriginales;
    
    public float porcentajeActual;
    private float radioPromedio;

    private NodoEstandar[] nodosAsignados;
    private float valorPorNodo;

    private void Awake()
    {
        shapePasto = GetComponent<SpriteShapeController>();
    }

    public void GenerarPastoInicial(DatosCrater[] crateresBase)
    {
        if (crateresBase == null || crateresBase.Length == 0) return;
        
        if (generadorNodos != null)
        {
            nodosAsignados = generadorNodos.NodosCreados;
            
            int totalNodos = nodosAsignados != null ? nodosAsignados.Length : 0;
            
            if (totalNodos <= 1)
            {
                valorPorNodo = 1.0f;
            }
            else if (totalNodos == 2)
            {
                valorPorNodo = 2f / 3f; 
            }
            else 
            {
                valorPorNodo = 0.5f;
            }
        }
        else
        {
            Debug.LogWarning("Falta asignar el GeneradorNodos en el gestor de terraformacion.");
        }
        
        StartCoroutine(CalcularFormaOculta(crateresBase));
    }

    private IEnumerator CalcularFormaOculta(DatosCrater[] crateresBase)
    {
        GameObject nodoFisico = new GameObject("CalculadoraFantasma");
        nodoFisico.transform.position = Vector3.zero;
        
        Rigidbody2D rb = nodoFisico.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        
        CompositeCollider2D composite = nodoFisico.AddComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;

        for (int i = 0; i < crateresBase.Length; i++)
        {
            GameObject circuloTemp = new GameObject("Circulo");
            circuloTemp.transform.parent = nodoFisico.transform;
            circuloTemp.transform.localPosition = crateresBase[i].posicion;
            
            CircleCollider2D circleCol = circuloTemp.AddComponent<CircleCollider2D>();
            circleCol.compositeOperation = Collider2D.CompositeOperation.Merge;
            
            if (i == 0)
            {
                circleCol.radius = crateresBase[i].radio * 0.93f;
            }
            else
            {
                circleCol.radius = crateresBase[i].radio * 0.87f; 
            }
        }

        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();
        composite.GenerateGeometry();

        if (composite.pathCount == 0)
        {
            Debug.LogError("El CompositeCollider2D no pudo generar la fusion geometrica.");
            Destroy(nodoFisico);
            yield break;
        }

        int cantidadPuntos = composite.GetPathPointCount(0);
        puntosOriginales = new Vector2[cantidadPuntos];
        composite.GetPath(0, puntosOriginales);

        Destroy(nodoFisico);

        float sumaDistancias = 0f;
        for (int i = 0; i < puntosOriginales.Length; i++)
        {
            sumaDistancias += puntosOriginales[i].magnitude;
        }
        radioPromedio = sumaDistancias / cantidadPuntos;

        porcentajeActual = escalaInicial;
        DibujarPasto(porcentajeActual);

        StartCoroutine(RutinaExpansion());
    }

    private void DibujarPasto(float escala)
    {
        Spline spline = shapePasto.spline;
        spline.Clear();
        spline.isOpenEnded = false;

        List<Vector2> puntosFiltrados = new List<Vector2>();

        for (int i = 0; i < puntosOriginales.Length; i++)
        {
            Vector2 puntoAchicado = puntosOriginales[i] * escala;

            if (puntosFiltrados.Count == 0)
            {
                puntosFiltrados.Add(puntoAchicado);
            }
            else if (Vector2.Distance(puntoAchicado, puntosFiltrados[puntosFiltrados.Count - 1]) > 0.05f)
            {
                puntosFiltrados.Add(puntoAchicado);
            }
        }

        if (puntosFiltrados.Count > 1 && 
            Vector2.Distance(puntosFiltrados[puntosFiltrados.Count - 1], puntosFiltrados[0]) <= 0.05f)
        {
            puntosFiltrados.RemoveAt(puntosFiltrados.Count - 1);
        }

        for (int i = 0; i < puntosFiltrados.Count; i++)
        {
            spline.InsertPointAt(i, puntosFiltrados[i]);
            spline.SetTangentMode(i, ShapeTangentMode.Continuous);
        }

        shapePasto.RefreshSpriteShape();
    }

    private IEnumerator RutinaExpansion()
    {
        yield return null;
        shapePasto.enabled = false;
        yield return null;
        shapePasto.enabled = true;

        while (porcentajeActual < 1f)
        {
            float sumaActivos = 0f;
            int totalNodosInstanciados = 0;
            
            if (nodosAsignados != null)
            {
                totalNodosInstanciados = nodosAsignados.Length;
                for (int i = 0; i < totalNodosInstanciados; i++)
                {
                    if (nodosAsignados[i] != null && nodosAsignados[i].EstaFuncionando())
                    {
                        sumaActivos += valorPorNodo;
                    }
                }
            }

            if (sumaActivos > 0f)
            {
                // Crecimiento normal hacia adelante
                float tiempoEspera = tiempoBaseLatido / sumaActivos;

                yield return new WaitForSeconds(tiempoEspera);

                float avancePorcentualPorSalto = distanciaPorSalto / radioPromedio;
                porcentajeActual += avancePorcentualPorSalto;

                if (porcentajeActual >= 1f)
                {
                    porcentajeActual = 1f;
                }

                DibujarPasto(porcentajeActual);
            }
            else
            {
                // Evaluamos que sucede cuando el nivel de defensas cae a cero
                bool esModoExtremo = datosNivel != null && datosNivel.modoExtremo;

                if (esModoExtremo && totalNodosInstanciados > 0)
                {
                    // Forzamos la velocidad al ritmo de las maquinas maximas
                    float sumaMaximaActivos = totalNodosInstanciados * valorPorNodo;
                    float tiempoEsperaRetroceso = tiempoBaseLatido / sumaMaximaActivos;

                    yield return new WaitForSeconds(tiempoEsperaRetroceso);

                    // Restamos la distancia para que el mapa se encoja
                    float retrocesoPorcentualPorSalto = distanciaPorSalto / radioPromedio;
                    porcentajeActual -= retrocesoPorcentualPorSalto;

                    // Evitamos perforar el limite minimo del charco de inicio
                    if (porcentajeActual <= escalaInicial)
                    {
                        porcentajeActual = escalaInicial;
                    }

                    DibujarPasto(porcentajeActual);
                }
                else
                {
                    // Comportamiento regular: pausa hasta que el jugador repare
                    yield return new WaitForSeconds(0.5f);
                }
            }
        }
    }
}