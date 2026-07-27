using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public enum SeccionUI
{
    Ninguna,
    Inventario,
    Equipado
}

public class BaseMenuController : MonoBehaviour
{
    [Header("Referencias de Datos (ScriptableObjects)")]
    [Tooltip("ScriptableObject del inventario (bolsa de 15 slots y 4 armas equipadas).")]
    [SerializeField] private DatosInventario inventarioSO;

    [Tooltip("ScriptableObject con las estadísticas del jugador.")]
    [SerializeField] private DatosPersonaje playerStatsSO;

    [Tooltip("ScriptableObject con las estadísticas globales de enemigos.")]
    [SerializeField] private DatosGlobalesEnemigos enemyStatsSO;

    [Tooltip("ScriptableObject con información del nivel/oleada.")]
    [SerializeField] private DatosNivel datosNivelSO;

    private UIDocument uiDocument;
    private VisualElement root;

    // --- ESTRUCTURAS DE LA INTERFAZ ---
    private HeaderUI headerUI = new HeaderUI();
    private PlayerStatsUI playerStatsUI = new PlayerStatsUI();
    private EnemyStatsUI enemyStatsUI = new EnemyStatsUI();
    private ShopUI shopUI = new ShopUI();

    // --- GRILLAS DE BOTONES ---
    private Button[] slotsInventario = new Button[15];
    private Button[] slotsEquipados = new Button[4];
    private Button btnListo;

    // --- VARIABLES DE CONTROL DE SELECCION ---
    private int indiceOrigen = -1;
    private SeccionUI seccionOrigen = SeccionUI.Ninguna;

    // --- VALORES Y COSTOS GENERADOS ALEATORIAMENTE PARA LA TIENDA ---
    private float valResistencia;
    private int costoResistencia;

    private float valDaño;
    private int costoDaño;

    private float valVelocidad;
    private int costoVelocidad;

    private float valAleatoriaVelDisp;
    private float valAleatoriaAlcance;
    private float valAleatoriaRegen;
    private int costoAleatoria;

    private void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        root = uiDocument.rootVisualElement;

