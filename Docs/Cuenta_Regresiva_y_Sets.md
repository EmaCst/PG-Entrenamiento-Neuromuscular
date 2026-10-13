# Cuenta regresiva, sets y conexión

Rama Unity: `feature/cloud-results`.

## Cuenta y resultados

Abre «Cuenta y nube» en MenuTelefono. Registro y login usan `username` y contraseña; el registro conserva nombre y federación. URL incorporada: `https://api-praxen.onrender.com`. El formulario ya no solicita correo ni una URL.

Inicia sesión antes de entrenar para asociar los resultados a tu cuenta. El invitado sigue siendo local. El JWT permanece en memoria y los pendientes conservan servidor y propietario.

## Escena CuentaRegresiva

Se incluye en EditorBuildSettings y el generador de escenas. El controlador crea un Canvas de pantalla completa con fondo opaco y dos contadores centrados en los ojos izquierdo y derecho. Respeta separación y altura del visor; no añade una cámara.

Se carga aditivamente y se oculta al terminar, conservando la cámara, el seguimiento y la calibración AR. Pausa los objetivos. La misma escena muestra «Prepárate» durante 5 segundos y «Descanso» durante el tiempo configurado. Cada set tiene preparación; el descanso y esos 5 segundos se cuentan por separado.

Si el Build Profile usa una lista propia de escenas, incluye `Assets/Scenes/CuentaRegresiva.unity`. Hay una UI temporal de respaldo si se omite la escena, con una advertencia en el registro.

## Individuales

Duración por set: 5–3600 s. Sets: 1–100. Descanso: 0–3600 s. Correr espera a delimitar el área. El primer set inicia el ejercicio y los siguientes lo reanudan, conservando estadísticas. No hay descanso después del último. El resultado conserva sets, duración y descanso seleccionados.

El tiempo por dificultad de manos excluye preparación y descansos. Las esperas de objetivos de pies se cancelan al pausar para evitar objetivos durante el descanso.

## Circuitos

0 segundos desactiva un ejercicio; los activos requieren 5–3600 s. El circuito no puede quedar vacío. Cada vuelta ejecuta solo los activos en orden manos → pies → correr. Hay descanso entre etapas incluidas, sin descanso final. Se exportan únicamente ejercicios ejecutados.

## Comprobación en Unity y teléfono

1. Ejecuta EditMode: TrainingPlanTests. Valida circuitos vacíos, límites, orden y omisión de ejercicios por set.
2. Individual de manos: 2 sets de 5 s y descanso de 3 s. Comprueba preparación de 5 s por set, pausa, reanudación, ausencia de descanso final y resultado con sets=2.
3. Repite con pies: no activa objetivos durante descanso y conserva estadísticas. En correr, delimita el área antes del primer contador; el segundo set mantiene esa delimitación.
4. Circuito: manos 5 s, pies 0 s, correr 5 s, dos vueltas. No carga ni exporta pies. Repite con solo pies activo y con los tres en 0 (Inicio debe bloquearse).
5. Visor: contador en ambos centros y sin objetivos ni HUD por delante. Comprueba descanso y cancelación sin pantalla atascada.
6. Cuenta de prueba en Render: login por username y consulta de los resultados del teléfono. Sin red, comprobar pendiente y reintento posterior.

Unity y el APK no se pudieron compilar ni ejecutar en este entorno. EditMode, revisión visual y seguimiento real quedan pendientes en Unity y en el teléfono.
