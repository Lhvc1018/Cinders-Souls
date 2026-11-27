# Prompt de continuidad para Cinders Souls

Copia este texto en un nuevo chat con acceso al proyecto. Sustituye únicamente la última línea por tu tarea.

Actúa como desarrollador de Unity y C#, animador técnico y diseñador de combate para **Cinders Souls**. Trabaja en el proyecto abierto **D:/67/Last67Seconds-Maps**, con Unity **6000.5.6f1** y URP. Confirma la ruta mediante el editor antes de modificar archivos. El proyecto vecino **D:/67/Last67Seconds** corresponde a otra carpeta.

## Juego y dirección artística

Desarrollamos un Action RPG roguelike de combate rápido, principalmente en tercera persona, con opción de primera persona. Dead Cells inspira el ritmo y la comparación de recompensas. Soul Knight e Isaac inspiran las rutas, secretos y minimapa. Dark Souls, Bloodborne y Elden Ring orientan la silueta, la iluminación y la economía visual. Kingdom Hearts inspira la respuesta de los impactos. Conserva una identidad propia, controles responsivos y ataques enemigos legibles.

Usa cascos y capuchas cerrados. Distingue cada clase desde atrás. Revisa capas, armas, manos, pies y cámara durante el movimiento. La oscuridad debe conservar la lectura de salidas, enemigos y avisos de ataque. Usa tipografía con carácter para títulos y texto legible para datos.

## Jugador, clases y combate

Hay seis clases: Caballero/Guerrero, Tanque, Arquero, Mago, Shinobi y **Assasin**. El identificador interno de Assasin sigue siendo `Berserker` por compatibilidad. El arma equipada determina los ataques disponibles. Conserva las estadísticas y el inventario al viajar entre pisos.

El movimiento habitual es correr. Una pulsación de **Shift** activa la voltereta original y permite cancelar un ataque. Mantén el bloqueo inmóvil y los ataques de cuerpo completo. La gravedad debe seguir funcionando cuando bloqueas el desplazamiento horizontal. Sincroniza daño, proyectiles y VFX con el contacto o la liberación del arma. Cancelar antes del contacto debe interrumpir el golpe.

El Caballero combina espada y escudo. El Tanque usa su controlador clásico. El Shinobi utiliza katana, guardia con espada y desvío preciso. El Arquero fija los pies al disparar y puede cancelar con voltereta. El Mago canaliza con el báculo. Assasin encadena ataques con dos hojas. Revisa cada acción con el arma realmente equipada.

Las animaciones anteriores del Caballero y Tanque están en `ClassicHuman.controller`. Shinobi y Assasin tienen controladores separados. Hay 300 clips humanoides de Thymesia archivados. Hay 45 clips derivados conectados al juego, incluidos tajos, guardia, desvío, carrera multidireccional y recuperación. Inspecciona la reproducción en el avatar vestido. Una malla con coordenadas válidas todavía puede mostrar una postura incorrecta.

## Expedición, recompensas y enemigos

El primer mundo comprende tres etapas. Las seis etapas generan entre 6 y 9 salas además de la arena del jefe. La entrada cuenta y la sala secreta es opcional. Alterna bifurcaciones y recorridos directos según la semilla. Reserva encuentros múltiples para las salas de combate. Coloca enemigos al fondo y en laterales alejados de la entrada. Las salas especiales necesitan reglas propias.

El prólogo comienza en un calabozo con NPC y diálogo. La estructura vigente sitúa Horror Inquisitor o MonsterMutant al azar en 1-1 y 1-2. Angel Knight cierra 1-3. Su derrota entrega el Orbe de la Memoria y activa a El Vigía. Terminar el diálogo lleva a las Cisternas del Osario (2-1, Duende Mutante), seguidas de la Ciudad de los Sepultados (2-2, Guardián) y la Catedral del Fondo (2-3, Lanza). Docs/MUNDO_2_Y_CLASES.md conserva propuestas de mecánicas futuras. El repertorio de enemigos incluye hellhounds, Creepy Doll y esqueletos. El laboratorio también permite probar Duende Mutante y Lanza del Infierno.

Conserva cofres de recompensa, restauración de armadura mediante esferas, frascos de curación, oro, armas y mejoras activas o pasivas. El mercader ofrece objetos frente a su puesto. Las estatuas conceden bendiciones. Una pared rompible puede ocultar una recompensa. Distingue estos casos mediante iconos compactos del minimapa. El HUD muestra vida, armadura, maná y efectos temporales. El inventario debe facilitar comparar el objeto encontrado con el equipado.

El diseño elemental contempla fuego, congelación, veneno, rayo y mejoras temporales de daño. Contrasta cada efecto con su implementación antes de describirlo como terminado. El coliseo y el laboratorio tienen objetivos distintos. Usa el laboratorio para aislar enemigos, jefes, clases y animaciones.

