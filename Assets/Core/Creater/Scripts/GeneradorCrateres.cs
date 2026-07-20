using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.SceneManagement;
using Unity.Netcode;

[System.Serializable]
public struct DatosCrater
{
    public Vector2 posicion;
    public float radio;

    public DatosCrater(Vector2 posicion, float radio)
    {
        this.posicion = posicion;
        this.radio = radio;
    }
}

public class GeneradorCrateres : NetworkBehaviour
{
    public NetworkVariable<int> mapSeed = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [Header("Renderizado Separado (Capas)")]
    [SerializeField] private SpriteShapeController shapeBorde;
    [SerializeField] private SpriteShapeController shapeSuelo;
    
    [Header("Referencias de Gestion")]
    [SerializeField] private GestorTerraformacion gestorTerraformacion;
    
    private DatosCrater[] crateres;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // El servidor elige una semilla aleatoria para el mapa
            mapSeed.Value = Random.Range(1, 999999);
        }

        // Ambos (Servidor y Cliente) usan la misma semilla para generar el mismo mapa
        Random.InitState(mapSeed.Value);

        GenerarDatosMatematicos();
        InstanciarColisionadoresFisicos();
        StartCoroutine(DibujarBordeVisual());
        
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayMusic(SoundID.MusicaNivel);
        }
    }

    void Update()
    {
        // Reinicio rapido de la escena para pruebas de nivel
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void GenerarDatosMatematicos()
    {
        int cantidadCrateres = Random.Range(1, 11);
        
        // 20% de probabilidad: 1 crater central
        if (cantidadCrateres == 1 || cantidadCrateres == 2)
        {
            crateres = new DatosCrater[1];
            crateres[0] = new DatosCrater(Vector2.zero, Random.Range(12f, 15f));
        }
        // 20% de probabilidad: 2 crateres
        else if (cantidadCrateres == 3 || cantidadCrateres == 4)
        {
            crateres = new DatosCrater[2];
            float radioCentral = Random.Range(11f, 14f);
            crateres[0] = new DatosCrater(Vector2.zero, radioCentral);

            float radioSecundario = Random.Range(6f, 8f);
            float angulo = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector2 posicion = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * radioCentral;
            
            crateres[1] = new DatosCrater(posicion, radioSecundario);
        }
        // 30% de probabilidad: 3 crateres
        else if (cantidadCrateres >= 5 && cantidadCrateres <= 7)
        {
            crateres = new DatosCrater[3];
            float radioCentral = Random.Range(10f, 13f);
            crateres[0] = new DatosCrater(Vector2.zero, radioCentral);

            float radioSecundario = Random.Range(5f, 7f);
            float angulo2 = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector2 posSecundario = new Vector2(Mathf.Cos(angulo2), Mathf.Sin(angulo2)) * radioCentral;
            crateres[1] = new DatosCrater(posSecundario, radioSecundario);

            float radioTerciario = Random.Range(5f, 7f);
            Vector2 posTerciario = Vector2.zero;
            bool colisiona = true;
            int intentos = 0;

            // Bucle para evitar superposicion con el segundo crater
            while (colisiona && intentos < 1000)
            {
                float angulo3 = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                posTerciario = new Vector2(Mathf.Cos(angulo3), Mathf.Sin(angulo3)) * radioCentral;

                if (Vector2.Distance(posSecundario, posTerciario) >= (radioSecundario + radioTerciario))
                {
                    colisiona = false;
                }
                intentos++;
            }
            crateres[2] = new DatosCrater(posTerciario, radioTerciario);
        }
        // 30% de probabilidad: 4 crateres
        else
        {
            crateres = new DatosCrater[4];
            float radioCentral = Random.Range(9f, 12f);
            crateres[0] = new DatosCrater(Vector2.zero, radioCentral);

            float radioSecundario = Random.Range(5f, 7f);
            float angulo2 = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector2 posSecundario = new Vector2(Mathf.Cos(angulo2), Mathf.Sin(angulo2)) * radioCentral;
            crateres[1] = new DatosCrater(posSecundario, radioSecundario);

            float radioTerciario = Random.Range(5f, 7f);
            Vector2 posTerciario = Vector2.zero;
            bool colisionaTerciario = true;
            int intentos = 0;

            while (colisionaTerciario && intentos < 1000)
            {
                float angulo3 = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                posTerciario = new Vector2(Mathf.Cos(angulo3), Mathf.Sin(angulo3)) * radioCentral;

                if (Vector2.Distance(posSecundario, posTerciario) >= (radioSecundario + radioTerciario))
                {
                    colisionaTerciario = false;
                }
                intentos++;
            }
            crateres[2] = new DatosCrater(posTerciario, radioTerciario);

            float radioCuarto = Random.Range(5f, 7f);
            Vector2 posCuarto = Vector2.zero;
            bool colisionaCuarto = true;
            intentos = 0;

            while (colisionaCuarto && intentos < 1000)
            {
                float angulo4 = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                posCuarto = new Vector2(Mathf.Cos(angulo4), Mathf.Sin(angulo4)) * radioCentral;

                if (Vector2.Distance(posSecundario, posCuarto) >= (radioSecundario + radioCuarto) && 
                    Vector2.Distance(posTerciario, posCuarto) >= (radioTerciario + radioCuarto))
                {
                    colisionaCuarto = false;
                }
                intentos++;
            }
            crateres[3] = new DatosCrater(posCuarto, radioCuarto);
        }
    }

    private void InstanciarColisionadoresFisicos()
    {
        // Generamos los limites fisicos sin necesidad de prefabs visuales
        foreach (DatosCrater crater in crateres)
        {
            GameObject obj = new GameObject("ColisionadorCraterInterno");
            obj.transform.parent = transform;
            obj.transform.localPosition = new Vector3(crater.posicion.x, crater.posicion.y, 0f);
            
            CircleCollider2D circleCol = obj.AddComponent<CircleCollider2D>();
            circleCol.radius = crater.radio;
            
            // Requisito de la API moderna para que el CompositeCollider2D los fusione
            circleCol.compositeOperation = Collider2D.CompositeOperation.Merge;
        }
    }

    private IEnumerator DibujarBordeVisual()
    {
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();

        CompositeCollider2D compositeCollider = GetComponent<CompositeCollider2D>();

        if (compositeCollider == null || shapeBorde == null || shapeSuelo == null)
        {
            Debug.LogError("Cuidado: Faltan asignar componentes en el GeneradorCrateres.");
            yield break;
        }

        compositeCollider.GenerateGeometry();
        
        Spline splineBorde = shapeBorde.spline;
        Spline splineSuelo = shapeSuelo.spline;

        splineBorde.Clear();
        splineBorde.isOpenEnded = false;

        splineSuelo.Clear();
        splineSuelo.isOpenEnded = false;

        if (compositeCollider.pathCount > 0)
        {
            int cantidadPuntos = compositeCollider.GetPathPointCount(0);
            Vector2[] todosLosPuntos = new Vector2[cantidadPuntos];
            
            compositeCollider.GetPath(0, todosLosPuntos);
            
            List<Vector2> puntosFiltrados = new List<Vector2>();

            for (int i = 0; i < cantidadPuntos; i++)
            {
                Vector2 vertice = todosLosPuntos[i];

                if (puntosFiltrados.Count == 0)
                {
                    puntosFiltrados.Add(vertice);
                }
                else if (Vector2.Distance(vertice, puntosFiltrados[puntosFiltrados.Count - 1]) > 0.05f)
                {
                    puntosFiltrados.Add(vertice);
                }
            }

            if (puntosFiltrados.Count > 1 && 
                Vector2.Distance(puntosFiltrados[puntosFiltrados.Count - 1], puntosFiltrados[0]) <= 0.05f)
            {
                puntosFiltrados.RemoveAt(puntosFiltrados.Count - 1);
            }

            for (int i = 0; i < puntosFiltrados.Count; i++)
            {
                // Borde con curvas continuas
                splineBorde.InsertPointAt(i, puntosFiltrados[i]);
                splineBorde.SetTangentMode(i, ShapeTangentMode.Continuous);

                // Suelo con curvas lineales
                splineSuelo.InsertPointAt(i, puntosFiltrados[i]);
                splineSuelo.SetTangentMode(i, ShapeTangentMode.Linear);
            }

            shapeBorde.RefreshSpriteShape();
            shapeSuelo.RefreshSpriteShape();

            // Ciclo de limpieza de memoria para ambas capas
            shapeBorde.enabled = false;
            shapeSuelo.enabled = false;
            
            yield return null; 
            
            shapeBorde.enabled = true;
            shapeSuelo.enabled = true;
        }

        // Conexion directa con la terraformacion
        if (gestorTerraformacion != null)
        {
            gestorTerraformacion.GenerarPastoInicial(crateres);
        }
    }
}