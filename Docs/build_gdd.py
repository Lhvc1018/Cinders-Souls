from docx import Document
from docx.shared import Inches,Pt,RGBColor
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from pathlib import Path
D=Document();sec=D.sections[0];sec.top_margin=sec.bottom_margin=Inches(.7);sec.left_margin=sec.right_margin=Inches(.8)
for name in ['Normal','Title','Heading 1','Heading 2']:
 st=D.styles[name];st.font.name='Calibri';st.font.color.rgb=RGBColor(0,0,0)
D.styles['Normal'].font.size=Pt(11);D.styles['Normal'].paragraph_format.space_after=Pt(7)
D.styles['Normal'].paragraph_format.line_spacing=1.1
foot=sec.footer.paragraphs[0];foot.text='Cinders Souls  •  GDD 2.0  •  29 octubre 2026     '
f=OxmlElement('w:fldSimple');f.set(qn('w:instr'),'PAGE');foot._p.append(f)
def p(t):D.add_paragraph(t)
def h(t):D.add_heading(t,2)
def page(t):D.add_page_break();D.add_heading(t,1)
def table(headers,rows):
 t=D.add_table(rows=1,cols=len(headers));t.style='Table Grid'
 for c,v in zip(t.rows[0].cells,headers):c.text=v
 for row in rows:
  for c,v in zip(t.add_row().cells,row):c.text=v
 for i,row in enumerate(t.rows):
  for c in row.cells:
   pr=c._tc.get_or_add_tcPr();shade=OxmlElement('w:shd');shade.set(qn('w:fill'),'333333' if i==0 else ('F2F2F2' if i%2 else 'FFFFFF'));pr.append(shade)
   for para in c.paragraphs:
    para.paragraph_format.space_after=Pt(5);para.paragraph_format.space_before=Pt(5)
    for r in para.runs:r.font.size=Pt(10);r.font.color.rgb=RGBColor.from_string('FFFFFF' if i==0 else '000000')
 p('')
