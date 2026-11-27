# Estado de entrega: rutas, enemigos y Fusion

Implementado en Assets/02_Scripts:
- Nueve salas, dos bifurcaciones y jefe final obligatorio.
- Mercader aleatorio, consumibles y estatuas con daño, maná o armadura.
- Barras enemigas reducidas; hellhound con fuego, muñeca con proyectiles y esqueleto con combinación doble.
- Carrera del inquisidor y clips de locomoción con articulación de pies y rodillas.
- Adaptador Photon Fusion 2 y sincronización de expedición en desarrollo.

Verificado: 26.000 comprobaciones sobre 2000 semillas; diez conexiones NavMesh; diez comprobaciones de economía; recorrido de las nueve salas mediante preparación de pruebas. Compilación anterior correcta.

Photon: aplicación Cinders Souls creada con plan gratuito 20 CCU. App ID configurado. La prueba previa falló InvalidAuthentication antes de crear esta aplicación. Falta repetir conexión con el App ID nuevo y verificar dos jugadores reales. No se ha validado latencia.

Bloqueo actual: Unity proceso 16064 no responde. editor_status y editor_play agotaron 30000 ms. No se forzó cierre para evitar perder trabajo del usuario.

Pendiente tras recuperar Unity: revisar visualmente nueva carrera y deformaciones, ejecutar ValidateInquisitor.Combat con sala 8, comprobar ataques de cada residente, recorrido físico con CharacterController y ejecutar ProbeFusion.Run. La integración cooperativa todavía requiere pruebas completas; no debe presentarse como terminada.
