# Natación en vertical

El teléfono se utiliza en vertical (Portrait), con la pantalla y la cámara de
selfies hacia la persona. Apoyarlo en una superficie estable; encuadrar cabeza,
manos y pies. Los tres ejercicios mantienen sus reglas y mediciones.

## Cambios

- Orientación Portrait en PlayerSettings y al iniciar la app. Desactivada la
  rotación a horizontal y a vertical invertida. Pantalla encendida al entrenar.
- El menú tiene una vista vertical de una columna: inicio, tutorial, configuración
  individual/circuito, cuenta, historial y resultados. Usa scroll y área segura.
  Reutiliza las acciones y validaciones existentes; no duplica autenticación ni
  modifica endpoints, datos o propietarios de sesiones.
- Login/registro estrechos: campos apilados, contraseñas y botón de envío completos.
- Resultados apilados y navegación de historial con las mismas etiquetas de Pose.
- El vídeo se sigue ajustando entero al rectángulo disponible, sin estirarlo ni
  recortarlo para llenar la pantalla. Conserva espejo y transformación compartida
  entre cámara y objetivos. La inferencia sigue con 640 px en el lado largo y 15 Hz.
- Android termina de girar a vertical antes de abrir la cámara. También se aplica
  al ejecutar directamente una escena de ejercicio.
- HUD adaptado a ancho vertical, panel de cuenta atrás más estrecho y alto,
  tipografía de giro ajustada al ancho. Cámara visible durante descanso.
- Tutorial inicial corregido a vertical. Voces nuevas todavía no integradas.

## Verificación

En este entorno no hay Unity ni compilador C#: no se ha compilado ni confirmado
la orientación real, el vídeo o la distribución visual en Android.
Se revisaron estáticamente las referencias de las clases parciales, metadatos,
orientación, páginas y dimensiones del formulario.

En Unity seleccionar Game > 9:16 (Portrait). Revisar inicio/tutorial, registro y
login con teclado, configuración individual/circuito y resultados/historial.
En Android abrir desde cero y ejecutar directamente cada escena: no debe pedir
horizontal ni dar un error por cambio de dimensiones al abrir la cámara.
Confirmar coincidencia visual de manos/pies con objetivos, giros izquierda/derecha,
cuenta de preparación y descanso con vídeo, y ausencia de textos cortados.
Las pantallas de configuración largas se recorren deslizando verticalmente.
