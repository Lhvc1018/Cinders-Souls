# Cinders Souls · Propuesta procedural después del piso 3

Estado: **propuesta**. Nada de este documento está implementado.
Base técnica: `ExpeditionLayout` (grafo + `ChamberPlan`), `ExpeditionFloor` (encuentros), `BasicEnemyEncounter`.
Regla común: separar el sorteo del grafo, la geometría y los encuentros. Guardar la semilla y cada decisión del jugador.

## Reglas comunes de generación (pisos 4–6)

| Paso | Qué decide | Validación |
|---|---|---|
| 1. Grafo | Nº de salas, ramas, sala del jefe y salas especiales | Toda sala alcanzable desde la entrada, en BFS sobre `Links` |
| 2. Geometría | `ChamberShape`, ancho/fondo, pilares, alturas | NavMesh continua de la puerta al centro, con todas las compuertas abiertas |
| 3. Mecanismo del piso | Agua / hornos / campanas | Estado guardado como `FloorChoice` en la semilla de la run |
| 4. Encuentros | Lista de residentes por sala | Puntos de aparición al fondo y en laterales, ≥6 m de la entrada |
| 5. Recompensas | Cofre, altar, mercader, estatua | Nunca dentro de una zona de daño persistente |

Enemigos comunes: crypt residents (hellhound, Creepy Doll, esqueleto) + residentes Muck (Lobo de hollín, Salteador, Gólem funerario y Hondero de la cripta).

---

## Piso 4 · Osario de las mareas

**Fantasía:** una cisterna funeraria bajo el sepulcro. El agua negra sube y baja. Los muertos flotan en nichos.

**Estructura (7–9 salas).**
```
Entrada ─┬─ Galería norte ─ Galería norte 2 ─┐
         │                                   ├─ Depósito central (torno) ─ Esclusa ─ JEFE
         └─ Pasarela sur ── Nichos del sur ──┘
           (atajo visible desde la entrada, se abre al bajar el agua)
```
- Dos rutas alrededor de un **depósito central**. El grafo usa la rama de `ExpeditionLayout` con `merge` forzado en el depósito.
- **Torno opcional** en el depósito. Activarlo baja el agua 1,2 m durante el resto del piso. Abre el atajo sur y revela un cofre en el fondo.
- Salas con **dos alturas**: suelo seco a +1,2 m y canal a 0 m. El agua alta cubre el canal. Avanzar por ella cuesta un 35 % de velocidad, sin daño.

**Encuentros.**
| Sala | Composición base | Comportamiento |
|---|---|---|
| Galerías | 2 Lobos de hollín + 1 Salteador | Los lobos flanquean por las pasarelas (`ApproachGoal` ya alterna los lados) |
| Nichos | 2 Honderos en nichos altos + 2 esqueletos | Los honderos disparan desde arriba y obligan a subir |
| Depósito | 1 Gólem funerario + 3 mixtos | El anillo del gólem se dibuja sobre el agua con contorno blanco |

**Campanero de las crecidas** (élite): toca la campana 3 s antes de cada crecida. Aviso doble: un sonido y marcas de cal que brillan en las paredes a la altura que alcanzará el agua.

**Jefe: Abad de la Sentina.** Inunda un sector circular por ataque (aviso de 1,2 s con anillo). Siempre queda **una orilla seca accesible**. El algoritmo marca 1 de 4 sectores como protegido y nunca lo inunda dos veces seguidas.

**Estética.** Piedra húmeda verde-negra, huesos amarillentos, faroles de aceite azul. Volumen URP con niebla baja y bloom suave en la superficie del agua. Paleta: #1B2124, #3E4A45, #C9B98A y acento #4FA3FF.

**Primer prototipo:** un cruce con dos alturas. Probar la cámara en tercera persona sobre el puente estrecho.

---

## Piso 5 · Fundición de los juramentos

**Fantasía:** una forja donde se fundían las armaduras de los caballeros perjuros.

