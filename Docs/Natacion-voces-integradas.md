# Voces de Natación

Integradas las diez grabaciones de Audios.zip en Resources/PraxenVoices.
Las ocho grabaciones de instrucciones/estado permanecen idénticas al ZIP.
left/right se importan como turn_left.wav / turn_right.wav, mono PCM, quitando
solo 80 ms / 50 ms de silencio inicial. Los nombres sin extensión son las claves.
Los diez audios se verificaron con ffprobe y decodificación ffmpeg sin errores.

- Al entrar en el primer ejercicio de cada entrenamiento: selfie_setup, luego
  pose_hands/pose_feet/pose_turns. En circuito no se repite la introducción larga
  después de la primera vuelta. pose_calibration solo si aún no está listo.
- Las instrucciones terminan antes de comenzar los cinco segundos de preparación.
  La detección/calibración puede funcionar mientras se escucha la introducción.
- Giro: clips cortos precargados antes del set. Señal visual y Play de izquierda /
  derecha se inician en la misma actualización. El reloj de respuesta se inicia
  después de llamar Play, sin cargar Resources en ese instante. No es sincronización
  de audio a nivel de muestra: falta medir latencia de altavoz en Android.
- Tras completar/agotar un giro: return_front. No se solicita durante pérdida de
  seguimiento ni cuando el plan pausa/termina el set. Se mantiene espera de 3–5 s
  después de recuperar la posición frontal. No se inicia otro giro mientras habla.
- Pérdida válida durante set >0.6 s: tracking_lost una vez, con un mínimo de 10 s
  entre avisos. Recuperación estable >0.5 s: tracking_ready, si se anunció la pérdida.
  Sin cola de mensajes y sin avisos de seguimiento durante introducción/descanso.
- Una sola voz, con música atenuada como antes. Los avisos de diez segundos y
  seguimiento solo se reproducen si el canal está libre; no cortan izquierda/derecha.
- Los assets incluyen metadatos de Unity, mono y precarga. No se añade otro detector,
  ni inferencias o búsquedas de objetos por cada frame de cámara.

Comprobado aquí: nombres/entradas del ZIP, archivos distintos de izquierda/derecha,
decodificación de 10 clips, conservación de 8 originales y revisión estática del
flujo. No hay Unity en este entorno: compilar y comprobar reproducción en Android.
Probar los tres individuales y un circuito de dos vueltas, voz/imagen del giro,
pérdida breve/larga de encuadre, recuperación, salida al menú y descanso. Verificar
con el teléfono que ningún aviso antiguo contradiga el nuevo flujo y que los
clips cortos sean audibles sobre la música. No se cambiaron la API ni las métricas.
