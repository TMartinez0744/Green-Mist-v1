# Documento de Diseño — Whispers of the Green Mist

> Una aventura mística contrarreloj, donde cada decisión define el destino de tu aldea.

**Género:** Acción y Aventura
**Jugadores:** Un solo jugador
**Formato técnico:** 3D, tercera persona
**Plataforma:** PC y consolas
**Lenguaje:** C#
**Duración estimada:** alrededor de 1 h 30

Versión en markdown del documento original. El archivo fuente está en [GDD.es.docx](GDD.es.docx) y la traducción al inglés en [GDD.md](GDD.md).

---

## 1. Sinopsis

En una aldea azotada por la sequía, Onji, una rana mensajera, se encamina en la misión de llevar un amuleto Daruma al santuario en la cima de la montaña. Una antigua leyenda dice que, al completar el ritual antes del amanecer, podrá invocar la lluvia y salvar a su gente. El tiempo empieza a correr desde que se pinta el ojo izquierdo del Daruma.

---

## 2. Elementos del juego

- Exploración de aldeas, bosque de bambú y santuarios ocultos
- Entrega de objetos y pequeñas misiones de mensajero en la primera parte, que funcionan como tutorial
- Encuentros con depredadores místicos —zorros de niebla, sombras rana, bestias del bosque— que actúan como enemigos y como espíritus benevolentes
- Aprendizaje gradual: las mecánicas se van desbloqueando o descubriendo de manera natural con el pasar de la historia, sin sobrecargar al jugador de entrada
- Pistas ocultas en objetos: al entregar ciertos ítems a NPCs, el jugador recibe pequeños objetos visuales (dibujos, notas, símbolos) que sirven como referencia directa para resolver puzzles
- Sigilo y observación en ciertas zonas del bosque donde el combate directo puede no ser la mejor opción
- Supervivencia y resistencia: se debe llegar antes del amanecer, con recursos limitados

---

## 3. Jugabilidad

El jugador controla a Onji, una rana arborícola japonesa antropomorfa que inicia como mensajera en su aldea. El flujo de juego se estructura en tres actos.

### A. Tutorial — La Aldea

Onji recibe encargos simples de mensajero: entrega cartas, paquetes y talismanes a los NPCs de la aldea. Algunos encargos requieren recolectar recursos —por ejemplo, cortar trozos de bambú—, lo que introduce de forma suave el combate ligero.

El jugador aprende mecánicas de movimiento, interacción, uso de objetos y las bases del combate. Los objetos obtenidos en esta fase (bambú, talismanes, hierbas) se utilizan más adelante como herramientas o recursos.

### B. Exploración y viaje

Durante la noche, Onji se adentra en el bosque embrujado y emprende el camino ascendente. Se incentiva la exploración de rutas alternativas, los acertijos ambientales y el uso estratégico de objetos recolectados.

**Combate estratégico y sigilo.** Enemigos más complejos introducen patrones de ataque y obligan al jugador a decidir entre luchar, esquivar o avanzar en sigilo. El sistema de combate es ágil y accesible, centrado en la estrategia y la lectura del entorno, con un estilo similar a un *Assassin's Creed* más simplificado.

**Gestión de recursos y tiempo.** El jugador debe llegar al santuario antes del amanecer. La tensión narrativa se mantiene con recursos limitados y decisiones estratégicas sobre qué camino tomar y cuándo arriesgarse a combatir.

### C. Clímax — El Santuario

El viaje culmina con el enfrentamiento contra el Guardián del Santuario. Al vencerlo y realizar el ritual a tiempo, Onji descubre su verdadero destino: convertirse en el nuevo guardián y protector del equilibrio natural.

---

## 4. Niveles

| Nivel | Contenido |
|---|---|
| **La Aldea** (tutorial) | Entrega de objetos, introducción de mecánicas básicas y primer combate contra criaturas menores y objetos del entorno |
| **Bosque de Bambú Embrujado** | Exploración no lineal, acertijos ambientales y mini-jefe |
| **Puente Colgante** | Desafío de equilibrio con vientos muy fuertes y mini-jefe |
| **Santuario en la Cima** | Acertijos finales y combate contra el Guardián, boss fight estilo Souls-lite |

---

## 5. Puzzles

### Runas alineadas (B)

