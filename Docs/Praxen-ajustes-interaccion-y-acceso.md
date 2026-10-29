# Interacción, sonido, orientación y acceso

Base: feature/praxen-modo-dual en 2598a31d8ec690056a928b9153172bab84563962.
Conserva los cinco commits posteriores a ef1895c, incluido el cambio obligatorio
de contraseña temporal. Issues #10, #11, #12 y #13.

## Ejercicios

- Pose manos: una observación nueva y fiable con la mano solicitada confirma el
  contacto. Sin espera mínima. Se mantiene el margen ampliado de manos, rechazo
  de observaciones antiguas/duplicadas y un único resultado por objetivo.
- Pose pies: margen invisible adicional pasa de 0.04 a 0.18 anchos de hombros en
  coordenadas de imagen. La esfera mantiene su tamaño. La misma geometría se usa
  para contacto y retirada, evitando que el área ampliada puntúe automáticamente.
  Dos observaciones fuera arman el intento; la primera entrada válida puntúa.
  Tras perder seguimiento, se exige salir de nuevo. No se aceptan pies inventados.
- Giros Pose y giroscopio: espera aleatoria uniforme entre 1, 2 y 3 segundos.
  En Pose empieza tras recuperar la posición frontal estable; no se acorta el
  tiempo disponible para completar el giro ni la confirmación angular.
- No se aumentan inferencias: Pose permanece en 20 Hz solicitados, una petición
  en curso y objetivo de render de 30 FPS (no mínimos garantizados).

## Sonido y UX

Dos tonos originales sintetizados una vez al abrir, de 140/200 ms, sin archivos
externos. Acierto: dos notas ascendentes; fallo: tono descendente suave. Una
fuente propia reproduce efectos sin detener voz o música. Volumen independiente
persistido y botones de prueba en Sonido. La emisión se hace en el registro de
resultados compartido, una vez por resolución, en ambos modos. Interrupciones de
seguimiento, descansos, salida y recalibración no suenan como fallo.

La configuración agrupa selección/dificultad y sets, reduce el desplazamiento y
conserva el gesto de deslizar. Se amplían los controles de salida/recalibración,
se suaviza el HUD y se añade confirmación visual breve en frontal. Perfil,
resultados e historial conservan los datos y el filtrado por modo.

## Orientación al arrancar

ProjectSettings guardaba defaultScreenOrientation=1, que corresponde a
UnityEditor.UIOrientation.PortraitUpsideDown. No es el enum de runtime
UnityEngine.ScreenOrientation (su Portrait vale 1). Ahora se guarda 0 (Portrait).
PraxenStartupOrientation reafirma el valor simbólico UIOrientation.Portrait en
preprocesado Android, antes de generar el inicio/splash. El modo visor sigue
cambiando a horizontal al entrar en su escena. Hay que construir un APK nuevo;
abrir solamente una escena del Editor no comprueba el arranque nativo.

Referencia: Unity-Technologies/UnityCsReference, Editor/Mono/PlayerSettings.bindings.cs.

## Contraseña temporal

Contrato revisado contra API-Praxen main 432f705b650a33a629e15d409467a434f4fa6239:
login devuelve user.mustChangePassword y un token; el middleware permite a esa
cuenta /api/auth/change-password. La app mantiene AthleteId=0 para no sincronizar
hasta activar el acceso. POST change-password envía currentPassword y newPassword
con Bearer. La API borra mustChangePassword e invalida tokens; la app elimina la
identidad y vuelve al login con el usuario conservado y un mensaje de éxito.

El formulario conserva los campos al fallar para poder corregirlos, los oculta
al terminar y los elimina al abandonar/cancelar. Un 401 vuelve al login; un 403
pide corregir la temporal. Validación: mínimo 8 caracteres, máximo 72 bytes UTF-8,
contraseña distinta de la temporal y de praxen01, confirmación coincidente. Se
alineó también el primer carácter del usuario y la longitud de nombre con la API.

Olvidé mi contraseña explica el restablecimiento por el entrenador/administrador.
No hay autoservicio por correo en el contrato actual. No se cambió el backend,
ninguna contraseña real ni la base de datos. Una cuenta web de administración
recibe una explicación específica en la app, sin asumir un perfil de atleta.

## Validación en Android pendiente

1. Abrir APK desde el icono con el teléfono vertical: sin giro de 180° previo al
   logo. Probar después visor y volver al menú vertical.
2. Manos: un contacto puntúa, mano equivocada no; ocultar mano no da aciertos.
3. Pies: aproximar borde del pie a esfera; comprobar mayor tolerancia, retirada
   obligatoria, no puntuar varias veces permaneciendo dentro.
4. Giros: 1–3 s de espera; acierto y timeout distintos de interrupción de tracking.
5. Probar los tres canales, silenciar uno sin alterar los demás, reiniciar la app.
6. Una cuenta de prueba restablecida por el administrador: temporal → contraseña
   nueva → login nuevo → sincronización. Temporal incorrecta, confirmación distinta,
   acceso caducado y fallo de red deben permitir corregir o volver a entrar.
7. Probar login/registro/cambio con teclado abierto y desplazamiento, individual,
   circuito, descanso y salida. Medir FPS y temperatura en sesiones prolongadas.

No hay Unity ni dispositivo Android en este entorno. El análisis sintáctico y las
pruebas de lógica independientes no equivalen a compilar el APK o probar la API
contra una cuenta real. Los issues permanecen abiertos para validación física.


Validación realizada: 42 casos de NUnit de manos, pies, giros/cadencia y política
de contraseña compilados y ejecutados con Mono fuera de Unity: 42 correctos,
0 fallidos. Análisis sintáctico C# con tree-sitter sin errores. Esto no ejecuta
MonoBehaviour, render, AudioSource ni UnityWebRequest. El APK y la recuperación
contra Render con una cuenta de prueba siguen pendientes de prueba física.
