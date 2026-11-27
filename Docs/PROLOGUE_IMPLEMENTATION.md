# Prólogo: La celda de ceniza

Implementado el 29/10/2026 en Last67Seconds-Maps.

## Entrada y recorrido
- Menú > Jugar > seleccionar clase > Iniciar descenso.
- Las partidas individuales desde ronda 1 cargan Prologo.
- LAN y selección de rondas avanzadas conservan su flujo anterior.
- Cortina negra, título rojo con trazos de sangre y fundido DOTween.
- Cinco salas conectadas, geometría y decoración KayKit existentes.
- Semilla nueva en cada descenso; fixedSeed permite reproducir un trazado.
- El Vigía usa el modelo existente Class_Mage, derivado de los modelos del proyecto.
- Diálogo inferior con retrato, cuatro intervenciones y escritura progresiva.
- E, Enter, Espacio o botón: completar texto / avanzar.
- WASD: caminar; Mayús: esquivar; Escape: pausa y regreso al menú.
- Tras hablar, recorrer el calabozo y pulsar E cerca del umbral rojo.
- El umbral carga Calabozo y comienza el sistema de rondas existente.

## Alcance
Prototipo procedural del prólogo individual. No incluye enemigos procedurales, generación de campaña completa ni sincronización del prólogo LAN.
Los modelos actuales siguen siendo provisionales; el estilo visual conserva sus proporciones KayKit.

## Verificación
AgentScripts/BuildPrologue.cs: 100 semillas, todas conectadas; 64 trazados distintos.
AgentScripts/CheckPrologue.cs: fotogramas activos, bloqueo narrativo, diálogo, suelo físico, ruta transitable y transición a Calabozo.
Capturas: Assets/Screenshots/prologue-curtain-final.png y prologue-dialogue-wide.png.
Respaldo previo: Backups/Prologue-20261029-1100.