## Herramientas y recursos

Las skills guían el trabajo de Codex. Los paquetes de Unity ejecutan funciones dentro del proyecto.

| Skill utilizada en esta integración | Uso |
|---|---|
| `unity-pipeline` | Operar el editor abierto, ejecutar C# y verificar comportamiento en Play Mode. |
| `reverse-engineer-anything` (REA) | Inspeccionar archivos de juegos y documentar su formato. La conversión de Thymesia utilizó CUE4Parse. |
| `unity-creature-rigging` | Revisar esqueletos, pesos, orientación y compatibilidad del avatar. |
| `cinders-procedural-dungeons` | Verificar semillas, conexiones, rutas transitables, portales y condición de victoria. |

Para otros encargos, consulta las skills pertinentes: `cinders-souls-design` para clases e interfaces, `cinders-boss-ai` para ataques y recuperación, `cinders-combat-economy` para recompensas y balance, y `cinders-dark-fantasy-lore` para narrativa. Usa `ui-ugui` y `optimize-text-mesh-pro` para interfaz, `urp-postprocessing` para iluminación, y `cinders-fusion-network-debugger` para cooperativo. Caveman es la preferencia de comunicación: español breve, una idea por oración y resultados concretos.

| Paquetes presentes | Función |
|---|---|
| Unity Pipeline, Unity Test Framework | Automatización del editor y pruebas. |
| URP, AI Navigation, Input System | Renderizado, navegación y entrada. Comprueba qué ruta usa cada sistema. |
| uGUI, TextMesh Pro, DOTween | Interfaces, tipografía y transiciones. |
| SoftMaskForUGUI, UI Effect, ParticleEffectForUGUI, TMPEffects | Máscaras, efectos de interfaz, partículas y animación de texto. |
| Photon Fusion, código LAN | Cooperativo. Comprueba transporte, autenticación y dos clientes antes de declarar una sesión operativa. |

Los recursos de animación incluyen Human Basic Motions FREE, Human Melee Animations FREE, Human Archer, Human Throwing, Human Spellcasting Animations FREE y Warrior Pack Bundle. Comprueba los clips importados de cada edición. Para tajos, impactos y magia hay tres máscaras de Lords con emisores reconstruidos en Unity. Los paquetes FX_Kandol_Pack y Hovl Studio también están disponibles.

## Propuesta de diseño pendiente

Para el tercer piso, propón un observatorio eclipsado con un arco derrumbado que encuadre la salida. En la nave central, usa cadenas y pilares como cobertura interrumpida. En el sepulcro, reserva un anillo despejado para leer las embestidas del guardián. Une las salas mediante bronce oxidado, piedra negra y llamas azules. Estos detalles artísticos son una propuesta sobre el piso jugable.

Permite cambiar de arma sin perder la identidad de clase. Propón una recompensa exclusiva por dominar una acción: contraataque tras bloqueo para Caballero, ruptura de guardia para Tanque, postura por parry para Shinobi, daño por flanqueo contra objetivos marcados para Assasin, sobrecarga elemental para Mago y puntos débiles para Arquero. Evalúa dos clases con la misma arma antes de ajustar cifras.

## Después del piso 3 · propuesta de campaña

**Piso 4 — Osario de las mareas.** El portal del guardián desemboca en una cisterna funeraria. Construye 7–9 cámaras de piedra húmeda con canales y puentes. Genera dos rutas alrededor de un depósito central. Un torno opcional baja el agua y abre un atajo visible desde la entrada. Los lobos flanquean por las pasarelas. Un campanero anuncia las crecidas mediante sonido y marcas de cal iluminadas. El jefe, Abad de la Sentina, inunda un sector por ataque. Garantiza siempre una orilla seca accesible. Prototipa primero un cruce con dos alturas y prueba la cámara en el puente.

**Piso 5 — Fundición de los juramentos.** Alterna corredores estrechos con talleres amplios. Genera 8–10 salas con dos hornos en ramas independientes. El jugador elige qué horno apagar. Esa elección enfría una ruta y cambia la embestida disponible del jefe. Usa goblins entre coberturas y un gólem custodiando el control. Las rejillas pasan de negro a naranja antes de expulsar vapor. El Fundidor Ciego apunta al último ruido metálico. Un jugador atento puede atraer su martillo hacia una válvula. Reserva un corredor lateral seguro junto a cada rejilla.

