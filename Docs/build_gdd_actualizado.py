from pathlib import Path
from docx import Document
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_CELL_VERTICAL_ALIGNMENT
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

ROOT = Path(r"D:\67\Last67Seconds-Maps")
OUT = ROOT / "Docs" / "GDD_Cinders_Souls_2026-10-29_actualizado.docx"
D = Document()
section = D.sections[0]
section.page_width, section.page_height = Inches(8.5), Inches(11)
section.top_margin = section.bottom_margin = Inches(.72)
section.left_margin = section.right_margin = Inches(.8)

for name in ("Normal", "Title", "Heading 1", "Heading 2"):
    style = D.styles[name]
    style.font.name = "Aptos"
    style.font.color.rgb = RGBColor(22, 24, 28)
D.styles["Normal"].font.size = Pt(10)
D.styles["Normal"].paragraph_format.space_after = Pt(6)
D.styles["Normal"].paragraph_format.line_spacing = 1.08
D.styles["Title"].font.size = Pt(28)
D.styles["Title"].font.bold = True
D.styles["Title"].paragraph_format.space_after = Pt(12)
D.styles["Heading 1"].font.size = Pt(17)
D.styles["Heading 1"].font.bold = True
D.styles["Heading 1"].paragraph_format.space_before = Pt(12)
D.styles["Heading 1"].paragraph_format.space_after = Pt(8)
D.styles["Heading 2"].font.size = Pt(12)
D.styles["Heading 2"].font.bold = True
D.styles["Heading 2"].paragraph_format.space_before = Pt(9)
D.styles["Heading 2"].paragraph_format.space_after = Pt(4)

footer = section.footer.paragraphs[0]
footer.alignment = WD_ALIGN_PARAGRAPH.RIGHT
footer.text = "Cinders Souls  |  GDD 3.0  |  29 octubre 2026  |  "
fld = OxmlElement("w:fldSimple")
fld.set(qn("w:instr"), "PAGE")
footer._p.append(fld)

def p(text=""):
    return D.add_paragraph(text)

def h(text):
    D.add_heading(text, 2)

def page(text):
    D.add_page_break()
    D.add_heading(text, 1)

def bullet(text):
    D.add_paragraph(text, style="List Bullet")

def table(headers, rows, widths=None):
    t = D.add_table(rows=1, cols=len(headers))
    t.autofit = False
    t.style = "Table Grid"
    if widths:
        for i, w in enumerate(widths):
            t.columns[i].width = Inches(w)
    for cell, label in zip(t.rows[0].cells, headers):
        cell.text = label
    for record in rows:
        for cell, value in zip(t.add_row().cells, record):
            cell.text = value
    borders = OxmlElement("w:tblBorders")
    for edge in ("top", "left", "bottom", "right", "insideH", "insideV"):
        el = OxmlElement("w:" + edge)
        el.set(qn("w:val"), "single")
        el.set(qn("w:sz"), "4")
        el.set(qn("w:color"), "D9D9D9")
        borders.append(el)
    t._tbl.tblPr.append(borders)
    for idx, row in enumerate(t.rows):
        for j, cell in enumerate(row.cells):
            if widths:
                cell.width = Inches(widths[j])
            cell.vertical_alignment = WD_CELL_VERTICAL_ALIGNMENT.CENTER
            tcpr = cell._tc.get_or_add_tcPr()
            shd = OxmlElement("w:shd")
            shd.set(qn("w:fill"), "2D3036" if idx == 0 else ("F4F4F4" if idx % 2 else "FFFFFF"))
            tcpr.append(shd)
            mar = OxmlElement("w:tcMar")
            for edge in ("top", "left", "bottom", "right"):
                v = OxmlElement("w:" + edge)
                v.set(qn("w:w"), "90")
                v.set(qn("w:type"), "dxa")
                mar.append(v)
            tcpr.append(mar)
            for para in cell.paragraphs:
                para.paragraph_format.space_after = Pt(0)
                para.paragraph_format.line_spacing = 1.03
                for run in para.runs:
                    run.font.size = Pt(8.7)
                    run.font.color.rgb = RGBColor(255, 255, 255) if idx == 0 else RGBColor(20, 20, 20)
                    if idx == 0:
                        run.bold = True
    for cell in t.rows[0].cells:
        for para in cell.paragraphs:
            para.paragraph_format.keep_with_next = True
    p()

def shot(name, caption):
    path = ROOT / "Assets" / "Screenshots" / name
    if path.exists():
        D.add_picture(str(path), width=Inches(6.55))
        para = D.add_paragraph(caption)
        para.alignment = WD_ALIGN_PARAGRAPH.CENTER
        for run in para.runs:
            run.font.size = Pt(8)
            run.italic = True

