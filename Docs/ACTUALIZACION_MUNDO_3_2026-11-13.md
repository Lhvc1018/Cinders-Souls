

## Actualización verificada — 13 de noviembre de 2026: Mundo 3

Esta sección sustituye cualquier límite histórico de seis etapas o iluminación exclusiva del Mundo 2.
El proyecto activo sigue siendo `D:/67/Last67Seconds-Maps`.

### Campaña y escenarios

- La campaña tiene nueve etapas; `RunDirector.FinalExpeditionFloor = 9`.
- Los selectores de `jam` y F3 incluyen 3-1, 3-2 y 3-3.
- 3-1: Campos del Último Suspiro, con Horror Inquisidor.
- 3-2: Jardín de los Sepultados, con Guardián del Abismo.
- 3-3: Trono de las Mil Tumbas, con Necromancer.
- La derrota del jefe de 2-3 permite continuar al tercer mundo mediante la progresión existente.
- Se mantienen 6–9 salas anteriores al jefe y conectividad por semilla.
- Cementerio: suelo terroso, lápidas intactas y rotas, rejas metálicas, árboles secos, santuarios y pilares.
- La arena final ocupa 17 × 17 celdas antes del recorte de esquinas; cada celda mide 4 m.
- Tiene doce monumentos periféricos, luces verdosas, arco monumental, osario y escalones laterales.
- El centro permanece llano y libre para combatir y desplazar esbirros.
- Las lápidas tienen lectura de malla habilitada para construir NavMesh durante la ejecución.

### Nuevos monstruos

Fuentes copiadas desde Descargas a `Assets/03_Models/Cemetery`:

- `HeadSpider_FREE`: cabeza arácnida, rápida y frágil.
- `Huntsman_Spider_FREE`: araña más pequeña y rápida.
- `Iron Oath Executioner`: enemigo pesado de 2.35 m.
- `THE STRAW REAPER– Retro PSX Monster`: atacante cuerpo a cuerpo de 2.05 m.
- `The Watcher of the Hollow Eye`: enemigo pesado de 2.45 m, más lento y resistente.

Prefabs: `Assets/Resources/ExpeditionEnemies/Cemetery`.
Los tres humanoides tienen Avatar Humanoid y movimientos retargeteados de los recursos existentes.
Sus controladores incluyen locomoción, ataque, reacción y muerte; no reproducen las largas pistas originales completas como ataques.
Las dos arañas gratuitas carecen de esqueleto y clips originales.
Usan animación procedural básica de piezas; todavía no tienen articulación independiente de cada pata.
No describirlas como criaturas con rig esquelético terminado.
Se corrigieron asignaciones de texturas para HeadSpider y Watcher.
El laboratorio descubre automáticamente estos prefabs mediante `Resources.LoadAll`.

### Necromancer

- Fuente existente: `Assets/Necromanser/Prefab/Necromanser Prefab.prefab`.
- Prefab integrado: `Assets/Resources/ExpeditionEnemies/Necromancer.prefab`.
- Altura visual objetivo: 2.8 m; vida base: 2200; recompensa base: 650 monedas.
- Conserva animaciones originales de espera, caminar, ataques, conjuro, invocación, reacción y muerte.
- Materiales URP independientes, tono frío verdoso y emisión verde para diferenciarlo del original.
- Ataque mágico: abanico de tres proyectiles, 22 de daño por impacto, velocidad 13 m/s.
- Ataque de área: círculo anticipado de 3.5 m y 32 de daño; permite salir antes del impacto.
- Invocación: preparación de 1.8 s, tres esbirros en fase 1 y cuatro en fases posteriores.
- Límite total: ocho invocaciones vivas; intervalo de 14 s inicialmente y 10 s en fases posteriores.
- Invoca esqueletos y Straw Reapers sobre NavMesh alcanzable, a más de 4 m del jugador.
- La muerte o desactivación del jefe limpia esbirros y peligros; mantiene el evento de finalización existente.
- Archivos clave: `BossCharacter.Necromancer.cs`, `ExpeditionFloor.Cemetery.cs`, `ExpeditionFloor.cs`, `ExpeditionLayout.cs`.

