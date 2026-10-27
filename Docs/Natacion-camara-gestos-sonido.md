# Cámara, desplazamiento y sonido

## Cámara vertical

Corregida la transformación inversa de UV del shader para los giros de 90°/270°:
Unity proporciona videoRotationAngle en sentido horario y el muestreo se realiza
con su inversa. El sentido anterior dejaba la imagen girada 180° para esos ángulos.
Se mantiene el flip vertical de la textura original y el espejo solo de presentación.
La imagen corregida es compartida por el vídeo y la entrada de Pose; no se rota
únicamente la vista del usuario. Referencia: documentación oficial de Unity,
WebCamTexture.videoRotationAngle.

Antes de crear la textura de inferencia se esperan tres frames con dimensiones,
rotación y flip iguales (hasta 4 s). Así no se fija el ángulo transitorio que algunas
cámaras Android publican al arrancar. Se conserva el tamaño de entrada y los 15 Hz.

## Deslizar para bajar/subir

El menú no muestra barras de desplazamiento. Se arrastra verticalmente sobre el
contenido, incluidos textos y botones. Input System lee un solo puntero (touch en
Android y ratón en Editor). Una tolerancia de 12 unidades de interfaz distingue
tap de arrastre; al arrastrar se cancela el click del botón/campo bajo el dedo.
El scroll se limita a la altura real del contenido y respeta el área segura.
Arrastrar horizontalmente un control de volumen sigue ajustándolo; la rueda del
ratón conserva el scroll de GUILayout. No se utiliza UnityEngine.Input antiguo.

## Música y voces independientes

En inicio/configuración del menú: Sonido · Voces y música. Cada canal tiene volumen
0–100 %, se guarda por separado en PlayerPrefs y 0 lo silencia. Se puede probar la
voz y una muestra de música de 10 segundos. Al salir de Sonido termina la muestra.

- PraxenVoice: únicamente voces y señales en Resources/PraxenVoices.
- PraxenMusic: componente y AudioSource propios; carga
  Resources/PraxenMusic/training_music.ogg. Al entrenar hace loop, atenúa durante
  descanso y baja inmediatamente al comenzar una voz para no tapar los giros.
- PraxenAudioSettings: preferencias independientes de los dos canales.

La pista nueva es instrumental original: 128 BPM, 16 compases / 30 s, bombo a negras,
percusión, bajo y arpegios con progresión Am–F–C–G. No utiliza muestras ni canciones
externas. Se añadió Tools/generate_training_music.py para poder regenerarla con
NumPy y ffmpeg. El loop usa colas circulares para evitar cortar notas en la unión;
la pista anterior no se utiliza. El archivo nuevo es mono Vorbis en memoria
comprimida, sin síntesis ni generación de música durante el entrenamiento.

## Comprobaciones

Ejecutado aquí: 32 esquinas contra las expresiones reales del shader (4 ángulos ×
2 flips × 4 esquinas), validación de rutas/controles separados, decodificación de
la pista nueva y duración exacta de 30 s; revisión estática de código/metadatos.
Añadidos EditMode PraxenSwipeGestureTests: tap, drag sobre botones, slider horizontal,
inicio fuera del contenido y final del gesto. Unity no está disponible: no se
compiló ni se ejecutaron EditMode o pruebas de vídeo/touch/reproducción en Android.

En el teléfono comprobar cabeza arriba/pies abajo y alineación de objetivos,
arrastre sobre textos/botones sin activarlos, tap normal en controles, volúmenes
independientes y persistentes, cambio de escenas, descanso y audio de giro claro.
