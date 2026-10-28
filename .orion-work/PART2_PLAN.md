# План порта Orion — часть 1 и часть 2

## Решения пользователя (2026-10-28)

- Часть 2 портируется прямо в `port/orion-inteq` (PR #62), отдельной ветки нет. PR #62 будет squash-нут.
- В отдельную часть 3 уходят тяжёлые #208, #76, #183.
- Все UNCERTAIN (#35, #66, #284, #287, #312, #331, #439) — CUT.
- #354 (квас) — откат; фактически уже удалён в `e18a69f61a9`.
- Хвосты части 1 (HermitInteQ, implant у RecruitPadInteQ) — доделаны.
- Добавлен #6 Better Lizards: история коммитов ПРа сломана (база — старый wizden), источник = собственные 5 коммитов
  `9ce57b63991..7e4647cb726` + список из описания ПРа: Rxup#749, #929, #1206, #1266, #1259, #1239, Goob#1404,
  Corvax#3288 (не смержен), Corvax#3300. Часть контента уже есть в `_White` (Goob#1404-линия) — портировать только отсутствующее.
- Старые `refs/orion/pr/1..36` (кроме 16, 17, 20, 25, 31) указывали на ПРы репозитория `Orion-Station`, а не
  `Orion-Station-14`; для 6, 23, 27, 29 ссылки заменены на `Orion-Station-14`.

Источники: `FINAL.json` (вердикты), `ids/modfiles.json` (полные списки файлов ПРов), `md_rows.json` (названия).
Пересечения посчитаны по путям Ориона; для общих (не `_Orion`/`_Europa`) путей — ещё и по `git diff upstream/master...port/orion-inteq`.

## Часть 1 — PR ArcaneSS14/arcane-new#62, ветка `port/orion-inteq`

- Состояние: `upstream/master` полностью влит в ветку (0 позади, 27 коммитов впереди), GitHub: mergeable. Конфликтов с master сейчас нет.
- Портировано: 17, 20, 25, 31, 38, 40, 64, 162, 283, 328, 354, 359, 370, 375, 382, 445 + InteQ 16, 57, 101, 123, 228, 254.
- **#354 (квас) есть в ветке, но не указан в описании PR #62** — добавить ссылку.
- Хвосты части 1, которые теперь разблокированы (InteQ уже в ветке):
  - `HermitInteQ` selectable set: вернуть блок в `_Arcane/Catalog/selectable_sets.yml` и `- HermitInteQ` в `possibleSets` `_Arcane/Entities/Objects/Devices/set_selector.yml`.
  - `RecruitmentScanning.implant: DeathRattleImplantInteQ` в `_Arcane/Entities/Objects/Devices/recruitment.yml` (прототип импланта уже есть).
- Вердикт в `FINAL.json` для #20 = CUT, но ПР портирован (решение пользователя) — поправить вердикт.

## Как ведём ветки

1. Ветка части 2 `port/orion-part2` создаётся **от `port/orion-inteq`**, а не от master: часть 2 правит файлы, уже изменённые в части 1 (см. ниже), и без части 1 не собирается.
2. Пока #62 на ревью: каждое изменение `port/orion-inteq` вливаем в `port/orion-part2` (`git merge port/orion-inteq`).
3. После мержа #62:
   - если мерж обычным merge-коммитом — `git merge upstream/master` в `port/orion-part2`;
   - если **squash** — `git rebase --onto upstream/master port/orion-inteq port/orion-part2`, иначе коммиты части 1 приедут в PR части 2 повторно и дадут конфликты.
4. Проверка конфликтов с актуальным master без переключения веток:
   `git fetch upstream && git merge-tree --write-tree --name-only upstream/master HEAD` (код возврата 1 = есть конфликты, список файлов в выводе).
5. После каждого шага: YAMLLinter, сборка `Content.Server`/`Content.Client`, `StartEndGameRulesTest` для ПРов с геймрулами/спавнерами.

## Часть 2 — что осталось (47 KEEP + 7 UNCERTAIN)

### Файлы, уже изменённые частью 1 (точки конфликта между частями)

| ПР Ориона | Файл | Уже менял в части 1 |
|---|---|---|
| #23 Goliath Broodmother | `_Lavaland/Procedural/lavaland_grid_ruins.yml`, `ruin_pools.yml` | #20 |
| #23 | `_Europa/Damage/modifier_sets.yml` → наш `_Arcane` | #31 |
| #61 Wormhole Anomaly | `_Europa/GameRules/events.yml`, `events.ftl` → наш `_Arcane` | #17 |
| #63 Body Bags, #143 Decal Grenade | `Research/civilianservices.yml` | #40 |
| #208 Underwear, #356 Cosmic Japan | `Resources/migration.yml` | часть 1 |
| #210 All Access | `Access/misc.yml` | часть 1 |
| #211 Orio Cookies | `Markers/Spawners/Random/trash.yml` | часть 1 |
| #356, #367, #381 | `_Orion/tags.yml` → наш `_Arcane` tags | #162 |

### Кластеры внутри части 2 (портировать строго по порядку внутри кластера)

- Джукбокс: **#29 → #351** (одни и те же 5 файлов Jukebox*).
- Тревоги: **#74 → #233 → #239**, затем **#110** (BwoinkSystem общий с #239).
- Аномалии/РНД: **#181 → #372 → #367**, затем **#363** (`computers.yml` общий с #181).
- Кровати/пристёгивание: **#350 → #420** (`SharedBuckleSystem.Buckle.cs`).
- Мобы: **#216 → #256** (`Mobs/base.yml`); species `base.yml` общий с #208, #350.
- `lathe.yml`: #63, #239, #350 — разносить по разным коммитам, не параллельно.
- Лобби/профиль: **#232**, #312 (`LobbyGui.xaml`); #76, #183, #208 (`HumanoidProfileEditor`, `HumanoidCharacterProfile`).

### Шаги

**Шаг 1 — мелкие изолированные (низкий риск):**
27, 55, 117, 220, 245, 258, 353, 383, 402, 434, 438, 450, 458.

**Шаг 2 — контент/предметы/рецепты:**
211, 349, 63, 143, 376, 381, 356, 187, 186, 61, 23.

**Шаг 3 — системы по кластерам:**
29 → 351; 74 → 233 → 239 → 110; 181 → 372 → 367 → 363; 350 → 420; 216 → 256; 302; 232; 275.

**Шаг 4 — тяжёлые, предлагаю вынести в часть 3 / отдельные PR:**
- #208 Extra Hotbar + Underwear — 620 файлов, 108 общих; пересекается с 216, 232, 239, 302, 350, 356, 450, 458, 76, 183.
- #76 Sponsor System — 6 csproj, новые проекты `_Europa/Content.Europa.Interfaces.*`, `JoinQueueManager` (Goob) в форке отсутствует.
- #183 Extended Flavor — миграции БД: в форке генерировать свои, оригинальные не копировать.

### Нужна переадаптация API (целевые файлы в форке переехали или удалены)

| ПР | Что у Ориона | Где у нас |
|---|---|---|
| #74, #233 | `Content.Server/AlertLevel/*` | `Content.Shared/AlertLevel/` |
| #220, #232, #208 | `Content.Server/GameTicking/GameTicker.{Lobby,RoundFlow}.cs` | `Content.Shared/GameTicking/` |
| #302 | `Content.Server/CartridgeLoader/CartridgeLoaderSystem.cs` | `Content.Shared/CartridgeLoader/` |
| #208 | `Content.Shared/Inventory/SlotFlags.cs` | `Content.Common/Inventory/SlotFlags.cs` |
| #186 | `Doors/SecretDoor/reinforced_secret_door.yml` | `_Goobstation/Entities/Structures/Doors/` |
| #239 | `Catalog/uplink_catalog.yml` | `_Arcane/Catalog/uplink_catalog.yml` |
| #208, #216, #350, #450 | `Entities/Mobs/Species/*`, `tajaran.yml` | нет — species в Trauma переделаны |
| #275 | `Hunger/ThirstSystem.cs` | нет — проверить, есть ли что портировать |
| #350 | `SharedBedSystem.cs` | нет |
| #438 | `_Shitmed/Surgery/SharedSurgerySystem.Steps.cs` | нет — хирургия переделана |
| #208, #76 | `MarkingCategories.cs`, `MarkingsSet.cs`, `SingleMarkingPicker` | нет — маркинги переделаны |

### Ошибки в описаниях `FINAL.json` (сдвинуты относительно ПРов — перепроверить, `BATCHES.json` строился по ним)

- #210 «All Access» — это правка доступов/ID-карт, а не PriceGun.
- #239 «Bunch Of Tweaks» — BwoinkSystem, звуки тревог, uplink, а не MeatSpike.
- #302 «Better PDA» — PDA/картриджи, а не ядра аномалий.
- #350 «Double Bed» — двуспальная кровать, а не свисток.

### UNCERTAIN — нужен вердикт (дифф не пустой, это спрайты/ассеты)

| ПР | Что | Замечание |
|---|---|---|
| #35 Doppler Hair Pack | 101 файл, `_Europa` волосы | новые ассеты, конфликтов нет |
| #66 Mindshield Funky Sprite | перерисовка `job_icons.rsi`, Goob `security_icons.rsi` | правит чужие текстуры |
| #284 TG Banners | перерисовка ванильных `banner.rsi` | правит ванильные текстуры |
| #287 TG Heretic Robes | 30 текстур `_Goobstation/Heretic` | правит Goob-текстуры |
| #312 Lobby Server Info | `LobbyGui.xaml` | пересечение с #232 |
| #331 Secret Door Sprite | ванильный `secret_door.rsi` | пара к #186 |
| #439 «start» | `cybersun.rsi` шлем | правит ванильные текстуры |

### Не портируем

- #429 Hiss to Mono — исходного `reptilian_hiss.ogg` в форке нет.

## Прогресс порта части 2 (рабочее дерево `port/orion-inteq`, без коммитов)

Готово (собирается: Content.Server, Content.Client, Content.Arcane.Server, Content.Arcane.Client):
- Хвосты: HermitInteQ, implant у RecruitPadInteQ. #354 уже был удалён.
- 27, 55, 61, 63, 110, 117, 143, 181, 186, 187, 210, 211, 216, 220, 23, 245, 256, 258, 29+351, 349, 350, 353, 363, 367, 372, 376, 381, 383, 402, 420, 434, 438, 458 (частично).

Адаптации и отклонения от исходника:
- #143: звук `yay_fnaf.ogg` не импортирован (нет лицензии/атрибуции); граната без звука. Пакет `ArcaneService` ни к одному лату не подключён (как и в Орионе).
- #63: `BodyBagsSyndicateStatic` подключён как `emagStaticPacks` (в исходнике dynamic — рецепт был недостижим).
- #110: антиспам только для не-админов, ключ по отправителю (в исходнике по каналу), причина бана в локали.
- #187: после постройки окна решётка возвращается в свой узел графа (в исходнике оставалась в узле `window` и не разбиралась).
- #29+#351: громкость 0..100 → -30..0 дБ; повтор трека — перезапуск потока движком, без своих таймеров.
- #61: червоточина — `Update` + `NextPulse` вместо `Timer.Spawn`.
- #372: разрушение BS-кристаллов от 1 урона не портировано; реактивация — по `SolutionChangedEvent`; прототип активного ядра выводится из id инертного (без правок 12 прототипов).
- #363: собственный визуализатор (меняет только стандартные корпуса `computer`/`broken`), правки ТВ/терминала/абдукторов (#458) не нужны.
- #350: двуспальная кровать на своём графе `double_bed` (без правки ванильного графа `bed`); поворот лежащих гуманоидов -90.
- #376: длина имени/описания штампа ограничена на сервере.
- #256: текст эмоута экранируется; проверки ChatProtection (Orion ServerProtection) нет.
- #450: неприменим. #458: криокапсула и патроны — уже покрыто/иная структура; инвентарь — к #208; карта aspid — нет у нас.

Дополнительно готово: 232, 356, 74, 233 (+ итоговые сигналы из 239), 239 — только кулдаун AHelp.
- #232: окно манифеста обновляется раз в 2 с (без правки `GameTicker`); кнопка подключается из Arcane UIController.
- #356: в изменённые орионовские файлы добавлена только дельта ПРа; `paper_tear.ogg` не импортирован (нет атрибуции) → `WoodDestroy`.
- #74: Amber + рамка объявления с инструкциями; новые звуки тревог (green/amber/red/gamma) не импортированы — нет атрибуции.
- #233: сигналы ламп — Arcane-прототип `alertLevelAlarm` + `EmergencyLightAlarmSystem` (без правок уровней); звуки TauCeti CC-BY-SA-3.0.
  Для Delta вместо `delta_light.ogg` (нет атрибуции) — `overload.ogg`.
- #275: неприменим (Hunger/Thirst переделаны, SpriteFade уже безопасен, в правке SolutionRegeneration у Ориона ошибка).
- #239 (под актуальные цены Trauma):
  - Аплинк: перенесены только цены, где база форка совпала с базой Ориона: гондола 30→15, леденцы 15→10 / 100→30,
    гранаты blast/frag 12→10, взятка 20→10 (и плазмаменская 20→10). Остальные позиции Trauma уже перебалансировал (5x),
    у кошачьих ушей другая база, Dreadnought/G8/Berserker/Mosin 7.62 в форке нет — не трогали.
  - Перенесено: топор (armorPenetration 0.4), aghost-слот глаз + мультиверсальный HUD, химвенд (кувшины 15, JugPlasma
    вместо ампулы), театр (уши кролика, карп-костюм 2, карп-плащи), винтердроб (ушанка), карп-плащи без BaseMagicalContraband,
    пустые мед-спреи (Bluespray tg CC-BY-SA) в медфабе, ресрпайт ChemMaster (tg, rework UmbiMax), двойной стеклянный шлюз
    (TauCeti, rework UmbiMax; оверлеи переведены на свой 64x32 RSI, покраска отключена, прочность 300 = 1.5x), delta.ogg для DeltaNuke.
  - Не перенесено: перчатки Northstar (Trauma уже 4→5), ELITE-хардсьют (уже 1), щит (состояния в RSI форка корректны),
    HUD-иконки болезней (уже в `ShowMedicalIcons`), omni → multiversal (без функциональной разницы), пистолеты и хим-раздатчики
    Ориона (их в форке нет), `code_danger.ogg`/`delta_light.ogg` (нет атрибуции).
- #302: Arcane `pdaSkin` (58 скинов) + `PdaScreen` (экран по активной программе) + кнопка выключения; `power.png` без
  лицензии → иконка `close.svg`. Картриджа MailMetrics в форке нет.
- #6: подмножество — маркировки (14 шт. + 3 языка), лимит Snout 2, шипение; ресрайт ящеров и диги-ноги уже есть (`_White`).
- #35: 96 причёсок `_Arcane`, без `markingCategory`.

Готово всё из части 2, кроме перенесённых в часть 3: #208, #76, #183.
Не запускались: YAMLLinter (OOM при последней попытке), интеграционные тесты.