Se presentan tres o cuatro tótems de madera con símbolos. El jugador debe golpear o girar los correctos para que coincidan con una secuencia vista antes; la pista llega en un objeto entregado por un NPC, que puede ser un mapita, una foto o un dibujo. Sirve para liberar un camino previo al puente.

### Estatuas guardianas (B, después del puente)

Se presentan aproximadamente cuatro estatuas de piedra, komainu o de ranas. El jugador debe rotarlas o ubicarlas para que apunten hacia el santuario o se alineen con un patrón del entorno: constelaciones, dirección de la luna, símbolos en el suelo. Es un puzzle de puerta: al colocarlas bien, se abre el acceso por la puerta Torii hacia el santuario y el jefe final.

### Puzzle final: ritual del santuario (C)

Onji coloca el daruma en el altar e inicia el ritual activando símbolos en el orden correcto, formando el kanji de lluvia (雨).

El ritual se desarrolla en paralelo al combate: mientras más tarda, más enemigos aparecen para interrumpirlo. Son criaturas de niebla, débiles individualmente pero numerosas, al estilo de los adds de hienas espectrales en la boss fight contra Anubis en *Assassin's Creed Origins*. Justo antes de completarlo, irrumpe el Guardián del Santuario. El jugador debe derrotarlo para poder finalizar la secuencia de símbolos y completar el ritual que invoca la lluvia.

---

## 6. Definición del jugador

El jugador controla a Onji, una rana antropomorfa mensajera que se transforma en héroe espiritual a lo largo de la aventura. Sus habilidades combinan movimiento ágil, uso de objetos rituales y combate ligero contra criaturas místicas.

### Propiedades

| Propiedad | Comportamiento |
|---|---|
| **Salud** | Barra de vida. Si baja demasiado, la velocidad de movimiento y la capacidad de esquivar disminuyen. Al llegar a cero, el jugador muere |
| **Energía** | Determina la capacidad de esquivar y realizar acciones rápidas. Disminuye con cada esquive y se recupera lentamente al descansar |
| **Armas / objetos** | Onji porta un arma principal única —un bastón o naginata improvisada de bambú— que utiliza tanto para el combate como para interactuar con ciertos elementos del entorno |
| **Tiempo restante** | Contador global hasta el amanecer. A medida que se acerca, la presión narrativa y la urgencia aumentan |

### Acciones

Moverse, saltar, agacharse, esquivar, atacar, interactuar con el entorno y usar consumibles.

### Ítems y recompensas

| Ítem | Efecto |
|---|---|
| **Hierbas curativas** | Restauran una parte de la salud |
| **Talismanes de protección** | Reducen el daño recibido durante un tiempo limitado |
| **Omamori** (talismán de poder) | Objeto raro y muy limitado, máximo 2 aproximadamente. Permite ejecutar la habilidad especial (R); se consume automáticamente y gasta la mitad de la energía |
| **Insectos comestibles** | Aportan curación ligera y aumentan temporalmente la resistencia |
| **Gyozas** | Restauran salud moderada y proporcionan un breve buff de regeneración |
| **Daruma** (objeto clave) | Debe ser transportado y protegido hasta el santuario |

---

## 7. Controles

El esquema está pensado inicialmente para PC. En caso de adaptar a consola, los botones se mapearán a un esquema estándar de gamepad.

| Acción | Tecla (PC) | Notas |
|---|---|---|
| Moverse | WASD | Dirección según cámara, controlada con el mouse |
| Sprintar | Shift | Mantener |
| Agacharse | Ctrl | Sigilo |
| Saltar | Espacio | Mantener para impulso |
| Interactuar | E | Mantener para acciones prolongadas |
| Inventario | Tab | El tiempo se pausa |
| Selección rápida de ítem | 1, 2, 3, 4 | Cambia el slot activo |
| Usar ítem equipado | Q | Se consume el ítem |
| Ataque rápido | Click izq. | No consume energía |
| Ataque fuerte | Click der. | Mantener para cargar, consume poca energía |
| Habilidad especial | R | Requiere talismán equipado que se consume al usar; gasta la mitad de la energía |
| Esquivar | Alt | Se hace hacia la dirección del movimiento actual, consume energía |

---

## 8. Interfaz de usuario

