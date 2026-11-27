# Prompt maestro de continuidad — Cinders Souls

Actualizado el 10 de noviembre de 2026. Copia este documento completo al iniciar otro chat con acceso al proyecto.

## 1. Encargo y reglas de continuidad

Actúa como desarrollador Unity/C#, animador técnico, diseñador de combate y diseñador de niveles para Cinders Souls.
Continúa el proyecto existente. Inspecciona los archivos activos antes de cambiar sistemas.
No reinicies el juego desde cero ni sustituyas sistemas funcionales por demostraciones aisladas.
Conserva referencias, GUID, identificadores de clases, materiales, inventario y progresión entre etapas.
Responde en español. Usa frases directas, una idea por frase y máximo 20 palabras por oración.
Trabaja hasta comprobar el resultado. Distingue implementación, prueba automatizada, revisión visual y propuesta.
Una prueba anterior no demuestra que todas las regresiones actuales estén resueltas.
No declares una animación correcta solamente porque compila o porque sus coordenadas son válidas.
No presentes assets disponibles como sistemas integrados ni skills disponibles como herramientas utilizadas.

## 2. Proyecto y entorno

- Proyecto real: `D:/67/Last67Seconds-Maps`.
- Carpeta vecina distinta: `D:/67/Last67Seconds`. El directorio inicial puede apuntar allí incorrectamente.
- Editor: Unity `6000.5.6f1`.
- Ejecutable del editor: `D:/MRD/Editor/Unity.exe`.
- Renderizado: Universal Render Pipeline `17.5.0`.
- Plataforma de trabajo: Windows. Los últimos errores de Toon afectaban Direct3D.
- Scripts principales: `Assets/02_Scripts`.
- Escenas: `Assets/00_Scenes` y `Assets/00_Scenes/Maps`.
- Prefabs: `Assets/01_Prefabs`.
- Modelos: `Assets/03_Models`.
- Materiales: `Assets/04_Materials`.
- Música: `Assets/05_Music`.
- Efectos: `Assets/06_Effects`.
- Animaciones: `Assets/07_Animacion`.
- Contenido cargado dinámicamente: `Assets/Resources`.
- Constructores y verificaciones auxiliares: `AgentScripts`, fuera de `Assets`.

Controla el editor abierto mediante Unity CLI y Unity Pipeline.
Esta instalación expone comandos planos: `editor_status`, `editor_play`, `editor_stop`, `run_script`, `eval`, `recompile`.
No uses `editor status`: anteriormente produjo `No command named 'editor status'`.

```powershell
& "$env:LOCALAPPDATA/Unity/bin/unity.exe" command editor_status --project-path D:/67/Last67Seconds-Maps
```

No importes paquetes durante Play Mode. Conserva cambios de escena antes de cambiar de escena.
Los arreglos permanentes de paquetes deben quedar fuera de `Library/PackageCache`.

## 3. Identidad del juego

Cinders Souls es un Action RPG roguelike 3D, principalmente en tercera persona.
Existe soporte de primera persona, pero la prioridad artística y mecánica es tercera persona.
Dead Cells inspira velocidad, recompensas y comparación de equipamiento.
Soul Knight e Isaac inspiran bifurcaciones, minimapa, secretos y exploración por habitaciones.
Dark Souls, Bloodborne y Elden Ring inspiran arquitectura gótica, tensión, personajes y presentación sobria.
Kingdom Hearts inspira respuesta inmediata e impactos claros.
Lords of the Fallen y Thymesia sirvieron como referencias de combate y animación.
Conserva identidad propia y registra procedencia y condiciones de uso de recursos externos.

La atmósfera debe resultar oscura, macabra y opresiva, pero siempre legible.
No confundas oscuridad con zonas completamente negras.
Evita personajes hechos de formas primitivas evidentes, interfaces saturadas y proyectiles que parezcan simples bolitas.
El usuario prefiere cascos y capuchas cerrados.
Cada clase debe distinguirse por silueta, equipo y postura.

## 4. Clases y modelos

Existen seis identidades principales: Caballero/Guerrero, Tanque, Arquero, Mago, Shinobi y Assasin.
Assasin conserva el identificador interno `Berserker` por compatibilidad.
No renombres ese identificador sin migrar referencias y datos.

