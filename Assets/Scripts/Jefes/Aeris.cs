using UnityEngine;
using UnityEngine.SceneManagement;

namespace PrimalEcho
{
    // AERIS · el Pteranodón Vigía (jefe del Nivel 2, arena alta dentro de la base).
    // Regla: "pelea en vertical". Vuela arriba de la arena; el arco en diagonal es la mejor arma.
    //
    //  FASE 1 (100-65 %)  PICADA en diagonal: se va a una esquina de arriba, la lente marca la trayectoria
    //                     con un láser rojo (aviso) y se lanza. Al llegar queda CLAVADO 1.4 s (ventana para la lanza).
    //                     HUEVOS-MINA: cruza volando y suelta 3 huevos que explotan al rato (uno es de Eco).
    //                     PLUMAS en abanico apuntadas a Kael.
    //  FASE 2 (65-30 %)   Agrega VIENTO (aletea y empuja a Kael; hay que caminar en contra) y RAYO a ras de suelo
    //                     (se salta o se esquiva subiendo a las rejillas). Plumas de 5.
    //  FASE 3 (30-0 %)    Ala dañada: vuela errático y hace PICADAS DOBLES.
    // El Destello de Kiro lo derriba al suelo un momento (daño x1.5) y rompe huevos y plumas.
    //
    // Aparece en el GameObject "Vista_Aeris" (lo pone el constructor del Nivel 2). Al entrar a la arena caen dos
    // compuertas blindadas que se rompen al vencerlo.
    // Hojas de sprites (una fila, mirando a la DERECHA) en Assets/Resources/Jefes/Aeris/:
    //   Vuelo(8) Aleteo(6) Apuntar(4) Picada(4) Clavado(6) Huevo(5) Plumas(5) CargarRayo(6) Danado(8) Muerte(8)
    //   Opcionales: Pluma.png, HuevoMina.png, Compuerta.png, Escombro.png
    public sealed class Aeris : JefeBase
    {
        private enum Estado { Dormido, Chillido, Planear, Posicionar, Apuntar, Picada, Clavado, Remontar, Huevos, Plumas, Viento, Rayo, Aturdido, Transicion, Muerte }

        [Header("Arena")]
        [SerializeField] private float mitadArena = 12.4f;
        [Header("Ritmo")]
        [SerializeField] private float[] descanso = { 1.1f, 0.85f, 0.55f };
        [SerializeField] private float[] avisoPicada = { 0.95f, 0.8f, 0.55f };
        [SerializeField] private float velocidadPicada = 19f;

        private Estado estado = Estado.Dormido;
        private float timer, duracion = 1f, techo, paso;
        private bool golpeHecho, picadaDoble, avisoClavado, avisoViento;
        private int ultimoAtaque = -1, huevosSoltados, ladoPlaneo = 1;
        private Vector2 destino, direccionPicada, objetivo;
        private readonly EstalactitaArena[] compuertas = new EstalactitaArena[2];
        private SpriteRenderer laser;
        private Rigidbody2D kaelBody;

        protected override string Carpeta => "Aeris";
        protected override string NombreVisible => "AERIS, EL PTERANODÓN VIGÍA";
        protected override float AltoVisual => 3.0f;
        protected override Vector2 TamanoCuerpo => new Vector2(3.0f, 1.4f);
        protected override Color ColorMarcador => new Color(0.82f, 0.84f, 0.88f);
        protected override float[] Umbrales => new[] { 0.65f, 0.3f };
        protected override string NombreFase => fase == 3 ? "ALA DAÑADA" : "";
        protected override bool DanaAlContacto => estado != Estado.Clavado && estado != Estado.Aturdido && estado != Estado.Transicion
                                                  && estado != Estado.Dormido && estado != Estado.Muerte;

        private float ArenaMin => home.x - mitadArena;
        private float ArenaMax => home.x + mitadArena;
        private float Piso => home.y;
        private float Alto => techo - 3.2f;   // altura de vuelo normal

        // ---------------- Aparición automática ----------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Instalar()
        {
            IntentarAparecer();
            SceneManager.sceneLoaded += (_, __) => IntentarAparecer();
        }

        private static void IntentarAparecer()
        {
            if (FindAnyObjectByType<Aeris>(FindObjectsInactive.Include) != null) return;
            var marca = GameObject.Find("Vista_Aeris");
            if (marca == null) return;
            var go = new GameObject("Aeris_Pteranodon");
            go.transform.position = marca.transform.position;
            go.AddComponent<Aeris>();
        }

