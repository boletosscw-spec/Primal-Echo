using UnityEngine;
using UnityEngine.SceneManagement;

namespace PrimalEcho
{
    // ATLAS · el Tricerátops Bastión (jefe del Nivel 1, guarda la llave de la base).
    // Regla: "no le puedes pegar de frente". Su gola es un escudo que bloquea todo lo que llega por delante.
    //
    //  FASE 1 (100-60 %)  Embestida (rasca el suelo 2 veces como aviso). Si choca contra una COLUMNA queda
    //                     ATURDIDO 3 s con el lomo abierto (daño x2). Pisotón con ondas a ras de suelo.
    //                     Empujón con el escudo si Kael se le pega de frente. Girar le cuesta 1.2 s.
    //  FASE 2 (60-25 %)   Las estalactitas se agrietan: tras 2 choques ya no lo aturden. Lanza 3 rocas
    //                     (una de Eco), dispara anillos de energía (bajo = saltar, alto = quedarse en el suelo)
    //                     y hace doble embestida.
    //  FASE 3 (25-0 %)    El escudo estalla: ya se le puede pegar de frente, pero entra en FRENESÍ:
    //                     embestidas encadenadas que rebotan y fragmentos eléctricos en el suelo.
    //
    // Aparece solo en la escena que tiene la llave (KeyPickup). En el Nivel 1 duerme en la sala grande
    // de la cueva (x≈351, arena 337-365) y bloquea el paso hacia el refugio de Orin. Para elegir el sitio exacto, crea un GameObject vacío llamado
    // "Vista_Atlas" en la escena: el jefe aparecerá ahí.
    //
    // Hojas de sprites (una fila, mirando a la DERECHA) en Assets/Resources/Jefes/Atlas/:
    //   Idle(6) Caminar(8) Rascar(6) Embestida(6) Aturdido(6) Pisoton(8) Girar(5)
    //   Rocas(6) Anillo(5) EscudoRoto(6) Frenesi(6) Muerte(8)
    //   Opcionales: Roca.png, RocaEco.png, Fragmento.png, Estalactita.png, Escombro.png
    // Mientras falten, se ve un marcador de color y la pelea ya se puede probar.
    public sealed class Atlas : JefeBase
    {
        private enum Estado { Dormido, Rugido, Acechar, Rascar, Embestida, Aturdido, Frenar, Pisoton, Empujon, Girar, Rocas, Anillo, Transicion, Muerte }

        [Header("Arena")]
        [SerializeField, Min(6f)] private float mitadArena = 14f;
        [Header("Embestida")]
        [SerializeField] private float[] velocidadEmbestida = { 10.5f, 12f, 13.5f };
        [SerializeField, Min(0.5f)] private float segundosAturdido = 3f;
        [Header("Ritmo (descanso entre ataques por fase)")]
        [SerializeField] private float[] descanso = { 0.9f, 0.7f, 0.35f };
        [SerializeField] private float[] segundosGiro = { 1.2f, 0.9f, 0.35f };

        private Estado estado = Estado.Dormido;
        private float timer, duracion = 1f;
        private bool golpeHecho, anilloAlto, dobleEmbestida, avisoEscudo;
        private int dirEmbestida = 1, rebotesFrenesi, ultimoAtaque = -1;
        private float ultimoBloqueo;
        private readonly EstalactitaArena[] columnas = new EstalactitaArena[2];
        private readonly int[] choques = new int[2];
        private readonly bool[] rotas = new bool[2];
        private GameObject llave;
        private SpriteRenderer escudoFx, nucleoFx;