### Iluminación y Golem

- 1-1 reutiliza la configuración ambiental de 2-1; 1-2 replica 2-2; 1-3 replica 2-3.
- Comparten sonda ambiental explícita, niebla, luz direccional, rellenos, exposición y ajustes de color.
- El Mundo 3 usa como base el ambiente frío de 2-1, con luces rituales verdes.
- El Golem funerario común vuelve a 2.2 m, con cápsula y agente ajustados.
- Este cambio no reduce al jefe gigante El Señor de los Terremotos.

### Verificación y límites

- `AgentScripts/VerifyWorldThree.cs`: 2304 combinaciones, 256 semillas por nueve etapas, conectividad y tamaños correctos.
- Todos los recursos de jefes y cinco monstruos nuevos encontrados.
- Prueba viva 3-3: Necromancer visible y alcanzable; invocación observada con seis esbirros activos.
- Derrota del Necromancer: etapa final terminada y esbirros eliminados correctamente.
- Prueba viva 3-1: HeadSpider, Huntsman y Executioner generados con rutas completas hacia el jugador.
- Tras corregir estados HeavyAttack/BlockIdle, consola sin errores ni advertencias en esa prueba.
- Compilación final sin errores; Unity quedó fuera de Play Mode.
- No se verificó cooperativo con dos clientes para el nuevo contenido.
- La articulación de patas de las arañas permanece básica; requiere rig dedicado para mejorar su acabado.
- Copia previa: `D:/67/CindersBackups/WorldThree-20261113`.

Herramientas empleadas: Unity Pipeline, scripts de editor C#, inspección local y capturas internas de comprobación.
Skills aplicadas: `cinders-procedural-dungeons`, `cinders-boss-ai`, `unity-creature-rigging` y `unity-pipeline`.
No se descargaron nuevos paquetes ni se extrajeron juegos externos durante esta actualización.

## Correcciones verificadas del cementerio
- Salas normales ampliadas, caminos procesionales de piedra, tumbas, pilares iluminados y terrazas laterales con escaleras.
- El indicador de sala se actualiza también al regresar a habitaciones visitadas.
- Acceso al jefe mediante una sala conectada despejada; corregida la preparación innecesaria de posiciones de enemigos normales al generar jefes.
- Necromancer visible y alcanzable en 3-3, comprobado en Play Mode.
- LaboratorioContenido guarda 20 entradas de enemigos y jefes para pruebas.
- Validación: 2304 distribuciones conectadas; encuentro con HeadSpider, Executioner y Huntsman alcanzables.
- Terraza alcanzable por NavMesh a altura 1.733333 m; etiqueta de entrada vuelve correctamente a 1.
- Consola actual sin errores ni advertencias al finalizar; no se recorrieron manualmente todas las semillas.

## Segunda revisión: recintos funerarios y regresiones
- Executioner: contacto visual de pies mediante CemeteryFootContact, sin modificar navegación ni aplicar la corrección al morir. 80 muestras en ejecución mantuvieron el pie inferior a 0.09 m sobre la raíz.
- JAM paso libre: al abandonar una sala con combate pendiente se termina ese encuentro para permitir iniciar el siguiente. Reproducido entrando al Necromancer sin limpiar previamente la sala normal; jefe generado y ruta completa.
- Cementerio: perímetro cerrado de 6 m, nueve módulos nuevos desde assets existentes (cripta completa, muro agrietado, banco, farol, ataúd, árbol, marcador funerario, camino y puerta).
- Seis módulos usan su atlas original oscurecido, en lugar de piedra aplicada a madera, árboles y hierro.
- Salas normales con dimensiones variables, patios con criptas, terrazas de piedra, escaleras y cámara secreta en cada nivel del Mundo 3.
- Eliminada decoración genérica y pinchos heredados en salas normales del cementerio; reducida separación y corredor adicional del jefe final.
- Cielo de gradiente brumoso sustituye fondo negro; suelo y muros siguen delimitando cada zona.
- Validación: 2304 distribuciones conectadas, tres enemigos alcanzables, terraza accesible y Necromancer generado mediante caso JAM. No se afirma revisión manual de todas las semillas.