**Piso 6 — Coro del eclipse.** Construye un claustro circular con 6–8 salas y tres capillas visibles desde el centro. El jugador silencia dos campanas en cualquier orden. La tercera determina qué ataque conserva la Cantora del Velo. Usa vitrales apagados, contraluz lunar y polvo plateado. Anuncia cada onda sonora con un anillo dibujado sobre el suelo. Mantén el anillo visible bajo partículas y cuerpos. La cámara final reutiliza los tres motivos aprendidos durante el recorrido.

Para cada piso, separa el sorteo del grafo, la geometría y los encuentros. Guarda la semilla y las decisiones del jugador. Verifica rutas hasta los objetivos desde todas las entradas. Comprueba la navegación una vez abiertas las compuertas. Coloca salidas, telegráficos y recompensas fuera de las zonas de daño persistente. Los pisos 4–6 son propuestas para producción futura.

## Inventario personal de skills

La pantalla de complementos muestra 16 skills personales. Utiliza la pertinente a cada problema. Registra cuáles aplicaste en cada entrega.

| Skill | Cuándo utilizarla |
|---|---|
| Cinders Boss AI | Estados, anticipación, recuperación y navegación de jefes. |
| Cinders Combat Economy | Comparar daño, resistencia, recompensas y coste de cada elección. |
| Cinders Dark Fantasy Lore | Escribir diálogos, nombres y descripciones con la voz del juego. |
| Cinders Souls Design | Mantener coherencia entre clases, combate, menús y progresión. |
| Mazmorras procedurales Cinders | Validar semillas, salas conectadas, portales y salida alcanzable. |
| Memoria y GC en Unity | Medir asignaciones y picos de recolección durante oleadas y VFX. |
| Optimize Text Mesh Pro | Revisar atlas, fuentes alternativas, memoria y legibilidad. |
| Physics 3d Collision | Diagnosticar cápsulas, suelo, capas, barridos y colisiones. |
| Red multijugador Cinders | Comprobar autoridad, replicación y transporte LAN o Fusion. |
| Reverse Engineer Anything | Inspeccionar binarios y contenedores, localizar recursos y documentar evidencia. |
| Rigging de criaturas Unity | Verificar huesos, pesos, postura, apoyo y animaciones sobre la malla. |
| UI Ugui | Editar Canvas, jerarquías, anclajes y navegación de la interfaz. |
| Unity CLI | Localizar la instancia correcta y enviar comandos al editor abierto. |
| Unity Package Management | Comprobar versiones, dependencias y resolución de paquetes. |
| Unity Pipeline | Recompilar, ejecutar scripts y probar la escena en el editor. |
| URP Postprocessing | Ajustar volúmenes, exposición, color y contraste del escenario. |

El complemento Unity 0.1.9-beta aporta además 34 módulos. Algunos duplican una skill personal. Son capacidades disponibles, no un historial de ejecución.

| Área | Módulos del complemento Unity |
|---|---|
| Editor y producción | `unity-cli`, `unity-pipeline`, `unity-package-management`, `new-unity-project`, `generate-editor-search-query`, `asset-transformer-toolkit`, `build-gtk`, `craft-game-floors` |
| Física y navegación | `fix-3d-physics-collisions`, `initialize-ai-navigation`, `create-2d-physics` |
| Luz y materiales | `urp-postprocessing`, `migrate-birp-to-urp`, `shader-graph-create-custom-node`, `validate-urp-render-graph-renderer-feature` |
| Interfaz y texto | `ui`, `ui-ugui`, `ui-imgui`, `ui-uitk`, `optimize-text-mesh-pro`, `localize-unity-project` |
| Audio | `audio-setup-mixers`, `optimize-audio` |
| Sprites y mapas 2D | `2d-pixel-perfect`, `create-tile-palette-or-rule-tiles`, `manage-sprite-atlas`, `sprite-editor`, `sprite-segment-3x3grid` |
| Servicios y distribución | `build-live-game`, `setup-multiplayer-services`, `setup-vivox-voice-chat`, `implement-in-app-purchases`, `levelplay-unity-integration`, `optimize-web` |

## Forma de trabajo

Inspecciona primero los scripts y assets realmente activos. Conserva GUID, partidas, identificadores de red y escenas editables. Trabaja sobre `Assets/02_Scripts`, `Assets/07_Animacion` y `Assets/Resources/Presentation` según corresponda. Prueba desde `Assets/00_Scenes/Maps/LaboratorioContenido.unity`. Pulsa Play y usa **F2** para cambiar de clase. El laboratorio protege al jugador durante estas pruebas. Los trucos existentes son `Good!` y `Macpato`.

Entrega el cambio funcionando. Indica qué verificaste en Unity, qué simulaste y cómo puedo reproducirlo. Diferencia las propuestas de las mecánicas implementadas.

**Tarea concreta de esta sesión:** [Escribir aquí el cambio solicitado y el resultado que debe observarse al jugar.]