        protected override string Carpeta => "Atlas";
        protected override string NombreVisible => "ATLAS, EL TRICERÁTOPS BASTIÓN";
        protected override float AltoVisual => 3.6f; // hojas de 512x296 px por cuadro: el cuerpo mide ≈ 4 unidades de largo
        protected override Vector2 TamanoCuerpo => new Vector2(3.8f, 1.9f); // bajo: Kael lo puede saltar con doble salto
        protected override Color ColorMarcador => new Color(0.55f, 0.42f, 0.25f);
        protected override float[] Umbrales => new[] { 0.6f, 0.25f };
        protected override string NombreFase => fase == 3 ? "FRENESÍ" : "";
        protected override bool DanaAlContacto =>
            estado != Estado.Aturdido && estado != Estado.Transicion && estado != Estado.Dormido && estado != Estado.Muerte;

        private static readonly Vector2 PosicionNivel1 = new Vector2(351f, 3f);
        private static bool tutorialVisto;          // la cámara lenta solo sale en la primera embestida de la partida
        private bool enTutorial;
        private float tutorialInicio, fixedOriginal = 0.02f;
        private float atascado;

        private float ArenaMin => home.x - mitadArena;
        private float ArenaMax => home.x + mitadArena;
        private float Tempo => fase >= 3 ? 0.7f : fase == 2 ? 0.85f : 1f;

        // ---------------- Aparición automática ----------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Instalar()
        {
            IntentarAparecer();
            SceneManager.sceneLoaded += (_, __) => IntentarAparecer();
        }

