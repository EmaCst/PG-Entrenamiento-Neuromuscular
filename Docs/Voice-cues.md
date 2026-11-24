# Voces de Praxen

Audios entregados por el usuario, incluidos en Resources/PraxenVoices para reproducción sin conexión. Un AudioSource 2D evita duplicar la voz entre ojos. Los clips se cargan una sola vez cuando se usan.

- Individual: instrucciones del ejercicio, aviso de último set, cuenta 5–1, aviso de diez segundos (solo sets mayores de diez segundos), descanso y fin del entrenamiento.
- Circuito: mismo flujo por etapa; último set se anuncia una vez al empezar la última vuelta.
- El aviso de pies suena al entrar en la escena, antes de esperar la alfombrilla, tanto individual como circuito. Las instrucciones terminan antes de los cinco segundos de preparación. No consumen tiempo del set.
- El descanso empieza con set_done. En descansos de al menos 15 segundos se avisa a ocho segundos del final. Si el descanso es más corto que el audio, se corta al terminar el tiempo configurado.
- La cámara sigue disponible durante instrucciones y descansos. Cambiar de escena o cancelar corta la voz pendiente.
- Correr usa turn_intro, turn_left y turn_right cuando se entreguen los nuevos audios. El aviso antiguo running, delimit_optional y ground quedan fuera del flujo; no hay delimitación ni suelo. También quedan reservados start (comienza), intro_optional y encourage_optional.

## Prueba en Android

1. Individual con dos sets: instrucción completa, números sincronizados, tiempo del set empieza al terminar el uno, sin comienza y descanso con cámara visible.
2. Set de 5 segundos: no debe anunciar diez segundos. Descanso cero: sin aviso de descanso.
3. Circuito con un ejercicio desactivado: sin instrucciones del ejercicio omitido; último set solo una vez por vuelta final.
4. Correr: cámara y giroscopio, preparación y giros a izquierda/derecha; reiniciar calibración o salir detiene el audio.
5. Final completo antes de volver al menú, sin voz repetida por ojo.

No se pudo ejecutar Unity ni generar APK en el entorno de edición. Se verificaron los archivos MP3, duraciones, metadatos y conexiones de código; falta escuchar y comprobar sincronización en dispositivo.

## Música

training_music.ogg es una pista instrumental original sintetizada para Praxen, de 110 BPM y ocho compases (17,45 segundos). Se repite mediante un AudioSource 2D independiente en las tres escenas de ejercicio, se conserva entre escenas de entrenamiento y se detiene en el menú. Volumen 18% durante ejercicio, 7% durante contador/descanso, 2,5% al hablar; transiciones suaves. Al finalizar un circuito baja a cero. Funciona sin red y no utiliza servicios de generación externos.

Comprobar en teléfono: voz comprensible sobre la música, continuidad al repetir el loop, volumen durante descansos y silencio al volver al menú. No se pudo escuchar la mezcla en este entorno.
