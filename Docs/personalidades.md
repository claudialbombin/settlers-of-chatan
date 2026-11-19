# Personalidades de los bots

## Qué es una personalidad

Cada bot de la partida tiene una **personalidad**: una ficha de rasgos guardada como
`ScriptableObject` (`PersonalityProfile`) en `Assets/_Project/Data/Personalities/`. Se
edita en el Inspector con sliders, así que se puede ajustar sin tocar código. Además de los
números, cada ficha tiene un nombre visible, una descripción corta y un `promptFile`
(`Data/Prompts/<Id>.txt`) con la voz del personaje para el modelo de lenguaje.

La ficha es solo datos. La leen tres sistemas: el **cerebro del bot** (ActionPlanner y
RobberStrategy) para decidir qué construir y a quién bloquear, el **TradeEvaluator** para
aceptar, rechazar o contraofertar un intercambio, y el **ResponseGenerator** para decidir
cómo habla en el chat. Las diez personalidades están listadas en
`PersonalityCatalog.asset`, que es lo que recibe el `PersonalityAssigner` al empezar la
partida.

## Tabla de rasgos

| Campo | Lo usa | Efecto |
|---|---|---|
| `aggression` | ActionPlanner, RobberStrategy | Más alto → ladrón al líder, cortar carreteras |
| `greed` | TradeEvaluator | Exige `valorRecibido ≥ valorDado × (1 + greed)` |
| `cooperation` | TradeEvaluator | Baja el umbral de aceptación y sube la prob. de contraofertar |
| `grudge` | RelationshipMemory, TradeEvaluator | Cuánto pesa la afinidad pasada |
| `bluffing` | ResponseGenerator | Se le permite mentir sobre lo que tiene |
| `riskTolerance` | ActionPlanner | Aguantar cartas, apuestas |
| `randomness` | ActionPlanner, TradeEvaluator | Ruido para que no sea predecible |
| `*Weight` | ActionPlanner | Qué prefiere construir |
| `*Preference` | ResourceValuator, TradeEvaluator | Cuánto valora cada recurso |
| `maxNegotiationRounds` | NegotiationSession | Paciencia en el chat |
| `chatTemperature` | ResponseGenerator | Lo "loco" que habla |

### Valores de las diez

Los rasgos van de 0 a 1; los pesos y preferencias son multiplicadores (1 = normal). Lo que
no aparece en la columna de pesos o preferencias vale 1. En **negrita**, el rasgo extremo
de cada una.

| Id | Nombre | aggr | greed | coop | grudge | bluff | risk | rand | Pesos | Preferencias | Rondas | Temp |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Aggressive | La Conquistadora | **0.9** | 0.5 | 0.2 | 0.5 | 0.2 | 0.8 | 0.1 | road 1.3, settlement 1.2 | — | 2 | 0.8 |
| Greedy | El Acaparador | 0.4 | **0.9** | 0.4 | 0.4 | 0.3 | 0.6 | 0.1 | city 1.2 | ore 1.3, grain 1.2 | 4 | 0.7 |
| Conciliator | La Diplomática | 0.2 | 0.1 | **0.9** | 0.2 | 0.0 | 0.4 | 0.1 | — | — | 5 | 0.7 |
| RoadBuilder | El Caminante | 0.5 | 0.4 | 0.5 | 0.3 | 0.1 | 0.6 | 0.1 | **road 2.2**, settlement 1.3 | wood 1.5, brick 1.5 | 3 | 0.8 |
| Industrialist | La Ingeniera | 0.3 | 0.5 | 0.5 | 0.3 | 0.1 | 0.4 | 0.05 | **city 2.2**, road 0.6 | ore 1.6, grain 1.5, wood 0.8 | 3 | 0.6 |
| CardStrategist | El Estratega | 0.5 | 0.5 | 0.4 | 0.3 | 0.3 | 0.5 | 0.1 | **devCard 2.2** | wool 1.3, grain 1.2, ore 1.3 | 3 | 0.7 |
| Merchant | La Mercader | 0.3 | 0.6 | **0.8** | 0.2 | 0.2 | 0.5 | 0.1 | **port 2.2** | — | 6 | 0.9 |
| Vengeful | El Rencoroso | 0.6 | 0.5 | 0.5 | **1.0** | 0.2 | 0.5 | 0.1 | — | — | 2 | 0.8 |
| Cautious | El Prudente | 0.1 | 0.4 | 0.4 | 0.3 | 0.0 | **0.1** | 0.05 | settlement 1.2, city 1.1 | — | 3 | 0.5 |
| Trickster | El Embaucador | 0.5 | 0.6 | 0.6 | 0.3 | **0.9** | 0.7 | **0.6** | — | — | 4 | 1.1 |

---

## La Conquistadora (`Aggressive`)

> *Va a por el líder sin piedad. Casi nunca negocia y, si lo hace, es para hacer daño.*

- **Rasgos extremos:** `aggression` 0.9, `riskTolerance` 0.8, `cooperation` 0.2, solo 2
  rondas de negociación.