| Clase | Dirección actual | Aspectos que deben preservarse |
|---|---|---|
| Caballero | Espada y escudo, defensa frontal y tajos | Animaciones clásicas restauradas y escudo visible |
| Tanque | Resistencia y armas pesadas | Controlador clásico restaurado y peso legible |
| Arquero | Arco, silueta reconocible y disparo estable | Pies detenidos durante disparo; cancelación mediante voltereta |
| Mago | Báculo, conjuros y efectos elementales | Liberación dirigida al enemigo, nunca accidentalmente hacia el suelo |
| Shinobi | Katana, guardia con espada y parry | El usuario confirmó recientemente sus animaciones como correctas |
| Assasin | Dos hojas y ataques encadenados | Identidad ágil; identificador histórico `Berserker` |

Se sustituyeron representaciones simples por personajes vestidos y texturizados.
Se trabajaron capas, cascos, capuchas, agarres y siluetas.
El Shinobi recibió materiales oscuros, detalles bronceados y ajustes de casco y cresta.
Su rediseño visual debe conservar el esqueleto y las animaciones aceptadas.
El Arquero debe parecer arquero desde atrás, incluyendo equipo y accesorios pertinentes.
Revisa penetración de capas, hombros, armas y manos durante ataques y volteretas.

## 5. Movimiento, combate y animación

El desplazamiento habitual debe sentirse como carrera, sin obligar a caminar lentamente.
El código actual también contempla sprint mediante Ctrl izquierdo.
Una pulsación de Shift izquierdo activa la voltereta original.
Esta decisión reemplaza la propuesta antigua de esquiva corta seguida de doble Shift.
No vuelvas al sistema de doble pulsación.

Bloquear impide desplazamiento horizontal.
Atacar debe usar cuerpo completo y evitar arrastrar pies por locomoción simultánea.
Mantén gravedad y colisiones mientras restringes movimiento horizontal.
La voltereta puede cancelar ataques y lanzamientos.
Cancelar antes del contacto debe cancelar daño y proyectiles pendientes.
No combines indiscriminadamente root motion, desplazamiento manual y correcciones de huesos.
Comprueba cápsula, suelo, modelo y cámara conjuntamente.

El arco inmoviliza durante el disparo, pero admite cancelación evasiva.
El bloqueo del Shinobi usa espada, no escudo.
El parry debe responder al impacto dentro de una ventana breve y producir reacción enemiga.
La petición original situaba esa ventana entre 0.2 y 0.4 segundos.
Comprueba el valor efectivo en `ShinobiGuard.cs` antes de modificarlo.
Existe sistema de fijación de objetivo en `PlayerLockOn.cs`.

Recursos de animación aportados:

- Human Basic Motions FREE: locomoción, saltos y movimiento direccional.
- Human Melee Animations FREE: espada, escudo, armas dobles y otras posturas disponibles.
- Human Spellcasting Animations FREE: el usuario aclaró que este paquete era su referencia a «Magic Spells».
- Human Archer Animations FREE: arco y acciones correspondientes.
- Human Throwing Animations FREE: arrojadizas.
- Warrior Pack Bundle 1, 2 y 3 FREE: variantes de combate y movimiento.
- RPG Animations Pack FREE y recursos identificados como Sword Animation.
- Referencia histórica: Katana Sword Animation Pack, Asset Store ID `257235`.

Comprueba el inventario importado: un vídeo promocional puede mostrar clips ausentes en la edición gratuita.
El historial registra 300 clips humanoides archivados de Thymesia y 45 derivados conectados.
Recuenta y verifica asignaciones antes de afirmar que esos números siguen representando el estado actual.
El Caballero y Tanque usan `ClassicHuman.controller` como referencia restaurada.
Shinobi y Assasin tienen controladores específicos.

Archivos clave:

