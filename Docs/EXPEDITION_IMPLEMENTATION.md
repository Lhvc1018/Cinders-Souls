# Expedición: Celdas de Ceniza

Implementación del 29/10/2026 en Last67Seconds-Maps.

## Flujo individual
Menú > clase > prólogo y Vigía > umbral > Descenso.
Descenso sustituye las rondas automáticas en este flujo.
Seis salas: vestíbulo seguro, combate, relicario, combate, combate, jefe.
La semilla cambia posiciones horizontales, corredores y composición de guardianes.
Las puertas se sellan al entrar en combate y se abren tras derrotar a todos los guardianes.
El relicario cura 40% de la vida máxima, otorga 50 de oro y genera un arma tier 2.
Las salas de combate dan 30 de oro adicional y munición.
El jefe usa el prefab existente del Rey Goblin, presentado como Carcelero de Ceniza.
Derrotarlo termina este primer piso. Morir o volver a jugar genera una nueva expedición.

## Arte y tipografía
Módulos existentes del Calabozo / KayKit: suelos, muros, rejas, pilares, barriles y antorchas.
Techos cerrados, piedra oscurecida, iluminación fría con antorchas cálidas, niebla y viñeta URP.
Uncial Antiqua para títulos; Crimson Text Semibold para textos.
Fuentes obtenidas de google/fonts, con archivos OFL incluidos en Assets/Fonts.
Atlas TMP estáticos de 2048x2048; caracteres latinos, acentos y signos españoles comprobados.
El estilo de los modelos continúa siendo estilizado; no se sustituyeron por modelos realistas.

## Validación
BuildExpedition.Layouts: 100 semillas conectadas y 100 trazados distintos.
CheckExpedition.Run: navegación, recorrido con CharacterController, suelos físicos, entrada secuencial, enemigos, relicario, jefe y victoria.
La prueba automatizada aplica daño elevado para comprobar transiciones; no demuestra equilibrio de dificultad.
Reinicio tras victoria y derrota comprobado; reinicia salas, bajas y semilla.
No se observaron errores nuevos de ejecución durante esos recorridos.

## Límites actuales
Primer piso procedural individual, con secuencia de tipos de sala fija y geometría/composición variable.
No implementa todavía bifurcaciones, campaña de varios biomas, guardado persistente ni metaprogresión.
LAN conserva el sistema anterior. Sus mapas no se migraron a generación sincronizada en esta fase.
La opción de trucos con rondas avanzadas sigue siendo una ruta de depuración antigua.
Respaldo previo: Backups/Expedition-20261029.
