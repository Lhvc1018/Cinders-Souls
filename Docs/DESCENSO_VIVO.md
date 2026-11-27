# Descenso vivo — implementación

El calabozo se recompone cada vez que muere un portador. Conserva las deudas, pero altera sus caminos.
Los recuerdos recuperados permiten reconocer cámaras que antes permanecían ocultas.

## Progreso permanente
- Los guardianes de 1-1, 2-1 y 3-1 dejan respectivamente la Llave del Sepulturero, el Sello del Exiliado y el Ascua del Recuerdo.
- Recoger con E. Las llaves no se consumen; sobreviven a muerte, reinicio y cierre del juego.
- Guardado local: PlayerPrefs `Cinders.DescentMemory.v1`, máscara de tres bits.
- Un atril al inicio permite consultar cuáles faltan.
- No altera vida, daño, oro ni dificultad permanentemente.

## Generación y rutas
- Cada intento sigue usando una semilla nueva.
- La misma semilla, piso y máscara de recuerdos reproducen el mismo trazado.
- Obtener recuerdos habilita anexos en los mundos 1 y 2, y cambia la selección de anexos existentes posteriormente.
- Los anexos pueden albergar encuentros seguros o combate con recompensas existentes.
- Algunas cámaras secretas desde 1-2 requieren una de las tres llaves. Su requisito aparece al acercarse.
- Las demás paredes secretas conservan la apertura mediante golpes.
- Ninguna llave bloquea la ruta obligatoria al jefe. Las cámaras selladas son opcionales.
- Los nuevos anexos aparecen al generar el siguiente piso o intento; no reconstruyen el piso bajo tus pies.

## Compañeros y encuentros
- Irel, peregrino: 180 oro. Dardos de ceniza cada 2,4 segundos; alcance 12 metros.
- Daño del dardo: 14 + 2 por piso. Comprueba línea de visión antes de preparar y liberar el disparo.
- Usa el rig de Nereo, vestidura fría, Human Basic Motions y Human Spellcasting.
- Lumbre, mascota espiritual: 100 oro. Recupera 4 maná cada 6 segundos durante combate.
- Máximo un Irel y una Lumbre por intento; comprar repetidos no cobra monedas.
- Viajan entre pisos. Reiniciar el intento elimina contratos; las llaves permanecen.
- Son apoyos no atacables en esta versión. No absorben golpes ni atraen enemigos.
- Se reagrupan al entrar en combate para evitar quedarse detrás de puertas selladas.
- Los refugios pueden ofrecer un brasero: 35% de vida máxima y 30 maná, gratis, una vez.
- Cada piso ofrece un reclutamiento en el mercader o la capilla; los anexos añaden encuentros según semilla.

## Alcance y comprobaciones
- Implementado para expediciones individuales. Desactivado en LAN para preservar el flujo existente.
- 5.760 combinaciones de semillas, pisos y recuerdos: conectividad, límite de 16 salas y centros únicos.
- Navegación real verificada en 1-2 con recuerdos y 11 salas accesibles, excluyendo el secreto cerrado.
- Verificados costes, fondos insuficientes, duplicados, puertas con/sin llave y daño del ayudante al jefe.
- Verificados cambio de piso y reinicio: contratos temporales, recuerdos permanentes.
- Los precios y el daño iniciales requieren balance mediante partidas completas.
