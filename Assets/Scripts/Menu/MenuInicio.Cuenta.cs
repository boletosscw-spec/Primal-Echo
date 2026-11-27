using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PrimalEcho
{
    // Páginas de cuenta del menú de inicio: iniciar sesión, crear cuenta y ver la sesión.
    // Los campos de texto son entradas del mismo menú: se escriben con el teclado cuando están seleccionados.
    public sealed partial class MenuInicio
    {
        private enum PaginaCuenta { Ninguna, Login, Registro, Sesion }

        private PaginaCuenta paginaCuenta;
        private Text txtEstado;
        private float repeticionBorrar;
        private bool estadoReciente;
        private string correoRecordado = "";
        private CanvasGroup grupoEstado;
        private Keyboard teclado;

        private void IniciarCuenta()
        {
            teclado = Keyboard.current;
            if (teclado != null) teclado.onTextInput += AlEscribir;
            txtEstado = Txt("Estado Cuenta", menuGrupo, fuenteNegrita, 22, Apagado, Vector2.zero, new Vector2(1100f, 40f));
            txtEstado.text = "";
            grupoEstado = txtEstado.gameObject.AddComponent<CanvasGroup>();
            grupoEstado.alpha = 0f;
            correoRecordado = CuentaJugador.Instancia.Correo;
        }

        private void OnDestroy()
        {
            if (teclado != null) teclado.onTextInput -= AlEscribir;
        }

        /// <summary>El mensaje aparece junto con las opciones del menú al terminar la intro.</summary>
        private void AnimarEstado(float tm)
        {
            if (grupoEstado != null) grupoEstado.alpha = Suave(Mathf.Clamp01((tm - 0.5f) / 0.4f));
        }

        /// <summary>Primera página: el menú si ya hay sesión (o Firebase no está configurado); si no, el login.</summary>
        private void AbrirInicio(bool animar)
        {
            if (!CuentaJugador.Configurado || CuentaJugador.Instancia.HaySesion) AbrirPrincipal(animar);
            else AbrirLogin(animar);
        }

        private Entrada EntradaCuenta()
        {
            return new Entrada
            {
                texto = "Cuenta",
                valor = () => !CuentaJugador.Configurado ? "Sin conexión"
                            : CuentaJugador.Instancia.HaySesion ? CuentaJugador.Instancia.Nombre
                            : "Invitado",
                confirmar = () =>
                {
                    if (!CuentaJugador.Configurado) { Estado("Firebase todavía no está configurado", Rojo); return; }
                    if (CuentaJugador.Instancia.HaySesion) AbrirSesion();
                    else AbrirLogin(true);
                },
            };
        }

        // ------------------------------------------------------------------ páginas

        private void AbrirLogin(bool animar)
        {
            var correo = Campo("Correo", correoRecordado, false, 60);
            var clave = Campo("Contraseña", "", true, 40);
            var e = new List<Entrada>
            {
                correo,
                clave,
                new Entrada { texto = "Entrar", confirmar = () => Entrar(correo.contenido, clave.contenido) },
                new Entrada { texto = "Crear cuenta", confirmar = () => AbrirRegistro(correo.contenido) },
                new Entrada { texto = "Recuperar contraseña", confirmar = () => Recuperar(correo.contenido) },
                new Entrada { texto = "Jugar sin cuenta", confirmar = JugarSinCuenta },
            };
            paginaCuenta = PaginaCuenta.Login;
            CambiarPagina(e, string.IsNullOrEmpty(correo.contenido) ? 0 : 1, animar, "TAB / FLECHAS   CAMBIAR CAMPO        ENTER   ACEPTAR");
            Estado("Inicia sesión para guardar tu progreso en la nube", Apagado);
        }

        private void AbrirRegistro(string correoInicial)
        {
            var nombre = Campo("Nombre", "", false, 20);
            var correo = Campo("Correo", correoInicial, false, 60);
            var clave = Campo("Contraseña", "", true, 40);
            var e = new List<Entrada>
            {
                nombre,
                correo,
                clave,
                new Entrada { texto = "Crear cuenta", confirmar = () => Registrar(nombre.contenido, correo.contenido, clave.contenido) },
                new Entrada { texto = "Volver", confirmar = Volver },
            };
            paginaCuenta = PaginaCuenta.Registro;
            CambiarPagina(e, 0, true, "TAB / FLECHAS   CAMBIAR CAMPO        ENTER   ACEPTAR        ESC   VOLVER");
            Estado("La misma cuenta sirve en la página web y en la app", Apagado);
        }

        private void AbrirSesion()
        {
            var cuenta = CuentaJugador.Instancia;
            var e = new List<Entrada>
            {
                new Entrada { texto = cuenta.Nombre, habilitada = false },
                new Entrada { texto = "Cerrar sesión", confirmar = () =>
                    {
                        correoRecordado = cuenta.Correo;
                        cuenta.CerrarSesion();
                        AbrirLogin(true);
                    } },
                new Entrada { texto = "Volver", confirmar = Volver },
            };
            paginaCuenta = PaginaCuenta.Sesion;
            CambiarPagina(e, 1, true, "ENTER / A   ACEPTAR        ESC / B   VOLVER");
            Estado(cuenta.Correo, Apagado);
        }

        /// <summary>Lo que hace "Volver" en las páginas de cuenta. Devuelve false si no es una de ellas.</summary>
        private bool VolverCuenta()
        {
            switch (paginaCuenta)
            {
                case PaginaCuenta.Login:
                    if (!CuentaJugador.ModoInvitado) return true; // es la primera página: no hay a dónde volver
                    paginaCuenta = PaginaCuenta.Ninguna;      // si ya estaba jugando como invitado, regresa al menú
                    return false;
                case PaginaCuenta.Registro:
                    Sonar(sVolver, 0.6f);
                    AbrirLogin(true);
                    return true;
                case PaginaCuenta.Sesion:
                    paginaCuenta = PaginaCuenta.Ninguna;
                    return false;
                default:
                    return false;
            }
        }

        // ------------------------------------------------------------------ acciones

        private void Entrar(string correo, string clave)
        {
            if (!CorreoValido(correo)) { Estado("Escribe un correo válido", Rojo); return; }
            if (string.IsNullOrEmpty(clave)) { Estado("Escribe tu contraseña", Rojo); return; }
            Esperar("Conectando…");
            CuentaJugador.Instancia.IniciarSesion(correo, clave, (ok, msg) => Respuesta(ok, msg));
        }

        private void Registrar(string nombre, string correo, string clave)
        {
            if (string.IsNullOrWhiteSpace(nombre)) { Estado("Escribe tu nombre de jugador", Rojo); return; }
            if (!CorreoValido(correo)) { Estado("Escribe un correo válido", Rojo); return; }
            if (clave.Length < 6) { Estado("La contraseña debe tener al menos 6 caracteres", Rojo); return; }
            Esperar("Creando cuenta…");
            CuentaJugador.Instancia.CrearCuenta(nombre, correo, clave, (ok, msg) => Respuesta(ok, msg));
        }

        private void Recuperar(string correo)
        {
            if (!CorreoValido(correo)) { Estado("Escribe tu correo para recuperar la contraseña", Rojo); return; }
            Esperar("Enviando correo…");
            CuentaJugador.Instancia.RecuperarContrasena(correo, (ok, msg) =>
            {
                ocupado = false;
                Estado(msg, ok ? Cian : Rojo);
            });
        }

        private void JugarSinCuenta()
        {
            CuentaJugador.ModoInvitado = true;
            paginaCuenta = PaginaCuenta.Ninguna;
            AbrirPrincipal(true);
            Estado("Jugando sin cuenta: el progreso solo se guarda en esta computadora", Apagado);
        }

        private void Esperar(string texto)
        {
            ocupado = true;
            Estado(texto, Cian);
        }

        private void Respuesta(bool ok, string msg)
        {
            ocupado = false;
            if (!ok) { Estado(msg, Rojo); return; }
            paginaCuenta = PaginaCuenta.Ninguna;
            AbrirPrincipal(true);
            Estado(msg, Cian);
        }

        private void Estado(string texto, Color color)
        {
            if (txtEstado == null) return;
            txtEstado.text = texto;
            txtEstado.color = Con(color, 0.9f);
            estadoReciente = true;
        }

        /// <summary>Coloca el mensaje debajo de la última opción. Se llama cada vez que cambia la página.</summary>
        private void ColocarEstado()
        {
            if (txtEstado == null) return;
            txtEstado.rectTransform.anchoredPosition = new Vector2(0f, -pagina.Count * 64f - 18f);
            if (!estadoReciente) txtEstado.text = ""; // el mensaje que se puso justo antes de cambiar de página se conserva
            estadoReciente = false;
        }

        private static bool CorreoValido(string c)
        {
            c = (c ?? "").Trim();
            int arroba = c.IndexOf('@');
            return arroba > 0 && c.IndexOf('.', arroba) > arroba + 1 && !c.EndsWith(".") && !c.Contains(" ");
        }

        // ------------------------------------------------------------------ campos de texto

        private Entrada Campo(string etiqueta, string inicial, bool oculto, int maximo)
        {
            var e = new Entrada { texto = etiqueta, esCampo = true, oculto = oculto, maximo = maximo, contenido = inicial ?? "" };
            e.valor = () => MostrarCampo(e);
            return e;
        }

        private string MostrarCampo(Entrada e)
        {
            bool activo = pagina.Count > 0 && sel < pagina.Count && pagina[sel] == e;
            string texto = e.oculto ? new string('*', e.contenido.Length) : e.contenido;
            string cursor = activo && Mathf.Repeat(Time.unscaledTime, 1f) < 0.55f ? "|" : " ";
            if (texto.Length == 0 && !activo) return "<color=#7f8f94>. . .</color>";
            return texto + "<color=#73F2FF>" + cursor + "</color>";
        }

        private bool CampoActivo => introLista && pagina.Count > 0 && sel < pagina.Count && pagina[sel].esCampo;

        private void AlEscribir(char c)
        {
            if (!CampoActivo || ocupado || char.IsControl(c)) return;
            var e = pagina[sel];
            if (e.contenido.Length >= e.maximo) return;
            if (e.texto == "Correo" && c == ' ') return;
            e.contenido += c;
        }

        /// <summary>Borrar (con repetición al mantener) y pegar con Ctrl+V en el campo seleccionado.</summary>
        private void EditarCampo(Keyboard k)
        {
            if (k == null || !CampoActivo) return;
            var e = pagina[sel];

            if (k.backspaceKey.isPressed && e.contenido.Length > 0)
            {
                bool primero = k.backspaceKey.wasPressedThisFrame;
                repeticionBorrar -= Time.unscaledDeltaTime;
                if (primero || repeticionBorrar <= 0f)
                {
                    e.contenido = e.contenido.Substring(0, e.contenido.Length - 1);
                    repeticionBorrar = primero ? 0.45f : 0.05f;
                }
            }

            bool ctrl = k.leftCtrlKey.isPressed || k.rightCtrlKey.isPressed;
            if (ctrl && k.vKey.wasPressedThisFrame)
            {
                string pegado = (GUIUtility.systemCopyBuffer ?? "").Replace("\n", "").Replace("\r", "").Trim();
                e.contenido = (e.contenido + pegado).Length > e.maximo ? (e.contenido + pegado).Substring(0, e.maximo) : e.contenido + pegado;
            }
        }

        private static bool EnterPulsado()
        {
            Keyboard k = Keyboard.current;
            Gamepad g = Gamepad.current;
            return (k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame))
                   || (g != null && g.buttonSouth.wasPressedThisFrame);
        }
    }
}