D.add_paragraph("Diseño del juego Cinders Souls", style="Title")
p("Documento de diseño actualizado y catálogo de herramientas del proyecto Last67Seconds Maps")
p("Versión 3.0  |  29 de octubre de 2026  |  Proyecto D:\\67\\Last67Seconds-Maps")
p("El flujo individual ya contiene menú, selección de clase, prólogo narrativo y un primer piso procedural. Este documento describe lo implementado, fija la dirección de diseño y explica las skills de Codex y los plugins de Unity presentes. Las funciones futuras aparecen señaladas como propuestas.")
shot("new-fonts-menu.png", "Menú actual con la nueva tipografía y la dirección gótica.")

h("Visión jugable")
p("Roguelike de acción en primera persona con combate rápido, cinco clases y expediciones de calabozo. Dead Cells orienta el ritmo y la repetición; Hollow Knight aporta exploración y atmósfera; Dark Souls y Bloodborne orientan tensión, decadencia y lectura solemne. Son referencias de diseño, no fuentes de contenido copiado.")
h("Decisión de diseño actual")
p("La partida individual abandona la progresión automática por rondas. El descenso pasa por un prólogo y un piso de seis salas conectadas, con jefe al final. El cooperativo LAN mantiene el sistema anterior mientras se diseña su migración.")

page("Estado comprobado del juego")
table(["Sistema", "Estado al 29 de octubre de 2026"], [
    ("Motor", "Unity 6000.5.6f1; URP 17.5.0; uGUI 2.5.0 y TextMeshPro."),
    ("Flujo individual", "Menú → clase → Prologo → diálogo del Vigía → umbral → Descenso → jefe y resultado."),
    ("Prólogo", "Cinco salas conectadas; cortina INICIA EL DESCENSO; Vigía con modelo Class_Mage; cuatro intervenciones."),
    ("Primer piso", "Seis salas: segura, combate, relicario, combate, combate y jefe. Semilla nueva por expedición."),
    ("Combate", "Puertas selladas durante encuentros; premios al limpiar; muerte y victoria reinician una expedición."),
    ("Cooperativo", "LAN sigue en la ruta de 25 rondas. Photon Fusion está importado, sin migración comprobada."),
    ("Creepy Doll", "Recursos importados. No se ha validado su comportamiento como enemigo."),
], [1.42, 5.48])
h("Evidencia de validación")
p("La generación del primer piso produjo 100 semillas conectadas y 100 trazados distintos. El recorrido automatizado comprobó entrada secuencial, suelo físico, enemigos, relicario, jefe, victoria y reinicio. Aplicó daño elevado para acelerar transiciones, por lo que no prueba el equilibrio real.")
p("El prólogo registró 100 semillas conectadas y 64 trazados distintos. Las pruebas recientes no detectaron errores nuevos de ejecución. No existe todavía una validación completa de rendimiento, accesibilidad y cooperativo procedural.")

page("Bucle de expedición y contenido")
h("Bucle individual implementado")
p("Elegir clase → iniciar descenso → conversar con el Vigía → cruzar el umbral → explorar seis salas → derrotar guardianes → obtener recursos → vencer al Carcelero de Ceniza → ver resultado. Al morir o volver a jugar se genera otra semilla.")
h("Reglas del piso Celdas de Ceniza")
table(["Sala", "Función y recompensa"], [
    ("Vestíbulo", "Entrada segura y orientación visual."),
    ("Combate 1", "Guardianes; puerta cerrada hasta limpiar; 30 de oro y munición."),
    ("Relicario", "Recupera 40 % de la vida máxima; entrega 50 de oro y un arma de tier 2."),
    ("Combate 2 y 3", "Encuentros con composición variable; cada sala concede 30 de oro y munición."),
    ("Jefe", "El prefab existente Rey Goblin representa provisionalmente al Carcelero de Ceniza."),
], [1.38, 5.52])
p("La secuencia de tipos de sala permanece fija. La semilla modifica posiciones, corredores y guardianes. Aún no hay bifurcaciones, campaña de varios biomas, guardado persistente ni metaprogresión.")
shot("expedition-final.png", "Primer piso procedural con módulos del Calabozo y tratamiento visual oscuro.")