- Barra de salud, centro inferior de la pantalla
- Indicador de energía / resistencia, debajo de la barra de salud
- Pequeño reloj que muestra el tiempo restante hasta el amanecer, centro superior
- Ícono del objeto equipado, esquina inferior derecha
- Mensajes contextuales discretos que aparecen únicamente cuando el jugador se acerca a un objeto o NPC interactivo

### Menús

Logo y título, con menú inicial y música tranquila del período Edo de fondo, a juego con el background estilo Ukiyo-e.

Opciones del juego: sonido, brillo, idioma, crear o continuar partida, y dificultad (Fácil, Normal, Difícil).

---

## 9. Condiciones de victoria y derrota

**Ganar.** Llegar al santuario con el daruma y completar el ritual antes del amanecer.

**Perder.** Morir en combate o no llegar a tiempo al santuario.

### Finales

**Éxito.** Onji completa el ritual, invoca la lluvia y salva a la aldea. Sin embargo, descubre que el verdadero propósito del ritual no era solo invocar la lluvia, sino convertirse en el nuevo guardián del santuario. Al vencer al antiguo guardián, toma su lugar, asegurando la protección del santuario y el equilibrio de la naturaleza. Su sacrificio trasciende lo personal: deja de ser mensajera y pasa a ser espíritu protector. Es un héroe que no regresa.

**Fracaso.** El amuleto se rompe, y la sequía se convierte en una maldición eterna.

---

## 10. Ambientación y temática

El juego está ambientado en un mundo inspirado en paisajes orientales tradicionales, con bosques de bambú embrujados, aldeas rurales y santuarios ocultos entre la niebla. La temática central es el viaje espiritual y heroico, donde el sacrificio personal trasciende la aventura física. Se combinan elementos de misticismo, naturaleza y urgencia contrarreloj, reforzados por música y estética del período Edo.

La atmósfera es predominantemente oscura y melancólica, iluminada por luces tenues de faroles que guían al jugador en medio de la penumbra, evocando un estilo visual similar al de *Stray* o las calles japonesas nocturnas.

La ambientación sonora se apoya en instrumentos tradicionales japoneses como el koto (cítara de 13 cuerdas), el shakuhachi (flauta de bambú) y el shamisen (laúd de tres cuerdas).

---

## 11. Pautas de diseño

**Enfoque en la experiencia atmosférica.** Todos los elementos —arte, música, iluminación y narrativa— deben reforzar la sensación de misticismo, melancolía y urgencia que define al viaje de Onji.

**Dificultad progresiva y justa.** El juego inicia con mecánicas simples y accesibles, evoluciona con enemigos más complejos y culmina en un boss final exigente estilo Souls-lite, sin llegar a frustrar al jugador casual.

**Exploración significativa.** Cada entorno debe incluir rutas opcionales, secretos o pistas que recompensen la curiosidad del jugador. La exploración nunca debe sentirse vacía.

**Aprendizaje orgánico.** Las mecánicas se introducen en situaciones naturales, evitando tutoriales invasivos.

**Tiempo como recurso narrativo.** El límite del amanecer no es solo una mecánica de presión, sino un pilar narrativo que guía decisiones y refuerza la tensión dramática.

**Diseño modular.** Cada nivel funciona como una pieza independiente, pero conectado dentro de la progresión de la historia. Esto permite ajustes y testeo por separado.

**Economía de recursos.** Los ítems y amuletos deben tener un valor claro y limitado, promoviendo decisiones estratégicas.

---

## 12. Público objetivo y referencias

Personas en un rango etario entre los 15 y 35 años con experiencia media en videojuegos, que buscan experiencias narrativas inmersivas con exploración, ambientaciones fantásticas y combate accesible pero épico.

**Referencias:** *God of War*, *Stray*, *Ghost of Tsushima*, *Assassin's Creed*.

### ¿Por qué todo esto es divertido?

Porque combina exploración inmersiva, acertijos ambientales y un sistema de combate progresivo que culmina en un enfrentamiento final desafiante. La presión del tiempo hasta el amanecer mantiene la tensión constante, mientras que los diálogos y pistas ocultas recompensan la atención y la curiosidad del jugador. Cada nivel ofrece un desafío único, evitando la repetición y asegurando una experiencia variada y memorable a pesar de la corta duración del juego.

El mundo transmite una vibra nostálgica y mística, como un eco de tiempos pasados, envolviendo al jugador en un tono melancólico y casi onírico.
