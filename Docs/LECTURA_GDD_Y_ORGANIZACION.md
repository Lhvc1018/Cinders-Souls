# Lectura del GDD y organización

Revisión del documento `GDD_Cinders_Souls_Actualizado-v3 (1).docx`, entregado el 2 de noviembre de 2026.

## Dirección

La fantasía oscura, las expediciones roguelike, la interfaz gótica y las criaturas encajan con el rumbo actual. El usuario también pidió combate rápido inspirado en Dead Cells y Souls. Deben conservarse la legibilidad y las ventanas de reacción durante el combate.

El documento propone tercera persona, fijación de objetivo, estamina y una red Photon Fusion 2. El prototipo revisado utiliza primera persona y conserva LAN propio. Estas propuestas no demuestran que esos sistemas estén implementados ni autorizan describir el multijugador como migrado. Diseñar y probar la transición antes de sustituir el control o la red.

Los productos comerciales citados, como Odin Inspector, Final IK, DunGen y A* Pathfinding Project Pro, necesitan una evaluación de utilidad, compatibilidad, licencia y coste. El documento no demuestra que estén instalados. No se requiere comprarlos para usar los recursos actuales.

## Carpetas principales

- `00_Scenes`: escenas del proyecto.
- `01_Prefabs`: prefabs generales.
- `02_Scripts`: código de juego e interfaz.
- `03_Models`: modelos, incluidos los enemigos nuevos.
- `04_Materials`: materiales generales.
- `05_Music`: música y audio organizados por paquetes.
- `06_Effects`: efectos y paquetes visuales. VolumetricLines y los efectos de presentación se movieron aquí conservando sus GUID.
- `07_Animacion`: animaciones y sus paquetes.
- `Resources`: recursos cargados con `Resources.Load`; sus rutas forman parte del código.
- `Editor`: herramientas que solo ejecuta el editor.
- `Plugins`, `Photon` y `TextMesh Pro`: dependencias con estructura propia.

El generador de presentación apunta a la nueva ubicación de efectos. Los paquetes y las carpetas Resources se conservan completos para proteger sus dependencias. El respaldo de la herramienta modificada está en `Backups/Organizacion-20261102`.

## Skills y complementos

Se añadieron instrucciones especializadas para economía y combate, jefes, Fusion, narrativa, memoria, rigging y mazmorras procedurales. También se instaló el plugin oficial de Unity para Codex. Debe cargarse en una nueva sesión para estar disponible.