        private static void IntentarAparecer()
        {
            if (FindAnyObjectByType<Atlas>(FindObjectsInactive.Include) != null) return;
            // Solo en el Nivel 1 (sala grande de la cueva: suelo plano x 330-372, arena 337-365)
            // o donde haya un marcador "Vista_Atlas". En ninguna otra escena.
            var marca = GameObject.Find("Vista_Atlas");
            if (marca == null && !SceneManager.GetActiveScene().name.Contains("Nivel_01")) return;
            var key = FindAnyObjectByType<KeyPickup>();
            if (marca == null && key == null) return;
            Vector2 spot = marca != null ? (Vector2)marca.transform.position : PosicionNivel1;
            var hit = Physics2D.Raycast(spot + Vector2.up * 3f, Vector2.down, 16f, 1 << TerrainLayer);
            if (hit.collider != null) spot = hit.point;

            // Atlas pelea solo: quita enemigos y emboscadas cercanas
            foreach (var e in FindObjectsByType<EnemyHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Vector2 p = e.transform.position;
                if (Mathf.Abs(p.x - spot.x) < 20f && Mathf.Abs(p.y - spot.y) < 9f) Destroy(e.gameObject);
            }
            foreach (var arena in FindObjectsByType<ArenaLock>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (Mathf.Abs(arena.transform.position.x - spot.x) < 24f) Destroy(arena.gameObject);

            var go = new GameObject("Atlas_Triceratops");
            go.transform.position = new Vector3(spot.x, spot.y + 0.02f, 0f);
            var atlas = go.AddComponent<Atlas>();
            // Si pelea junto a la llave, la llave no aparece hasta vencerlo
            if (key != null && Mathf.Abs(key.transform.position.x - spot.x) < 20f)
            {
                atlas.llave = key.gameObject;
                key.gameObject.SetActive(false);
            }
        }

        // ---------------- Ciclo de vida ----------------

        protected override void Awake()
        {
            maxHealth = 80;
            attackDamage = 1; // medio corazón menos por golpe: la pelea es larga, no de un error
            base.Awake();
            string[] nombres = { "Idle", "Caminar", "Rascar", "Embestida", "Aturdido", "Pisoton", "Girar", "Rocas", "Anillo", "EscudoRoto", "Frenesi", "Muerte" };
            int[] frames = { 6, 8, 6, 6, 6, 8, 5, 6, 5, 6, 6, 8 };
            for (int i = 0; i < nombres.Length; i++) CargarClip(nombres[i], frames[i]);

            // Efectos que solo se ven mientras se usa el marcador provisional
            escudoFx = CrearFx("Escudo", new Color(0.25f, 0.96f, 1f, 0.45f), new Vector3(2.4f, 2.3f, 1f), new Vector3(2.3f, 1.6f, 0f));
            nucleoFx = CrearFx("Nucleo", new Color(0.25f, 0.96f, 1f, 1f), new Vector3(1.1f, 1.1f, 1f), new Vector3(-0.6f, 2.9f, 0f));
        }

        private SpriteRenderer CrearFx(string nombre, Color c, Vector3 escala, Vector3 pos)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(sprite.transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = escala;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Circulo; sr.color = c; sr.sortingOrder = sprite.sortingOrder + 1;
            sr.enabled = false;
            return sr;
        }

        private void Cambiar(Estado e, float segundos)
        {
            if (e != estado)
            {
                if (e == Estado.Rascar) Sonido("atlas_rascar", 0.65f);
                else if (e == Estado.Aturdido) Sonido("atlas_aturdido", 0.6f);
            }
            estado = e; timer = duracion = Mathf.Max(0.05f, segundos); golpeHecho = false;
        }

        private float pasoT;
        private void Sonido(string nombre, float volumen, float tono = 1f) => Sonidos.Play(nombre, volumen, tono, transform.position);

        protected override void Despertar()
        {
            if (engaged || muerto) return;
            base.Despertar();
            facing = Kael.x >= transform.position.x ? 1 : -1;
            CrearColumnas();
            LevelHUD.Notify("ATLAS, el Tricerátops Bastión", 3f);
            Cambiar(Estado.Rugido, 1.4f);
            Sonido("atlas_rugido", 0.9f);
        }

        // ---------------- Tutorial: cámara lenta en la primera embestida ----------------

        private void IniciarTutorial()
        {
            tutorialVisto = true;
            enTutorial = true;
            tutorialInicio = Time.unscaledTime;
            fixedOriginal = Time.fixedDeltaTime;
            Time.timeScale = 0.15f;
            Time.fixedDeltaTime = fixedOriginal * Time.timeScale;
            BarraEco.PrepararDestello();
            BarraEco.Usado += AlUsarKiro;
            BarraEco.AvisoCentral = "¡Va a embestir! Presiona RB (o Q) para que Kiro suelte un DESTELLO";
            Vibracion.Pulso(0.35f, 0.25f, 0.35f);
        }

        private void AlUsarKiro() => TerminarTutorial(true);

        private void TerminarTutorial(bool usoDestello)
        {
            if (!enTutorial) return;
            enTutorial = false;
            BarraEco.Usado -= AlUsarKiro;
            BarraEco.AvisoCentral = null;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = fixedOriginal;
            if (usoDestello && !muerto)
            {
                Aturdir(2f); // en el tutorial siempre funciona, aunque Atlas esté un poco lejos
                LevelHUD.Notify("Kiro: ¡Lo paralicé! Aprovecha, su lomo quedó expuesto. El destello también rompe sus rocas", 5f);
            }
        }

        protected override void ReiniciarPelea()
        {
            TerminarTutorial(false);
            if (muerto) return;
            base.ReiniciarPelea();
            QuitarColumnas();
            estado = Estado.Dormido; rebotesFrenesi = 0; ultimoAtaque = -1;
        }

        protected override void OnDestroy()
        {
            TerminarTutorial(false);
            base.OnDestroy();
            QuitarColumnas();
        }

        // ---------------- Escudo: qué golpes entran ----------------

        protected override int FiltrarGolpe(int dano, Vector2 impulso)
        {
            if (muerto || EnTransicion || estado == Estado.Transicion) return 0;
            if (!engaged || BarraEco.PulsoActivo) return dano;
            if (estado == Estado.Aturdido) return dano * 2;            // núcleo expuesto
            if (fase >= 3) return dano;                                // escudo roto
            if (impulso.y < -0.5f * impulso.magnitude) return dano;    // desde arriba (pogo al lomo)
            if (Mathf.Abs(impulso.x) > 0.01f && (int)Mathf.Sign(impulso.x) == facing) return dano; // por detrás
            Bloqueado();
            return 0;
        }

        private void Bloqueado()
        {
            if (Time.time - ultimoBloqueo >= 0.08f) Sonido("atlas_escudo_bloqueo", 0.6f);
            if (Time.time - ultimoBloqueo < 0.08f) return;
            ultimoBloqueo = Time.time;
            Vector2 frente = (Vector2)transform.position + new Vector2(facing * (TamanoCuerpo.x / 2f + 0.2f), 1.8f);
            var fx = new GameObject("Chispa_Escudo");
            fx.transform.position = frente;
            var sr = fx.AddComponent<SpriteRenderer>();
            sr.sprite = Circulo; sr.color = new Color(0.6f, 1f, 1f, 0.9f); sr.sortingOrder = 14;
            fx.AddComponent<EfectoCrecer>().Init(1.8f, 0.15f);
            if (!avisoEscudo)
            {
                avisoEscudo = true;
                LevelHUD.Notify("Kiro: ¡Su escudo solo cubre el frente! Hazlo chocar o pégale por detrás", 4.5f);
            }
        }

        public override void Aturdir(float segundos)
        {
            if (muerto || !engaged || estado == Estado.Transicion) return;
            if (estado == Estado.Aturdido) { timer = Mathf.Max(timer, segundos); return; }
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            Cambiar(Estado.Aturdido, segundos);
        }

        protected override void AlCambiarFase(int nueva)
        {
            TerminarTutorial(false);
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            Cambiar(Estado.Transicion, SegundosTransicion);
            if (nueva == 2)
            {
                Sonido("atlas_rugido", 0.85f, 1.1f);
                choques[0] = choques[1] = 0;
                LevelHUD.Notify("Kiro: ¡Las estalactitas no aguantarán muchos golpes más!", 4f);
            }
            else if (nueva == 3)
            {
                Bounds b = box.bounds;
                for (int i = 0; i < 4; i++)
                {
                    float x = Mathf.Clamp(b.center.x + (i - 1.5f) * 3.2f, ArenaMin + 1.5f, ArenaMax - 1.5f);
                    Registrar(ProyectilesJefe.Fragmento(new Vector2(x, b.min.y), 4f, Carpeta));
                }
                Sacudida.Hacer(3f, 0.5f); // el escudo estalla
                Sonido("atlas_escudo_roto", 0.85f);
                Sonido("atlas_rugido", 0.8f, 1.2f);
                LevelHUD.Notify("Kiro: ¡Su escudo estalló! Ya puedes pegarle de frente... ¡pero está furioso!", 4.5f);
            }
        }

        protected override void Morir()
        {
            TerminarTutorial(false);
            base.Morir();
            body.linearVelocity = Vector2.zero;
            Cambiar(Estado.Muerte, 2.5f);
            Sacudida.Hacer(2.5f, 0.7f);
            Sonido("atlas_muerte", 0.9f);
            for (int i = 0; i < 2; i++) if (columnas[i] != null) columnas[i].Romper(1.4f + i * 0.25f); // se abren las salidas
            if (llave != null)
            {
                llave.SetActive(true); // el cristal vuelve a brillar en su pedestal
                LevelHUD.Notify("¡Atlas cayó! El cristal de la llave quedó libre", 4.5f);
            }
            else LevelHUD.Notify("¡Atlas cayó! Las estalactitas se derrumban: el camino está libre", 4.5f);
        }

        // ---------------- Combate ----------------

        private void FixedUpdate()
        {
            if (player == null) return;
            Bounds b = box.bounds;
            float dx = Kael.x - b.center.x;
            int haciaKael = dx >= 0f ? 1 : -1;
            float hueco = Mathf.Abs(dx) - b.extents.x - (playerCollider != null ? playerCollider.bounds.extents.x : 0.3f);
            timer -= Time.fixedDeltaTime;
            float p = 1f - Mathf.Clamp01(timer / duracion);
            float vx = 0f;

            switch (estado)
            {
                case Estado.Dormido:
                    if (Mathf.Abs(dx) < mitadArena - 2f && Mathf.Abs(Kael.y - b.min.y) < 5f) Despertar();
                    break;

                case Estado.Rugido:
                    if (timer <= 0f) Cambiar(Estado.Acechar, descanso[0]);
                    break;

                case Estado.Transicion:
                    if (timer <= 0f) Cambiar(Estado.Acechar, 0.3f);
                    break;

                case Estado.Acechar:
                    // Kael se le puso detrás: le cuesta girarse (ventana para pegarle al lomo)
                    if (dx * facing < -1.2f) { Cambiar(Estado.Girar, segundosGiro[fase - 1]); break; }
                    if (hueco > 4f && fase == 1 && HaySuelo(b.center.x + facing * (b.extents.x + 0.4f), b.min.y))
                    {
                        vx = facing * 2f; // se acerca despacio
                        pasoT -= Time.fixedDeltaTime;
                        if (pasoT <= 0f) { pasoT = 0.5f; Sonido("atlas_paso", 0.35f); }
                    }
                    if (timer > 0f) break;
                    ElegirAtaque(hueco);
                    break;

                case Estado.Girar:
                    if (timer <= 0f)
                    {
                        if (dx * facing < 0f) facing = -facing;
                        Cambiar(Estado.Acechar, 0.25f);
                    }
                    break;

                case Estado.Rascar: // aviso: rasca el suelo, sigue a Kael con la mirada y fija la dirección
                    if (!tutorialVisto && fase == 1 && p > 0.3f) IniciarTutorial();
                    if (enTutorial)
                    {
                        timer += Time.fixedDeltaTime; // se queda en el aviso hasta que Kael use el destello
                        if (Time.unscaledTime - tutorialInicio > 7f) TerminarTutorial(false);
                    }
                    if (p < 0.75f && fase < 3 && Mathf.Abs(dx) > 1.2f) facing = haciaKael;
                    if (timer <= 0f) { dirEmbestida = facing; Cambiar(Estado.Embestida, 6f); }
                    break;

                case Estado.Embestida:
                    pasoT -= Time.fixedDeltaTime;
                    if (pasoT <= 0f) { pasoT = 0.2f; Sonido("atlas_paso", 0.5f, 1.1f); }
                    vx = dirEmbestida * velocidadEmbestida[fase - 1];
                    Golpear(b, attackDamage);
                    // Borde de la arena, pared del mapa o se quedó atorado: termina la embestida
                    bool pared = Physics2D.Raycast(b.center, Vector2.right * dirEmbestida, b.extents.x + 0.25f, 1 << TerrainLayer);
                    // Atorado (contra la columna o algo del mapa) aunque no haya llegado justo al límite
                    atascado = duracion - timer > 0.2f && Mathf.Abs(body.linearVelocity.x) < 1.5f ? atascado + Time.fixedDeltaTime : 0f;
                    if (LlegoAlBorde(b, dirEmbestida) || timer <= 0f) { vx = 0f; atascado = 0f; AlLlegarAlBorde(); }
                    else if (atascado > 0.08f)
                    {
                        vx = 0f; atascado = 0f;
                        if (CercaDelBorde(b, dirEmbestida)) AlLlegarAlBorde(); // pegado a la estalactita
                        else Cambiar(Estado.Frenar, 0.3f);                    // algo raro a media arena: solo frena
                    }
                    else if (pared) { vx = 0f; Cambiar(Estado.Frenar, 0.3f); }
                    break;

                case Estado.Aturdido:
                    if (timer <= 0f) Cambiar(Estado.Acechar, descanso[fase - 1]);
                    break;

                case Estado.Frenar:
                    vx = dirEmbestida * velocidadEmbestida[fase - 1] * Mathf.Clamp01(timer / duracion) * 0.4f;
                    if (LlegoAlBorde(b, dirEmbestida)) vx = 0f;
                    if (timer > 0f) break;
                    if (dobleEmbestida)
                    {
                        dobleEmbestida = false;
                        facing = -dirEmbestida;
                        Cambiar(Estado.Rascar, 0.35f);
                    }
                    else Cambiar(Estado.Acechar, descanso[fase - 1]);
                    break;

                case Estado.Pisoton:
                    if (!golpeHecho && p >= 0.62f)
                    {
                        golpeHecho = true;
                        float alto = 0.9f;
                        Registrar(ProyectilesJefe.Onda(new Vector2(b.min.x, b.min.y), -1, 9f, alto, ArenaMin, Carpeta));
                        Registrar(ProyectilesJefe.Onda(new Vector2(b.max.x, b.min.y), 1, 9f, alto, ArenaMax, Carpeta));
                        if (fase >= 3)
                        {
                            Registrar(ProyectilesJefe.Fragmento(new Vector2(Mathf.Clamp(Kael.x - 2f, ArenaMin + 1f, ArenaMax - 1f), b.min.y), 3f, Carpeta));
                            Registrar(ProyectilesJefe.Fragmento(new Vector2(Mathf.Clamp(Kael.x + 2f, ArenaMin + 1f, ArenaMax - 1f), b.min.y), 3f, Carpeta));
                        }
                        Sacudida.Hacer(1.6f, 0.3f); // pisotón
                        Sonido("atlas_pisoton", 0.85f);
                    }
                    if (timer <= 0f) Cambiar(Estado.Acechar, descanso[fase - 1]);
                    break;

                case Estado.Empujon: // empuja con la gola (sin daño): castiga quedarse pegado al frente
                    if (!golpeHecho && p >= 0.55f)
                    {
                        golpeHecho = true;
                        var zona = new Bounds(new Vector3(b.center.x + facing * (b.extents.x + 0.9f), b.min.y + 1.2f, 0f), new Vector3(2.2f, 2.6f, 10f));
                        if (playerCollider != null && zona.Intersects(playerCollider.bounds) && playerController != null)
                            playerController.ApplyImpulse(new Vector2(facing * 11f, 6f), 0.3f);
                        Sonido("atlas_paso", 0.6f, 0.9f);
                    }
                    if (timer <= 0f) Cambiar(Estado.Acechar, descanso[fase - 1] * 0.6f);
                    break;

                case Estado.Rocas:
                    if (!golpeHecho && p >= 0.5f) { golpeHecho = true; LanzarRocas(b); }
                    if (timer <= 0f) Cambiar(Estado.Acechar, descanso[fase - 1]);
                    break;

                case Estado.Anillo:
                    if (!golpeHecho && p >= 0.6f) { golpeHecho = true; DispararAnillo(b); }
                    if (timer <= 0f) Cambiar(Estado.Acechar, descanso[fase - 1] * 0.7f);
                    break;

                case Estado.Muerte:
                    if (timer <= 0f && box.enabled)
                    {
                        box.enabled = false;
                        foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
                        body.bodyType = RigidbodyType2D.Kinematic;
                        body.linearVelocity = Vector2.zero;
                    }
                    break;
            }

            if (estado != Estado.Muerte) body.linearVelocity = new Vector2(vx, body.linearVelocity.y);
        }

        private void ElegirAtaque(float hueco)
        {
            // Pegado al frente: empujón (con escudo) o pisotón
            if (hueco < 1.4f) { Cambiar(fase < 3 ? Estado.Empujon : Estado.Pisoton, fase < 3 ? 0.75f : 1f * Tempo); return; }

            // Tabla de pesos por fase: Embestida, Pisotón, Rocas, Anillo
            int[] pesos = fase == 1 ? new[] { 60, 40, 0, 0 } : fase == 2 ? new[] { 35, 15, 25, 25 } : new[] { 55, 20, 25, 0 };
            if (ultimoAtaque >= 0 && pesos[ultimoAtaque] > 0) pesos[ultimoAtaque] /= 3; // no repetir tanto
            int total = 0; foreach (int w in pesos) total += w;
            int r = Random.Range(0, total), elegido = 0;
            for (int i = 0; i < pesos.Length; i++) { if (r < pesos[i]) { elegido = i; break; } r -= pesos[i]; }
            ultimoAtaque = elegido;

            switch (elegido)
            {
                case 0:
                    dobleEmbestida = fase == 2 && Random.value < 0.5f;
                    rebotesFrenesi = fase >= 3 ? 3 : 0;
                    Cambiar(Estado.Rascar, (fase >= 3 ? 0.55f : fase == 2 ? 0.8f : 1.0f)); // aviso más largo para reaccionar
                    break;
                case 1: Cambiar(Estado.Pisoton, 1f * Tempo); break;
                case 2: Cambiar(Estado.Rocas, 1.1f * Tempo); break;
                default: Cambiar(Estado.Anillo, 1f * Tempo); break;
            }
        }

        private bool LlegoAlBorde(Bounds b, int dir)
        {
            float limite = dir > 0 ? ArenaMax - 0.6f - b.extents.x : ArenaMin + 0.6f + b.extents.x; // justo antes de la estalactita
            if ((b.center.x - limite) * dir >= 0f) return true;
            return !HaySuelo(b.center.x + dir * (b.extents.x + 0.4f), b.min.y); // borde del suelo
        }

        private bool CercaDelBorde(Bounds b, int dir)
        {
            float limite = dir > 0 ? ArenaMax - 0.6f - b.extents.x : ArenaMin + 0.6f + b.extents.x;
            return Mathf.Abs(b.center.x - limite) < 1.5f;
        }

        private void AlLlegarAlBorde()
        {
            int lado = dirEmbestida > 0 ? 1 : 0;
            bool columna = columnas[lado] != null && !rotas[lado] && fase < 3;
            if (columna)
            {
                Sacudida.Hacer(2.4f, 0.35f); // se estrella contra la estalactita
                Sonido("atlas_choque", 0.9f);
                if (fase >= 2 && ++choques[lado] >= 2) RomperColumna(lado);
                dobleEmbestida = false;
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
                Cambiar(Estado.Aturdido, segundosAturdido);
                return;
            }
            if (fase >= 3 && rebotesFrenesi-- > 0)
            {
                // Frenesí: rebota en el borde y vuelve a cargar casi sin pausa
                facing = dirEmbestida = -dirEmbestida;
                Cambiar(Estado.Rascar, 0.3f);
                return;
            }
            Cambiar(Estado.Frenar, 0.5f);
        }

        private void LanzarRocas(Bounds b)
        {
            Sonido("atlas_lanzar", 0.7f);
            Vector2 desde = new Vector2(b.center.x + facing * (b.extents.x - 0.3f), b.max.y + 0.3f);
            int eco = Random.Range(0, 3);
            for (int i = 0; i < 3; i++)
            {
                float tx = Mathf.Clamp(Kael.x + (i - 1) * 2.4f, ArenaMin + 1f, ArenaMax - 1f);
                float ty = b.min.y;
                bool esEco = i == eco;
                float g = Physics2D.gravity.magnitude * (esEco ? 1.6f : 2.2f);
                float t = 1.0f + i * 0.12f;
                var vel = new Vector2((tx - desde.x) / t, (ty - desde.y + 0.5f * g * t * t) / t);
                Registrar(ProyectilesJefe.Roca(desde, vel, esEco, Carpeta));
            }
        }

        private void DispararAnillo(Bounds b)
        {
            Sonido("atlas_anillo", 0.65f);
            float suelo = b.min.y;
            anilloAlto = !anilloAlto;
            // Bajo: hay que saltarlo. Alto: pasa por encima de la cabeza de Kael si se queda en el suelo.
            float y = anilloAlto ? suelo + AltoKael + 0.6f : suelo + 0.45f;
            float x = b.center.x + facing * (b.extents.x + 0.3f);
            float limite = facing > 0 ? ArenaMax : ArenaMin;
            Registrar(ProyectilesJefe.Anillo(new Vector2(x, y), facing, 8.5f, 0.9f, limite));
        }

        // ---------------- Estalactitas que cierran la arena ----------------

        private void CrearColumnas()
        {
            QuitarColumnas();
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? ArenaMin : ArenaMax;
                float pie = SueloBajo(new Vector2(x, transform.position.y + 3f), 10f);
                columnas[i] = EstalactitaArena.Crear(x, pie, Carpeta, 0.15f + i * 0.35f);
                choques[i] = 0; rotas[i] = false;
            }
        }