        // ---------------- Ciclo de vida ----------------

        protected override void Awake()
        {
            maxHealth = 70;
            attackDamage = 1;
            base.Awake();
            body.gravityScale = 0f;
            body.bodyType = RigidbodyType2D.Kinematic;
            string[] nombres = { "Vuelo", "Aleteo", "Apuntar", "Picada", "Clavado", "Huevo", "Plumas", "CargarRayo", "Danado", "Muerte" };
            int[] frames = { 8, 6, 4, 4, 6, 5, 5, 6, 8, 8 };
            for (int i = 0; i < nombres.Length; i++) CargarClip(nombres[i], frames[i]);

            // techo de la arena
            var hit = Physics2D.Raycast((Vector2)home + Vector2.up * 1.5f, Vector2.up, 30f, 1 << TerrainLayer);
            techo = hit.collider != null ? hit.point.y : home.y + 14f;
            // posa arriba, al centro, mientras duerme
            home = new Vector3(home.x, home.y, 0f);
            transform.position = new Vector3(home.x, Alto, 0f);

            var l = new GameObject("Laser_Aviso");
            l.transform.SetParent(transform.parent, false);
            laser = l.AddComponent<SpriteRenderer>();
            laser.sprite = Cuadro; laser.color = new Color(1f, 0.15f, 0.1f, 0.75f); laser.sortingOrder = 12;
            laser.enabled = false;
        }

        protected override void Start()
        {
            base.Start();
            if (player != null) kaelBody = player.GetComponent<Rigidbody2D>();
        }

        private void Cambiar(Estado e, float segundos)
        {
            estado = e; timer = duracion = Mathf.Max(0.05f, segundos); golpeHecho = false; paso = 0f;
            if (e != Estado.Apuntar) laser.enabled = false;
        }

        private void Sonido(string n, float v, float tono = 1f) => Sonidos.Play(n, v, tono, transform.position);

        protected override void Despertar()
        {
            if (engaged || muerto) return;
            base.Despertar();
            for (int i = 0; i < 2; i++)
                compuertas[i] = EstalactitaArena.Crear(i == 0 ? ArenaMin : ArenaMax, Piso, Carpeta, 0.1f + i * 0.25f, "Compuerta", "compuerta");
            LevelHUD.Notify("AERIS, el Pteranodón Vigía", 3f);
            Sonido("aeris_chillido", 0.9f);
            Cambiar(Estado.Chillido, 1.6f);
        }

        protected override void ReiniciarPelea()
        {
            if (muerto) return;
            base.ReiniciarPelea();
            QuitarCompuertas();
            transform.position = new Vector3(home.x, Alto, 0f);
            estado = Estado.Dormido; ultimoAtaque = -1;
            laser.enabled = false;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            QuitarCompuertas();
            if (laser != null) Destroy(laser.gameObject);
        }

        private void QuitarCompuertas()
        {
            for (int i = 0; i < 2; i++) { if (compuertas[i] != null) Destroy(compuertas[i].gameObject); compuertas[i] = null; }
        }

        protected override int FiltrarGolpe(int dano, Vector2 impulso)
        {
            if (muerto || EnTransicion || estado == Estado.Transicion) return 0;
            if (estado == Estado.Aturdido || estado == Estado.Clavado) return Mathf.CeilToInt(dano * 1.5f);
            return dano;
        }

        public override void Aturdir(float segundos)
        {
            if (muerto || !engaged || estado == Estado.Transicion) return;
            if (estado == Estado.Aturdido) { timer = Mathf.Max(timer, segundos + 0.4f); return; }
            Cambiar(Estado.Aturdido, segundos + 0.6f); // cae al suelo y se queda un momento
            Sonido("aeris_chillido", 0.6f, 1.35f);
        }

        protected override void AlCambiarFase(int nueva)
        {
            Cambiar(Estado.Transicion, SegundosTransicion);
            Sonido("aeris_chillido", 0.9f, nueva == 3 ? 0.85f : 1.1f);
            if (nueva == 2) LevelHUD.Notify("Kiro: ¡Está furioso! Cuidado con el viento y el rayo del suelo", 4f);
            else LevelHUD.Notify("Kiro: ¡Le diste en el ala! Ya no vuela derecho... ¡pero ataca dos veces!", 4.5f);
        }

