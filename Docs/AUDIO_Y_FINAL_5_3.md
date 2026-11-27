# Audio y cierre del quinto mundo

## Audio conectado
La biblioteca contiene 34 señales con clips existentes del proyecto.
Se preservan volumen maestro, música del menú y sonidos anteriores de armas, impactos, pasos, interfaz, cofres y monedas.
Descenso selecciona música de exploración. Al comenzar combate cambia con fundido a percusión de batalla.
Los jefes emiten entrada, preparación, impacto/conjuro, transición de fase, estado crítico y muerte.
Los avisos de 65% y 30% de vida se emiten una sola vez por jefe.
Al 30% se añade una capa musical de tensión; desaparece al terminar el combate.
Se conectaron también curación, orbes, llaves, contrataciones, muros secretos, sellos y portales.
La música final sustituye la percusión después de elegir un desenlace.
Se eliminó el rugido genérico que se repetía por cada animación de ataque del jefe.

## Causalidad narrativa
Los hornos del mundo 4 fabricaron el mecanismo, pero la Horda se apoderó de su corazón.
El Berserker lo conserva bajo su altar como garantía de que sus guerreros volverán a levantarse.
Vencerlo elimina al último custodio, pero no decide qué hacer con las vidas retenidas.

## Después de 5-3
La muerte del Berserker deja la partida en estado Cleared, no en Victoria automática.
Aparecen el Umbral del Alba, sus dos hojas móviles, el Corazón de los Nombres y un memorial alternativo.

### Requisitos del final verdadero, durante el intento actual
- Memoria: recuperar el orbe de 1-3.
- Voluntad: recuperar el orbe de 2-3.
- Duelo: recuperar el orbe de 3-3.
- Sello de la última campana: se obtiene automáticamente al vencer al jefe de 4-3.

El umbral muestra cada requisito cumplido o pendiente. Los orbes no se consumen.
El jugador abre con E, cruza y activa el corazón para liberar los nombres.
La apertura tarda 1,6 segundos. El corazón no acepta interacción antes de abrir la entrada.

**El Alba sin Deuda:** «Rompes el sello. Los nombres vuelven a sus muertos. Por primera vez, el alba no exige otra vida.»

Si faltan requisitos, o el jugador decide conservar el ciclo, el memorial permite concluir igualmente.
Requiere dos interacciones dentro de 8 segundos para evitar una elección accidental.

**El Último Vigía:** «Abandonas el altar y custodias su entrada. La Horda ha caído; bajo Vespera, la campana aún espera.»

Ambos desenlaces muestran su epílogo y permiten volver a jugar o regresar al menú.
Los logros locales se guardan en `Cinders.Ending.Dawn` y `Cinders.Ending.Vigil`.
No son logros de Steam ni de otra plataforma.
Los requisitos del final se reinician con el intento; los logros y llaves permanentes anteriores permanecen.

## Verificación
- Biblioteca: 34 señales únicas, sin clips vacíos, más pistas de tensión y cierre.
- Play Mode, 5-3: música de exploración/combate, umbrales únicos de vida, capa crítica, muerte y aparición del umbral.
- Rechazo del final verdadero sin requisitos.
- Apertura física y activación del corazón con requisitos presentes.
- Final alternativo con confirmación doble.
- Recolección independiente de los tres orbes y sello de 4-3.
- Epílogo revisado visualmente, sin texto recortado.
- Se corrigieron las curvas de velocidad inconsistentes del VFX del Berserker: sus tres ejes usan el mismo modo.
- Los clips se comprobaron mediante referencias y reproducción de AudioSources; no se realizó una mezcla auditiva exhaustiva.
- No se verificó el cierre en cooperativo.

Herramientas: Unity Pipeline. Guías locales consultadas: `commands/scripts.md` y `commands/editor-lifecycle-and-observability.md`.
Respaldo inicial: `D:/67/CindersBackups/AudioFinale-20261114`.
