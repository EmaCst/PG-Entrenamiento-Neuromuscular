# Voces de Praxen

Audios entregados por el usuario, incluidos en Resources/PraxenVoices para reproducción sin conexión. Un AudioSource 2D evita duplicar la voz entre ojos. Los clips se cargan una sola vez cuando se usan.

- Individual: instrucciones del ejercicio, aviso de último set, cuenta 5–1, comienza, aviso de diez segundos (solo sets mayores de diez segundos), descanso y fin del entrenamiento.
- Circuito: mismo flujo por etapa; último set se anuncia una vez al empezar la última vuelta.
- Las instrucciones terminan antes de los cinco segundos de preparación. No consumen tiempo del set.
- El descanso empieza con set_done. En descansos de al menos 15 segundos se avisa a ocho segundos del final. Si el descanso es más corto que el audio, se corta al terminar el tiempo configurado.
- La cámara sigue disponible durante instrucciones y descansos. Cambiar de escena o cancelar corta la voz pendiente.
- Correr reproduce ground mientras espera detectar suelo. delimit_optional no se activa: el audio indica caminar, pero la calibración actual pide tocar cuatro esquinas. También quedan reservados intro_optional (otro comienzo) y encourage_optional para evitar avisos redundantes.

## Prueba en Android

1. Individual con dos sets: instrucción completa, números sincronizados, tiempo del set empieza con comienza y descanso con cámara visible.
2. Set de 5 segundos: no debe anunciar diez segundos. Descanso cero: sin aviso de descanso.
3. Circuito con un ejercicio desactivado: sin instrucciones del ejercicio omitido; último set solo una vez por vuelta final.
4. Correr: suelo, cuatro esquinas, instrucciones y preparación; reiniciar calibración o salir detiene el audio.
5. Final completo antes de volver al menú, sin voz repetida por ojo.

No se pudo ejecutar Unity ni generar APK en el entorno de edición. Se verificaron los archivos MP3, duraciones, metadatos y conexiones de código; falta escuchar y comprobar sincronización en dispositivo.