        protected override void Morir()
        {
            base.Morir();
            Cambiar(Estado.Muerte, 2.4f);
            Sonido("aeris_muerte", 0.9f);
            Sacudida.Hacer(2.5f, 0.6f);
            for (int i = 0; i < 2; i++) if (compuertas[i] != null) compuertas[i].Romper(1.6f + i * 0.3f);
            LevelHUD.Notify("¡Aeris cayó! Las compuertas se abren", 4f);
        }

        // ---------------- Combate ----------------

        private void FixedUpdate()
        {
            if (player == null) return;
            float dt = Time.fixedDeltaTime;
            timer -= dt;
            float p = 1f - Mathf.Clamp01(timer / duracion);
            Vector2 pos = body.position;
            Bounds b = box.bounds;
            if (estado != Estado.Picada && estado != Estado.Clavado && estado != Estado.Muerte)
                facing = Kael.x >= pos.x ? 1 : -1;

            switch (estado)
            {
                case Estado.Dormido:
                    if (Kael.x > ArenaMin + 2.5f && Kael.x < ArenaMax && Mathf.Abs(Kael.y - Piso) < 16f) Despertar();
                    break;

                case Estado.Chillido:
                    Mover(new Vector2(home.x, Alto), 3f);
                    if (timer <= 0f)
                    {
                        LevelHUD.Notify("Kiro: ¡Vuela muy alto! Dispara en diagonal hacia arriba", 4f);
                        Cambiar(Estado.Planear, descanso[0]);
                    }
                    break;

                case Estado.Transicion:
                    Mover(new Vector2(home.x, Alto), 6f);
                    if (timer <= 0f) Cambiar(Estado.Planear, 0.4f);
                    break;

                case Estado.Planear:
                {
                    // va de lado a lado por arriba; en la fase 3 se tambalea
                    float x = home.x + ladoPlaneo * (mitadArena - 4f);
                    float y = Alto + (fase >= 3 ? Mathf.Sin(Time.time * 7f) * 1.2f : Mathf.Sin(Time.time * 2f) * 0.5f);
                    if (Mathf.Abs(pos.x - x) < 0.5f) ladoPlaneo = -ladoPlaneo;
                    Mover(new Vector2(x, y), fase >= 3 ? 7f : 5f);
                    if (timer <= 0f) ElegirAtaque();
                    break;
                }

                case Estado.Posicionar:
                    Mover(destino, 14f);
                    if (Vector2.Distance(pos, destino) < 0.3f || timer <= 0f)
                    {
                        Cambiar(Estado.Apuntar, avisoPicada[fase - 1] * (picadaDoble ? 0.6f : 1f));
                        Sonido("aeris_apuntar", 0.6f);
                    }
                    break;

                case Estado.Apuntar:
                    // la lente sigue a Kael y fija la trayectoria al 70 % del aviso
                    if (p < 0.7f) objetivo = Kael + Vector2.up * 0.6f;
                    direccionPicada = (objetivo - pos).normalized;
                    facing = direccionPicada.x >= 0f ? 1 : -1;
                    DibujarLaser(pos + Vector2.up * 0.7f, direccionPicada, p >= 0.7f);
                    if (timer <= 0f) { Cambiar(Estado.Picada, 2.5f); Sonido("aeris_picada", 0.8f); }
                    break;

                case Estado.Picada:
                {
                    Vector2 sig = pos + direccionPicada * velocidadPicada * dt;
                    bool choca = sig.y <= Piso + 0.05f || sig.x <= ArenaMin + 1.2f || sig.x >= ArenaMax - 1.2f || sig.y >= techo - 0.5f || timer <= 0f;
                    sig.x = Mathf.Clamp(sig.x, ArenaMin + 1.2f, ArenaMax - 1.2f);
                    sig.y = Mathf.Max(sig.y, Piso);
                    body.MovePosition(sig);
                    Golpear(b, attackDamage);
                    if (choca)
                    {
                        Sonido("aeris_clavado", 0.8f);
                        Sacudida.Hacer(1.6f, 0.25f);
                        Cambiar(Estado.Clavado, picadaDoble ? 0.55f : (fase >= 3 ? 1.0f : 1.4f));
                        if (!avisoClavado && !picadaDoble) { avisoClavado = true; LevelHUD.Notify("Kiro: ¡Se quedó clavado! ¡Pégale con la lanza!", 3.5f); }
                    }
                    break;
                }

                case Estado.Clavado:
                    if (timer <= 0f)
                    {
                        if (picadaDoble)
                        {
                            picadaDoble = false;
                            objetivo = Kael + Vector2.up * 0.6f;
                            Cambiar(Estado.Apuntar, avisoPicada[fase - 1] * 0.6f);
                            Sonido("aeris_apuntar", 0.6f, 1.2f);
                        }
                        else Cambiar(Estado.Remontar, 0.8f);
                    }
                    break;

                case Estado.Remontar:
                    Mover(new Vector2(Mathf.Clamp(pos.x, ArenaMin + 3f, ArenaMax - 3f), Alto), 9f);
                    if (timer <= 0f) Cambiar(Estado.Planear, descanso[fase - 1]);
                    break;

                case Estado.Huevos:
                {
                    // cruza la arena por arriba soltando 3 huevos
                    float desde = ladoPlaneo > 0 ? ArenaMin + 2.5f : ArenaMax - 2.5f;
                    float hasta = ladoPlaneo > 0 ? ArenaMax - 2.5f : ArenaMin + 2.5f;
                    Mover(new Vector2(Mathf.Lerp(desde, hasta, p), Alto), 30f);
                    int deberia = Mathf.FloorToInt(p * 3.5f);
                    if (huevosSoltados < 3 && huevosSoltados < deberia)
                    {
                        bool eco = huevosSoltados == eco3;
                        SoltarHuevo(pos + Vector2.down * 0.3f, eco);
                        huevosSoltados++;
                    }
                    if (timer <= 0f) { ladoPlaneo = -ladoPlaneo; Cambiar(Estado.Planear, descanso[fase - 1]); }
                    break;
                }

                case Estado.Plumas:
                    Mover(new Vector2(pos.x, Alto - 1f), 3f);
                    if (!golpeHecho && p >= 0.55f) { golpeHecho = true; DispararPlumas(pos + Vector2.up * 0.6f); }
                    if (timer <= 0f) Cambiar(Estado.Planear, descanso[fase - 1]);
                    break;

                case Estado.Viento:
                {
                    // aletea desde un lado y empuja a Kael hacia la pared contraria
                    float lado = Kael.x >= home.x ? -1f : 1f;
                    if (paso == 0f) { paso = lado; Sonido("aeris_viento", 0.75f); if (!avisoViento) { avisoViento = true; LevelHUD.Notify("Kiro: ¡Camina contra el viento!", 3f); } }
                    Mover(new Vector2(home.x + paso * (mitadArena - 2.5f), Piso + 6f), 10f);
                    if (p > 0.2f && p < 0.92f)
                    {
                        if (kaelBody != null) kaelBody.position += new Vector2(-paso * 4.6f * dt, 0f);
                        if (Random.value < 0.25f) RafagaVisual(-paso);
                        if (fase >= 3 && Random.value < 0.02f) DispararPlumas(pos + Vector2.up * 0.6f);
                    }
                    if (timer <= 0f) Cambiar(Estado.Planear, descanso[fase - 1]);
                    break;
                }

                case Estado.Rayo:
                    Mover(new Vector2(ladoPlaneo > 0 ? ArenaMin + 2.5f : ArenaMax - 2.5f, Alto), 10f);
                    if (!golpeHecho && p > 0.15f)
                    {
                        golpeHecho = true;
                        Sonido("aeris_rayo", 0.8f);
                        Registrar(RayoSuelo.Crear(ArenaMin + 1.2f, ArenaMax - 1.2f, Piso, 1.0f, 0.65f));
                    }
                    if (timer <= 0f) { ladoPlaneo = -ladoPlaneo; Cambiar(Estado.Planear, descanso[fase - 1]); }
                    break;

                case Estado.Aturdido:
                    Mover(new Vector2(pos.x, Piso), 14f); // cae al suelo
                    if (timer <= 0f) Cambiar(Estado.Remontar, 0.8f);
                    break;

                case Estado.Muerte:
                    Mover(new Vector2(pos.x + facing * 0.5f, Piso), 4f + p * 8f);
                    if (timer <= 0f && box.enabled)
                    {
                        box.enabled = false;
                        foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
                    }
                    break;
            }
        }

