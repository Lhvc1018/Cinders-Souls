# Cinders Souls · mazmorra, HUD y jefe

Proyecto: `D:\67\Last67Seconds-Maps`.
Escena: `Assets/00_Scenes/Maps/Descenso.unity`.

## Implementado

- HUD compacto: vida roja, armadura gris y maná azul, con valores actuales/máximos y avisos de reserva baja.
- Equipo en cruz: arma actual, dos activas y frasco. Tab/rueda cambia arma; F/G activa reliquias; Q usa el especial.
- Las pasivas y sus descripciones aparecen al pausar. La presentación del jefe dura 1,8 segundos y deja libre el centro.
- Minimapa derecho: habitaciones cuadradas, corredores rectangulares, sala actual blanca, visitadas claras y pendientes oscuras.
- Iconos propios: entrada verde, cofre dorado, sala especial y jefe. El número inferior indica piso y sala.
- Seis salas conectadas, con tres formas, dimensiones variables, pilares y trampas con aviso previo.
- Cofre inicial gratuito T1; recompensas posteriores de armas y reliquias. Las armas caídas al inicio también son T1.
- Frasco H: 3 cargas; cada uso cura 40% de vida máxima tras 1,05 segundos. El daño interrumpe la bebida.
- La fuente repone cargas una vez. No permite gastar la reposición si el frasco está lleno.
- Hellhounds coloreados, Creepy Doll y Skeleton demo integrados con materiales y animaciones.
- Cofres uniformes de 1,05 m de ancho. Frasco de 0,18 m de altura.
- Reliquias pequeñas de hueso, gemas y metal; flechas pequeñas, cartuchos y esencias azules sustituyen las pilas.
- El oro de enemigos y jefe entra directamente al inventario.

## Recursos del jugador

| Clase | Armadura máxima | Maná máximo |
|---|---:|---:|
| Guerrero | 30 | 60 |
| Tanque | 55 | 60 |
| Arquero | 20 | 60 |
| Mago | 15 | 100 |
| Shinobi | 25 | 60 |

La armadura absorbe daño antes de la vida. Se regenera a 5 puntos/s después de 6 segundos sin recibir daño.
El veneno daña la vida directamente. El maná se regenera a 8 puntos/s tras una espera de 1,5 segundos.

| Activa | Coste de maná | Recarga |
|---|---:|---:|
| Incensario | 12 | 10 s |
| Campana | 18 | 14 s |
| Lanza de sangre | 15 | 8 s |
| Velo de ceniza | 20 | 18 s |

## Inquisidor

Conserva el modelo importado. Se corrigieron orientación, pesos del arma, animaciones y presentación; no es un modelo nuevo.
Altura configurada: 2,6 m. Vida base: 1200.

- Barrido: aviso de 0,85 s, arco frontal de 3,5 m y 28 de daño.
- Golpe: aviso circular de 1,3 s, radio de 3,4 m y 38 de daño.
- Ambos golpes dejan sangre tóxica. Los charcos avisan durante 0,65 s antes de contaminar.
- Veneno: 4 de daño/s durante 3 segundos; permanecer en sangre renueva la duración, sin sumar instancias.
- Salva: aviso de 1,05 s, proyectiles a 11 m/s y 18 de daño directo.
- Las fases lanzan 1, 3 y 5 proyectiles. Cambian al bajar de 65% y 30% de vida.
- Los proyectiles chocan con paredes y dejan pequeños charcos.
- Aturdir interrumpe ataques y elimina sus peligros. Morir elimina charcos, proyectiles y veneno del encuentro.

## Validación

- 52.045 comprobaciones: 2000 semillas conectadas y reproducibles, 1998 disposiciones distintas y tres formas de sala.
- Navegación y recorrido con CharacterController comprobados en partidas reales del editor.
- Frasco, interrupción, pausa, reposición, cofres, trampas y saltos comprobados en Play Mode.
- 60 comprobaciones de armadura, maná, absorción, regeneración y persistencia para las cinco clases.
- Medidas verificadas: flechas 0,408 m; reliquia 0,220 m; esencia 0,184 m; cartuchos 0,228 m.
- El informe `Inquisitor_BloodValidation.txt` registra las pruebas del combate del jefe.
- El informe `CleanHUD_Validation.txt` registra las comprobaciones de interfaz y resoluciones.
- Última revisión: 34 comprobaciones del HUD aprobadas a 1280×720, 1920×1080 y 3840×2160.
- Proyectiles detenidos por un muro sólido sin dañar al jugador protegido; interrupción y victoria verificadas.
- Corregidos avisos del Animator al cambiar entre modelos de clase inactivos. Última sesión: 0 errores y 0 advertencias.
- Compilación completada correctamente. Escena guardada, semilla aleatoria (`fixedSeed: 0`), Play Mode detenido y resolución original restaurada.

Documentación local consultada: `commands/scripts.md`, `commands/runtime.md` y `commands/editor-lifecycle-and-observability.md` del paquete Unity Pipeline instalado.
Se conservan scripts de comprobación en `AgentScripts`, capturas en `Docs` y puntos de restauración en `Docs/Restore_2026-11-02_*`.
Unity Pipeline mantiene auto-tick habilitado para permitir tareas del editor en segundo plano.

## Alcance y siguientes pasos

El piso actual mantiene una ruta de seis salas; todavía no genera bifurcaciones, tiendas o biomas múltiples.
Las pruebas de esta entrega corresponden al modo local. Falta verificar estos cambios en una compilación distribuida y en LAN.
El siguiente paso de diseño es introducir rutas opcionales y ajustar la dificultad mediante partidas completas con cada clase.

Los modelos importados conservan su autoría original. Los materiales, iconos, lógica y ajustes específicos pertenecen a esta adaptación.