- `Assets/02_Scripts/Characters/PlayerCharacter.cs`
- `Assets/02_Scripts/Characters/PlayerCharacter.Motor.cs`
- `Assets/02_Scripts/Characters/PlayerCombat.cs`
- `Assets/02_Scripts/Characters/PlayerCombat.Combo.cs`
- `Assets/02_Scripts/Characters/ShinobiGuard.cs`
- `Assets/02_Scripts/Presentation/ActorPresentation.cs`
- `Assets/02_Scripts/Presentation/ClassAnimationControllers.cs`
- `Assets/02_Scripts/Presentation/HumanoidWeaponGrip.cs`
- `Assets/02_Scripts/Presentation/HumanoidSupportHand.cs`
- `Assets/02_Scripts/Presentation/HumanGroundClearance.cs`

## 6. Armas, agarres y correcciones concretas

El ataque básico de espada debe ser un tajo, no una única estocada.
Los combos deben respetar las ventanas del clip y evitar golpes múltiples accidentales.
Espada y escudo deben mantener agarres independientes y naturales.
Las armas dobles necesitan orientación coherente en ambas manos.

Se corrigió acumulación de rotación en `WeaponGroundGuard`.
Ahora restaura la rotación local de reposo antes de corregir proximidad al suelo.
La verificación registrada mantuvo estabilidad durante 120 correcciones de una misma pose.
También se ajustó el escudo mediante rotación y desplazamiento local.
No interpretes esta prueba como revisión visual completa de todas las armas.

Spear/Lanza presentó repetidamente problemas de escala y penetración del suelo.
Conserva esta regresión en la lista de pruebas visuales del laboratorio.

## 7. Vida, armadura, maná y recompensas

El HUD muestra vida roja, armadura gris y maná azul, con valores actuales y máximos.
La regeneración pasiva de armadura está desactivada: `ArmorRegen=0`.
La armadura se recupera mediante esferas de recompensa y mecanismos explícitos.
No restaures regeneración automática por clase accidentalmente.

La estructura de recompensas contempla cofres, armas, reliquias activas, pasivas, oro y frascos de curación.
La solicitud vigente exige un cofre central al derrotar al último enemigo de una sala de combate.
Al abrirlo deben aparecer esferas pequeñas de armadura.
El botín adicional utiliza probabilidades para mejoras, armas y pociones poco frecuentes.
Comprueba probabilidades reales antes de publicar porcentajes.
El oro se incorpora automáticamente al inventario.
Mantén escalas consistentes: cofres y pociones no deben ser gigantes.
El objeto debe salir visualmente del interior del cofre, no aparecer a un lado.

Existen mercaderes y estatuas de bendición.
La sala de estatua debe ser pequeña, sin combate, con la estatua como centro visual.
El mercader ocupa un puesto reconocible y presenta sus objetos delante.
Evita un rótulo flotante permanente del mercader; su identificación principal pertenece al minimapa.

## 8. Hechizos y estados elementales

`RunSpellbook.cs` contiene seis hechizos:

| Hechizo | Función actual en código | Coste de maná |
|---|---|---:|
| Bola de fuego | Proyectil con quemadura | 18 |
| Escarcha inmóvil | Proyectil con congelación | 25 |
| Veneno de la cripta | Proyectil con veneno | 22 |
| Rayo del exiliado | Impacto eléctrico | 28 |
| Arde llama en mí | Multiplicador de daño 1.15 durante 20 segundos | 25 |
| Llama dame poder | Multiplicador 1.8 durante 12 segundos, con drenaje de vida | 35 |

El drenaje configurado consume 7% de vida máxima por segundo, sin bajar del 20% por ese drenaje.
Los dos multiplicadores pueden combinarse según el código actual.
El libro cambia selección con C y lanza con T.
Existen restricciones para LAN en este sistema; no declares sincronización cooperativa completa.
`ElementalStatus.cs` gestiona estados.
Las mejoras temporales y acumulaciones deben permanecer legibles en el HUD.
Vuelve a comprobar balance, interrupciones y muerte del objetivo al modificar proyectiles.

## 9. Mazmorra procedural y progresión vigente

La campaña implementada contiene seis etapas, agrupadas en dos mundos.
Cada etapa genera entre 6 y 9 habitaciones además de la arena del jefe.
La entrada cuenta dentro del conjunto previo al jefe.
La sala secreta, cuando existe, es opcional y también forma parte del total generado.
El total del nivel resulta entre 7 y 10 habitaciones incluyendo al jefe.
No interpretes esta regla como 6–9 combates obligatorios.

