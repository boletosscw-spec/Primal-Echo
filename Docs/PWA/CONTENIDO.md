# Contenido de Primal Echo

Revisión local: 12 de noviembre de 2026. Solo se modificó la PWA.

## Fuentes del juego

- README.md: premisa, Unity 6, Kael y controles.
- Assets/Niveles/LEEME_Niveles.md y mapas Nivel_01 / Nivel_02: nombres, checkpoints, obstáculos y transición.
- ProjectSettings/EditorBuildSettings.asset: escenas habilitadas.
- Assets/Scripts/PlayerController.cs, KaelTraversal.cs, KaelSpear.cs y Niveles/KaelShooter.cs: movimiento y combate.
- KaelHealth.cs, Niveles/KaelRespawn.cs y LevelHUD.cs: daño, reaparición, vida, fragmentos y cronómetro.
- Niveles/RobotPatrol.cs, WaspFlyer.cs y CarnivorousPlant.cs: comportamientos enemigos.
- KaelAnimationDriver.cs: bloqueo e interacción tienen animaciones; no se afirma una defensa efectiva ni un sistema de interacción terminado.

Los sprites de game/static/game/img/desarrollo se copiaron de Assets/Sprites/Enemigos y Assets/Sprites/Environment/Relics. Se muestran como recursos, no como capturas. El arte anterior de la PWA se conserva; Kiro, Nara y Rex se identifican como ilustraciones sin integración confirmada.

## Límites editoriales

No se anuncia lanzamiento, porcentaje de avance, Astra, Aeternum, arco MK-II, mejoras de equipo ni compañeros funcionales. Los próximos pasos son propuestas de validación, no una hoja de ruta oficial. No se ejecutó Unity ni se certifica la jugabilidad de las escenas.

## Verificación

Esta PWA usa Django y no tiene compilación frontend. Ejecutar:

    .venv/bin/python manage.py check
    .venv/bin/python manage.py test
    .venv/bin/python manage.py collectstatic --noinput

Las pruebas cubren recursos locales de las páginas, acceso privado, login/logout y disponibilidad de la página offline y del service worker. El service worker conserva la estrategia de navegación con fallback offline y usa una nueva versión de caché para servir los recursos actualizados.