page("Clases y dirección del combate")
p("Las cinco clases tienen identidad diferenciada. Los valores siguientes aparecen en la interfaz; requieren contraste con estadísticas activas antes de aprobar el balance.")
table(["Clase", "Valores mostrados", "Papel de diseño"], [
    ("Caballero", "120 vida; velocidad 6.5", "Espada y escudo; presión equilibrada."),
    ("Arquero", "100 vida; velocidad 8", "Distancia, munición y movilidad."),
    ("Tanque", "180 vida; velocidad 4.8", "Aguantar y controlar espacio con mazo."),
    ("Mago", "100 vida; velocidad 6.5; 100 maná", "Hechizos y gestión de maná."),
    ("Shinobi", "120 vida; velocidad 6.5", "Katana, shuriken, parry y postura."),
], [1.15, 2.17, 3.58])
h("Principios de combate para la siguiente iteración")
bullet("Los ataques deben tener lectura clara, respuesta inmediata y recuperación que permita decidir.")
bullet("La esquiva y el parry deben recompensar el tiempo correcto sin volver irrelevante la posición.")
bullet("Cada sala debe ofrecer espacio seguro, presión y una salida de emergencia legible.")
bullet("El jefe debe probar los patrones aprendidos en el piso, no depender solo de más vida.")
p("Estas son reglas de diseño para la siguiente fase. No implican que todas las animaciones, ventanas y patrones estén terminados.")
shot("new-fonts-classes.png", "Selección de clases actual con retrato, ficha y jerarquía tipográfica.")

page("Arte interfaz y narrativa")
h("Dirección visual")
p("Carbón, piedra fría, rojo sangre apagada y latón envejecido estructuran la interfaz. Los módulos KayKit del Calabozo forman el primer piso: suelos, muros, rejas, pilares, barriles y antorchas. Techos cerrados, niebla, viñeta URP y contraste de luz fría con fuego cálido refuerzan el descenso.")
p("Los modelos actuales mantienen proporciones estilizadas. Su contraste con el tono gótico requiere una dirección artística coherente antes de sustituir grandes cantidades de contenido.")
h("Tipografía")
p("Uncial Antiqua se usa en títulos; Crimson Text SemiBold en lectura funcional. Los archivos de licencia OFL están en Assets/Fonts. TextMeshPro usa atlas estáticos de 2048 × 2048 con caracteres latinos, tildes y signos españoles verificados. Las fuentes provienen de google/fonts; no se extrajeron de juegos comerciales.")
h("Pantallas")
p("El menú prioriza Iniciar descenso. La selección presenta las cinco clases, retrato y datos jugables. El prólogo usa una cortina y la frase INICIA EL DESCENSO. El diálogo del Vigía aparece abajo con retrato y escritura progresiva; E, Enter, Espacio o el botón completan o avanzan el texto.")
p("En el prólogo, WASD mueve, Mayús esquiva y Escape abre la pausa. Tras el diálogo, E junto al umbral rojo inicia Descenso. La interfaz debe conservar contraste legible sobre fondos oscuros y foco visible para teclado.")
h("Criterios visuales pendientes")
p("Revisar 1280 × 720, 1920 × 1080, 2560 × 1440 y ultrapanorámico. Confirmar tamaños de texto, solapamientos, controles con teclado y ratón, y una opción para reducir movimiento y destellos. Medir partículas y cámaras de retrato.")

page("Plugins de Unity instalados")
p("Plugin significa código o paquete dentro del proyecto Unity. Su presencia no demuestra uso en todas las pantallas. Estado inferido del manifiesto y las referencias del código revisado.")
table(["Plugin", "Versión", "Función y uso comprobado"], [
    ("DOTween", "1.2.815", "Anima transiciones y estados de botones; el prólogo usa un fundido."),
    ("UIEffect", "5.9.7", "Efectos sobre uGUI y TMP; el título del menú usa sombra."),
    ("UIParticle", "4.14.2", "Dibuja partículas en Canvas; MenuEmbers lo utiliza."),
    ("SoftMaskForUGUI", "3.6.5", "Permite máscaras suaves en UI; instalado, sin uso activo confirmado."),
    ("TMPEffects", "0.3.2", "Permite animaciones de texto TMP; instalado, sin efecto activo confirmado."),
    ("Photon Fusion", "Sin versión verificada", "SDK de red importado en Assets/Photon/Fusion; la ruta LAN no está migrada."),
], [1.35, 1.12, 4.43])
h("Paquetes técnicos en manifest.json")
table(["Paquete", "Versión", "Aporte al proyecto"], [
    ("URP", "17.5.0", "Render e iluminación, niebla y efectos de volumen."),
    ("uGUI", "2.5.0", "Canvas, botones y fichas de interfaz."),
    ("Input System", "1.20.0", "Base moderna para entradas; su adopción total en juego no está auditada."),
    ("AI Navigation", "2.0.14", "Herramientas de navegación de agentes."),
    ("Timeline", "1.8.12", "Secuencias temporales y cinemáticas."),
    ("Test Framework", "1.7.0", "Infraestructura de pruebas automatizadas."),
    ("Unity Pipeline", "0.8.0-exp.1", "Control del Editor para compilación y pruebas desde herramientas de agente."),
    ("Visual Scripting", "1.9.11", "Edición visual de lógica; presencia no demuestra uso."),
    ("Multiplayer Center", "1.0.1", "Herramientas de configuración multijugador; no sustituye la lógica LAN."),
], [1.65, 1.05, 4.2])
p("TextMeshPro forma parte del flujo de texto actual. Cinzel y las nuevas fuentes son recursos, no plugins. En Codex, la captura mostraba cero complementos externos instalados; esa pantalla no describe los paquetes del proyecto Unity.")

