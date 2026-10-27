# Natación: recorrido con teléfono fijo (issue #8)

## Interfaz y flujo

- El menú de esta rama presenta cámara de selfies, una persona por teléfono y
  ayuda para colocar el teléfono; se retiró el acceso a los ajustes del visor.
- Tutoriales de preparación, manos, pies y piques con giro. No piden lentes,
  alfombrillas, detectar suelo ni delimitar un área.
- Conserva usuario/contraseña, invitado, dificultad máxima, sets, tiempo por set,
  descanso y selección del circuito (0 s desactiva un ejercicio).
- HUD compartido, alto contraste y tipografía proporcional al teléfono. Cámara
  visible, estado arriba a la izquierda, reloj arriba a la derecha y métricas debajo.
  El área de contacto y las reglas de los ejercicios no cambian en este incremento.
- Preparación y descanso: un panel centrado en el área segura, sobre cámara viva.
  Se puede salir al menú también durante estas transiciones. Si se pierde la pose
  en preparación, se vuelve a la interfaz de encuadre con Recalibrar disponible;
  al recuperar la posición comienza otra cuenta completa de cinco segundos.
- La cuenta del ejercicio solo avanza con TrackingReady. Descanso sigue su propio
  reloj y conserva la cámara. Al finalizar se muestran resultados tras la transición
  de cinco segundos, sin instrucciones de quitar lentes ni otra cuenta hablada.
- Se oculta el panel antiguo de final de circuito mientras se presentan resultados.

## Resultados y cuenta

Nuevas capturas añaden un campo opcional measurementMode en cada resultado:

| Ejercicio | Etiqueta |
| --- | --- |
| Manos | pose_hand_screen_contact_v1 |
| Pies | pose_foot_screen_contact_v1 |
| Piques y giros | pose_body_turn_response |

No se inicializan las etiquetas de manos/pies al deserializar: los resultados
anteriores sin campo siguen sin método identificado y no se reclasifican como Pose.
Los giros ya llevaban su etiqueta desde #7. Los nombres internos de los ejercicios
(aimlab, feet_targets, running_direction_change) permanecen compatibles con la API.

Se revisaron en modo lectura el validador, controlador de sesiones y los servicios
results.query/results.analytics del backend main. El validador acepta las etiquetas
y el controlador conserva los objetos en sus listas JSON. Los agregados actuales
no separan measurementMode. Por eso el historial de esta rama consulta únicamente
GET /api/resultados/me/sesiones?limit=20&offset=N y filtra los resultados explícitos
de Pose, además de verificar su athleteId. No usa resumen/evolución mezclados.

El historial permite paginar todas las sesiones disponibles, explica que los
contadores corresponden a la página, permite abrir sus resultados y volver a la
misma página. No presenta un cambio de rendimiento entre métodos incompatibles.
Una página puede no contener Pose aunque existan resultados en otra página.
Los datos anteriores permanecen en el servidor; no se eliminan ni se reetiquetan.

El filtro crea nuevas listas y no cambia los objetos que están pendientes de envío.
Se mantienen el JWT, los controles de cambio de identidad, la cola local ligada
al atleta/servidor y los reintentos. El invitado sigue siendo local y no se asigna
retroactivamente a otra cuenta. No se modificó ni desplegó la API ni la base de datos.

## Voces pendientes (integración posterior)

Las introducciones antiguas de manos/pies/giros quedan desactivadas en las escenas
Pose. Se conservan música, números de preparación y avisos generales del plan.
No se añadieron archivos de voz ni señales habladas de dirección.

| Clave propuesta | Frase / evento |
| --- | --- |
| selfie_setup | Apoya el teléfono con la pantalla hacia ti y aléjate hasta que se vea todo tu cuerpo. |
| pose_calibration | Ponte de frente y quédate quieto un momento. |
| pose_hands | Prepara las manos. Toca los objetivos azules con la izquierda y los rojos con la derecha. |
| pose_feet | Mantén los piques. Alcanza el objetivo azul con el pie izquierdo y el rojo con el derecho. |
| pose_turns | Mantén los piques en tu lugar. Gira el cuerpo al lado indicado y vuelve de frente. |
| turn_left | ¡Izquierda! |
| turn_right | ¡Derecha! |
| return_front | Vuelve de frente. |
| tracking_lost | No te veo completo. Vuelve a colocarte dentro de la imagen. |
| tracking_ready | ¡Listo! Continuamos. |

En la integración futura: no apilar avisos ni reproducir tracking_lost en cada
frame; sincronizar el comienzo de izquierda/derecha con la señal visual y el reloj
de respuesta. Revisar el contenido de los avisos generales antes de reutilizarlos
con la nueva voz. Esta revisión no incluyó escuchar/transcribir los clips binarios.

## Validación

- Ejecutado en este entorno: cinco casos contra el validador real del backend
  (circuito Pose, tres individuales y compatibilidad de JSON anterior); revisión
  estática de campos, navegación, llamadas de preparación y referencias retiradas.
  No se hicieron peticiones autenticadas ni escrituras remotas de sesiones.
- Añadidos EditMode: NatacionPreparationTests para pérdida de pose, reinicio de
  cuenta completa, duraciones inválidas y exclusión de métodos incompatibles.
- Unity no está disponible aquí: falta compilar, ejecutar EditMode y verificar
  visualmente en Editor/Android. No se da por comprobado el rendimiento o legibilidad
  a distancia hasta esa prueba en dispositivo.

Prueba en el Xiaomi: login/invitado, tutorial, un set individual de cada tipo,
dos sets con descanso y circuito con los tres; repetir con uno desactivado.
Durante los cinco segundos salir del encuadre: deben reaparecer los controles de
calibración y luego una cuenta completa, sin objetivos activos ni tiempo de set.
En descanso comprobar vídeo y una sola cuenta. Salir también desde esa pantalla.
Ver resultados sin conexión; volver a iniciar sesión y sincronizar. En historial,
revisar una página con resultados antiguos, otra con Pose, detalles y regreso;
al cambiar de cuenta no deben aparecer resultados de la cuenta anterior.