El generador permite recorridos directos y bifurcaciones según semilla.
Hay habitaciones rectangulares, cruzadas y con esquinas recortadas.
Incluye trampas, pilares, espacios especiales, corredores y paredes secretas rompibles.
La puerta del jefe comprueba la limpieza de salas públicas anteriores.
La sala secreta no bloquea el acceso al jefe.
Los enemigos aparecen al fondo y en laterales alejados de la entrada.
Mantén encuentros múltiples; evita regresar al bug de un único enemigo por habitación.

| Índice interno | Etapa | Entorno | Jefe vigente |
|---:|---|---|---|
| 1 | 1-1 | Mazmorra base | Horror Inquisidor o Monster Mutant, según semilla |
| 2 | 1-2 | Mazmorra base | Horror Inquisidor o Monster Mutant, según semilla |
| 3 | 1-3 | Mazmorra y santuario ampliado | Angel Knight |
| 4 | 2-1 | Cisternas del Osario | Duende Mutante |
| 5 | 2-2 | Ciudad de los Sepultados | Guardián del Abismo |
| 6 | 2-3 | Catedral del Fondo | Lanza del Infierno |

`RunDirector.FinalExpeditionFloor` vale 6.
Documentos antiguos proponían otros jefes y conteos para pisos 4–6.
La tabla anterior sustituye esas propuestas para la campaña vigente.

Archivos principales:

- `Assets/02_Scripts/Expedition/ExpeditionLayout.cs`
- `Assets/02_Scripts/Expedition/ExpeditionFloor.cs`
- `Assets/02_Scripts/Expedition/ExpeditionFloor.Contents.cs`
- `Assets/02_Scripts/Expedition/ExpeditionFloor.Secret.cs`
- `Assets/02_Scripts/Expedition/ExpeditionFloor.WorldTwo.cs`
- `Assets/02_Scripts/Expedition/ExpeditionFloor.AngelSanctuary.cs`
- `Assets/02_Scripts/Expedition/DescentPortal.cs`

## 10. Arquitectura del Mundo 2 y últimas ampliaciones

El Mundo 1 conserva la mazmorra base.
El santuario del Angel Knight aumenta tamaño e incorpora arcos, pilares y estatuas.
Su arena usa un plan mayor que los jefes normales.
Las grandes arcadas conservan libre el espacio transitable.

El Mundo 2 utiliza módulos distintos, no solamente colores nuevos.
Se prepararon recursos desde KayKit Halloween, KayKit Dungeon y Kenney Mini Arena.
Los prefabs derivados están en `Assets/Resources/GothicArchitecture`.
`AgentScripts/BuildGothicArchitecture.cs` construye esos recursos.

Módulos preparados: Arcade, Facade, Tracery, Paving, Grating, Monument, Ossuary, BareTree, Shrine y BrokenPillar.
Ampliación reciente: TimberDeck, DockCargo, ScaffoldWindow, WakeTable, VotiveShelf y ChoirBanner.
Los módulos recientes conservan texturas de origen adaptadas a materiales URP de tonos apagados.

- 2-1: arcadas, rejas, pavimento de rejilla y elementos de muelle.
- 2-2: fachadas funerarias, árboles secos, ventanas con andamios y composición más abierta.
- 2-3: arcadas dobles, cercados elevados, arcos interiores, santuarios y estandartes.

Se añadieron galerías laterales elevadas en salas compatibles.
Su altura es 1.6 metros.
Cada extremo incorpora una escalera de ocho peldaños, con desniveles de 0.2 metros.
La entrada y las arenas del Mundo 2 admiten estas galerías.
Otras habitaciones las reciben cuando su forma y tamaño permiten circulación segura.
Los secretos incorporan mobiliario votivo y luz cálida.

Pendientes artísticos opcionales: agua dinámica, válvulas, inundaciones, coberturas destructibles y grandes cambios ambientales.
Estos mecanismos fueron propuestas; no deben describirse como implementados.

## 11. Jefes y enemigos