        private int eco3;

        private void ElegirAtaque()
        {
            // pesos: Picada, Huevos, Plumas, Viento, Rayo
            int[] pesos = fase == 1 ? new[] { 45, 35, 20, 0, 0 } : fase == 2 ? new[] { 30, 15, 20, 18, 17 } : new[] { 40, 15, 15, 12, 18 };
            if (ultimoAtaque >= 0 && pesos[ultimoAtaque] > 0) pesos[ultimoAtaque] /= 3;
            int total = 0; foreach (int w in pesos) total += w;
            int r = Random.Range(0, total), elegido = 0;
            for (int i = 0; i < pesos.Length; i++) { if (r < pesos[i]) { elegido = i; break; } r -= pesos[i]; }
            ultimoAtaque = elegido;

            switch (elegido)
            {
                case 0: // picada desde la esquina contraria a Kael (queda en diagonal)
                    picadaDoble = fase >= 3;
                    destino = new Vector2(Kael.x > home.x ? ArenaMin + 2.2f : ArenaMax - 2.2f, techo - 2.2f);
                    Cambiar(Estado.Posicionar, 1.2f);
                    Sonido("aeris_aleteo", 0.6f);
                    break;
                case 1:
                    huevosSoltados = 0; eco3 = Random.Range(0, 3);
                    Cambiar(Estado.Huevos, 1.9f);
                    Sonido("aeris_aleteo", 0.6f);
                    break;
                case 2: Cambiar(Estado.Plumas, 0.9f); break;
                case 3: Cambiar(Estado.Viento, 2.8f); break;
                default: Cambiar(Estado.Rayo, 1.8f); break;
            }
        }

