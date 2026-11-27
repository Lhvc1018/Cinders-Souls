# Edición y pruebas

## Menú principal
Abre `Assets/00_Scenes/MainMenu.unity`, fuera de Play Mode.
Expande `Main Menu > MENU EDITABLE` en Hierarchy.
Modifica RectTransform, colores, fuentes y textos existentes en Inspector. Guarda la escena.
El juego aplica esos ajustes al menú funcional. No renombres objetos: la vinculación usa sus nombres.
Los botones conservan sus acciones. No necesitas regenerar el menú después de editarlo.
`Generar menú editable` reconstruye el diseño desde código y reemplaza la edición previa; úsalo solo para restablecerlo.
Para ver Opciones, Trucos o Elegir clase en edición, activa su panel y desactiva Main Menu temporalmente.

## Sala de pruebas
Abre `Cinders Souls > Edición > Abrir sala de pruebas`.
También puedes abrir `Assets/00_Scenes/Maps/SalaPruebas.unity`.
La geometría está guardada en la escena y se puede modificar sin Play Mode.
Selecciona el objeto que contiene Combat Test Room para editar Enemy Prefabs, Enemy Level y Spawn Points.
Duplica un prefab de enemigo antes de cambiarlo; arrastra tu variante al listado Enemy Prefabs.
Mueve los tres objetos bajo PUNTOS DE PRUEBA para cambiar dónde aparecen.
Pulsa Play. Usa Crear enemigo, Pausar IA y Limpiar enemigos.
F2 oculta el panel y devuelve el control al jugador; F2 otra vez muestra los controles.
La sala no inicia oleadas automáticamente. El NavMesh se calcula al comenzar.

## Trucos de sesión
MainMenu > TRUCOS:
- `Good!`: invulnerabilidad, maná infinito y daño letal con ataques del jugador.
- `Macpato`: compras ilimitadas sin consumir oro; el HUD muestra infinito.
Repite exactamente el mismo comando para desactivarlo.
Los comandos se reinician al cerrar el juego o salir y entrar en Play Mode.
`Jam2026` conserva el selector de rondas anterior.