Roster principal: Angel Knight, Duende Mutante, Guardián del Abismo, Horror Inquisidor, Lanza del Infierno y Monster Mutant.
Monster Mutant procede del prefab existente con animaciones y recibió ajustes de materiales.
Su recurso jugable se integra mediante `Resources/ExpeditionEnemies`.

El historial incluye modelos y animaciones procedentes de Muck para Gronk, Guardian y Spear.
Sus nombres de presentación solicitados son Duende Mutante, Guardián del Abismo y Lanza del Infierno.
Algunos textos históricos contienen «Gurdi del Abismo»; revisa identificadores antes de normalizarlos.
El usuario pidió integrar también enemigos comunes de Muck.
Verifica el catálogo activo para distinguir importación, disponibilidad en laboratorio y aparición procedural.

Otros enemigos incluyen hellhounds, Creepy Doll y esqueletos.
No deben compartir únicamente una IA idéntica con modelos diferentes.
Diferencia velocidad, alcance, ataques, recuperación y proyectiles.
Los hellhounds contemplan ataques de fuego.
Horror Inquisidor contempla sangre, daño persistente y ataques a distancia.
Angel Knight recibió trabajo de agresividad, movilidad, ataques y VFX.
Revisa pies, telegráficos, colisiones, ventanas de daño y recuperación en cada jefe.

Archivos de IA: `Assets/02_Scripts/Bosses/BossCharacter.cs` y sus parciales `Angel`, `Blood` y `Wardens`.
Las barras de vida de enemigos normales deben permanecer pequeñas.
No declares todos los jefes terminados sin pruebas visuales independientes.

## 12. Orbe, El Vigía y transición

Derrotar al Angel Knight en 1-3 crea el Orbe de la Memoria.
Interactuar con E recoge el orbe y muestra a El Vigía.
El diálogo bloquea temporalmente el control del jugador.
Terminar sus páginas permite viajar a 2-1.
No omitas esta condición cuando se abra el portal normal.

Implementación: `Assets/02_Scripts/Expedition/DestinyEncounter.cs`.
Prefab del NPC: `Resources/WorldProgression/Watcher`.

Diálogo instalado, preservado literalmente:

> El ángel guardaba una promesa, no un tesoro. Ahora la promesa te pertenece.
>
> Has obtenido uno de los tres Orbes del Destino. Este conserva la memoria de quienes desafiaron su final.
>
> Los otros dos siguen latiendo bajo tierras que el sol abandonó. Reunirlos abre el camino; elegir qué hacer con ellos será tu condena.
>
> Más allá del umbral yace el Osario de las Mareas. Busca la campana que suena bajo el agua. Yo vigilaré tu regreso.

La existencia narrativa de tres orbes no demuestra que los tres encuentros estén implementados.

## 13. UI, minimapa y herramientas de prueba

El menú principal ofrece descenso, coliseo infinito, cooperativo LAN, opciones, salir y trucos.
Parte del contenido se construye en ejecución.
Una escena vacía fuera de Play Mode no implica pérdida del contenido generado.
Conserva herramientas de autoría y explica qué objetos son editables antes de ejecutar.

El HUD sigue una composición sobria inspirada en Elden Ring.
El inventario presenta equipamiento, consumibles, pasivas, mejoras y oro.
La comparación de reemplazos toma como referencia Dead Cells.
El minimapa utiliza salas cuadradas y conexiones rectangulares, siguiendo Soul Knight.
Debe distinguir sala actual, visitada, pendiente, entrada, jefe, cofre y eventos especiales.
Su indicador muestra mundo y etapa.
Se ampliaron estructuras del minimapa para soportar hasta 10 habitaciones.

Escena de pruebas: `Assets/00_Scenes/Maps/LaboratorioContenido.unity`.
El laboratorio permite seleccionar enemigos y jefes, cambiar de clase con F2 y probar sin morir.
No confundas laboratorio con coliseo infinito.
El coliseo contempla oleadas y puertas de aparición con apertura y cierre.
Verifica su comportamiento actual antes de afirmar una cobertura completa de oleadas infinitas.

## 14. Trucos vigentes