- **En partida:** pone el ladrón casi siempre sobre el jugador que va primero y construye
  carreteras para cortarle la expansión. Guarda muchas cartas sin miedo al 7. En el chat
  corta rápido: si la primera oferta no le convence, se acabó.
- **Cómo ganarle:** no ir primero en puntos demasiado pronto, porque concentra su ataque en el
  líder. Si hace falta comerciar con ella, una sola oferta clara y generosa.
- **Cómo explotarla:** se le puede "apuntar" hacia otro rival que vaya por delante; mientras
  ataca al líder, deja de molestar al resto.
- **Ejemplo:**
  - *Jugador:* ¿Te cambio una lana por una madera?
  - *Conquistadora:* ¿Una lana? No me hagas reír. Vuelve cuando tengas algo serio.

## El Acaparador (`Greedy`)

> *Acumula cartas y exige mucho más de lo que da. Le pierden el mineral y el trigo.*

- **Rasgos extremos:** `greed` 0.9 (exige recibir casi el doble de lo que da), preferencia
  por mineral (1.3) y trigo (1.2).
- **En partida:** junta recursos para ciudades y solo suelta cartas si sale claramente
  ganando. Regatea varias rondas (4) antes de rendirse.
- **Cómo ganarle:** no depender de él para conseguir recursos; usar el banco o los puertos.
- **Cómo explotarlo:** paga muy bien por mineral y trigo. Si te sobran, véndeselos caros a
  cambio de lo que él no valora (lana, madera).
- **Ejemplo:**
  - *Jugador:* Te doy dos trigos por un ladrillo.
  - *Acaparador:* ¿Dos? Ese ladrillo vale oro, amigo. Pon un mineral encima y hablamos.

## La Diplomática (`Conciliator`)

> *Busca tratos justos y buenas relaciones. Acepta casi cualquier oferta equilibrada y
> propone alianzas contra el líder.*

- **Rasgos extremos:** `cooperation` 0.9, `greed` 0.1, `bluffing` 0 (nunca miente), 5 rondas.
- **En partida:** es la socia comercial más fácil. Casi no ataca (`aggression` 0.2) y cuando
  rechaza algo suele proponer una alternativa.
- **Cómo ganarle:** comerciar con ella pronto y a menudo; mantener buena relación.
- **Cómo explotarla:** acepta tratos 1:1 que a otros les parecerían pobres. El riesgo es que
  ayude a todo el mundo por igual, también a tus rivales.
- **Ejemplo:**
  - *Jugador:* ¿Me das una oveja por un trigo?
  - *Diplomática:* ¡Claro, me parece justo! Y si quieres, hablamos de frenar a la Conquistadora.

## El Caminante (`RoadBuilder`)

> *Obsesionado con la carretera más larga. Paga lo que sea por madera y ladrillo.*

- **Rasgos extremos:** `roadWeight` 2.2, preferencia por madera y ladrillo (1.5 cada una).
- **En partida:** construye carreteras siempre que puede y busca la carta de la carretera
  más larga. Pueblos sí (1.3), ciudades poco.
- **Cómo ganarle:** cortarle el camino con un pueblo propio en mitad de su ruta.
- **Cómo explotarlo:** madera y ladrillo valen para él mucho más que para el resto. Cámbialos
  por mineral o trigo con buen margen.
- **Ejemplo:**
  - *Jugador:* Tengo dos maderas. ¿Qué me das?
  - *Caminante:* ¡Madera! Te doy un mineral y un trigo ahora mismo, esa ruta no se construye sola.

## La Ingeniera (`Industrialist`)

> *Convierte todo en ciudades. Valora mucho el mineral y el trigo y casi nada la madera.*

- **Rasgos extremos:** `cityWeight` 2.2, `roadWeight` 0.6, mineral 1.6, trigo 1.5,
  madera 0.8, `randomness` 0.05 (muy predecible).
- **En partida:** pocos caminos, muchas ciudades. Decide de forma casi determinista y explica
  sus tratos con números.
- **Cómo ganarle:** controlar las casillas de mineral con el ladrón la frena mucho.
- **Cómo explotarla:** suelta madera con facilidad; es buena fuente de madera barata.
- **Ejemplo:**
  - *Jugador:* Te cambio un mineral por dos maderas.
  - *Ingeniera:* Aceptado. El mineral me acerca a una ciudad; la madera no me sirve de nada.

## El Estratega (`CardStrategist`)

> *Compra y juega cartas de desarrollo. Busca el ejército más grande y nunca enseña su mano.*

- **Rasgos extremos:** `devCardWeight` 2.2, preferencia por lana (1.3), trigo (1.2) y
  mineral (1.3).
- **En partida:** compra cartas de desarrollo pronto, juega caballeros para mover el ladrón y
  busca el ejército más grande. En el chat no revela qué tiene (`bluffing` 0.3).