        private void Mover(Vector2 a, float velocidad) => body.MovePosition(Vector2.MoveTowards(body.position, a, velocidad * Time.fixedDeltaTime));

        private void DibujarLaser(Vector2 desde, Vector2 dir, bool fijo)
        {
            // largo hasta el piso o la pared
            float largo = 30f;
            if (dir.y < -0.01f) largo = Mathf.Min(largo, (desde.y - Piso) / -dir.y);
            if (dir.x > 0.01f) largo = Mathf.Min(largo, (ArenaMax - 1f - desde.x) / dir.x);
            if (dir.x < -0.01f) largo = Mathf.Min(largo, (ArenaMin + 1f - desde.x) / dir.x);
            laser.enabled = true;
            laser.transform.position = desde + dir * largo / 2f;
            laser.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            laser.transform.localScale = new Vector3(largo, fijo ? 0.16f : 0.07f, 1f);
            float a = fijo ? 0.95f : 0.45f + 0.3f * Mathf.Abs(Mathf.Sin(Time.time * 18f));
            laser.color = new Color(1f, 0.15f, 0.1f, a);
        }

        private void SoltarHuevo(Vector2 desde, bool eco)
        {
            Sonido("aeris_huevo", 0.55f);
            if (eco) { Registrar(ProyectilesJefe.Roca(desde, new Vector2(Random.Range(-1f, 1f), -2f), true, Carpeta)); return; }
            Registrar(HuevoMina.Crear(desde, Carpeta));
        }

        private void DispararPlumas(Vector2 desde)
        {
            Sonido("aeris_plumas", 0.65f);
            int n = fase >= 2 ? 5 : 3;
            Vector2 baseDir = (Kael + Vector2.up * 0.8f - desde).normalized;
            float ang0 = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
            for (int i = 0; i < n; i++)
            {
                float a = ang0 + (i - (n - 1) / 2f) * 14f;
                Vector2 d = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                Registrar(Pluma.Crear(desde, d, 9.5f, Carpeta));
            }
        }