        // Fase 2: tras 2 choques se agrieta y ya no lo aturde (sigue cerrando la arena)
        private void RomperColumna(int i)
        {
            rotas[i] = true;
            if (columnas[i] != null) columnas[i].Agrietar();
            LevelHUD.Notify("Kiro: ¡Se agrietó! Ya no lo va a detener... tendrás que saltarlo", 3.5f);
        }

        private void QuitarColumnas()
        {
            for (int i = 0; i < 2; i++)
            {
                if (columnas[i] != null) Destroy(columnas[i].gameObject);
                columnas[i] = null;
            }
        }

        // ---------------- Animación ----------------

        protected override Color ColorBase()
        {
            bool aviso = estado == Estado.Rascar || (estado == Estado.Pisoton && timer / duracion > 0.4f) || estado == Estado.Anillo || estado == Estado.Empujon;
            if (aviso) return new Color(1f, 0.72f, 0.62f, 1f); // ojos/cuerpo hacia rojo: va a atacar
            if (fase >= 3 && !muerto) return new Color(1f, 0.88f, 0.84f, 1f);
            return Color.white;
        }

        private void Update()
        {
            if (sprite == null) return;
            float p = 1f - Mathf.Clamp01(timer / duracion);
            switch (estado)
            {
                case Estado.Dormido: Mostrar("Idle", -1f, 5f); break;
                case Estado.Rugido: case Estado.Rascar: Mostrar("Rascar", -1f, 10f); break;
                case Estado.Acechar: Mostrar(Mathf.Abs(body.linearVelocity.x) > 0.3f ? "Caminar" : "Idle", -1f, 8f); break;
                case Estado.Girar: Mostrar("Girar", p); break;
                case Estado.Embestida: Mostrar(fase >= 3 ? "Frenesi" : "Embestida", -1f, 14f); break;
                case Estado.Frenar: Mostrar("Idle", -1f, 8f); break;
                case Estado.Aturdido: Mostrar("Aturdido", -1f, 8f); break;
                case Estado.Pisoton: Mostrar("Pisoton", p); break;
                case Estado.Empujon: Mostrar("Embestida", -1f, 10f); break;
                case Estado.Rocas: Mostrar("Rocas", p); break;
                case Estado.Anillo: Mostrar("Anillo", p); break;
                case Estado.Transicion: Mostrar(fase >= 3 ? "EscudoRoto" : "Rascar", fase >= 3 ? p : -1f, 10f); break;
                case Estado.Muerte: Mostrar("Muerte", p); break;
            }

            // Marcador provisional: escudo y núcleo dibujados encima para que se entienda la pelea
            escudoFx.enabled = UsandoMarcador && engaged && fase < 3 && !muerto;
            nucleoFx.enabled = UsandoMarcador && estado == Estado.Aturdido;
            if (nucleoFx.enabled) nucleoFx.transform.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(Time.time * 12f));
        }
    }
}
