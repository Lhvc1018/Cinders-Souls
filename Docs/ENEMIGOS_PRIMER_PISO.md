# Enemigos del primer piso

El primer piso individual usa Hellhound para las salas de combate y Horror Inquisitor para el jefe. El resto de rutas conserva sus configuraciones anteriores. Los modelos OBJ originales siguen en `Assets/03_Models/Enemies`.

Los FBX añadidos contienen esqueletos genéricos con pesos aproximados y clips de reposo, desplazamiento, ataque, reacción y muerte. El ataque visual acompaña al sistema de daño existente. El estado de aturdimiento reproduce la reacción. Los `.blend` editables están en `Docs/BORRADOR_Hellhound_rig.blend` y `Docs/BORRADOR_HorrorInquisitor_rig.blend`.

Los esqueletos y las animaciones son un primer prototipo automático. Es necesario corregir a mano pesos y poses, revisar contacto de patas y pies, ajustar sincronización de impacto y afinar la muerte. El cenicero del inquisidor forma parte de una sola malla; necesita separación o pesos más cuidadosos para moverse con naturalidad.

En Play Mode se comprobaron tres Hellhounds, aturdimiento, copias visuales de muerte, cierre y apertura de sala, secuencia de salas, jefe y victoria. La prueba usó daño elevado y teletransporte: no comprueba equilibrio ni un recorrido normal. Los prefabs definitivos incluyen luces sin sombras para mejorar lectura. Las pruebas visuales de iluminación están en `Docs/Hellhound_lit_test.png` y `Docs/HorrorInquisitor_lit_test.png`.

Los prefabs y controladores generados están en `Assets/Resources/ExpeditionEnemies`. El menú de Unity `Tools/Cinders/Build Expedition Enemies` los regenera a partir de los FBX. El generador recrea los controladores; guarda una copia antes de personalizarlos. El modelo del inquisidor contiene aproximadamente 191333 vértices y requiere pruebas de rendimiento y optimización antes de multiplicar su uso.