        InicializarReferencias();
        RegistrarEventos();
        GenerarMejorasAleatorias();
        RefrescarPantalla();
    }

    private void OnDisable()
    {
        DesregistrarEventos();
    }

    private void InicializarReferencias()
    {
        // 1. Inicializar bloques de texto y estadisticas
        headerUI.Inicializar(root);
        playerStatsUI.Inicializar(root);
        enemyStatsUI.Inicializar(root);
        shopUI.Inicializar(root);

        // 2. Cargar los 4 slots de armas equipadas
        for (int i = 0; i < slotsEquipados.Length; i++)
        {
            slotsEquipados[i] = root.Q<Button>($"Equi-{i + 1}");
            if (slotsEquipados[i] != null)
            {
                slotsEquipados[i].userData = i;
            }
        }

        // 3. Cargar los 15 slots del inventario general
        for (int i = 0; i < slotsInventario.Length; i++)
        {
            slotsInventario[i] = root.Q<Button>($"Inv-{i + 1}");
            if (slotsInventario[i] != null)
            {
                slotsInventario[i].userData = i;
            }
        }

        // 4. Boton para avanzar de oleada
        btnListo = root.Q<Button>("Listo");
    }

    private void RegistrarEventos()
    {
        // Eventos para slots de equipadas
        for (int i = 0; i < slotsEquipados.Length; i++)
        {
            int index = i;
            if (slotsEquipados[i] != null)
            {
                slotsEquipados[i].clicked += () => OnSlotClicked(index, SeccionUI.Equipado);
            }
        }

        // Eventos para slots de inventario
        for (int i = 0; i < slotsInventario.Length; i++)
        {
            int index = i;
            if (slotsInventario[i] != null)
            {
                slotsInventario[i].clicked += () => OnSlotClicked(index, SeccionUI.Inventario);
            }
        }

        // Eventos de compra en la tienda
        if (shopUI.CardResistencia.BtnComprar != null)
            shopUI.CardResistencia.BtnComprar.clicked += () => OnComprarUpgrade(0);

        if (shopUI.CardDanio.BtnComprar != null)
            shopUI.CardDanio.BtnComprar.clicked += () => OnComprarUpgrade(1);

        if (shopUI.CardVelocidad.BtnComprar != null)
            shopUI.CardVelocidad.BtnComprar.clicked += () => OnComprarUpgrade(2);

        if (shopUI.CardAleatoria.BtnComprar != null)
            shopUI.CardAleatoria.BtnComprar.clicked += () => OnComprarUpgrade(3);

        // Evento boton listo
        if (btnListo != null)
        {
            btnListo.clicked += OnListoClicked;
        }
    }

    private void DesregistrarEventos()
    {
        if (btnListo != null)
        {
            btnListo.clicked -= OnListoClicked;
        }
    }

    // --- RECALCULO DINAMICO DE ESTADISTICAS DE ENEMIGOS ---

    private void RecalcularEstadisticasEnemigos()
    {
        if (enemyStatsSO == null || playerStatsSO == null) return;

        // Calcular incrementos del jugador por encima de su base (100%)
        float deltaArmadura = Mathf.Max(0f, playerStatsSO.armadura - 100f);
        float deltaDaño = Mathf.Max(0f, playerStatsSO.daño - 100f);
        float deltaVelMov = Mathf.Max(0f, playerStatsSO.velocidadMovimiento - 100f);
        float deltaVelAtaque = Mathf.Max(0f, playerStatsSO.velocidadAtaque - 100f);
        float deltaAlcance = Mathf.Max(0f, playerStatsSO.rangoAtaque - 100f);
        float deltaCuracion = Mathf.Max(0f, playerStatsSO.curacion - 100f);

        // Crecimiento promedio general del jugador
        float deltaPromedioTotal = (deltaArmadura + deltaDaño + deltaVelMov + deltaVelAtaque + deltaAlcance + deltaCuracion) / 6f;

        // Cada stat del enemigo escala 50% del crecimiento de su stat correspondiente directa + 25% del promedio total
        enemyStatsSO.vida = 100f + (deltaArmadura * 0.50f) + (deltaPromedioTotal * 0.25f);
        enemyStatsSO.daño = 100f + (deltaDaño * 0.50f) + (deltaPromedioTotal * 0.25f);
        enemyStatsSO.velocidadMovimiento = 100f + (deltaVelMov * 0.50f) + (deltaPromedioTotal * 0.25f);
        enemyStatsSO.velocidadAtaque = 100f + (deltaVelAtaque * 0.50f) + (deltaPromedioTotal * 0.25f);
    }

    // --- GENERACION ALEATORIA DE LA TIENDA ---

    private void GenerarMejorasAleatorias()
    {
        int nivel = datosNivelSO != null ? Mathf.Max(1, datosNivelSO.numeroNivel) : 1;
        
        // El costo por punto se escala con el nivel: Nivel 1 = 5 materiales por punto
        int costoPorPunto = 5 + (nivel - 1);

        // 1. Resistencia (Card 0)
        valResistencia = UnityEngine.Random.Range(1, 6);
        costoResistencia = Mathf.RoundToInt(valResistencia * costoPorPunto);

        // 2. Daño (Card 1)
        valDaño = UnityEngine.Random.Range(1, 6);
        costoDaño = Mathf.RoundToInt(valDaño * costoPorPunto);

        // 3. Velocidad de Movimiento (Card 2)
        valVelocidad = UnityEngine.Random.Range(1, 6);
        costoVelocidad = Mathf.RoundToInt(valVelocidad * costoPorPunto);

        // 4. Tarjeta Aleatoria Triple (Card 3)
        valAleatoriaVelDisp = UnityEngine.Random.Range(1, 6);
        valAleatoriaAlcance = UnityEngine.Random.Range(1, 6);
        valAleatoriaRegen = UnityEngine.Random.Range(1, 6);
        costoAleatoria = Mathf.RoundToInt((valAleatoriaVelDisp + valAleatoriaAlcance + valAleatoriaRegen) * costoPorPunto);
    }

    // --- METODOS DE ACTUALIZACION DE PANTALLA ---

    public void RefrescarPantalla()
    {
        RecalcularEstadisticasEnemigos();
        ActualizarHeader();
        ActualizarEstadisticasJugador();
        ActualizarEstadisticasEnemigos();
        ActualizarTienda();
        DibujarSlots();
    }

    private void ActualizarHeader()
    {
        if (headerUI.NombreJugador != null)
        {
            string nombre = PlayerPrefs.GetString("PlayerName", "Génesis");
            headerUI.NombreJugador.text = nombre;
        }

        if (headerUI.Oleada != null)
        {
            int oleada = datosNivelSO != null ? datosNivelSO.numeroNivel : 1;
            headerUI.Oleada.text = oleada.ToString();
        }

        if (headerUI.Materiales != null)
        {
            int mats = inventarioSO != null ? inventarioSO.Materiales : 0;
            headerUI.Materiales.text = mats.ToString();
        }
    }

    private void ActualizarEstadisticasJugador()
    {
        if (playerStatsSO == null) return;

        if (playerStatsUI.Resistencia != null)
            playerStatsUI.Resistencia.text = $"{playerStatsSO.armadura:F0}%";

        if (playerStatsUI.Danio != null)
            playerStatsUI.Danio.text = $"{playerStatsSO.daño:F0}%";

        if (playerStatsUI.VelMovimiento != null)
            playerStatsUI.VelMovimiento.text = $"{playerStatsSO.velocidadMovimiento:F0}%";

        if (playerStatsUI.VelDisparo != null)
            playerStatsUI.VelDisparo.text = $"{playerStatsSO.velocidadAtaque:F0}%";

        if (playerStatsUI.Alcance != null)
            playerStatsUI.Alcance.text = $"{playerStatsSO.rangoAtaque:F0}%";

        if (playerStatsUI.Regen != null)
            playerStatsUI.Regen.text = $"{playerStatsSO.curacion:F0}%";
    }

    private void ActualizarEstadisticasEnemigos()
    {
        if (enemyStatsSO == null) return;

        if (enemyStatsUI.Vida != null)
            enemyStatsUI.Vida.text = $"{enemyStatsSO.vida:F0}%";

        if (enemyStatsUI.VelMovimiento != null)
            enemyStatsUI.VelMovimiento.text = $"{enemyStatsSO.velocidadMovimiento:F0}%";

        if (enemyStatsUI.Danio != null)
            enemyStatsUI.Danio.text = $"{enemyStatsSO.daño:F0}%";

        if (enemyStatsUI.VelAtaque != null)
            enemyStatsUI.VelAtaque.text = $"{enemyStatsSO.velocidadAtaque:F0}%";
    }

    private void ActualizarTienda()
    {
        // Card Resistencia (Mejora 1)
        if (shopUI.CardResistencia.ValorOfrecido != null)
            shopUI.CardResistencia.ValorOfrecido.text = $"+{valResistencia:F0}%";

        if (shopUI.CardResistencia.Costo != null)
            shopUI.CardResistencia.Costo.text = $"{costoResistencia}";

        // Card Daño (Mejora 2)
        if (shopUI.CardDanio.ValorOfrecido != null)
            shopUI.CardDanio.ValorOfrecido.text = $"+{valDaño:F0}%";

        if (shopUI.CardDanio.Costo != null)
            shopUI.CardDanio.Costo.text = $"{costoDaño}";

        // Card Velocidad (Mejora 3)
        if (shopUI.CardVelocidad.ValorOfrecido != null)
            shopUI.CardVelocidad.ValorOfrecido.text = $"+{valVelocidad:F0}%";

        if (shopUI.CardVelocidad.Costo != null)
            shopUI.CardVelocidad.Costo.text = $"{costoVelocidad}";

        // Card Aleatoria (Mejora 4 Triple)
        if (shopUI.CardAleatoria.ValorVelDisparo != null)
            shopUI.CardAleatoria.ValorVelDisparo.text = $"+{valAleatoriaVelDisp:F0}%";

        if (shopUI.CardAleatoria.ValorAlcance != null)
            shopUI.CardAleatoria.ValorAlcance.text = $"+{valAleatoriaAlcance:F0}%";

        if (shopUI.CardAleatoria.ValorRegen != null)
            shopUI.CardAleatoria.ValorRegen.text = $"+{valAleatoriaRegen:F0}%";

        if (shopUI.CardAleatoria.Costo != null)
            shopUI.CardAleatoria.Costo.text = $"{costoAleatoria}";
    }

    private void DibujarSlots()
    {
        if (inventarioSO == null) return;

        // 1. Dibujar 4 Slots de Armas Equipadas
        for (int i = 0; i < slotsEquipados.Length; i++)
        {
            Button btn = slotsEquipados[i];
            if (btn == null) continue;

            GameObject prefab = (inventarioSO.armasEquipadas != null && i < inventarioSO.armasEquipadas.Length)
                ? inventarioSO.armasEquipadas[i]
                : null;

            AsignarSpriteASlot(btn, prefab);
        }

        // 2. Dibujar 15 Slots del Inventario General
        for (int i = 0; i < slotsInventario.Length; i++)
        {
            Button btn = slotsInventario[i];
            if (btn == null) continue;

            GameObject prefab = (inventarioSO.bolsa != null && i < inventarioSO.bolsa.Length)
                ? inventarioSO.bolsa[i]
                : null;

            AsignarSpriteASlot(btn, prefab);
        }
    }

    private void AsignarSpriteASlot(Button slotButton, GameObject prefab)
    {
        if (slotButton == null) return;

        if (prefab != null)
        {
            SpriteRenderer sr = prefab.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                slotButton.style.backgroundImage = Background.FromSprite(sr.sprite);
                slotButton.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            }
            else
            {
                slotButton.style.backgroundImage = null;
            }
        }
        else
        {
            slotButton.style.backgroundImage = null;
        }
    }

    // --- MANEJO DE SELECCION E INTERCAMBIO DE SLOTS ---

    private void OnSlotClicked(int index, SeccionUI seccion)
    {
        Button clickedButton = ObtenerBotonSlot(index, seccion);
        if (clickedButton == null) return;

        // CASO A: No hay origen seleccionado (Primer Clic)
        if (indiceOrigen == -1 || seccionOrigen == SeccionUI.Ninguna)
        {
            GameObject itemOrigen = GetPrefabAt(index, seccion);

            // Si el slot está vacío (null), ignorar el clic
            if (itemOrigen == null)
            {
                return;
            }

            // Seleccionar casilla de origen y aplicar borde brillante de selección
            indiceOrigen = index;
            seccionOrigen = seccion;
            clickedButton.AddToClassList("slot-selected");
        }
        // CASO B: Re-clic en la misma casilla exacta (Deseleccionar)
        else if (indiceOrigen == index && seccionOrigen == seccion)
        {
            DesmarcarTodosLosSlots();
            indiceOrigen = -1;
            seccionOrigen = SeccionUI.Ninguna;
        }
        // CASO C: Segundo Clic en casilla destino (Intercambio mediante transportista)
        else
        {
            // 1. Guardar referencia de origen en variable temporal transportista
            GameObject transportista = GetPrefabAt(indiceOrigen, seccionOrigen);
            GameObject itemDestino = GetPrefabAt(index, seccion);

            // 2. Realizar el swap en el ScriptableObject DatosInventario
            SetPrefabAt(indiceOrigen, seccionOrigen, itemDestino);
            SetPrefabAt(index, seccion, transportista);

            // 3. Limpiar selección y refrescar pantalla
            DesmarcarTodosLosSlots();
            indiceOrigen = -1;
            seccionOrigen = SeccionUI.Ninguna;

            RefrescarPantalla();
        }
    }

    private Button ObtenerBotonSlot(int index, SeccionUI seccion)
    {
        if (seccion == SeccionUI.Equipado && index >= 0 && index < slotsEquipados.Length)
        {
            return slotsEquipados[index];
        }
        else if (seccion == SeccionUI.Inventario && index >= 0 && index < slotsInventario.Length)
        {
            return slotsInventario[index];
        }
        return null;
    }

    private void DesmarcarTodosLosSlots()
    {
        for (int i = 0; i < slotsEquipados.Length; i++)
        {
            if (slotsEquipados[i] != null)
            {
                slotsEquipados[i].RemoveFromClassList("slot-selected");
            }
        }
        for (int i = 0; i < slotsInventario.Length; i++)
        {
            if (slotsInventario[i] != null)
            {
                slotsInventario[i].RemoveFromClassList("slot-selected");
            }
        }
    }

    private GameObject GetPrefabAt(int index, SeccionUI seccion)
    {
        if (inventarioSO == null) return null;

        if (seccion == SeccionUI.Equipado)
        {
            return (inventarioSO.armasEquipadas != null && index >= 0 && index < inventarioSO.armasEquipadas.Length)
                ? inventarioSO.armasEquipadas[index]
                : null;
        }
        else if (seccion == SeccionUI.Inventario)
        {
            return (inventarioSO.bolsa != null && index >= 0 && index < inventarioSO.bolsa.Length)
                ? inventarioSO.bolsa[index]
                : null;
        }
        return null;
    }

    private void SetPrefabAt(int index, SeccionUI seccion, GameObject prefab)
    {
        if (inventarioSO == null) return;

        if (seccion == SeccionUI.Equipado)
        {
            if (inventarioSO.armasEquipadas != null && index >= 0 && index < inventarioSO.armasEquipadas.Length)
            {
                inventarioSO.armasEquipadas[index] = prefab;
            }
        }
        else if (seccion == SeccionUI.Inventario)
        {
            if (inventarioSO.bolsa != null && index >= 0 && index < inventarioSO.bolsa.Length)
            {
                inventarioSO.bolsa[index] = prefab;
            }
        }
    }

    private void OnComprarUpgrade(int tipoMejora)
    {
        if (inventarioSO == null || playerStatsSO == null) return;

        switch (tipoMejora)
        {
            case 0: // Resistencia
                if (inventarioSO.GastarMateriales(costoResistencia))
                {
                    playerStatsSO.armadura += valResistencia;
                    Debug.Log($"Compra exitosa: +{valResistencia}% Resistencia por {costoResistencia} materiales.");
                    GenerarMejorasAleatorias();
                    RefrescarPantalla();
                }
                else
                {
                    Debug.LogWarning("Materiales insuficientes para comprar Resistencia.");
                }
                break;

            case 1: // Daño
                if (inventarioSO.GastarMateriales(costoDaño))
                {
                    playerStatsSO.daño += valDaño;
                    Debug.Log($"Compra exitosa: +{valDaño}% Daño por {costoDaño} materiales.");
                    GenerarMejorasAleatorias();
                    RefrescarPantalla();
                }
                else
                {
                    Debug.LogWarning("Materiales insuficientes para comprar Daño.");
                }
                break;

            case 2: // Velocidad
                if (inventarioSO.GastarMateriales(costoVelocidad))
                {
                    playerStatsSO.velocidadMovimiento += valVelocidad;
                    Debug.Log($"Compra exitosa: +{valVelocidad}% Velocidad por {costoVelocidad} materiales.");
                    GenerarMejorasAleatorias();
                    RefrescarPantalla();
                }
                else
                {
                    Debug.LogWarning("Materiales insuficientes para comprar Velocidad.");
                }
                break;

            case 3: // Aleatoria Triple
                if (inventarioSO.GastarMateriales(costoAleatoria))
                {
                    playerStatsSO.velocidadAtaque += valAleatoriaVelDisp;
                    playerStatsSO.rangoAtaque += valAleatoriaAlcance;
                    playerStatsSO.curacion += valAleatoriaRegen;
                    Debug.Log($"Compra exitosa: Triple mejora por {costoAleatoria} materiales.");
                    GenerarMejorasAleatorias();
                    RefrescarPantalla();
                }
                else
                {
                    Debug.LogWarning("Materiales insuficientes para comprar Mejora Aleatoria Triple.");
                }
                break;
        }
    }

    private void OnListoClicked()
    {
        Debug.Log("Confirmación de listo. Recargando armas del jugador y avanzando a la escena de gameplay 'Level'.");

        // 1. Buscar todos los PlayerController en la escena y recargar sus armas equipadas según el inventario actualizado
        PlayerController[] jugadores = FindObjectsOfType<PlayerController>();
        foreach (PlayerController player in jugadores)
        {
            if (player != null)
            {
                player.RecargarArmasEquipadas();
            }
        }

        // 2. Si NetworkManager no está escuchando (offline/singleplayer), iniciar Host local para que Netcode genere el PlayerPrefab
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
        {
            var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            if (transport != null)
            {
                transport.SetConnectionData("127.0.0.1", 7777);
            }
            NetworkManager.Singleton.StartHost();
        }

        // 3. Cargar la escena 'Level' mediante NetworkSceneManager para instanciar correctamente al jugador y los nodos
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null && NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("Level", LoadSceneMode.Single);
        }
        else
        {
            SceneManager.LoadScene("Level");
        }
    }

    // --- CLASES INTERNAS DE ORGANIZACION ---

    private class HeaderUI
    {
        public Label NombreJugador;
        public Label Oleada;
        public Label Materiales;

        public void Inicializar(VisualElement root)
        {
            NombreJugador = root.Q<Label>("Jugador");
            Oleada = root.Q<Label>("Oleada");
            Materiales = root.Q<Label>("Mat");
        }
    }

    private class PlayerStatsUI
    {
        public Label Resistencia;
        public Label Danio;
        public Label VelMovimiento;
        public Label VelDisparo;
        public Label Alcance;
        public Label Regen;

        public void Inicializar(VisualElement root)
        {
            Resistencia = root.Q<Label>("Stats-Resistencia");
            Danio = root.Q<Label>("Stats-Danio");
            VelMovimiento = root.Q<Label>("Stats-Mov");
            VelDisparo = root.Q<Label>("Stats-Dis");
            Alcance = root.Q<Label>("Stats-Alcance");
            Regen = root.Q<Label>("Stats-Rege");
        }
    }

    private class EnemyStatsUI
    {
        public Label Vida;
        public Label VelMovimiento;
        public Label Danio;
        public Label VelAtaque;

        public void Inicializar(VisualElement root)
        {
            Vida = root.Q<Label>("Stats-Ene-Vida");
            VelMovimiento = root.Q<Label>("Stats-Ene-Mov");
            Danio = root.Q<Label>("Stats-Ene-Danio");
            VelAtaque = root.Q<Label>("Stats-Ene-Dis");
        }
    }

    private class ShopUI
    {
        // Tres tarjetas estandar
        public UpgradeCardStandard CardResistencia = new UpgradeCardStandard();
        public UpgradeCardStandard CardDanio = new UpgradeCardStandard();
        public UpgradeCardStandard CardVelocidad = new UpgradeCardStandard();

        // Una tarjeta aleatoria con triple valor
        public UpgradeCardRandom CardAleatoria = new UpgradeCardRandom();

        public void Inicializar(VisualElement root)
        {
            CardResistencia.Inicializar(root, "Cant-Resistencia", "Cost-1", "Compra-1");
            CardDanio.Inicializar(root, "Cant-Danio", "Cost-2", "Compra-2");
            CardVelocidad.Inicializar(root, "Cant-Mov", "Cost-3", "Compra-3");

            CardAleatoria.Inicializar(
                root,
                "Cant-Dis",
                "Cant-Alcance",
                "Cant-Rege",
                "Cost-4",
                "Compra-4"
            );
        }
    }

    private class UpgradeCardStandard
    {
        public Label ValorOfrecido;
        public Label Costo;
        public Button BtnComprar;

        public void Inicializar(VisualElement root, string nameValor, string nameCosto, string nameBoton)
        {
            ValorOfrecido = root.Q<Label>(nameValor);
            Costo = root.Q<Label>(nameCosto);
            BtnComprar = root.Q<Button>(nameBoton);
        }
    }

    private class UpgradeCardRandom
    {
        public Label ValorVelDisparo;
        public Label ValorAlcance;
        public Label ValorRegen;
        public Label Costo;
        public Button BtnComprar;

        public void Inicializar(VisualElement root, string nameVelDisp, string nameAlcance, string nameRegen, string nameCosto, string nameBoton)
        {
            ValorVelDisparo = root.Q<Label>(nameVelDisp);
            ValorAlcance = root.Q<Label>(nameAlcance);
            ValorRegen = root.Q<Label>(nameRegen);
            Costo = root.Q<Label>(nameCosto);
            BtnComprar = root.Q<Button>(nameBoton);
        }
    }
}