| Código | Comportamiento |
|---|---|
| `Good!` | Activa inmortalidad, maná ilimitado y daño letal del jugador |
| `Good! off` | Desactiva ese modo |
| `Macpato` | Alterna el modo de riqueza |
| `jam` | Desbloquea selección de etapa y generación para pruebas |

`Good!` es idempotente: repetirlo no desactiva el modo.
`Good!` admite variaciones de mayúsculas y espacios exteriores.
`Macpato` conserva comparación literal en el código actual.
Los trucos sobreviven cambios de escena dentro de la sesión.
El reinicio del subsistema los restablece; no equivalen a una preferencia permanente guardada.
Se corrigió gasto de reliquias para respetar maná infinito.

El selector `jam` abarca índices 1–6 y muestra sus equivalentes 1-1 a 2-3.
«Generar y entrar» permite inspeccionar una etapa sin completar las anteriores.
La navegación de prueba bloquea entradas duplicadas durante carga y evita esa ruta cuando LAN está activo.

## 15. VFX, iluminación y materiales

Se integraron estelas de hoja y efectos de impacto vinculados a ventanas de contacto.
`BladeContactTrail` gestiona la estela del arma.
La cancelación debe detenerla junto con el ataque.
El parry perfecto utiliza impacto y chispas.
Existen recursos de Lords convertidos y emisores reconstruidos en Unity.
Eso no equivale a importar automáticamente todos los sistemas originales del juego.

Archivos relevantes: `PresentationVfx`, `ImportedAttackVfx`, `LordsAttackVfx`, `SpellFlightVisual`, `StaffBeamVisual` y `PlayerVisibilityLight`.
También existen recursos FX_Kandol_Pack, Hovl Studio, fuego, rayos y sangre.
Selecciona efectos realmente compatibles con URP.
Los materiales rosados deben corregirse en su shader y dependencias, no ocultarse mediante cambio arbitrario de color.
La iluminación necesita luz ambiental y relleno suficientes para leer el combate.
Conserva contraste gótico sin perder visibilidad.

## 16. Correcciones recientes: TextMesh Pro y Toon

### TextMesh Pro

El aviso de recursos esenciales faltantes persistía aunque los 43 archivos esperados existían.
Se detectó una versión ausente en TMP Settings frente a la versión esperada `2`.
Se actualizó el ajuste y se reimportaron los recursos esenciales oficiales del paquete uGUI.
Se verificaron versión `2`, LiberationSans SDF y Default Style Sheet.
No se verificó completamente un sprite predeterminado: permanecía nulo tras la importación oficial.
No afirmes que todos los sprites TMP están validados.

Copia anterior: `D:/67/CindersBackups/TMP-settings-before-essentials-refresh.asset`.
El popup de Warrior aparecía repetidamente durante importaciones.
`Assets/ExplosiveLLC/Editor/SetupInputLayers.cs` dejó de abrirlo automáticamente.
Sus instrucciones siguen disponibles desde `Tools/Warrior Animation/Input and Tag Presets`.

### Toon y Toon(Tessellation)

URP generaba una redeclaración de `_Surface` en ambos shaders.
Se integró Toon `0.11.0-preview` como paquete embebido en `Packages/com.unity.toonshader`.
`packages-lock.json` confirma `source: embedded`.
Los GUID y las asignaciones existentes se conservan.

Archivo corregido: `Packages/com.unity.toonshader/Runtime/UniversalRP/Shaders/UniversalToonInput.hlsl`.
La declaración antigua pasó a `_UTSReservedSurface`, preservando espacio en el buffer de material.
Se incluyó explícitamente `SurfaceType.hlsl`.
Se evitó duplicar `LitInput.hlsl`, incorporado por `LitForwardPass` en URP 17.5.
La prueba detectó y resolvió también la redeclaración secundaria de `_BaseMap_ST`.

`AgentScripts/VerifyToonCompatibility.cs` comprueba ambos shaders.
Compila ForwardLit fragment para D3D y Windows64 con la combinación reportada por el usuario.
Resultado comprobado: PASS para Toon y Toon(Tessellation).
Esta prueba no cubre todas las variantes, plataformas ni el resultado visual de todos los materiales.
Una actualización futura de Toon requiere revisar y migrar este parche local.

## 17. Pruebas y evidencias registradas

