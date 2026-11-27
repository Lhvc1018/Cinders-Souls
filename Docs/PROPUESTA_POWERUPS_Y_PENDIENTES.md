# Power ups y próximos pasos

Actualizado el 2 de noviembre de 2026. Este documento propone cambios; no indica que estén implementados.

## Lo que ya existe

El jugador puede equipar pasivas. El código contiene Frenzy, VampiricHeart, Overload, MercuryBoots y ThornArmor. Existen recogidas y reemplazo de espacios. El relicario del primer piso cura, entrega oro y genera un arma. Estas funciones aún no forman una selección completa de reliquias por sala.

## Propuesta: reliquias de ceniza

Cada reliquia cambia una decisión de combate durante la expedición. El jugador conserva tres espacios y puede reemplazar uno. La pantalla debe mostrar efecto, condiciones, duración y comparación antes de confirmar. Evitar mejoras invisibles o descripciones ambiguas.

1. **Corazón impío**: curación al derrotar enemigos. Reduce la recuperación contra jefes a una condición específica, visible en pantalla.
2. **Ceniza hambrienta**: los ataques consecutivos aumentan velocidad durante un breve periodo. Recibir daño corta la cadena.
3. **Paso de mercurio**: mejora una esquiva y permite reposicionarse. El tiempo de recuperación debe ser visible.
4. **Manto de espinas**: reduce parte del daño recibido y devuelve daño cuerpo a cuerpo.
5. **Sello de sobrecarga**: genera más carga especial, a cambio de una penalización que se definirá mediante pruebas.
6. **Marca del inquisidor**: el último golpe de un combo aplica una marca; consumirla exige cambiar de arma o esquivar.

Las cifras, rarezas y penalizaciones se decidirán después de probarlas con las cinco clases. Mantener identidad y efectos actuales de las pasivas hasta completar esa revisión.

## Cómo se obtienen

Al terminar algunas salas, ofrecer dos o tres reliquias compatibles con la clase. El relicario permite elegir una, intercambiar una equipada o continuar. En cada partida, las reliquias y las mejoras temporales se reinician. No hay guardado permanente implementado para esta propuesta.

El generador debería evitar opciones duplicadas inútiles. Cada efecto necesita icono legible, nombre, descripción exacta y una señal visual corta durante su activación. La interfaz debe poder manejarse con teclado y ratón.

## Pendientes para avanzar

1. Preparar esqueletos y pesos del Hellhound y del Horror Inquisitor. Ambos modelos descargados son OBJ, sin animaciones incluidas.
2. Crear y revisar las animaciones de reposo, movimiento, ataque, aturdimiento y muerte. Sincronizar impactos con las ventanas reales de daño.
3. Incorporar Hellhound como enemigo del primer piso y Horror Inquisitor como jefe. Ajustar navegación, colisiones, salud, daño y lectura visual.
4. Probar todas las salas, los cierres, las recompensas, la muerte y la victoria con varias semillas. Confirmar que ningún enemigo quede inaccesible.
5. Sustituir el relicario actual por una primera selección de reliquias. Mostrar comparación y reemplazo de las tres ranuras.
6. Medir equilibrio y sensación de las cinco clases: velocidad, daño, recursos, esquiva y parry.
7. Ampliar variedad de salas y rutas. Garantizar conectividad, navegación y registros de semilla reproducibles.
8. Revisar HUD, pausa, legibilidad, sonido, rendimiento y resoluciones. Registrar y corregir errores antes de añadir más pisos.
9. Diseñar persistencia, nuevos biomas y cooperación procedural. La ruta LAN todavía conserva el sistema de rondas.
10. Revisar procedencia y licencia de ambos modelos antes de distribuir el juego.

## Organización

Los nuevos archivos originales están en `Assets/03_Models/Enemies/Hellhound` y `Assets/03_Models/Enemies/HorrorInquisitor`. El paquete DoubleL pasó a `Assets/07_Animacion/DoubleL`; FourEvilDragonsHP pasó a `Assets/03_Models/FourEvilDragonsHP`. Se conservaron sus archivos `.meta` y se actualizó una ruta literal de editor. Los recursos ubicados en `Resources` deben conservar rutas compatibles con `Resources.Load`.