D.add_paragraph('Diseño del juego Cinders Souls',style='Title')
p('Documento de diseño y auditoría del proyecto Last67Seconds Maps')
p('Versión 2.0  |  29 de octubre de 2026  |  Documento de trabajo')
p('La prioridad actual es consolidar el menú y la selección de clases con una identidad de horror gótico. La generación procedural llegará después. Este documento distingue código existente, recursos importados y propuestas pendientes.')
D.add_picture('Assets/Resources/CindersMenu/Cathedral.png',width=Inches(6.5))
h('Visión del juego')
p('Roguelike de acción en primera persona, con cinco clases y una futura expedición procedural. La atmósfera busca oscuridad, decadencia y misterio. Bloodborne y Dark Souls sirven como referencias de tono, composición y solemnidad.')
p('Cinders Souls es el nombre visible del menú. El GDD anterior usaba Códice de Ceniza como concepto narrativo. El nombre comercial queda por decidir.')
h('Conclusión de la revisión')
p('La base jugable ya incluye combate, rondas, economía, clases y cooperativo LAN. El principal problema visual es la mezcla de modelos caricaturescos con escenarios góticos. Añadir plugins no resuelve esa diferencia artística.')
page('Estado real del proyecto')
p('Proyecto revisado: D:\\67\\Last67Seconds-Maps. La revisión combina archivos, manifiesto de paquetes y pruebas visuales recientes. No constituye una certificación completa del juego.')
table(['Área','Estado comprobado'],[
['Motor','Unity 6000.5.6f1 y URP 17.5.0.'],['Interfaz','uGUI 2.5.0, TextMeshPro y construcción de pantallas mediante código.'],['Juego','Clases, armas, especiales, pasivas, mercader, botín y jefes presentes en código.'],['Progresión','25 rondas; jefe cada 5 rondas.'],['Cooperativo','Sistema LAN propio mediante TCP; hasta cuatro jugadores según diseño existente.'],['Menú','Catedral original, Cinzel, transiciones, retratos y selección de clase.'],['Creepy Doll','Recursos importados. Retirado del menú. Su integración como enemigo no está validada.'],['Procedural','Propuesta futura. No se ha comprobado una expedición procedural funcional.']])
h('Mapas actuales')
p('Rondas 1–5: Calabozo. Rondas 6–10: Coliseo. Rondas 11–15: Criptas. Rondas 16–20: Bastion. Rondas 21–24: Calabozo. Ronda 25: FinalArena.')
h('Riesgos actuales')
p('Los errores ScriptableSingleton del administrador de paquetes dejaron de aparecer en la prueba posterior al reinicio. No existe una corrección permanente demostrada. Debemos comprobar cada nueva importación y recarga.')
p('Las pruebas visuales confirmaron navegación de menú y cambios entre clases. Faltan pruebas sistemáticas de resoluciones, accesibilidad y cooperativo.')
page('Clases combate y progresión')
p('Cada clase conserva su identidad jugable. La interfaz debe mostrar diferencias reales, sin prometer habilidades aún no implementadas.')
table(['Clase','Valores mostrados','Identidad actual'],[
['Caballero','120 vida · velocidad 6.5','Espada y escudo; combate equilibrado.'],['Arquero','100 vida · velocidad 8','Arco y munición; combate a distancia.'],['Tanque','180 vida · velocidad 4.8','Mazo; resistencia y ataques en área.'],['Mago','100 vida · velocidad 6.5','Bastón; 100 de maná y hechizos.'],['Shinobi','120 vida · velocidad 6.5','Katana, shuriken, parry y postura.']])
p('Estos valores proceden de los textos del menú. Deben contrastarse con estadísticas de ejecución antes de cerrar el equilibrio.')
h('Bucle actual')
p('Elegir clase → entrar al mapa → combatir → recoger recursos → comprar mejoras → completar rondas → derrotar al jefe. La ronda final termina la partida.')
h('Sistemas que se conservan')
p('Dos espacios de armas, categorías de armas, tiers 1–4, especiales con Q y tres espacios de pasivas. Se conservan oro, botín, munición y mercader.')
h('Bucle objetivo propuesto')
p('Refugio → distrito → explorar habitaciones → completar dos objetivos → abrir una puerta → derrotar al jefe → elegir recompensa. La expedición combina decisiones de ruta y construcción de personaje.')
p('El perfil podría conservar desbloqueos de opciones. Oro temporal, equipo y reliquias de la expedición se perderían al terminar. Esta persistencia todavía requiere implementación.')
page('Dirección artística e interfaz')
p('Paleta: carbón, marfil, sangre apagada y latón envejecido. La oscuridad debe crear atmósfera sin ocultar texto, enemigos o controles.')
h('Tipografía y composición')
p('Cinzel está disponible para títulos y botones. TextMeshPro presenta el texto funcional. No se importaron fuentes extraídas de juegos comerciales. El diseño busca un tono comparable mediante recursos propios.')
p('Usar una familia ornamental para títulos y una familia legible para descripciones. Mantener contraste, espaciado y jerarquía en todas las pantallas.')
h('Menú principal')
p('La catedral sirve como fondo. El título ocupa el lado oscuro. Iniciar descenso y cooperativo son acciones principales. Opciones y salir permanecen visibles. Creepy Doll no forma parte del menú.')
h('Selección de clases')
p('Lista de cinco clases a la izquierda, retrato frontal central y ficha lateral. Mostrar nombre, identidad, estadísticas, equipo y controles. Separar selección y confirmación de inicio.')
p('Ratón y teclado deben producir el mismo estado. La selección persistente debe distinguirse del foco y del hover. Evitar iniciar accidentalmente una partida.')
h('Arte de personajes')
p('Los modelos KayKit actuales tienen proporciones caricaturescas. Funcionan como prototipos, pero contrastan con la catedral. Elegir una dirección estilizada coherente o sustituirlos gradualmente por personajes góticos compatibles.')
h('Criterios de aceptación')
p('Probar 1280×720, 1920×1080, 2560×1440 y ultrapanorámico. Verificar títulos completos, ausencia de solapamientos, foco visible y lectura de estadísticas. Probar las cinco clases y el retorno desde opciones.')
p('Incorporar reducción de movimiento y destellos. Validar el coste de partículas y cámaras de retrato antes de aumentar efectos.')
page('Plugins instalados y uso real')
table(['Plugin','Versión o ubicación','Uso observado'],[
['DOTween','1.2.815 · Assets/Plugins/Demigiant','Transiciones y estados de botones.'],['UIEffect','5.9.7 · paquete local','Sombra del título; soporte TMP importado.'],['UIParticle','4.14.2 · manifiesto Git','MenuEmbers usa partículas dentro del Canvas.'],['SoftMaskForUGUI','3.6.5 · manifiesto Git','Instalado; uso activo no confirmado en el menú.'],['TMPEffects','0.3.2 · manifiesto Git','Disponible; el título actual no tiene animación TMPAnimator activa.'],['Photon Fusion','Assets/Photon/Fusion','Importado; versión exacta no verificada. Sin migración del juego demostrada.']])
h('Paquetes técnicos presentes')
p('Input System 1.20.0, AI Navigation 2.0.14, Timeline 1.8.12, Test Framework 1.7.0 y Unity Pipeline 0.8.0-exp.1. También figuran Visual Scripting 1.9.11 y Multiplayer Center 1.0.1.')
p('Multiplayer Center no implementa el cooperativo por sí solo. Fusion importado tampoco significa que la partida lo utilice. El código de juego revisado conserva LanSession.')
h('Decisión inmediata')
p('Aprovechar DOTween, UIEffect y UIParticle antes de instalar más herramientas visuales. SoftMask y TMPEffects se activarán donde aporten una mejora verificable. Evitar efectos que dificulten leer o navegar.')
p('Cinzel, Cathedral y los modelos son recursos artísticos, no plugins. Revisar sus licencias y atribuciones antes de distribuir el proyecto.')
page('Extensiones recomendadas y habilidades de IA')
table(['Opción','Prioridad','Decisión'],[
['Unity Localization','Media','Separar textos del código y preparar idiomas. No instalado en el manifiesto revisado.'],['Feel','Opcional','Evaluar respuestas sonoras y visuales. Se solapa parcialmente con DOTween y código existente.'],['DunGen','Posterior','Evaluar habitaciones modulares cuando comience la fase procedural.'],['Más sistemas de UI','Baja','No añadir otro framework completo durante el rediseño.']])
p('No se instalaron ni compraron herramientas durante esta auditoría. Confirmar licencia, coste y compatibilidad con Unity 6000.5.6f1 antes de incorporar paquetes.')
h('Habilidades recomendadas')
p('Primera opción: las skills oficiales de Unity Technologies. Incluyen flujos para trabajar con Unity y su CLI. El paquete Unity Pipeline ya presente puede facilitar integración, pero su conexión aún requiere verificación.')
p('Segunda opción: una skill propia para Cinders Souls. Debe registrar estructura del proyecto, paleta, tipografía, plugins, reglas de clases y pruebas obligatorias. Sería creada específicamente; no existe todavía.')
p('Tercera opción: Figma para diseño de pantallas y componentes. Complementa la implementación, pero no sustituye las pruebas dentro de Unity.')
p('Ya están disponibles habilidades de generación de imágenes, documentos y control del escritorio. No hace falta añadirlas nuevamente.')
h('Información que conviene aportar')
p('Referencias visuales concretas, estilo final de personajes, resolución objetivo, presupuesto para assets y necesidad de cooperativo sin internet. Estas decisiones reducen cambios posteriores.')
page('Evolución procedural y plan de producción')
h('Diseño futuro propuesto')
p('Cuatro distritos y una zona final. El primer prototipo tendrá ocho habitaciones modulares. Cada distrito ofrecerá tres objetivos y exigirá completar dos. Son objetivos de diseño, no funciones terminadas.')
p('El generador debe garantizar entrada, salida, rutas transitables, espacio para encuentros y conexión de navegación. La semilla permite reproducir errores. Validar 100 semillas antes de ampliar contenido.')
h('Enemigos y cooperación')
p('Reutilizar enemigos y jefes existentes antes de añadir nuevos sistemas. Creepy Doll necesita rol, animaciones, hitboxes, daño y pruebas propias. Importar su modelo no completa el enemigo.')
p('Conservar LAN estable durante la evaluación de Fusion. Un solo sistema debe controlar cada sesión. El anfitrión validará daño, compras, objetivos y generación.')
h('Orden de trabajo')
p('1. Cerrar menú y clases: coherencia visual, navegación y pruebas de resolución.\n2. Unificar opciones, HUD, pausa, tienda y resultados.\n3. Decidir estilo de personajes y sustituir prototipos incompatibles.\n4. Construir una zona procedural gris y validar semillas.\n5. Integrar objetivos, encuentros, recompensas y cooperativo.\n6. Medir rendimiento y preparar una compilación distribuible.')
h('Fuentes y trazabilidad')
p('Evidencia local: Packages/manifest.json, Packages/com.coffee.ui-effect/package.json, Assets/Photon, MainMenuUI.cs, MenuEmbers.cs, MenuModelShowcase.cs, RoundRules.cs y el GDD anterior. Revisión del 29/10/2026.')
for t in ['Unity Skills — https://github.com/Unity-Technologies/skills','DunGen — https://dungen-docs.aegongames.com/2.19/','Feel — https://feel-docs.moremountains.com/','Localization — https://docs.unity.com/en-us/engine/6000.7/manual/packages-list/packages-all/pack-core/com-unity-localization']:
 p(t)
D.save('Docs/GDD_Cinders_Souls_2026-10-29.docx')