**Estructura (8–10 salas).**
```
Entrada ─ Corredor ─┬─ Taller A ─ HORNO OESTE ─┐
                    └─ Taller B ─ HORNO ESTE ──┴─ Sala de moldes ─ Vestíbulo ─ JEFE
```
- Alterna **corredores estrechos** (ancho 3) con **talleres amplios** (`ChamberShape.Rectangle`, 14×14 con pilares).
- **Dos hornos en ramas independientes.** El jugador apaga **uno** (palanca, 2 s canalizando):
  - Esa rama se enfría: las rejillas dejan de expulsar vapor.
  - Cambia la embestida del jefe. Horno oeste apagado → el jefe pierde la **carga en línea**. Horno este apagado → pierde la **onda de escoria**.

**Rejillas de vapor.** Pasan de negro a naranja en 1,0 s antes de expulsar (aviso por emisión del material). Cada rejilla tiene un **corredor lateral seguro** de al menos 1,5 m.

**Encuentros.**
| Sala | Composición | Nota |
|---|---|---|
| Corredores | 2 Salteadores + 1 Hondero al fondo | El combo del salteador castiga avanzar en línea |
| Talleres | 3–4 goblins entre coberturas + 1 Gólem funerario que custodia la palanca | El gólem no deja el radio de la palanca (6 m) |
| Sala de moldes | Oleada mixta de 5–7 | Sin rejillas: un respiro antes del jefe |

**Jefe: el Fundidor Ciego.** Apunta al **último ruido metálico**: golpes del jugador, bloqueos y yunques golpeados. Un jugador atento golpea un yunque cerca de una **válvula** para que el martillo la rompa y aturda al jefe 2,5 s.

**Estética.** Hierro negro, escoria naranja, cadenas de bronce oxidado. Luz cálida de horno contra un relleno frío. Paleta: #141210, #3A2A20, #FF7A1A y acento #9C6B3A.

---

## Piso 6 · Coro del eclipse

**Fantasía:** un claustro circular donde un coro de difuntos mantiene el eclipse.

**Estructura (6–8 salas).**
```
              Capilla I
                 │
Entrada ── Claustro central ── Capilla II
                 │
              Capilla III ── Cámara final (JEFE)
```
- Claustro circular (`ChamberShape.Cross` girado + anillo de pilares). Las **tres capillas son visibles desde el centro** con vitrales apagados.
- Cada capilla tiene una campana. El jugador **silencia dos en cualquier orden**. La **tercera** decide qué ataque conserva la Cantora del Velo:
  - Campana I viva → **Onda concéntrica** (anillo que se expande, se salta o se esquiva).
  - Campana II viva → **Coro en abanico** (3 conos telegrafiados).
  - Campana III viva → **Silencio** (zona sin sonido. Los avisos pasan a ser solo visuales).
- Silenciar una campana dispara un encuentro en esa capilla (oleada de 4–6).

**Lectura de avisos.** Cada onda sonora se anuncia con un **anillo dibujado sobre el suelo**. El anillo se dibuja en una capa por encima de partículas y cuerpos (`sortingOrder` alto, ZTest siempre), así sigue visible bajo el polvo plateado.

**Jefe: la Cantora del Velo.** La cámara final reutiliza los tres motivos aprendidos (anillo, abanico, silencio). La campana viva elegida aparece con el doble de frecuencia.

**Estética.** Contraluz lunar, polvo plateado, mármol negro, vitrales que se encienden al silenciar cada campana. Paleta: #0E0F16, #2B2D3F, #C8CCE0 y acento #7A5CFF.

---

## Implementación sugerida (orden)
1. `ExpeditionLayout`: añadir `floor==4..6` con plantillas de grafo propias (como `BuildThirdDescent`).
2. `FloorChoice` en la semilla de la run (torno, horno apagado, campana viva) y su lectura por los jefes.
3. Prototipos sueltos en `LaboratorioContenido`: el cruce con dos alturas, una rejilla de vapor y el anillo sobre partículas.
4. Validar con `cinders-procedural-dungeons`: 200 semillas por piso, BFS a todos los objetivos y NavMesh con las compuertas abiertas.