page("Skills de Codex instaladas para el proyecto")
p("Las ocho skills personales viven en C:\\Users\\Hp\\.codex\\skills. Son instrucciones de trabajo para el asistente; no se cargan como plugins dentro de Unity.")
table(["Skill personal", "Qué aporta a este juego"], [
    ("cinders-souls-design", "Mantiene contexto y dirección gótica de Cinders Souls; distingue hechos de propuestas."),
    ("ui-ugui", "Edita y revisa Canvas, RectTransform, layouts y prefabs de interfaz."),
    ("optimize-text-mesh-pro", "Configura fuentes SDF, atlas, fallbacks y coste de texto TMP."),
    ("urp-postprocessing", "Ajusta Volume, bloom, color, viñeta y otros efectos URP."),
    ("physics-3d-collision", "Diagnostica colliders, triggers y raycasts en combate y salas."),
    ("unity-cli", "Gestiona el proyecto y controla tareas de Unity desde comandos."),
    ("unity-pipeline", "Conduce Editor activo, compila, ejecuta pruebas y captura pantallas."),
    ("unity-package-management", "Instala, actualiza y comprueba paquetes UPM y versiones."),
], [2.23, 4.67])
h("Skills del sistema instaladas")
table(["Skill del sistema", "Uso práctico"], [
    ("Image Gen", "Genera o edita arte raster, conceptos, texturas y variantes para evaluación."),
    ("OpenAI Docs", "Consulta documentación oficial de Codex, skills y herramientas OpenAI."),
    ("Review Agent", "Busca defectos accionables en cambios de código."),
    ("Skill Creator", "Crea o actualiza instrucciones reutilizables del proyecto."),
    ("Skill Installer", "Instala skills desde catálogo o repositorios autorizados."),
], [2.23, 4.67])
p("También hay capacidades de documentos, PDF, visualización y control del escritorio disponibles mediante el entorno. La skill de documentos se utilizó para este Word. No son complementos Unity ni evidencian funciones nuevas del juego.")

page("Plan de producción y decisiones pendientes")
h("Siguiente incremento recomendado")
table(["Prioridad", "Entrega verificable"], [
    ("1", "Afinar ritmo real de las cinco clases: daño, esquiva, recuperación, maná y parry."),
    ("2", "Variar objetivos y rutas del primer piso sin perder conectividad de las semillas."),
    ("3", "Diseñar patrones propios del Carcelero de Ceniza y sustituir el prefab provisional si procede."),
    ("4", "Unificar HUD, pausa, botín, resultado y opciones con la nueva tipografía y paleta."),
    ("5", "Medir rendimiento, probar resoluciones y registrar errores de expedición reproducibles por semilla."),
    ("6", "Diseñar sincronización de generación y combate antes de migrar LAN a expediciones."),
], [.85, 6.05])
h("Alcance posterior")
p("La campaña con varios biomas, bifurcaciones, metaprogresión, guardado persistente y enemigos nuevos queda propuesta. Creepy Doll necesita rol, animación, colisiones, daño y pruebas antes de incorporarse. No se requiere instalar más plugins visuales para terminar la actual fase de UI.")
h("Fuentes internas y discrepancias")
p("Fuentes: Packages/manifest.json, ProjectSettings/ProjectVersion.txt, Docs/EXPEDITION_IMPLEMENTATION.md, Docs/PROLOGUE_IMPLEMENTATION.md, Assets/02_Scripts/Expedition, Assets/02_Scripts/UI y capturas en Assets/Screenshots. La nota antigua del prólogo menciona Calabozo como destino; el flujo individual actualizado y comprobado conduce a Descenso.")
p("Versiones de DOTween y Photon Fusion requieren comprobación en sus archivos o Editor antes de una publicación formal. El balance, la accesibilidad y el rendimiento aún necesitan pruebas de producto.")

D.save(OUT)
print(OUT)