- **Cómo ganarle:** vigilar cuántos caballeros ha jugado y competirle el ejército más grande.
- **Cómo explotarlo:** la lana, que a otros les sobra, para él es valiosa.
- **Ejemplo:**
  - *Jugador:* ¿Necesitas algo?
  - *Estratega:* Quizá. Una lana me vendría bien... por razones que me guardo.

## La Mercader (`Merchant`)

> *Vive para comerciar. Se instala en los puertos y regatea durante muchas rondas.*

- **Rasgos extremos:** `portWeight` 2.2, `cooperation` 0.8, 6 rondas de negociación,
  `chatTemperature` 0.9.
- **En partida:** busca pueblos en puertos y cambia mucho, con jugadores y con el banco.
  Siempre contraoferta antes de rechazar.
- **Cómo ganarle:** ocupar antes los puertos buenos.
- **Cómo explotarla:** tiene paciencia para regatear; empezar con una oferta baja y dejar que
  suba.
- **Ejemplo:**
  - *Jugador:* Un ladrillo por un trigo.
  - *Mercader:* ¡Hecho no, casi hecho! Pon una lana y te lo dejo, que con mi puerto la lana me vuela.

## El Rencoroso (`Vengeful`)

> *No olvida quién le robó o le rechazó un trato. Leal con quien le trata bien, implacable
> con el resto.*

- **Rasgos extremos:** `grudge` 1.0 (la afinidad pasada pesa al máximo), solo 2 rondas.
- **En partida:** usa `RelationshipMemory` más que nadie. A quien le robó o le rechazó le
  pone el ladrón y le niega tratos; a quien le ayudó le da mejores condiciones.
- **Cómo ganarle:** no robarle ni rechazarle sin motivo; un trato generoso al principio
  compra su lealtad.
- **Cómo explotarlo:** si otro jugador le ha ofendido, el Rencoroso se convierte en un aliado
  natural contra él.
- **Ejemplo:**
  - *Jugador:* ¿Te interesa un trigo por una madera?
  - *Rencoroso:* Qué curioso que me pidas favores después de ponerme el ladrón. No.

## El Prudente (`Cautious`)

> *Evita riesgos y nunca guarda muchas cartas por miedo al 7. Prefiere tratos pequeños y
> seguros.*

- **Rasgos extremos:** `riskTolerance` 0.1, `aggression` 0.1, `bluffing` 0,
  `chatTemperature` 0.5 (habla de forma muy contenida).
- **En partida:** gasta enseguida para no pasar de 7 cartas, construye pueblos y ciudades de
  forma conservadora y casi nunca ataca.
- **Cómo ganarle:** es poco peligroso a corto plazo, pero constante; no subestimarlo al final.
- **Cómo explotarlo:** cuando tiene muchas cartas acepta tratos peores con tal de bajar la
  mano. Las ofertas pequeñas (1 por 1) le convencen más que las grandes.
- **Ejemplo:**
  - *Jugador:* Te doy tres cartas por dos.
  - *Prudente:* Uf, no sé... ¿y si mejor una por una? Así no me arriesgo.

## El Embaucador (`Trickster`)

> *Imprevisible y bromista. Exagera lo que tiene y cambia de idea a mitad de la
> negociación.*

- **Rasgos extremos:** `bluffing` 0.9, `randomness` 0.6, `chatTemperature` 1.1.
- **En partida:** decisiones con mucho ruido, difícil de leer. En el chat miente sobre sus
  recursos y cambia de tema. Ojo: la decisión de aceptar la sigue tomando el
  `TradeEvaluator`; lo que se inventa es solo el texto.
- **Cómo ganarle:** ignorar lo que dice y fijarse en lo que hace.
- **Cómo explotarlo:** como su `randomness` es alta, a veces acepta tratos malos para él;
  merece la pena repetir la oferta.
- **Ejemplo:**
  - *Jugador:* ¿Tienes ladrillo?
  - *Embaucador:* ¿Ladrillo? Tengo una montaña. O ninguno, quién sabe. ¿Qué me ofreces?

---

## Cómo añadir una personalidad nueva

1. Añade un valor nuevo **al final** del enum `PersonalityId` (nunca en medio: Unity guarda
   los enums por número y cambiaría la identidad de los assets ya creados).
2. Crea el prompt `Assets/_Project/Data/Prompts/<Id>.txt` en UTF-8: 4–8 líneas, en segunda
   persona ("Eres…"), solo voz y carácter, sin reglas del juego ni números.
3. *Create → Chatan → Personality Profile* en `Data/Personalities/`, llámalo
   `Personality_<Id>`, rellena los valores y arrastra el `.txt` a `promptFile`. Dale un rasgo
   extremo propio para que se distinga de las demás.
4. Abre `PersonalityCatalog.asset` y añade el asset nuevo a `profiles`.
5. Comprueba que la consola no muestra avisos y pasa los tests de *Test Runner → EditMode*
   (`PersonalityAssetsTests` comprueba que el catálogo tiene una ficha por cada valor del enum).