        private void RafagaVisual(float dir)
        {
            var go = new GameObject("Rafaga");
            go.transform.position = new Vector3(dir > 0 ? ArenaMin + 1.5f : ArenaMax - 1.5f, Piso + Random.Range(0.5f, 7f), 0f);
            go.transform.localScale = new Vector3(Random.Range(1.2f, 2.4f), 0.06f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Cuadro; sr.color = new Color(0.9f, 0.97f, 1f, 0.45f); sr.sortingOrder = 11;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = new Vector2(dir * 16f, 0f);
            go.AddComponent<Desvanecer>().Init(1.2f);
        }

        // ---------------- Animación ----------------

        protected override Color ColorBase()
        {
            if (estado == Estado.Apuntar) return new Color(1f, 0.75f, 0.7f, 1f);
            if (fase >= 3 && !muerto) return Time.time % 0.5f < 0.08f ? new Color(1f, 0.9f, 0.6f, 1f) : new Color(1f, 0.9f, 0.88f, 1f);
            return Color.white;
        }

        private void Update()
        {
            if (sprite == null) return;
            float p = 1f - Mathf.Clamp01(timer / duracion);
            string vuelo = fase >= 3 ? "Danado" : "Vuelo";
            switch (estado)
            {
                case Estado.Dormido: Mostrar("Vuelo", -1f, 4f); break;
                case Estado.Chillido: case Estado.Transicion: Mostrar("Aleteo", -1f, 12f); break;
                case Estado.Planear: case Estado.Remontar: case Estado.Posicionar: case Estado.Huevos: Mostrar(estado == Estado.Huevos ? "Huevo" : vuelo, -1f, 10f); break;
                case Estado.Apuntar: Mostrar("Apuntar", -1f, 8f); break;
                case Estado.Picada: Mostrar("Picada", -1f, 12f); break;
                case Estado.Clavado: Mostrar("Clavado", -1f, 10f); break;
                case Estado.Plumas: Mostrar("Plumas", p); break;
                case Estado.Viento: Mostrar("Aleteo", -1f, 16f); break;
                case Estado.Rayo: Mostrar("CargarRayo", -1f, 10f); break;
                case Estado.Aturdido: Mostrar("Danado", -1f, 6f); break;
                case Estado.Muerte: Mostrar("Muerte", p); break;
            }
            // en picada, el sprite se inclina en la dirección del vuelo
            float ang = estado == Estado.Picada ? Mathf.Atan2(direccionPicada.y, Mathf.Abs(direccionPicada.x)) * Mathf.Rad2Deg * facing : 0f;
            sprite.transform.localRotation = Quaternion.Euler(0f, 0f, ang);
        }
    }

    // ---------- Proyectiles de Aeris ----------

    /// <summary>Huevo-mina: cae, parpadea y explota al rato. El Destello lo rompe antes.</summary>
    public sealed class HuevoMina : MonoBehaviour
    {
        private const float Mecha = 1.3f, Radio = 1.7f;
        private bool posado; private float tPosado; private SpriteRenderer sr;

        public static GameObject Crear(Vector2 desde, string carpeta)
        {
            var go = new GameObject("Huevo_Mina");
            go.transform.position = desde;
            var sr = go.AddComponent<SpriteRenderer>();
            Sprite propio = ProyectilesJefe.Cargar(carpeta, "HuevoMina", null, 0.8f);
            sr.sprite = propio != null ? propio : JefeBase.Circulo;
            sr.color = propio != null ? Color.white : new Color(0.92f, 0.9f, 0.82f);
            if (propio == null) go.transform.localScale = new Vector3(0.65f, 0.8f, 1f);
            sr.sortingOrder = 9;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1.8f;
            rb.linearVelocity = new Vector2(Random.Range(-0.8f, 0.8f), -1f);
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true; col.radius = 0.4f;
            go.AddComponent<ProyectilJefe>();
            var h = go.AddComponent<HuevoMina>(); h.sr = sr;
            Destroy(go, 6f);
            return go;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (posado || other.isTrigger || other.gameObject.layer != 6) return;
            posado = true;
            var rb = GetComponent<Rigidbody2D>();
            rb.linearVelocity = Vector2.zero; rb.bodyType = RigidbodyType2D.Kinematic;
            Sonidos.Play("roca_impacto", 0.35f, 1.5f, transform.position);
        }

        private void Update()
        {
            if (!posado) return;
            tPosado += Time.deltaTime;
            float f = tPosado / Mecha;
            if (sr != null) sr.color = Mathf.Repeat(tPosado * (4f + 10f * f), 1f) < 0.5f ? new Color(1f, 0.35f, 0.25f) : Color.white;
            if (tPosado >= Mecha) Explotar();
        }

