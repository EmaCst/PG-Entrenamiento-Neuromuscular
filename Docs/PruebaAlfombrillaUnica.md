# Prueba con una alfombrilla blanca de 14 cm

Rama: `feature/pies-alfombrillas-referencia`. Compila la APK normalmente; no regeneres las escenas.

1. Coloca el círculo blanco con borde negro de 14 cm sobre el suelo, bien iluminado, con el borde entero visible.
2. Abre **Un ejercicio → Pies**, preferiblemente en dificultad Inicial y duración 60 s. Mantén ambos pies visibles y fuera del círculo mientras se localiza.
3. Se pide una referencia consistente en tres imágenes antes de iniciar la preparación de 5 segundos. Un aro verde identifica la alfombrilla; después el único objetivo será azul o rojo, según el pie solicitado.
4. Mueve el pie indicado desde fuera hacia el círculo y retíralo después del contacto. Mantenerlo encima no debe acumular aciertos. Se conserva YOLO para localizar los pies.
5. Antes de pisar, mueve suavemente el teléfono y comprueba si el aro sigue el círculo. Comprueba también que no salte a las baldosas o a reflejos y que la fluidez se mantenga.
6. Si no localiza la referencia, acerca la cámara, muestra el círculo entero y mejora la iluminación. Tras 120 s sin una referencia lista, vuelve al menú.

## Alcance de esta prueba

El objetivo se coloca sobre la imagen detectada de la alfombrilla. No es un anclaje de ARCore ni una estimación de su posición en metros: el diámetro 14 cm identifica la referencia de prueba, mientras su tamaño visible se obtiene de la imagen. Todavía no se integran tres alfombrillas.

Cuando el pie tapa el círculo se conserva su última posición hasta 0.8 s desde la captura original. Durante esa ventana, evita mover el teléfono. Pasado ese plazo se oculta la referencia y se bloquean aciertos hasta recuperarla. No se registra fallo por perder únicamente la alfombrilla en el timeout del objetivo; la duración general de la sesión continúa.

La asignación izquierda/derecha conserva el método anterior (posición horizontal de las detecciones), por lo que conviene mostrar ambos pies y evitar cruzarlos en esta prueba. El contacto es solapamiento visual de la caja de YOLO con la elipse; no mide presión física sobre el papel.

El detector analiza una cuadrícula de 160×160 de los datos RGB que ya prepara YOLO: busca regiones claras con forma elíptica y comprueba el borde oscuro a mayor resolución. No usa una segunda cámara, otro modelo ni una descarga adicional de imagen. Sus buffers y las texturas de los indicadores se reutilizan. Marcador para medir coste: `Praxen.Feet.FindMat`.

Pendiente: compilación y prueba en Unity/Android, seguimiento con cambios de iluminación/perspectiva y medición de FPS. La comprobación offline con la foto proporcionada no sustituye esas pruebas.