| Verificación | Resultado registrado | Límite |
|---|---|---|
| `VerifySixStages.cs` | 768 layouts: 128 semillas por seis etapas; conectividad y tamaño correctos | No sustituye revisión artística |
| Pruebas vivas de seis etapas | Rutas públicas, puerta del jefe y navegación comprobadas | No equivale a seis partidas manuales completas |
| Galerías del Mundo 2 | Acceso NavMesh desde ambos extremos y superficie elevada | Falta valoración artística final del usuario |
| `VerifyWorldProgression.cs` | Distribución 51 Horror / 49 Mutant en 100 semillas; orbe y transición | Distribución observada, no probabilidad garantizada exacta |
| Trucos y viajes | Persistencia en cambios de escena, maná y daño letal comprobados | No valida todas las situaciones cooperativas |
| Intercambio de armas | 120 correcciones estables de una pose | No cubre todas las combinaciones visuales |
| `VerifyToonCompatibility.cs` | Ambos shaders compilan la variante D3D reportada | No cubre catálogo completo de variantes |

Otros scripts históricos prueban roll, cancelación, clases, agarres, laboratorio, hechizos y jefes.
Su existencia no significa que se hayan ejecutado nuevamente tras el último cambio.
Ejecuta solamente pruebas pertinentes y registra sus resultados actuales.

## 18. Diferenciación de clases: propuesta pendiente

La identidad exclusiva sigue siendo una tarea de diseño pendiente de implementación completa.
Permitir armas compartidas no debe eliminar la elección de clase.
Propuesta: ataque básico compartido, técnica exclusiva, recurso propio y afinidades por clase.

| Clase | Propuesta exclusiva |
|---|---|
| Caballero | Bloqueo preciso carga represalia de espada y escudo |
| Tanque | Absorber golpes frontales acumula firmeza para romper postura |
| Shinobi | Parry genera concentración y habilita ejecución |
| Assasin | Marca un enemigo y obtiene beneficios por flanqueo |
| Mago | Combina estados y descarga sobrecarga elemental |
| Arquero | Apuntado estable revela puntos débiles |

Compara clases usando la misma arma antes de ajustar cifras.
Mide daño recibido, tiempo de eliminación y frecuencia de decisiones especiales.
No presentes esta tabla como habilidades ya terminadas.

## 19. Próximas prioridades

1. Mantener estable la compilación y revisar materiales Toon en una escena representativa.
2. Revisar visualmente las seis clases con ataques, guardia, roll, intercambio de armas y hechizos.
3. Confirmar que el Shinobi conserva las animaciones aceptadas.
4. Revisar Lanza del Infierno contra suelo, paredes y cámara, incluyendo escala y alcance.
5. Evaluar artísticamente Mundo 2, galerías, secretos y santuario del Angel Knight.
6. Comprobar recompensas, spawns y navegación en semillas nuevas de todas las etapas.
7. Implementar identidad de clases cuando se concrete el diseño.
8. Validar cooperativo con dos clientes antes de declararlo funcional.

Photon Fusion 2 estuvo bloqueado por `InvalidAuthentication`.
El usuario inició sesión posteriormente, pero eso no demuestra configuración válida ni sincronización correcta.
Hay código LAN y Photon en el proyecto; identifica el transporte activo antes de modificar red.

## 20. Tarea del siguiente chat

Primero confirma la instancia y revisa los archivos relevantes.
Explica brevemente qué vas a corregir y utiliza las skills pertinentes.
Implementa y verifica el cambio autorizado, conservando las decisiones anteriores.
Entrega rutas editadas, pruebas ejecutadas y limitaciones concretas.

**Nueva tarea:** [Escribir aquí el cambio solicitado.]

## 21. Skills disponibles y uso pertinente

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



## 22. Paquetes UPM declarados al cierre

Versiones leídas de `Packages/manifest.json`. La presencia no implica que cada función esté integrada.