        private void Explotar()
        {
            Sonidos.Play("aeris_explosion", 0.75f, 1f, transform.position);
            Sacudida.Hacer(1.2f, 0.2f);
            var fx = new GameObject("Explosion");
            fx.transform.position = transform.position;
            var r = fx.AddComponent<SpriteRenderer>();
            r.sprite = JefeBase.Circulo; r.color = new Color(1f, 0.6f, 0.25f, 0.85f); r.sortingOrder = 13;
            fx.AddComponent<EfectoCrecer>().Init(Radio * 2.2f, 0.3f);
            var kael = GameObject.FindGameObjectWithTag("Player");
            if (kael != null && Vector2.Distance(kael.transform.position + Vector3.up * 0.9f, transform.position) < Radio)
            {
                var vida = kael.GetComponent<KaelHealth>();
                if (vida != null) vida.Hit(1, transform.position);
            }
            Destroy(gameObject);
        }
    }

    /// <summary>Pluma-cuchilla que vuela recta.</summary>
    public static class Pluma
    {
        public static GameObject Crear(Vector2 desde, Vector2 dir, float velocidad, string carpeta)
        {
            var go = new GameObject("Pluma");
            go.transform.position = desde;
            go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            var sr = go.AddComponent<SpriteRenderer>();
            Sprite propio = ProyectilesJefe.Cargar(carpeta, "Pluma", null, 0.35f);
            if (propio != null) sr.sprite = propio;
            else { sr.sprite = JefeBase.Cuadro; sr.color = new Color(0.95f, 0.95f, 1f); go.transform.localScale = new Vector3(0.9f, 0.18f, 1f); }
            sr.sortingOrder = 10;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = dir * velocidad;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true; col.size = propio != null ? (Vector2)propio.bounds.size * 0.8f : new Vector2(0.9f, 0.6f);
            JsonUtility.FromJsonOverwrite("{\"damage\":1,\"allowsPogo\":false}", go.AddComponent<ContactHazard>());
            go.AddComponent<ProyectilJefe>();
            go.AddComponent<PlumaClavada>();
            Object.Destroy(go, 4f);
            return go;
        }
    }

    /// <summary>La pluma se clava al tocar una pared o el piso y desaparece.</summary>
    public sealed class PlumaClavada : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.isTrigger || other.gameObject.layer != 6) return;
            var rb = GetComponent<Rigidbody2D>(); if (rb != null) rb.linearVelocity = Vector2.zero;
            var hz = GetComponent<ContactHazard>(); if (hz != null) Destroy(hz);
            gameObject.AddComponent<Desvanecer>().Init(0.6f);
        }
    }

    /// <summary>Rayo a ras de suelo: primero una línea de aviso, luego la descarga (se salta).</summary>
    public sealed class RayoSuelo : MonoBehaviour
    {
        private SpriteRenderer sr; private BoxCollider2D col; private float t, aviso, dura;

        public static GameObject Crear(float x0, float x1, float piso, float avisoSegundos, float duracion)
        {
            var go = new GameObject("Rayo_Suelo");
            go.transform.position = new Vector3((x0 + x1) / 2f, piso + 0.55f, 0f);
            go.transform.localScale = new Vector3(x1 - x0, 0.08f, 1f);
            var r = go.AddComponent<RayoSuelo>();
            r.sr = go.AddComponent<SpriteRenderer>();
            r.sr.sprite = JefeBase.Cuadro; r.sr.color = new Color(1f, 0.2f, 0.15f, 0.6f); r.sr.sortingOrder = 12;
            var rb = go.AddComponent<Rigidbody2D>(); rb.bodyType = RigidbodyType2D.Kinematic;
            r.col = go.AddComponent<BoxCollider2D>(); r.col.isTrigger = true; r.col.size = new Vector2(1f, 1f); r.col.enabled = false;
            JsonUtility.FromJsonOverwrite("{\"damage\":1,\"allowsPogo\":false}", go.AddComponent<ContactHazard>());
            r.aviso = avisoSegundos; r.dura = duracion;
            return go;
        }

        private void Update()
        {
            t += Time.deltaTime;
            if (t < aviso)
            {
                sr.color = new Color(1f, 0.2f, 0.15f, 0.35f + 0.4f * Mathf.Abs(Mathf.Sin(t * 20f)));
                return;
            }
            if (!col.enabled)
            {
                col.enabled = true;
                transform.localScale = new Vector3(transform.localScale.x, 1.0f, 1f);
                Sacudida.Hacer(1f, 0.2f);
            }
            sr.color = new Color(0.7f, 1f, 1f, 0.85f - 0.3f * Mathf.Abs(Mathf.Sin(t * 40f)));
            if (t > aviso + dura) Destroy(gameObject);
        }
    }
}