| Paquete | Versión o referencia declarada |
|---|---|
| com.coffee.softmask-for-ugui | https://github.com/mob-sakai/SoftMaskForUGUI.git?path=Packages/src#3.6.5 |
| com.coffee.ui-effect | file:Packages/com.coffee.ui-effect |
| com.coffee.ui-particle | https://github.com/mob-sakai/ParticleEffectForUGUI.git?path=Packages/src#v4.14.2 |
| com.luca3317.tmpeffects | https://github.com/Luca3317/TMPEffects.git?path=Package#0.3.2 |
| com.unity.ads | 4.19.0 |
| com.unity.ai.navigation | 2.0.14 |
| com.unity.analytics | 3.8.2 |
| com.unity.collab-proxy | 2.13.3 |
| com.unity.ide.rider | 3.0.38 |
| com.unity.ide.visualstudio | 2.0.26 |
| com.unity.inputsystem | 1.20.0 |
| com.unity.multiplayer.center | 1.0.1 |
| com.unity.nuget.mono-cecil | 1.10.2 |
| com.unity.pipeline | 0.8.0-exp.1 |
| com.unity.purchasing | 4.15.1 |
| com.unity.render-pipelines.universal | 17.5.0 |
| com.unity.test-framework | 1.7.0 |
| com.unity.timeline | 1.8.12 |
| com.unity.toonshader | 0.11.0-preview |
| com.unity.ugui | 2.5.0 |
| com.unity.visualscripting | 1.9.11 |

## 23. Recursos importados y diferencia entre skills y plugins

Las skills son instrucciones de trabajo para el asistente.
Los paquetes y assets de Unity aportan código, shaders, modelos, fuentes o animaciones al juego.
No instales herramientas adicionales solamente para aumentar el número de complementos.

| Recurso | Uso previsto o existente | Precaución de continuidad |
|---|---|---|
| DOTween | Transiciones y animaciones de interfaz | Comprobar setup y módulos antes de atribuirle fallos de UI |
| TextMesh Pro, incluido mediante uGUI | Fuentes SDF y texto | Conservar recursos esenciales y versión de ajustes |
| SoftMaskForUGUI | Recortes y máscaras suaves | Revisar materiales y Canvas afectados |
| UI Effect | Efectos visuales de interfaz | Evitar sobrecargar la presentación |
| ParticleEffectForUGUI | Partículas dentro de Canvas | No confundir con VFX del mundo |
| TMPEffects | Animación de textos | Mantener legibilidad y rendimiento |
| AI Navigation | NavMesh, agentes y rutas | Regenerar después de cambiar geometría procedural |
| Input System | Entrada del jugador | Revisar conflictos entre combate, inventario y laboratorio |
| Unity Pipeline | Operar editor y ejecutar comprobaciones | Usar comandos realmente expuestos por esta instalación |
| Unity Test Framework | Infraestructura de pruebas | Diferenciar pruebas formales de verificadores de AgentScripts |
| Photon Fusion 2 | Trabajo cooperativo pendiente de validación completa | Autenticación y dos clientes siguen siendo requisitos de comprobación |
| Código LAN | Ruta cooperativa alternativa presente | No asumir equivalencia con Fusion |
| FX_Kandol_Pack y Hovl Studio | Biblioteca de efectos | Inspeccionar prefabs y compatibilidad URP antes de utilizarlos |
| Human y Warrior animation packs | Biblioteca de movimientos | Comprobar clips presentes y edición gratuita |
| KayKit y Kenney | Arquitectura, utilería y módulos reutilizados | Mantener escala, materiales y navegación |
| Toon embebido | Shaders Toon y Toon(Tessellation) | Conservar el parche de compatibilidad URP |

REA ayuda a inspeccionar artefactos y documentar formatos.
Ghidra y CUE4Parse aparecen en el historial como herramientas de análisis o conversión.
Su disponibilidad actual debe comprobarse; no equivalen a paquetes de gameplay Unity.
No asegures haber extraído recursos nuevos sin evidencia concreta de archivos, conversiones y asignaciones.

Para el cierre más reciente se utilizó Unity Pipeline y lectura directa de shaders y paquetes.
La corrección anterior de TMP utilizó diagnóstico de TextMesh Pro.
Los cambios de escenarios utilizaron la metodología de mazmorras procedurales y verificaciones de navegación.
Las demás skills listadas representan capacidades disponibles, no ejecuciones simultáneas.
